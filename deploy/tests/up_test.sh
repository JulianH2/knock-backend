#!/usr/bin/env bash
# Pruebas simuladas de deploy/up.sh (KNOCK-34). No usan Azure ni credenciales: `terraform` y `az` son falsos y
# viven en un PATH temporal. Bash puro, sin dependencias externas.
#
#   bash deploy/tests/up_test.sh
#
# Cada prueba imprime OK o FALLA; el script termina con código distinto de cero si alguna falla.
set -u

TESTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UP="$TESTS_DIR/../up.sh"
[[ -f "$UP" ]] || { echo "No se encontró $UP" >&2; exit 2; }

WORK="$(mktemp -d)"
trap 'rm -rf "${WORK:?}"' EXIT
FAKE_BIN="$WORK/bin"
mkdir -p "$FAKE_BIN" "$WORK/tmp" "$WORK/tmp-int"
LOG="$WORK/calls.log"
OUT="$WORK/out.txt"
SENTINEL="SECRETO-CENTINELA-NO-DEBE-SALIR-7f3a"
SUB="11111111-2222-3333-4444-555555555555"
ADMIN="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
ALL_SECRETS="jwt-signing-key redis-password redis-connection-string pii-encryption-key pii-hmac-key mongo-identity mongo-market mongo-messaging field-encryption-key identity-field-encryption-key search-cursor-key"

# ---- terraform falso ----
# Registra cada llamada en $LOG con el formato «subcomando|directorio|argumentos».
# Se controla con variables de entorno: PLAN_TEXT_<dir>, PLAN_FAIL_<dir>, APPLY_FAIL_<dir>, STATE_EMPTY, STATE_FAIL, PLAN_HOLD.
cat > "$FAKE_BIN/terraform" <<'FAKE'
#!/usr/bin/env bash
dir=""
if [[ "${1:-}" == -chdir=* ]]; then dir="$(basename "${1#-chdir=}")"; shift; fi
sub="${1:-}"; shift || true
echo "$sub|$dir|$*" >> "$LOG"
case "$sub" in
  version) echo "Terraform v1.17.1"; echo "on fake_platform" ;;
  init) echo "Terraform has been successfully initialized!" ;;
  plan)
    out=""
    for a in "$@"; do [[ "$a" == -out=* ]] && out="${a#-out=}"; done
    pf="PLAN_FAIL_$dir"; [[ -z "${!pf:-}" ]] || { echo "Error: plan simulado fallido" >&2; exit 1; }
    pt="PLAN_TEXT_$dir"
    echo "${!pt:-No changes. Your infrastructure matches the configuration.}"
    echo "plan-de-$dir" > "$out"
    if [[ -n "${PLAN_HOLD:-}" && "$dir" == "$PLAN_HOLD" ]]; then touch "$HOLD_FLAG"; sleep 1.5; fi
    ;;
  apply)
    af="APPLY_FAIL_$dir"; [[ -z "${!af:-}" ]] || { echo "Error: apply simulado fallido" >&2; exit 1; }
    last=""
    for last in "$@"; do :; done
    [[ -f "$last" ]] || { echo "apply sin plan guardado" >&2; exit 9; }
    echo "Apply complete! Resources: 0 added, 0 changed, 0 destroyed." ;;
  state)
    [[ -z "${STATE_FAIL:-}" ]] || { echo "Error: no se pudo leer el estado" >&2; exit 1; }
    [[ -n "${STATE_EMPTY:-}" ]] || { echo "azurerm_resource_group.a"; echo "azurerm_key_vault.b"; } ;;
  *) echo "terraform falso: subcomando no previsto: $sub" >&2; exit 3 ;;
esac
FAKE

