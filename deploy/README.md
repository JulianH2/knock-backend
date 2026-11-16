# Despliegue en Azure con Terraform (spec 005)

Todo se crea y se borra con comandos. Hay tres directorios, cada uno con su propio estado remoto, para que borrar
una parte no toque las demás:

| Directorio | Qué contiene | Se destruye |
|---|---|---|
| `database/` | Clúster gratuito de Azure DocumentDB (solo se importa) | **Nunca** (`prevent_destroy`, CA-03) |
| `infra/` | Red, entorno de Container Apps, Key Vault, Blob Storage, ACR, logs (CA-01) | Paso 4 |
| `apps/` | Gateway, Identity, Messaging, Market y Redis | Paso 3 |

`modules/shared` es el módulo compartido; `modules/service` es un solo módulo de servicio que se instancia una vez
por servicio, cada una con su identidad administrada y acceso solo a sus secretos (R-05).

## Requisitos
Terraform >= 1.9, Azure CLI con `az login`, y la región `westus` (las otras permitidas por la suscripción:
`chilecentral`, `norwayeast`, `francecentral`, `northcentralus`; ADR-006).

## Paso 0 — una sola vez
`bootstrap/bootstrap.sh` crea el almacenamiento del estado y la identidad federada del pipeline. Lo corre una
persona; no guarda contraseñas. Luego, en cada directorio: `terraform init -backend-config=backend.hcl`.

## Todo en un comando (`up.sh`)
`deploy/up.sh` recorre `infra` → `database` → `apps` con `terraform init -backend-config=backend.hcl` y `plan` en cada uno. Por defecto **solo hace `plan`** (modo seguro, no cambia nada en Azure); aplica únicamente con `--apply`.
```
az login
./deploy/up.sh --image-tag <sha> --name-suffix <sufijo>               # solo plan
./deploy/up.sh --image-tag <sha> --name-suffix <sufijo> --no-lock     # plan de solo lectura, sin bloquear el estado
./deploy/up.sh --image-tag <sha> --name-suffix <sufijo> --apply       # aplica en orden infra, database y apps
```
- Prerrequisitos que valida: Terraform >= 1.9, Azure CLI con sesión iniciada, que exista el grupo de recursos (`--resource-group`, por defecto `rg-knock-dev`; Terraform no lo crea), una `--image-tag` distinta de `latest` y el `--name-suffix` del entorno. La suscripción sale de `az account show` (o `--subscription-id`) y los administradores de secretos, de tu sesión (o `--secret-admin-ids`).
- No imprime secretos: pasa solo valores no secretos a Terraform y de Key Vault únicamente lista los **nombres** de los secretos. Con `--apply` exige que existan los ocho (`jwt-signing-key`, `redis-password`, `redis-connection-string`, `pii-encryption-key`, `pii-hmac-key`, `mongo-identity`, `mongo-market`, `mongo-messaging`), que una persona escribe a mano.
- Respeta `prevent_destroy` y la importación de `knock-docdb`: `database/` solo importa el clúster (en un entorno nuevo el plan dice «1 to import»; después, «No changes»). Si el plan de cualquiera de los tres directorios destruye o reemplaza algún recurso, el script se detiene aunque tenga `--apply`.
- Con `--apply` primero planea `infra` y `database` (si cualquiera destruye algo, se detiene sin haber cambiado nada), luego aplica exactamente el plan guardado de cada directorio, en orden, y se detiene en el primer error. Si `terraform state list` de `infra` no devuelve recursos, no continúa con `apps`. `--no-lock` no se permite con `--apply`.
- En un entorno que todavía no existe, el plan de `apps` no se puede calcular (lee las salidas del estado de `infra`): el script lo omite y avisa. Con `--apply` crea `infra` y planea `apps` enseguida.
- El perfil «Consumption» ya está declarado en el entorno y en las Container Apps, así que un plan de un entorno ya desplegado no debe mostrar diferencias de `workload_profile`. Si aparecen, es un cambio real que hay que revisar.
- La identidad `id-knock-deploy` (la que usa `ci-deploy` para subir imágenes) necesita **dos** credenciales federadas para la rama `main`, porque GitHub puede presentar el sujeto con dos formas: `repo:Tensei-Devs/knock-backend:ref:refs/heads/main` y `repo:Tensei-Devs@<id-organización>/knock-backend@<id-repositorio>:ref:refs/heads/main`. Sin la segunda, el inicio de sesión falla con `AADSTS700213`. Hoy existen `github-main` y `github-main-ids`; se crean a mano con `az identity federated-credential create` (emisor `https://token.actions.githubusercontent.com`, audiencia `api://AzureADTokenExchange`).
- Con `--help` se ven todas las opciones.

