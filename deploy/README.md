# Infraestructura de Knock (Terraform)

> TT-AZ-05 (KNOCK-158, estado remoto) y TT-AZ-02 (KNOCK-34, el entorno). Specs: `specs/TT-AZ-05-estado-remoto-de-terraform/` y `specs/TT-AZ-02-infraestructura-como-codigo-terraform/`.
> **Los comandos de Azure y de Terraform los ejecuta una persona** (el Tech Lead). Un agente no opera Azure. Este repositorio es público: no pegues aquí el ID de suscripción, el tenant, correos, contraseñas ni salidas con identificadores.

## Dos raíces, dos estados

| Raíz | Qué crea | Estado en Azure |
|---|---|---|
| `deploy/persistent/` | El clúster gratuito de DocumentDB (MongoDB). El `destroy` principal no lo toca | `dev-persistent.tfstate` |
| `deploy/` | Entorno de Container Apps, aplicaciones, Redis, Key Vault y Blob | `dev.tfstate` |

Los dos estados viven en el contenedor `tfstate` de una cuenta de almacenamiento del grupo `rg-knock-tfstate`, aparte del grupo que borra `terraform destroy`.

## 1. Arranque del estado remoto (una sola vez)

Requisitos: Azure CLI con sesión iniciada en la suscripción Azure for Students (`az login`) y Terraform 1.9 o superior.

La cuenta de almacenamiento necesita un nombre único en todo Azure (3 a 24 letras minúsculas y números). Elige un sufijo corto de 4 caracteres y úsalo siempre igual.

```bash
# Git Bash: evita que MSYS convierta las rutas que empiezan con /subscriptions
export MSYS_NO_PATHCONV=1

export ARM_SUBSCRIPTION_ID="$(az account show --query id -o tsv)"
export TF_RG="rg-knock-tfstate"
export TF_LOC="northcentralus"
export TF_SA="stknocktfstate<sufijo>"      # por ejemplo stknocktfstate7k2q
export TAGS="proyecto=knock entorno=dev responsable=julian"

# 1) Grupo de recursos aparte, para que terraform destroy no se lleve el estado
az group create --name "$TF_RG" --location "$TF_LOC" --tags $TAGS

# 2) Cuenta del estado: TLS 1.2, sin acceso público a blobs y sin llaves compartidas (solo Entra ID)
az storage account create --name "$TF_SA" --resource-group "$TF_RG" --location "$TF_LOC" \
  --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 \
  --allow-blob-public-access false --allow-shared-key-access false --tags $TAGS

# 3) Versionado de blobs, para recuperar una versión anterior del estado
az storage account blob-service-properties update --account-name "$TF_SA" --resource-group "$TF_RG" \
  --enable-versioning true

# 4) Contenedor del estado, por el plano de administración (no necesita permiso de datos)
az storage container-rm create --storage-account "$TF_SA" --resource-group "$TF_RG" --name tfstate

# 5) Rol de datos de blobs para quien aplica, SOLO a nivel del contenedor (el rol mínimo)
SA_ID="$(az storage account show --name "$TF_SA" --resource-group "$TF_RG" --query id -o tsv)"
CT_ID="$SA_ID/blobServices/default/containers/tfstate"
ME="$(az ad signed-in-user show --query id -o tsv)"
az role assignment create --assignee-object-id "$ME" --assignee-principal-type User   --role "Storage Blob Data Contributor" --scope "$CT_ID"
```

> La propagación del rol tarda uno o dos minutos; si `terraform init` responde 403, espera y repite.

### Iniciar cada raíz

El nombre de la cuenta se pasa al iniciar porque lleva sufijo y un bloque `backend` no admite variables. `use_azuread_auth` ya está en `backend.tf`.

```bash
export ARM_USE_AZUREAD=true
terraform -chdir=deploy init -backend-config="storage_account_name=$TF_SA"
terraform -chdir=deploy/persistent init -backend-config="storage_account_name=$TF_SA"
```

## Cómo verificar cada caso de TT-AZ-05

| Caso | Comando | Qué debe pasar |
|---|---|---|
| 01 | `terraform -chdir=deploy init -backend-config="storage_account_name=$TF_SA"` y `ls deploy` | «Successfully configured the backend "azurerm"»; no hay `terraform.tfstate` en `deploy/` (solo `.terraform/`, que `.gitignore` excluye). Igual en `deploy/persistent/` |
| 02 | `git ls-files \| grep -E "\.tfstate\|\.tfvars\|\.terraform/"` y `git check-ignore deploy/.terraform.lock.hcl` | Ambos sin salida: nada de estado versionado y el archivo de bloqueo de proveedores **no** está ignorado |
| 03 | En una terminal `terraform -chdir=deploy apply` y **no confirmes** (déjalo en la pregunta); en otra repite `terraform -chdir=deploy plan` | La segunda falla con «state blob is already locked» y no cambia nada. Cancela la primera con `no` |
| 04 | `az storage account show -n "$TF_SA" -g "$TF_RG" --query "{tls:minimumTlsVersion,publico:allowBlobPublicAccess,llaves:allowSharedKeyAccess}"`; `az storage account blob-service-properties show --account-name "$TF_SA" -g "$TF_RG" --query isVersioningEnabled`; `az role assignment list --scope "$CT_ID" --query "[].roleDefinitionName"` | `TLS1_2`, `false`, `false`; versionado `true`; el rol aparece en el contenedor (oculta los principales en la evidencia) |
| 05 | `gitleaks` en el CI y revisión del diff | Solo nombres de cuenta, contenedor y archivo; ni `subscription_id` ni `tenant_id` |

La evidencia va en `docs/evidencia/KNOCK-158-estado-remoto.md`, con el ID de suscripción, el tenant y los correos ocultos (`<id-de-suscripcion>`, `<correo-del-responsable>`).

## Costo

Almacenamiento LRS estándar con un archivo pequeño. No es gratuito: cuenta contra el crédito de estudiante.