# ---- az falso ----
# Los valores de los secretos solo saldrían con `secret show` o sin --query; el centinela detecta cualquier fuga.
cat > "$FAKE_BIN/az" <<'FAKE'
#!/usr/bin/env bash
echo "az|$*" >> "$LOG"
case "$1 $2" in
  "account show") if [[ "$*" == *"--query"* ]]; then echo "11111111-2222-3333-4444-555555555555"; else echo '{"id":"x"}'; fi ;;
  "group show") [[ -z "${AZ_GROUP_FAIL:-}" ]] || exit 3; echo '{"name":"rg"}' ;;
  "ad signed-in-user") echo "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" ;;
  "keyvault secret")
    case "$3" in
      list)
        if [[ "$*" == *"--query"* ]]; then for s in ${KV_SECRETS-$ALL_SECRETS}; do echo "$s"; done
        else echo "[{\"name\":\"jwt-signing-key\",\"value\":\"$SENTINEL\"}]"; fi ;;
      show) echo "$SENTINEL" ;;
      *) exit 4 ;;
    esac ;;
  *) echo "az falso: no previsto: $*" >&2; exit 5 ;;
esac
FAKE
chmod +x "$FAKE_BIN/terraform" "$FAKE_BIN/az"

export LOG SENTINEL ALL_SECRETS TMPDIR="$WORK/tmp"
export HOLD_FLAG="$WORK/hold.flag"
export PATH="$FAKE_BIN:$PATH"
unset IMAGE_TAG SUBSCRIPTION_ID RESOURCE_GROUP NAME_SUFFIX CLUSTER_NAME SECRET_ADMIN_IDS APPS_ENV
unset STATE_EMPTY STATE_FAIL PLAN_HOLD KV_SECRETS AZ_GROUP_FAIL

BASE=(--image-tag abc1234 --name-suffix tn01 --subscription-id "$SUB" --resource-group rg-test --secret-admin-ids "$ADMIN")
RC=0
FAILS=0
LEAK=0

# run_up [VAR=valor ...] -- [argumentos de up.sh]
run_up() {
  local envs=()
  while [[ "${1:-}" != "--" ]]; do envs+=("$1"); shift; done
  shift
  : > "$LOG"
  env ${envs[@]+"${envs[@]}"} bash "$UP" "$@" > "$OUT" 2>&1
  RC=$?
  if grep -q "$SENTINEL" "$OUT"; then LEAK=1; fi
  return 0
}

ok() { echo "OK     $1"; }
fail() { echo "FALLA  $1 -> $2"; FAILS=$((FAILS + 1)); }
# check <nombre> <comando...>: OK si el comando devuelve 0.
check() { local name="$1"; shift; if "$@"; then ok "$name"; else fail "$name" "no se cumplió: $*"; fi; }

n_calls() { grep -c -E "$1" "$LOG" || true; }
has_out() { grep -q -- "$1" "$OUT"; }
no_call() { [[ "$(n_calls "$1")" == "0" ]]; }
rc_nonzero() { [[ "$RC" -ne 0 ]]; }
rc_zero() { [[ "$RC" -eq 0 ]]; }
# Secuencia de planes y aplicaciones (sin init, version ni state), en una línea.
sequence() { grep -E '^(plan|apply)\|' "$LOG" | cut -d'|' -f1,2 | tr '\n' ' '; }
# Falla (devuelve 0 = cumplido) si terminó con error y la llamada dada no ocurrió.
failed_without() { rc_nonzero && no_call "$1"; }

echo "== Pruebas simuladas de deploy/up.sh =="

# (a) sin --apply nunca se llama a apply
run_up -- "${BASE[@]}"
check "a. sin --apply termina bien" rc_zero
check "a. sin --apply nunca se llama a terraform apply" no_call '^apply\|'
check "a. sin --apply se planean los tres directorios" test "$(sequence)" = "plan|infra plan|database plan|apps "

# (b) --apply --no-lock se rechaza
run_up -- "${BASE[@]}" --apply --no-lock
check "b. --apply --no-lock se rechaza" rc_nonzero
check "b. --apply --no-lock explica el motivo" has_out "no-lock"
check "b. --apply --no-lock no llama a init, plan ni apply" no_call '^(plan|apply|init)\|'
run_up -- "${BASE[@]}" --no-lock
check "b. --no-lock sin --apply planea con -lock=false" bash -c "grep -E '^plan\|' '$LOG' | grep -q -- '-lock=false'"