## Paso 1 — crear
```
cd deploy/infra
terraform apply -var subscription_id=<ID> -var resource_group_name=<RG> -var name_suffix=<2-8 letras/números> \
  -var 'secret_admin_object_ids=["<tu object id>"]'
```
Con `database/` se importa el clúster la primera vez: `terraform apply -var subscription_id=... -var resource_group_name=... -var cluster_name=...`.

## Secretos (una persona, a mano)
Los valores no pasan por Terraform ni por el repositorio (CA-04). Nombres fijos en Key Vault:
`jwt-signing-key`, `redis-password` (contraseña del servidor Redis), `redis-connection-string` (la cadena que leen los servicios: `knock-redis:6379,password=<la misma contraseña>`), `pii-encryption-key`, `pii-hmac-key`, `mongo-identity`, `mongo-market`,
`mongo-messaging`.
```
az keyvault secret set --vault-name <kv> --name redis-password --value "<valor>"
```

### Formato que exige el código (si no, falla en el primer inicio de sesión)
- `jwt-signing-key`: Base64 de **al menos 32 bytes**.
- `pii-encryption-key`: Base64 de **exactamente 32 bytes** (AES-256-GCM).
- `pii-hmac-key`: Base64 de **al menos 32 bytes** y **distinta** de la de cifrado.
- `redis-connection-string`: `knock-redis:6379,password=<la misma de redis-password>`.
Una forma de generarlas: `python -c "import os,base64;print(base64.b64encode(os.urandom(32)).decode())"`. Los valores nunca se pegan en un chat ni en el repositorio.

## Paso 2 — desplegar
Las imágenes deben estar en el ACR (las publica el pipeline, spec 007).
```
cd deploy/apps
terraform apply -var-file=envs/dev.tfvars -var subscription_id=<ID> -var resource_group_name=<RG> -var image_tag=<sha>
```
El gateway, Identity y Market confían en `X-Forwarded-For` solo desde la subred del entorno (`container_apps_subnet_cidr`, por defecto `10.20.0.0/23`; debe ser igual a la del módulo compartido). En el primer despliegue comprueba que la IP que ve el gateway sea la del cliente y no la del ingreso.

Para la ventana de evaluación (25-nov al 5-dic) usar `envs/evaluacion.tfvars`. Si el primer apply falla por permisos
sobre un secreto, espera unos minutos a que se propague el rol y repite.

## Imagen de los Container Apps
Desde KNOCK-202 (CA-03 de la spec 007), la imagen de los servicios la manda `ci-deploy` en cada commit de `main` (`az containerapp update --image <acr>/knock-<servicio>:<sha>`). El módulo `deploy/modules/service` ignora los cambios de imagen (`lifecycle.ignore_changes`), así que `terraform apply` ya no la regresa a `var.image_tag`; esa variable solo cuenta al **crear** una app.

Redis usa el mismo módulo, así que Terraform tampoco actualiza su imagen. Para cambiarla (está fijada por digest y casi nunca cambia), una persona hace una de estas dos cosas:
- `az containerapp update --name knock-redis --resource-group <grupo> --image <imagen@sha256:digest>`
- `terraform apply -replace='module.redis.azurerm_container_app.this'` con el nuevo `redis_image`. Recrea la app y Redis pierde todo lo que tiene en memoria: la lista de sesiones revocadas (un token cerrado vuelve a servir hasta que vence), los contadores de límite de peticiones y la caché del catálogo y de perfiles. Hazlo fuera del horario de pruebas y avisa al equipo.

