# TT-SEC-01 — Secretos en Key Vault con identidad administrada; ningún secreto en el repositorio

> **Jira:** KNOCK-36 · **Área:** SEC · Seguridad (DevSecOps) · **Épica:** EP-13 · **Responsable:** José Guillermo Rangel Hernández (reasignada por carga del Sprint 2, ADR-005) · **Sprint:** S2 (movida desde el Sprint 1: depende de ADR-001 y ADR-002) · **Puntos:** 2 · **Capas:** Back (configuración y documentación), CI
> **Repositorio:** knock-backend · **Otra parte en:** — · **Ficha:** `docs/backlog/TT-SEC-01.md` (generada del `.docx` original; la ficha está desfasada: ver §1) · **Rama:** `docs/KNOCK-36-secrets-keyvault-spec` (solo esta spec) → PR a `sprint-02` (la ficha decía `sprint-01`); la implementación va en su propia rama, también a `sprint-02`, después de la aprobación
> **Depende de:** TT-AZ-05 (KNOCK-158, estado remoto) y TT-AZ-02 (KNOCK-34, Terraform: crea el Key Vault, las identidades administradas y sus roles); las dos specs ya están fusionadas en `sprint-02` y su implementación la hace el Tech Lead. **Relacionadas:** HU-001 (KNOCK-43, #75: llaves del celular), TT-BD-06 (cadena de MongoDB), TT-API-02 (clave del JWT), `Media` (cadena de Blob).
> **Fuentes:** ficha de Jira y `docs/backlog/TT-SEC-01.md` · constitución 2.2.x (principio 3; §3 «Acceso»; §6 «Seguridad y privacidad»; §12 «Redis»; §13 «IaC») · ADR-002 (regla 5: «los secretos de conexión viven en Key Vault») y ADR-003 · specs TT-AZ-02 (casos 06, 07 y 10; decisiones 6 y 12) y TT-AZ-05 · `docs/evidencia/KNOCK-27-azure.md` · lectura de `JwtSettingsFactory.cs`, `MongoExtensions.cs`, `appsettings*.json`, `.gitleaks.toml`, `ci-pr.yml` y `tests/SecurityTest/Secrets/`
> **Skills:** story-workflow, security-core, testing-strategy-backend, foundation-backend
> **Estado:** Borrador

## 1. Objetivo
Que ningún secreto viva en el código, en `appsettings*.json`, en Terraform ni en el historial del repositorio (público), y que en Azure la API reciba sus secretos desde **Key Vault con identidad administrada**, sin contraseñas ni claves pegadas en variables de despliegue. En local se usan `dotnet user-secrets`. El CI verifica con gitleaks que no entre ninguno.

**Terminada cuando** (ficha): no hay secretos en el repositorio y el CI lo verifica.

**La ficha de Jira está desfasada y esta spec la corrige** (leído en `sprint-02` el 2026-11-03):

| La ficha dice | Hoy |
|---|---|
| Guardar «cadena de PostgreSQL» | PostgreSQL se retiró (ADR-002): es `ConnectionStrings:KnockMongo` y, para `Media`, `ConnectionStrings:KnockBlob`. |
| Guardar «cuenta de servicio de Firebase» y «clave del hash de la CURP» | No existen en el código: la API solo valida el ID token con `Firebase:ProjectId` (no es secreto) y no hay hash de CURP. Se reabren cuando existan FCM o la verificación de identidad. En su lugar entran las **llaves del celular** de HU-001. |
| «Agregar el job gitleaks y un `.gitleaks.toml`» | Ya existen (KNOCK-26): el job «Secretos (Gitleaks)» de `ci-pr.yml` y `.gitleaks.toml` con reglas propias (cadena de conexión, cuenta de servicio de Firebase, llave de Azure Storage y, desde #101, cadenas de MongoDB). |
| Paquete `Azure.Extensions.AspNetCore.Configuration.Secrets` en la API | Es un paquete nuevo (y con `Azure.Identity`, ambos sin evaluar); hay una alternativa sin paquetes ni código: ver duda 1. |
| Rama a `sprint-01` | `sprint-02`. |

**Qué ya existe:**

| Pieza | Hoy |
|---|---|
| Lectura de secretos | Todo se lee de `IConfiguration`, así que llega igual por user-secrets (Development, `UserSecretsId` ya definido en `Knock.Api.csproj`), por variable de entorno (`Jwt__SigningKey`, `ConnectionStrings__KnockMongo`) o por cualquier otro proveedor. `JwtSettingsFactory` exige `Jwt:SigningKey` (Base64, 32 bytes o más) fuera de Development; `MongoExtensions` valida la cadena al arrancar; el comentario de `MongoExtensions` ya dice «en Azure, Key Vault (TT-SEC-01)». |
| Pruebas de secretos | `SecurityTest/Secrets/AppSettingsSecretsTests` (ningún secreto en `appsettings*.json`), `JwtKeyConfigurationTests` y `SecurityTest/Hosting/KnockMongoSecretTests`. |
| gitleaks | Job del CI con `.gitleaks.toml`; el escaneo recorre los commits de cada PR. |
| Key Vault, identidades y roles | **No existen:** los crea la implementación de TT-AZ-02 (autorización por roles, protección contra purga, sufijo aleatorio en el nombre; el rol de escritura a nivel de bóveda para quien carga los secretos y el de lectura para cada aplicación). |

## 2. Historia y casos de prueba
**TT-SEC-01** — Tarea técnica (no es historia de usuario). Los casos 01 a 04 se verifican con pruebas de código; el 05 a 08 y el 09, con comandos y evidencia (la salida va en el PR; los comandos de Azure los ejecuta una persona, los agentes no operan Azure). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_SEC_01_KK`. Solo valores ficticios, construidos en ejecución: ningún literal con forma de secreto en el código de pruebas (aprendizaje de #89).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-SEC-01-01 | Dados todos los `appsettings*.json`, `.env.example` y los `*.tfvars` y `*.tf` versionados, cuando se leen, entonces ninguno contiene valor alguno para las claves del inventario de §5 (`Jwt:SigningKey`, `ConnectionStrings:KnockMongo`, `ConnectionStrings:KnockBlob`, `Identity:PhoneProtection:EncryptionKeys:*`, `Identity:PhoneProtection:HmacKeys:*`); `.env.example` las lista **vacías** y el archivo `.env` está en `.gitignore`. Los nombres de versión (`EncryptionKeyVersion` y `HmacKeyVersion`) sí pueden estar: no son secretos. | `SecurityTest/Secrets/SecretInventoryTests.NoInventoryKeyHasAValueInRepositoryFiles_TC_TT_SEC_01_01` (Theory por archivo) |
| TC-TT-SEC-01-02 | Dado el entorno Production sin ninguno de los secretos del inventario, cuando se arranca el host, entonces **no arranca** y el mensaje nombra la clave que falta (`Jwt:SigningKey`, `ConnectionStrings:KnockMongo`, y las llaves del celular cuando HU-001 esté fusionada) sin repetir ningún valor; con todos presentes (generados en ejecución) arranca. En Development sin secretos no se exige nada que no se exija hoy. | `SecurityTest/Secrets/SecretsFailClosedTests.ProductionWithoutAnySecretFailsNamingTheKey_TC_TT_SEC_01_02` (Theory por clave) |
| TC-TT-SEC-01-03 | Dado `docs/SECRETOS.md`, cuando se lee, entonces tiene una fila por cada clave del inventario con su nombre en Key Vault, su clave de configuración (`ConnectionStrings__KnockMongo`…), quién la carga y cómo se rota; las secciones «Local con user-secrets», «Rotación» y «Si se filtra un secreto»; y los comandos de ejemplo usan marcadores (`<valor>`), nunca un valor; y el README y `CONTRIBUTING.md` enlazan a ese documento. | `UnitTest/Architecture/SecretsDocumentationTests.InventoryMatchesTheCodeKeysAndHasNoValues_TC_TT_SEC_01_03` |
| TC-TT-SEC-01-04 | Dados los lectores de secretos de la API, cuando se inspecciona el código por reflexión y por texto, entonces las claves del inventario se leen **solo** de `IConfiguration` (sin `Environment.GetEnvironmentVariable` ni lecturas directas de archivos), de modo que el origen (user-secrets, variable de Container Apps con referencia a Key Vault o un proveedor de Key Vault) puede cambiar sin tocar el código. | `UnitTest/Architecture/SecretsDocumentationTests.SecretsAreReadOnlyFromConfiguration_TC_TT_SEC_01_04` |
| TC-TT-SEC-01-05 | Dado el job «Secretos (Gitleaks)» del CI y la `.gitleaks.toml`, cuando un commit agrega una clave falsa con forma de secreto (de cada una de las reglas propias: cadena de conexión, cuenta de servicio de Firebase, llave de Azure Storage y cadena de MongoDB), entonces gitleaks la detecta y el job falla; y con las cadenas con marcadores (`<usuario>`, `${VARIABLE}`) no falla. Es el criterio V2 de la ficha. | (comando) corrida de la imagen oficial de gitleaks con la `.gitleaks.toml` sobre una muestra (salida en el PR) y captura del job fallando en un PR de prueba en borrador que no se fusiona (ver duda 4) |
| TC-TT-SEC-01-06 | Dado el repositorio completo, cuando se corre gitleaks sobre **todo el historial** (`--log-opts "--all"`) con la `.gitleaks.toml`, entonces no hay hallazgos o cada uno está resuelto: rotado el secreto (si era real) o registrado en `security/exceptions.yaml` con huella y justificación (como EXC-004 y EXC-005). | (comando) salida de gitleaks con los valores ocultos (`--redact`), en el PR |
| TC-TT-SEC-01-07 | Dado el entorno de Azure creado por TT-AZ-02, cuando la API desplegada arranca, entonces cada aplicación recibe los secretos de su inventario desde **Key Vault con su identidad administrada** (cada referencia aparece en `az containerapp show` con la URL del secreto en Key Vault y **sin el valor**), el estado de `/health/ready` es correcto y los logs de arranque no contienen ningún valor de secreto. Es el criterio V1 de la ficha. | (comando) `az containerapp show --query` de las referencias (sin valores), `curl` a `/health/ready` y revisión de los logs de arranque; la ejecuta una persona |
| TC-TT-SEC-01-08 | Dado el procedimiento de rotación de `docs/SECRETOS.md`, cuando se ensaya en el entorno de desarrollo con valores ficticios (una nueva versión de `jwt-signing-key` y la adición de la versión 2 de las llaves del celular), entonces la aplicación vuelve a arrancar con el valor nuevo, los access tokens firmados con la clave anterior dejan de valer, y los valores cifrados con la versión 1 de las llaves del celular siguen siendo legibles. | (comando) capturas del ensayo, sin valores; la ejecuta una persona |
| TC-TT-SEC-01-09 | Dado el trabajo terminado, cuando se compila y se revisa gobierno, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings, `dotnet format Knock.slnx --verify-no-changes` no reporta cambios, `python scripts/sdd_lint.py --trace TT-SEC-01`, `python scripts/codemap.py --check`, `python scripts/ci/check_exceptions.py` y `python scripts/sync_agents.py --check` terminan en OK, no se agregó ningún paquete (`git diff` de `*.csproj` y `packages.lock.json` vacío, salvo que se elija la opción B de la duda 1) y `gitleaks` queda limpio. | (comando) salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - **`docs/SECRETOS.md` (nuevo):** inventario de secretos (§5), de dónde viene cada uno en local y en Azure, quién lo carga, cómo se rota y qué hacer si se filtra (rotar de inmediato, la ficha §8); y un enlace desde el README y desde `CONTRIBUTING.md` con el uso de `dotnet user-secrets` en local, con marcadores y sin valores.
  - **Contrato de nombres con TT-AZ-02:** el nombre de cada secreto en Key Vault y la clave de configuración a la que llega (variable de entorno con `__`), para que el Terraform de TT-AZ-02 y la API hablen igual. Los módulos de Terraform los escribe TT-AZ-02; esta tarea fija el inventario.
  - **Pruebas** de los casos 01 a 04 en `tests/SecurityTest/Secrets/` y `tests/UnitTest/Architecture/`.
  - **Evidencia** de los casos 05 y 06 (gitleaks sobre una muestra y sobre el historial completo) y, con una persona, de los casos 07 y 08.
  - **Código solo si hace falta:** con la opción A de la duda 1 no hay cambios en `src/`. Con la opción B, el registro del proveedor de Key Vault en `Hosting/` solo fuera de Development y los dos paquetes con su evaluación en `Memoria.md`.
  - `Memoria.md`.
- **Out:**
  - Crear el Key Vault, las identidades, los roles y las referencias en Container Apps (TT-AZ-02) y el estado remoto (TT-AZ-05).
  - **Cargar los valores en Key Vault y rotarlos en Azure:** lo hace una persona con el rol de escritura de la bóveda; esta tarea solo documenta el procedimiento. Los agentes no operan Azure ni ven valores.
  - La cuenta de servicio de Firebase (FCM) y la clave del hash de la CURP: no existen todavía; se agregan al inventario cuando existan.
  - CodeQL y el escaneo de dependencias (TT-SEC-02), y el secreto del proveedor de la pre-revisión de IA (`AI_REVIEW_API_KEY`, secreto de GitHub, no de Azure).
  - Cambiar la blob a identidad administrada con SAS de delegación de usuario (necesita `Azure.Identity` y cambios en `Media`): tarea aparte (decisión (a) de TT-AZ-02); mientras tanto la cuenta de la aplicación conserva su llave y la cadena de Blob es un secreto del inventario.
  - Algoritmo asimétrico para el JWT (constitución §6: riesgo aceptado, se reevalúa después del 2-dic).
  - `.github/` y `.gitleaks.toml` (archivos protegidos: si hace falta una regla nueva, la agrega el Tech Lead).

## 4. Contratos API y eventos
Sin endpoints, eventos ni cambios de OpenAPI: no cambia `docs/openapi.v1.json`, `docs/endpoints.md` ni `docs/events.md`.

## 5. Modelo de datos
Sin colecciones ni índices. **Inventario de secretos** (los nombres de Key Vault no admiten `:` ni `_`; se usa `-`, y la aplicación los recibe como variable con `__`):

| Secreto en Key Vault | Clave de configuración | Quién lo usa | Origen en local | Notas |
|---|---|---|---|---|
| `mongo-connection` | `ConnectionStrings:KnockMongo` | todos los servicios con base | `dotnet user-secrets` (o `.env` local ignorado) | La cadena de Azure DocumentDB; en local la del contenedor `documentdb-local`. |
| `blob-connection` | `ConnectionStrings:KnockBlob` | `Media` | `dotnet user-secrets` (Azurite) | Lleva la llave de la cuenta: se conserva hasta la tarea con `Azure.Identity` (TT-AZ-02, decisión (a)); rotarla invalida los SAS vigentes. |
| `jwt-signing-key` | `Jwt:SigningKey` | gateway y cada servicio | `dotnet user-secrets` (en Development hay una clave efímera) | Base64, 32 bytes o más; HS256; el mismo valor en el gateway y los servicios (constitución §6). |
| `phone-encryption-key-<v>` | `Identity:PhoneProtection:EncryptionKeys:<v>` | Identidad (HU-001) | `dotnet user-secrets` | AES-256 (32 bytes), versionada; la versión vigente va en `Identity:PhoneProtection:EncryptionKeyVersion` (no secreta). |
| `phone-hmac-key-<v>` | `Identity:PhoneProtection:HmacKeys:<v>` | Identidad (HU-001) | `dotnet user-secrets` | HMAC-SHA256 (32 bytes o más), distinta de la de cifrado; versión vigente en `HmacKeyVersion`. |
| `redis-password` | (la lee el contenedor de Redis) | Redis (TT-AZ-02, caso 06) | `.env` local ignorado | La **carga una persona** en Key Vault y Terraform solo la referencia (`key_vault_secret_id`); nunca `random_password`. La conexión de los servicios a Redis se agrega con la tarea que lo use. |

## 6. Seguridad, privacidad y amenazas
STRIDE breve. El riesgo es de fuga de información (I) y de elevación (E): quien tenga la clave del JWT puede emitir sesiones; quien tenga las llaves del celular puede leer los celulares cifrados.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** un secreto (cadena de conexión, clave del JWT, llave del celular) queda en el repositorio público, en `appsettings`, en Terraform o en un `.env.example`. | Ningún valor en los archivos versionados; gitleaks con reglas propias en cada PR; escaneo del historial completo; `.env` ignorado. | TC-TT-SEC-01-01 · TC-TT-SEC-01-05 · TC-TT-SEC-01-06 |
| **Fuga (I):** un secreto sale en los logs de arranque o en un mensaje de error. | Los errores de configuración nombran la clave y nunca el valor; los logs de arranque se revisan en el despliegue. | TC-TT-SEC-01-02 · TC-TT-SEC-01-07 |
| **Elevación (E):** la aplicación arranca sin una clave obligatoria y firma o cifra con un valor por defecto. | Falla cerrada fuera de Development: no hay valores por defecto en código. | TC-TT-SEC-01-02 |
| **Suplantación (S) / Elevación (E):** un secreto vive en una variable de despliegue en claro o lo ve quien no debe. | Referencias a Key Vault con la identidad administrada de cada aplicación (rol de solo lectura de secretos) y rol de escritura solo para quien los carga, a nivel de la bóveda (TT-AZ-02, caso 07). | TC-TT-SEC-01-07 |
| **Alteración (T):** una lectura de secretos fuera de `IConfiguration` impide cambiar el origen o deja valores en variables que otros procesos leen. | Los secretos se leen solo de `IConfiguration`. | TC-TT-SEC-01-04 |
| **Repudio (R) / continuidad:** un secreto filtrado no se rota o la rotación rompe la API. | Procedimiento de rotación y de respuesta a una filtración documentado y ensayado; las llaves del celular son versionadas, así que añadir una versión no invalida lo cifrado. | TC-TT-SEC-01-03 · TC-TT-SEC-01-08 |
| **Fuga (I) a la IA:** pegar un secreto en un prompt, un PR o un chat. | Regla de la ficha §8 y de `AGENTS.md` §4 (nunca pasar secretos a la IA); los comandos del documento usan marcadores; el `security-reviewer` lo revisa. | TC-TT-SEC-01-03 |
| **Denegación (D):** no aplica: sin endpoints nuevos. | — | — |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias / arquitectura | Sí | Casos 03 y 04 en `tests/UnitTest/Architecture/` (el documento y el origen de lectura). |
| Seguridad negativa | Sí | Casos 01 y 02 en `tests/SecurityTest/Secrets/`; usan `KnockApiFactory` o lectura de archivos, sin Testcontainers. |
| Integración, contrato, base de datos, UI, rendimiento | No | Sin endpoints ni persistencia nuevos. |
| Comandos y evidencia | Sí | Casos 05 a 09 (gitleaks sobre muestra e historial, despliegue con una persona, ensayo de rotación, gates). |
| Mutación | No | Sin meta para configuración; los casos 01 y 02 se prueban quitando una barrera a mano (revertida), con evidencia en el PR. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit (con Architecture), Integration, Security y Database en verde con resultado real, `sdd_lint.py --trace TT-SEC-01`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, gitleaks, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Límites conocidos:**
- Con la opción A de la duda 1, una rotación en Key Vault llega a la aplicación al crear una revisión nueva o reiniciarla (por confirmar el comportamiento exacto de Container Apps en la documentación vigente); la opción B puede recargar sin reiniciar.
- La clave del JWT es simétrica (HS256) y la comparte el gateway con cada servicio: quien llegue a un servicio y a su secreto podría emitir tokens (riesgo aceptado en la constitución §6).
- La cuenta de la aplicación conserva su llave compartida para Blob hasta la tarea con `Azure.Identity` (decisión de TT-AZ-02).
- El repositorio es público: una filtración no se puede «deshacer» borrando el commit; la respuesta es rotar.

**Dudas del líder** (con recomendación; no se resuelven inventando):
1. **Cómo llegan los secretos a la API en Azure.** (A) **Referencias de Key Vault en Container Apps** (el recurso de Terraform de cada aplicación declara el secreto con `key_vault_secret_id` y la identidad de la aplicación, y lo expone como variable de entorno): sin paquetes, sin cambios en `src/`, y es lo que TT-AZ-02 ya pide para Redis; (B) **proveedor de configuración de Key Vault en la API** (`Azure.Extensions.AspNetCore.Configuration.Secrets` y `Azure.Identity`, como dice la ficha): permite recarga sin reiniciar, pero agrega dos paquetes que hay que evaluar (licencia, mantenimiento, OSV, dependencias transitivas), código en `Hosting/` y un modo distinto en local y en Azure. *Recomendación:* A. La ficha decía B porque se escribió antes de decidir Container Apps y Terraform; la meta («la API lee Key Vault con identidad administrada») se cumple con A, con la identidad de la aplicación leyendo la bóveda.
2. **Quién carga los secretos y cuándo.** *Recomendación:* una persona con el rol de escritura de la bóveda (el Tech Lead) los carga con `az keyvault secret set` leyendo de un archivo o de la entrada estándar (nunca como argumento, para que no queden en el historial de la terminal), generados con una fuente aleatoria (por ejemplo `openssl rand -base64 32`), y registra solo los nombres y las versiones en la evidencia.
3. **Rotación de la clave del JWT.** Rotarla invalida los access tokens vigentes (hasta 15 minutos); los refresh tokens guardan solo su huella en MongoDB y no dependen de la clave, así que la sesión se renueva sola. *Recomendación:* rotarla solo ante una filtración y antes de la ventana de evaluación, sin calendario fijo; el ensayo del caso 08 la prueba una vez.
4. **Evidencia del criterio V2 en un repositorio público.** La ficha pide la captura del job fallando con una clave falsa. Eso exige un commit con un literal con forma de secreto en un PR de prueba (en borrador, que no se fusiona y se cierra). *Recomendación:* corrida local de la imagen oficial de gitleaks con la `.gitleaks.toml` (salida en el PR) y, además, el PR de prueba con una clave inventada por quien lo hace; si el líder prefiere no dejar ese literal en el historial de PR, solo la corrida local.
5. **Puntos.** La ficha dice 2. Con la opción A caben; con la B (dos paquetes con evaluación y código) conviene reestimar.
6. **Orden con HU-001.** Las llaves del celular llegan con #75. El caso 02 las exige solo cuando #75 esté fusionada, y el caso 01 usa la lista completa del inventario desde ya. *Recomendación:* aceptar ese orden.

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
