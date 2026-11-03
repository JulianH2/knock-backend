# Entorno de Azure con Terraform (TT-AZ-02, KNOCK-34)

> Spec: `specs/TT-AZ-02-infraestructura-como-codigo-terraform/spec.md`. **Los comandos de Azure y de Terraform los ejecuta una persona** (el Tech Lead). Este repositorio es público: no pegues aquí el ID de suscripción, el tenant, correos, contraseñas ni salidas con identificadores.
> **Antes de esta guía** hace falta el estado remoto de Terraform (TT-AZ-05, KNOCK-158): la guía de arranque está en `deploy/README.md`, sección 1 (PR de KNOCK-158). Esta guía supone que ya existen `TF_SA` (la cuenta del estado), `ARM_SUBSCRIPTION_ID` y `ARM_USE_AZUREAD=true` en la terminal.

## Dos raíces, dos estados

| Raíz | Qué crea | Estado en Azure |
|---|---|---|
| `deploy/persistent/` | **Adopta** el clúster gratuito de DocumentDB que ya existe (`knock-docdb`). El `destroy` principal no lo toca | `dev-persistent.tfstate` |
| `deploy/` | Entorno de Container Apps, la API, Key Vault y Blob; opcionalmente el gateway, los servicios separados y Redis | `dev.tfstate` |

## Crear el entorno y publicar la API (TT-AZ-02)

Un comando por raíz. **Los ejecutas tú, en este orden.** Terraform 1.9 o superior (`terraform -version`) y Azure CLI con sesión iniciada.

Por defecto solo se despliega **una aplicación, `ca-knock-api`**, con la imagen del monolito (`src/Knock.Api/Dockerfile`, puerto 8080), ingreso externo por HTTPS y `/health` como sonda. El gateway y los servicios separados quedan detrás de `enable_other_services=false` y Redis detrás de `enable_redis=false`, para no gastar crédito.

### Paso 1. Publicar la imagen en GHCR (desde la raíz del repositorio)

La imagen aún no existe. Necesitas un token de GitHub con permiso `write:packages` (`gh auth refresh -s write:packages` lo agrega a la sesión de `gh`).

```bash
export TAG="$(git rev-parse --short HEAD)"          # la rama que quieras desplegar, por ejemplo sprint-02 actualizada
docker build -f src/Knock.Api/Dockerfile -t "ghcr.io/julianh2/knock-api:$TAG" .
gh auth token | docker login ghcr.io -u julianh2 --password-stdin
docker push "ghcr.io/julianh2/knock-api:$TAG"
```

**Hazlo público** (una sola vez): en GitHub → tu perfil → Packages → `knock-api` → Package settings → *Change visibility* → Public. Así Azure descarga la imagen sin credenciales. La imagen no lleva secretos (la configuración llega por variables de entorno).

### Paso 2. `deploy/persistent/`: adoptar el clúster de DocumentDB que ya existe

La suscripción solo permite **un** clúster gratuito y `knock-docdb` ya existe (Free, West US, 32 GB, versión 8.0). Esta raíz **no lo crea**: lo importa con un bloque `import` (en `main.tf`) y lo declara con los mismos valores, con `prevent_destroy = true`. *Difiere de la spec TT-AZ-02 (decisión 4), que lo daba por creado aquí; la spec no se cambia.*

```bash
export TF_VAR_subscription_id="$ARM_SUBSCRIPTION_ID"
export TF_VAR_administrator_username="<usuario-admin-del-clúster>"   # el que usaste al crear el clúster
export TF_VAR_administrator_password="no-se-lee-de-azure"             # Terraform la ignora (ignore_changes); no uses la real

terraform -chdir=deploy/persistent init -backend-config="storage_account_name=$TF_SA"
terraform -chdir=deploy/persistent plan
```

El `plan` debe decir **«1 to import»** y **0 to destroy**. Si propone reemplazar (`must be replaced`) o destruir el clúster, **no apliques**: avisa antes.

```bash
terraform -chdir=deploy/persistent apply
```

Alternativa sin el bloque `import` (`<id-de-suscripcion>` es un marcador):