# (c) --image-tag latest o ausente se rechaza
run_up -- "${BASE[@]}" --image-tag latest
check "c. --image-tag latest se rechaza" failed_without '^(plan|apply)\|'
run_up -- --name-suffix tn01 --subscription-id "$SUB" --secret-admin-ids "$ADMIN"
check "c. sin --image-tag se rechaza" failed_without '^(plan|apply)\|'
run_up IMAGE_TAG=latest -- --name-suffix tn01 --subscription-id "$SUB" --secret-admin-ids "$ADMIN"
check "c. IMAGE_TAG=latest por entorno se rechaza" failed_without '^(plan|apply)\|'

# (d) un plan con destroy o replace detiene el script antes de aplicar nada
run_up "PLAN_TEXT_infra=Plan: 0 to add, 1 to change, 1 to destroy." -- "${BASE[@]}" --apply
check "d. destroy en infra detiene el script y no aplica nada" failed_without '^apply\|'
run_up "PLAN_TEXT_database=Plan: 0 to add, 0 to change, 1 to destroy." -- "${BASE[@]}" --apply
check "d. destroy en database detiene el script y no aplica ni infra" failed_without '^apply\|'
run_up "PLAN_TEXT_database=  # azurerm_x.y must be replaced" -- "${BASE[@]}" --apply
check "d. replace en database no aplica nada" failed_without '^apply\|'
run_up "PLAN_TEXT_infra=  # azurerm_x.y will be destroyed" -- "${BASE[@]}"
check "d. destroy sin --apply también detiene el script" rc_nonzero
run_up "PLAN_TEXT_apps=Plan: 0 to add, 0 to change, 2 to destroy." -- "${BASE[@]}" --apply
check "d. destroy en apps no aplica apps" failed_without '^apply\|apps'
run_up PLAN_FAIL_database=1 -- "${BASE[@]}" --apply
check "d. si falla el plan de database no se aplica nada" failed_without '^apply\|'

# (e) fallo al leer el estado de infra
run_up STATE_FAIL=1 -- "${BASE[@]}" --apply
check "e. fallo al leer el estado con --apply termina con error y no planea apps" failed_without '^(plan|apply)\|apps'
run_up STATE_EMPTY=1 -- "${BASE[@]}" --apply
check "e. estado vacío con --apply termina con error y no planea apps" failed_without '^(plan|apply)\|apps'
run_up STATE_FAIL=1 -- "${BASE[@]}"
check "e. fallo al leer el estado sin --apply termina con error y no planea apps" failed_without '^(plan|apply)\|apps'

# (f) faltan secretos de Key Vault
run_up "KV_SECRETS=jwt-signing-key redis-password" -- "${BASE[@]}"
check "f. sin --apply avisa de los secretos que faltan" has_out "AVISO: faltan secretos"
check "f. el aviso nombra mongo-messaging" has_out "mongo-messaging"
run_up "KV_SECRETS=jwt-signing-key redis-password" -- "${BASE[@]}" --apply
check "f. con --apply y secretos faltantes termina con error y no aplica apps" failed_without '^apply\|apps'
run_up KV_SECRETS= -- "${BASE[@]}" --apply
check "f. con Key Vault vacío y --apply no aplica apps" failed_without '^apply\|apps'

# (g) entorno vacío: se omite el plan de apps y se avisa
run_up STATE_EMPTY=1 -- "${BASE[@]}"
check "g. entorno vacío termina bien" rc_zero
check "g. entorno vacío avisa que se omite apps" has_out "se omite el plan"
check "g. entorno vacío planea solo infra y database" test "$(sequence)" = "plan|infra plan|database "

# (h) orden y plan guardado exacto
run_up -- "${BASE[@]}" --apply
check "h. --apply completo termina bien" rc_zero
check "h. orden: planes de infra y database, apply infra, apply database, plan y apply de apps" \
  test "$(sequence)" = "plan|infra plan|database apply|infra apply|database plan|apps apply|apps "
