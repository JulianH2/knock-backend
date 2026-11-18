#!/usr/bin/env bash
# Levanta el entorno completo desde cero con un solo comando (KNOCK-34, spec 005 CA-01 y CA-02):
# infra -> database -> apps, cada uno con su propio estado remoto.
#
#   ./deploy/up.sh --image-tag <sha>            # modo seguro (por defecto): solo `terraform plan`
#   ./deploy/up.sh --image-tag <sha> --apply    # aplica, pero solo si ningún plan destruye o reemplaza recursos
#
# No crea nada que no esté en Terraform y no imprime secretos: los valores de Key Vault los escribe una persona
# con `az keyvault secret set` (CA-04) y este script solo comprueba que existan sus nombres. El clúster de
# DocumentDB solo se importa y tiene `prevent_destroy`: si algún plan intentara borrarlo, el script se detiene.
set -euo pipefail
umask 077

DEPLOY_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

APPLY=false
NO_LOCK=false
IMAGE_TAG="${IMAGE_TAG:-}"
SUBSCRIPTION_ID="${SUBSCRIPTION_ID:-}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-knock-dev}"
NAME_SUFFIX="${NAME_SUFFIX:-}"
CLUSTER_NAME="${CLUSTER_NAME:-knock-docdb}"
SECRET_ADMIN_IDS="${SECRET_ADMIN_IDS:-}"
APPS_ENV="${APPS_ENV:-dev}"

# Nombres fijos de los secretos en Key Vault (deploy/README.md, «Secretos»).
REQUIRED_SECRETS=(jwt-signing-key redis-password redis-connection-string pii-encryption-key pii-hmac-key
  mongo-identity mongo-market mongo-messaging field-encryption-key identity-field-encryption-key search-cursor-key)

usage() {
  cat <<'EOF'
Uso: deploy/up.sh --image-tag <etiqueta> [opciones]

Por defecto solo ejecuta `terraform plan` en infra, database y apps (modo seguro). Para aplicar hay que pasar
--apply de forma explícita.

Opciones:
  --image-tag <etiqueta>      Etiqueta de las imágenes en el ACR (SHA del commit, nunca `latest`). Obligatoria.
  --apply                     Ejecuta `terraform apply` con el plan guardado de cada directorio, en orden.
  --name-suffix <2-8 chars>   Sufijo de los nombres únicos (Key Vault, ACR, Storage); el del entorno existente.
  --subscription-id <id>      Suscripción de Azure (por defecto, la activa en `az account show`).
  --resource-group <nombre>   Grupo de recursos existente (por defecto rg-knock-dev).
  --cluster-name <nombre>     Clúster de DocumentDB que se importa (por defecto knock-docdb).
  --secret-admin-ids <a,b>    Object IDs con permiso de escribir secretos (por defecto, quien tiene la sesión de az).
  --apps-env <dev|evaluacion> Archivo deploy/apps/envs/<nombre>.tfvars (por defecto dev).
  --no-lock                   Usa `-lock=false` en los planes (solo lectura sobre el estado). No se permite con --apply.
  -h, --help                  Muestra esta ayuda.

Las mismas opciones se pueden dar como variables de entorno: IMAGE_TAG, SUBSCRIPTION_ID, RESOURCE_GROUP,
NAME_SUFFIX, CLUSTER_NAME, SECRET_ADMIN_IDS, APPS_ENV.
EOF
}