## Permisos de `id-knock-deploy`
`ci-deploy` entra a Azure por OIDC con la identidad administrada `id-knock-deploy`. Con mínimo privilegio necesita solo:

| Rol | Alcance | Para qué |
|---|---|---|
| `AcrPush` | el registro `acrknockdevtn01` | `az acr login` y `docker push` de las imágenes |
| `Container Apps Contributor` | el grupo de recursos `rg-knock-dev` | `az containerapp show`, `update` y `revision show` de `knock-*` |

No necesita `Contributor` ni `Owner`, ni permisos en la suscripción, en Key Vault ni en la base: las apps leen sus secretos con su propia identidad. Se revisa con `az role assignment list --assignee <principalId de id-knock-deploy> --all -o table`.

## Paso 3 — eliminar lo desplegado
`cd deploy/apps && terraform destroy ...` (mismas variables). Deja la infraestructura y la base.

## Paso 4 — destruir
`cd deploy/infra && terraform destroy ...`. **No toques `database/`**: el clúster gratuito es único por suscripción.

## Comprobaciones sin Azure
```
terraform fmt -check -recursive deploy
for d in infra apps database; do (cd deploy/$d && terraform init -backend=false && terraform validate); done
(cd deploy/modules/shared && terraform init -backend=false && terraform test)
(cd deploy/modules/service && terraform init -backend=false && terraform test)
```

## Cómo validar `up.sh` desde cero
Crear el entorno completo consume crédito de Azure, así que la validación se hace por etapas, de la más barata a la más cara. Las etapas 1 a 4 no crean ni cambian nada.

| Etapa | Qué se hace | Dónde corre | Qué demuestra | Resultado |
|---|---|---|---|---|
| 1. Estática | `bash -n deploy/up.sh deploy/tests/up_test.sh` y ShellCheck (`docker run --rm -v "$PWD:/mnt" -w /mnt koalaman/shellcheck:stable deploy/up.sh deploy/tests/up_test.sh`) | Cualquier máquina con Docker | Sintaxis y errores típicos de bash | Sin errores ni hallazgos |
| 2. Simulada | `bash deploy/tests/up_test.sh`: ejecuta el `up.sh` real con `terraform` y `az` falsos en un `PATH` temporal | Linux, CI o Git Bash; sin credenciales ni red | La lógica del script: modo seguro, rechazos, destrucciones, orden, plan guardado, secretos, entorno vacío e interrupción | 44 comprobaciones OK en unos 20 s |
| 3. Terraform sin credenciales | `terraform fmt -check -recursive deploy` y, por cada directorio (`infra`, `apps`, `database`), `terraform init -backend=false` y `terraform validate`; además `terraform test` en `modules/shared` y `modules/service` | Cualquier máquina con Terraform >= 1.9 | La configuración es sintácticamente válida y coherente | `fmt` sin diferencias; `validate` válido en los tres; `terraform test`: 3 y 6 pruebas pasan (Terraform 1.17.1) |
| 4. Plan de solo lectura | `./deploy/up.sh --image-tag <sha> --name-suffix <sufijo> --no-lock` contra el entorno existente | Una persona con acceso a `rg-knock-dev` y a `stknocktfstate` | Que el script y el estado remoto reales se entienden | Esperado: infra «No changes»; database «No changes»; apps 0 por agregar, solo los cambios reales de la versión (por ejemplo, la imagen), 0 por destruir |
| 5. Plan «desde cero» | Planear en un entorno vacío, sin crear nada | Una persona con otra suscripción, o con otro sufijo y otro grupo, y un estado distinto | Que el plan de creación es completo y sin destrucciones | Ver «Qué haría falta para planear desde cero» |
| 6. Apply real desde cero | `./deploy/up.sh ... --apply` en un entorno vacío | Una persona autorizada | Que la creación funciona de verdad (cuotas, nombres, permisos, orden) | Ver «Apply real desde cero» |