planned_ok=1
for d in infra database apps; do
  p="$(grep -E "^plan\|$d\|" "$LOG" | sed -E 's/.*-out=([^ ]+).*/\1/')"
  a="$(grep -E "^apply\|$d\|" "$LOG" | sed -E 's/^apply\|[a-z]+\|//; s/.* //')"
  [[ -n "$p" && "$p" == "$a" ]] || planned_ok=0
  if grep -E "^apply\|$d\|" "$LOG" | grep -q -- '-var'; then planned_ok=0; fi
done
check "h. cada apply recibe exactamente el plan guardado y ninguna variable" test "$planned_ok" = "1"
check "h. el plan de infra lleva el sufijo y los administradores de secretos" bash -c "grep -E '^plan\|infra\|' '$LOG' | grep -q 'name_suffix=tn01' && grep -E '^plan\|infra\|' '$LOG' | grep -q 'secret_admin_object_ids=\[\"$ADMIN\"\]'"
check "h. el plan de apps lleva la etiqueta y el tfvars del entorno" bash -c "grep -E '^plan\|apps\|' '$LOG' | grep -q 'image_tag=abc1234' && grep -E '^plan\|apps\|' '$LOG' | grep -q 'var-file=envs/dev.tfvars'"
check "h. los tres init usan backend.hcl" bash -c "[[ \$(grep -cE '^init\|' '$LOG') == 3 ]] && ! grep -E '^init\|' '$LOG' | grep -qv 'backend-config=backend.hcl'"
run_up APPLY_FAIL_infra=1 -- "${BASE[@]}" --apply
check "h. si falla el apply de infra no se aplica database ni apps" failed_without '^apply\|(database|apps)'
run_up APPLY_FAIL_database=1 -- "${BASE[@]}" --apply
check "h. si falla el apply de database no se planea ni aplica apps" failed_without '^(plan|apply)\|apps'

# (i) no se imprimen valores de secretos
run_up -- "${BASE[@]}" --apply
check "i. el centinela no aparece en la salida de un apply completo" test "$LEAK" = "0"
check "i. nunca se pide el valor de un secreto (secret show / --value)" no_call 'secret show|--value'
check "i. la lista de secretos pide solo los nombres (--query)" bash -c "grep -E '^az\|keyvault secret list' '$LOG' | grep -q -- '--query'"
run_up "KV_SECRETS=jwt-signing-key" -- "${BASE[@]}"
check "i. el centinela tampoco sale cuando faltan secretos" test "$LEAK" = "0"

# (j) interrupción: limpia los planes
rm -f "$HOLD_FLAG"
: > "$LOG"
set -m
env PLAN_HOLD=database TMPDIR="$WORK/tmp-int" bash "$UP" "${BASE[@]}" --apply > "$OUT" 2>&1 &
UP_PID=$!
set +m
for _ in $(seq 1 100); do [[ -f "$HOLD_FLAG" ]] && break; sleep 0.1; done
had_dir=0
[[ -n "$(ls -A "$WORK/tmp-int" 2> /dev/null)" ]] && had_dir=1
kill -INT "$UP_PID" 2> /dev/null
wait "$UP_PID" 2> /dev/null
RC=$?
check "j. durante la ejecución existía el directorio de planes" test "$had_dir" = "1"
check "j. SIGINT termina con 130" test "$RC" = "130"
check "j. SIGINT borra los planes temporales" test -z "$(ls -A "$WORK/tmp-int" 2> /dev/null)"
check "j. SIGINT no deja aplicar nada" no_call '^apply\|'

# Otros prerrequisitos
run_up AZ_GROUP_FAIL=1 -- "${BASE[@]}"
check "extra. grupo de recursos inexistente se rechaza antes de planear" failed_without '^plan\|'
run_up -- "${BASE[@]}" --name-suffix "MAL SUFIJO"
check "extra. sufijo inválido se rechaza antes de planear" failed_without '^plan\|'

echo
if ((FAILS > 0)); then
  echo "RESULTADO: $FAILS prueba(s) con FALLA"
  exit 1
fi
echo "RESULTADO: todas las pruebas OK"