die() { echo "ERROR: $*" >&2; exit 1; }
info() { echo "==> $*"; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --apply) APPLY=true; shift ;;
    --no-lock) NO_LOCK=true; shift ;;
    --image-tag) [[ $# -ge 2 ]] || die "--image-tag necesita un valor"; IMAGE_TAG="$2"; shift 2 ;;
    --name-suffix) [[ $# -ge 2 ]] || die "--name-suffix necesita un valor"; NAME_SUFFIX="$2"; shift 2 ;;
    --subscription-id) [[ $# -ge 2 ]] || die "--subscription-id necesita un valor"; SUBSCRIPTION_ID="$2"; shift 2 ;;
    --resource-group) [[ $# -ge 2 ]] || die "--resource-group necesita un valor"; RESOURCE_GROUP="$2"; shift 2 ;;
    --cluster-name) [[ $# -ge 2 ]] || die "--cluster-name necesita un valor"; CLUSTER_NAME="$2"; shift 2 ;;
    --secret-admin-ids) [[ $# -ge 2 ]] || die "--secret-admin-ids necesita un valor"; SECRET_ADMIN_IDS="$2"; shift 2 ;;
    --apps-env) [[ $# -ge 2 ]] || die "--apps-env necesita un valor"; APPS_ENV="$2"; shift 2 ;;
    -h | --help) usage; exit 0 ;;
    *) usage >&2; die "opción desconocida: $1" ;;
  esac
done

# ---- Prerrequisitos ----

command -v terraform > /dev/null 2>&1 || die "terraform no está en el PATH (se necesita >= 1.9)"
command -v az > /dev/null 2>&1 || die "az (Azure CLI) no está en el PATH"

TF_VERSION_OUT="$(terraform version)" || die "no se pudo ejecutar terraform version"
TF_VERSION="$(sed -nE '1s/^Terraform v([0-9]+\.[0-9]+).*/\1/p' <<< "$TF_VERSION_OUT")"
TF_MAJOR="${TF_VERSION%%.*}"
TF_MINOR="${TF_VERSION#*.}"
if [[ ! "$TF_MAJOR" =~ ^[0-9]+$ || ! "$TF_MINOR" =~ ^[0-9]+$ ]] || ((TF_MAJOR < 1 || (TF_MAJOR == 1 && TF_MINOR < 9))); then
  die "se necesita Terraform >= 1.9 (encontrada: $TF_VERSION)"
fi

az account show > /dev/null 2>&1 || die "no hay sesión de Azure: ejecuta \`az login\`"

if [[ -z "$SUBSCRIPTION_ID" ]]; then
  SUBSCRIPTION_ID="$(az account show --query id --output tsv)"
fi
[[ "$SUBSCRIPTION_ID" =~ ^[0-9a-fA-F-]{36}$ ]] || die "SUBSCRIPTION_ID no tiene formato de GUID"

[[ -n "$IMAGE_TAG" ]] || die "falta --image-tag (la etiqueta de las imágenes en el ACR, por ejemplo el SHA corto del commit)"
[[ "$IMAGE_TAG" != "latest" ]] || die "--image-tag no puede ser \`latest\`: usa el SHA del commit"
[[ "$IMAGE_TAG" =~ ^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$ ]] || die "--image-tag no es una etiqueta de imagen válida"
[[ "$RESOURCE_GROUP" =~ ^[A-Za-z0-9_.()-]+$ ]] || die "--resource-group tiene caracteres no válidos"
[[ "$CLUSTER_NAME" =~ ^[A-Za-z0-9-]+$ ]] || die "--cluster-name tiene caracteres no válidos"
[[ "$APPS_ENV" =~ ^[a-z]+$ ]] || die "--apps-env tiene caracteres no válidos"
[[ -f "$DEPLOY_DIR/apps/envs/$APPS_ENV.tfvars" ]] || die "no existe deploy/apps/envs/$APPS_ENV.tfvars"
[[ -n "$NAME_SUFFIX" ]] || die "falta --name-suffix (2 a 8 letras o números; es el que ya usa el entorno, por ejemplo el de kv-knockdev<sufijo>)"
[[ "$NAME_SUFFIX" =~ ^[a-z0-9]{2,8}$ ]] || die "--name-suffix debe tener de 2 a 8 letras minúsculas o números"

if $APPLY && $NO_LOCK; then
  die "--no-lock solo es para planes de lectura; con --apply el estado siempre se bloquea"
fi

az group show --name "$RESOURCE_GROUP" --subscription "$SUBSCRIPTION_ID" > /dev/null 2>&1 \
  || die "el grupo de recursos $RESOURCE_GROUP no existe en la suscripción (Terraform no lo crea; lo crea una persona)"

if [[ -z "$SECRET_ADMIN_IDS" ]]; then
  SECRET_ADMIN_IDS="$(az ad signed-in-user show --query id --output tsv 2> /dev/null || true)"
fi
[[ -n "$SECRET_ADMIN_IDS" ]] || die "no se pudo obtener tu object id: pásalo con --secret-admin-ids"
SECRET_ADMIN_HCL="["
sep=""
IFS=',' read -r -a _ids <<< "$SECRET_ADMIN_IDS"
for id in "${_ids[@]}"; do
  [[ "$id" =~ ^[0-9a-fA-F-]{36}$ ]] || die "object id no válido en --secret-admin-ids"
  SECRET_ADMIN_HCL+="${sep}\"${id}\""
  sep=","
done
SECRET_ADMIN_HCL+="]"

LOCK_ARG=()
$NO_LOCK && LOCK_ARG=(-lock=false)

PLAN_DIR="$(mktemp -d)"
trap 'rm -rf "$PLAN_DIR"' EXIT
trap 'exit 130' INT TERM

if $APPLY; then
  info "Modo APPLY: se aplicará cada directorio en orden infra -> database -> apps"
else
  info "Modo seguro: solo plan, no se modifica nada (usa --apply para aplicar)"
fi
info "Suscripción $SUBSCRIPTION_ID · grupo $RESOURCE_GROUP · sufijo $NAME_SUFFIX · etiqueta $IMAGE_TAG"

# ---- Funciones ----

# tf_init <dir>: inicializa con el estado remoto del directorio.
tf_init() {
  info "[$1] terraform init"
  terraform -chdir="$DEPLOY_DIR/$1" init -backend-config=backend.hcl -input=false -no-color > "$PLAN_DIR/$1.init.log" 2>&1 \
    || { cat "$PLAN_DIR/$1.init.log" >&2; die "falló terraform init en deploy/$1"; }
}

# tf_plan <dir> <args...>: guarda el plan y rechaza cualquier destrucción o reemplazo.
tf_plan() {
  local dir="$1"
  shift
  info "[$dir] terraform plan"
  terraform -chdir="$DEPLOY_DIR/$dir" plan -input=false -no-color ${LOCK_ARG[@]+"${LOCK_ARG[@]}"} \
    -var "subscription_id=$SUBSCRIPTION_ID" -var "resource_group_name=$RESOURCE_GROUP" "$@" \
    -out="$PLAN_DIR/$dir.tfplan" | tee "$PLAN_DIR/$dir.plan.txt" \
    || die "falló terraform plan en deploy/$dir"
  if grep -Eq 'Plan: .*[1-9][0-9]* to destroy|will be destroyed|must be replaced|Terraform will destroy' "$PLAN_DIR/$dir.plan.txt"; then
    die "el plan de deploy/$dir destruye o reemplaza recursos (prevent_destroy y los datos no se tocan con este script). Revísalo a mano"
  fi
}

# tf_apply <dir>: aplica exactamente el plan guardado.
tf_apply() {
  info "[$1] terraform apply"
  terraform -chdir="$DEPLOY_DIR/$1" apply -input=false -no-color "$PLAN_DIR/$1.tfplan" \
    || die "falló terraform apply en deploy/$1"
}

# check_secrets: solo lista los nombres de los secretos en Key Vault, nunca sus valores.
check_secrets() {
  local vault="kv-knockdev${NAME_SUFFIX}"
  [[ "$APPS_ENV" == "dev" || "$APPS_ENV" == "evaluacion" ]] || return 0
  local found
  found="$(az keyvault secret list --vault-name "$vault" --subscription "$SUBSCRIPTION_ID" --query '[].name' --output tsv 2> /dev/null || true)"
  local missing=()
  local s
  for s in "${REQUIRED_SECRETS[@]}"; do
    grep -qx "$s" <<< "$found" || missing+=("$s")
  done
  if ((${#missing[@]} > 0)); then
    echo "AVISO: faltan secretos en $vault: ${missing[*]}" >&2
    echo "       Escríbelos con \`az keyvault secret set --vault-name $vault --name <nombre> --value <valor>\` (deploy/README.md)." >&2
    return 1
  fi
  info "Key Vault $vault: están los ${#REQUIRED_SECRETS[@]} secretos requeridos"
}

# ---- 1. infra ----

tf_init infra
tf_plan infra -var "name_suffix=$NAME_SUFFIX" -var "secret_admin_object_ids=$SECRET_ADMIN_HCL"

# ---- 2. database (solo importa el clúster gratuito, prevent_destroy) ----

tf_init database
tf_plan database -var "cluster_name=$CLUSTER_NAME"

# Los planes de infra y database se revisan antes de aplicar cualquiera: si el de database destruye algo,
# el script se detiene sin haber cambiado nada en Azure.
if $APPLY; then
  tf_apply infra
  tf_apply database
fi

# ---- 3. apps ----

tf_init apps
INFRA_RESOURCES="$(terraform -chdir="$DEPLOY_DIR/infra" state list 2> /dev/null | wc -l | tr -d ' ')"
if [[ "$INFRA_RESOURCES" == "0" ]]; then
  $APPLY && die "con --apply, infra debe tener recursos en su estado antes de planear apps (no se pudo leer el estado)"
  # apps lee las salidas del estado de infra: sin infra creada no hay nada que planear.
  info "[apps] se omite el plan: infra todavía no existe (usa --apply para crearla y planear apps enseguida)"
  exit 0
fi
if ! check_secrets; then
  $APPLY && die "con --apply los secretos deben existir antes de desplegar las apps"
fi
tf_plan apps -var-file="envs/$APPS_ENV.tfvars" -var "image_tag=$IMAGE_TAG"
$APPLY && tf_apply apps

if $APPLY; then
  info "Listo: infra, database y apps aplicados"
else
  info "Listo: planes revisados sin cambios en Azure. Para aplicar, repite con --apply"
fi