Cada prueba de la etapa 2 imprime `OK` o `FALLA` y el script termina con código distinto de cero si alguna falla. Cubre, con `terraform` y `az` simulados: sin `--apply` nunca se llama a `apply`; `--apply --no-lock` se rechaza; `--image-tag` ausente o `latest` se rechaza; un plan con destrucción o reemplazo detiene el script antes de aplicar nada (también si ocurre solo en `database` o en `apps`); un fallo al leer el estado de `infra` termina con error y no omite `apps` en silencio; faltan secretos de Key Vault; entorno vacío (se omite el plan de `apps` y se avisa); el orden `infra` → `database` → `apps` y que cada `apply` recibe exactamente el plan guardado; ningún valor de secreto aparece en la salida (los falsos devuelven un valor centinela); y una interrupción (SIGINT) borra los planes temporales y termina con 130. No hay un paso de CI para estas pruebas: se corren a mano.

Lo que la etapa 2 **no** demuestra: que Azure acepte la creación. Las pruebas comprueban lo que el script decide, no lo que Terraform o Azure responden.

### Qué haría falta para planear desde cero
- Un entorno aparte, porque el estado remoto (`stknocktfstate`, claves `infra.tfstate`, `database.tfstate` y `apps.tfstate`) ya describe el entorno existente. Opciones: otra suscripción, o la misma con otro `--name-suffix`, otro `--resource-group` y un estado distinto (otra clave en `backend.hcl`, o estado local).
- Un grupo de recursos creado a mano (Terraform no lo crea) y una sesión de `az login` con permisos para crear recursos y asignar roles.
- Mientras `infra` no exista, el plan de `apps` no se puede calcular (lee las salidas del estado de `infra`): el script lo omite y avisa. Solo se ve completo después de aplicar `infra`.
- `database/` solo importa un clúster que ya existe; sin ese clúster en la suscripción de pruebas, su plan no es representativo.

### Apply real desde cero
- **Qué crearía:** red virtual y subred, entorno de Container Apps, Log Analytics, Application Insights, Key Vault con la asignación de rol de quien administra secretos, cuenta de almacenamiento con su contenedor privado, registro de contenedores (ACR, SKU Basic) y, en `apps`, el gateway, Identity, Messaging, Market y Redis como Container Apps.
- **Por qué cuesta:** las Container Apps, el ACR, el almacenamiento y los registros facturan por uso o por día aunque el entorno esté inactivo. El clúster gratuito de DocumentDB ya existe y solo se permite uno por suscripción, así que `database/` no se puede crear de nuevo en la misma suscripción.
- **Quién lo autoriza:** el líder técnico, porque gasta crédito de la suscripción. No debe correrlo un agente ni el pipeline.
- **Antes de correrlo:** imágenes publicadas en el ACR con la etiqueta que se pasa a `--image-tag`, los ocho secretos escritos a mano en el Key Vault (CA-04) y las etapas 1 a 4 sin fallas.
- **Cómo se revierte:** `terraform destroy` en `apps/` y luego en `infra/` (pasos 3 y 4); nunca en `database/`. Después de borrar un Key Vault, su nombre puede quedar reservado por la eliminación lógica, así que conviene probar con un sufijo propio.
- **Lo que no se puede saber sin crearlo:** si la creación desde cero tiene éxito. Las cuotas de la suscripción y los nombres ya usados solo se descubren al crear.

### Criterios para dar por cumplido KNOCK-34 con evidencias parciales
El criterio «el entorno se crea desde cero con un comando» se puede dar por cumplido sin apply real cuando se tienen estas evidencias y queda anotado que el apply desde cero no se ejecutó:
1. Etapas 1 y 2 sin fallas (estática y simulada).
2. Etapa 3 sin errores en `infra`, `apps` y `database`.
3. Etapa 4: el plan de solo lectura contra el entorno existente coincide con lo esperado, sin destrucciones.
4. Un solo comando basta una vez cubiertos los prerrequisitos manuales (grupo de recursos, secretos e imágenes), que el README documenta.

Si el líder técnico exige la prueba completa, la evidencia adicional es la etapa 6 en un entorno desechable, con el resultado y el `destroy` posterior anotados.