```bash
terraform -chdir=deploy/persistent import azurerm_mongo_cluster.this \
  "/subscriptions/<id-de-suscripcion>/resourceGroups/rg-knock-dev/providers/Microsoft.DocumentDB/mongoClusters/knock-docdb"
```

### Paso 3. `deploy/`, primera parte: la bóveda y el entorno

Container Apps lee los secretos de Key Vault al crear la aplicación, así que primero se crea la bóveda y se cargan los secretos.

```bash
export TF_VAR_secrets_admin_object_id="$(az ad signed-in-user show --query id -o tsv)"   # quien carga los secretos
export TF_VAR_image_tag="$TAG"

terraform -chdir=deploy init -backend-config="storage_account_name=$TF_SA"
terraform -chdir=deploy apply -target=module.shared
```

### Paso 4. Cargar los secretos de la API en Key Vault (los cargas tú)

Terraform nunca ve estos valores. El rol «Key Vault Secrets Officer» tarda uno o dos minutos en propagarse; si responde 403, espera y repite. Ninguno de los comandos imprime el valor.

| Secreto en Key Vault | Variable de entorno de la API | Qué es |
|---|---|---|
| `knock-mongo` | `ConnectionStrings__KnockMongo` | Cadena de conexión de `knock-docdb` con usuario y contraseña |
| `knock-blob` | `ConnectionStrings__KnockBlob` | Cadena de la cuenta de Blob de la aplicación |
| `jwt-signing-key` | `Jwt__SigningKey` | Llave HS256 en Base64 de al menos 32 bytes |
| `firebase-project-id` | `Firebase__ProjectId` | ID del proyecto de Firebase |

```bash
KV="$(terraform -chdir=deploy output -raw key_vault_name)"

# JWT: 48 bytes aleatorios en Base64
az keyvault secret set --vault-name "$KV" --name jwt-signing-key --value "$(openssl rand -base64 48)" --output none

# Firebase: el ID de tu proyecto (no es secreto, pero se lee igual desde la bóveda)
az keyvault secret set --vault-name "$KV" --name firebase-project-id --value "<id-del-proyecto-firebase>" --output none

# Blob: la cadena de la cuenta que acabó de crear Terraform
BLOB_CS="$(az storage account show-connection-string --name "$(terraform -chdir=deploy output -raw storage_account_name)" \
  --resource-group rg-knock-dev --query connectionString -o tsv)"
az keyvault secret set --vault-name "$KV" --name knock-blob --value "$BLOB_CS" --output none

# MongoDB: la cadena del clúster trae <user> y <password> como marcadores; escribe la contraseña sin que se vea
MONGO_CS="$(az resource show --ids "/subscriptions/$ARM_SUBSCRIPTION_ID/resourceGroups/rg-knock-dev/providers/Microsoft.DocumentDB/mongoClusters/knock-docdb" \
  --api-version 2024-07-01 --query properties.connectionString -o tsv)"
read -rp "Usuario del clúster: " DB_USER
read -rsp "Contraseña del clúster: " DB_PASS; echo
MONGO_CS="${MONGO_CS/<user>/$DB_USER}"; MONGO_CS="${MONGO_CS/<password>/$DB_PASS}"
az keyvault secret set --vault-name "$KV" --name knock-mongo --value "$MONGO_CS" --output none
unset DB_PASS MONGO_CS BLOB_CS
```

> Si la contraseña lleva caracteres especiales (`@`, `:`, `/`), la cadena debe llevarla codificada como URL.

### Paso 5. `deploy/`, segunda parte: la API

```bash
terraform -chdir=deploy plan  -var evaluation_window=true
terraform -chdir=deploy apply -var evaluation_window=true
terraform -chdir=deploy output api_url
```

`evaluation_window=true` deja **2 réplicas fijas** (hoy es evaluación); con `false` queda 1. La salida `api_url` es la URL pública (HTTPS). Comprueba:

```bash
curl -i "$(terraform -chdir=deploy output -raw api_url)/health"     # HTTP 200
```

### Paso 6. Más adelante

```bash
# Gateway y servicios separados (hoy con imagen de ejemplo; gasta crédito):
terraform -chdir=deploy apply -var evaluation_window=true -var enable_other_services=true

# Redis (primero carga la contraseña; Terraform solo la referencia):
az keyvault secret set --vault-name "$KV" --name redis-password --value "$(openssl rand -base64 32)" --output none
terraform -chdir=deploy apply -var evaluation_window=true -var enable_redis=true

# Llaves del celular, cuando se fusione HU-001 (cargar antes phone-encryption-key-1 = 32 bytes en Base64
# y phone-hmac-key-1 = al menos 32 bytes en Base64):
terraform -chdir=deploy apply -var evaluation_window=true -var enable_phone_protection=true

# Volver a 1 réplica al terminar la evaluación:
terraform -chdir=deploy apply -var evaluation_window=false
```

Repite siempre los mismos `-var` (`evaluation_window`, `enable_*`) en cada `apply`: lo que no pases vuelve a su valor por defecto y Terraform lo destruiría.

### Eliminar lo desplegado

```bash
terraform -chdir=deploy destroy -var evaluation_window=true -var enable_other_services=true -var enable_redis=true
```

No toca el clúster de DocumentDB (otra raíz, con `prevent_destroy`) ni la cuenta del estado (otro grupo de recursos). La bóveda queda en borrado temporal con protección contra purga; el siguiente `apply` crea otra con un sufijo nuevo y hay que volver a cargar los secretos.

## Cómo verificar cada caso de TT-AZ-02

| Caso | Comando | Qué debe pasar |
|---|---|---|
| 01 | `terraform fmt -check -recursive`, `terraform init -backend=false` y `terraform validate` en cada raíz | Sin errores y sin credenciales de Azure |
| 02 | `terraform -chdir=deploy plan -destroy -var image_tag=x` y revisar que no nombre `azurerm_mongo_cluster` | El clúster solo aparece en `deploy/persistent/` |
| 03 | `terraform -chdir=deploy plan -var image_tag=x -var location=eastus` y `terraform -chdir=deploy/persistent plan -var location=chilecentral` | Los dos fallan con el mensaje de validación y no proponen recursos |
| 04 | `az containerapp list -g rg-knock-dev --query "[].{n:name,ext:properties.configuration.ingress.external,http:properties.configuration.ingress.allowInsecure}"` | Solo las aplicaciones públicas (`ca-knock-api` y, si está activo, `ca-knock-gateway`) con `ext=true`; todas con `allowInsecure=false` |
| 05 | `plan` con y sin `-var evaluation_window=true` | 1 réplica sin la bandera y 2 con ella; Redis siempre 1; sin reglas de escalado |
| 06 | `plan` con `-var enable_redis=true` y revisión de `modules/redis/main.tf` | Imagen por digest, ingreso TCP interno, secreto por `key_vault_secret_id`, sin `random_password` |
| 07 | `az role assignment list --scope <id-de-la-bóveda>` | Cada aplicación solo con «Key Vault Secrets User»; la persona con «Key Vault Secrets Officer» solo en la bóveda |
| 08 | `az storage account show -n <cuenta-de-blob> --query "{tls:minimumTlsVersion,publico:allowBlobPublicAccess}"` | `TLS1_2` y `false`; contenedores privados |
| 09 | `plan` | Todos los recursos con `proyecto`, `entorno` y `responsable` |
| 10 | `git ls-files \| grep -E "\.tfstate\|\.tfvars"` y `gitleaks` | Sin resultados y limpio |
| 11 | Checkov sobre `deploy/` en el CI (cambio protegido de `ci-pr.yml`, lo hace el Tech Lead) | Sin hallazgos sin registrar |
| 12 | `apply` en las dos raíces y luego un segundo `plan` | Entorno creado y el segundo `plan` sin cambios |

La evidencia va en `docs/evidencia/KNOCK-34-terraform.md`, con el ID de suscripción, el tenant y los correos ocultos.

## Costo

El entorno: la API (0.5 vCPU y 1 GiB; el doble con 2 réplicas), Log Analytics, Key Vault y Blob; cada aplicación extra (gateway, servicios, Redis) suma consumo. Nada de esto es gratuito: cuenta contra el crédito de estudiante. Estimación con tarifas: `docs/evidencia/KNOCK-145-costo-cero.md`. Revisa el `plan` y el costo antes de aplicar.
