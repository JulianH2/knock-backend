# TT-BD-03 — Semillas ficticias en JSON para MongoDB: clientes, técnicos y solicitudes de prueba

> **Jira:** KNOCK-40 · **Área:** Base de datos · **Servicio:** Identidad y confianza (clientes y técnicos, base `identity`) y Mercado (solicitudes, base `market`) (ADR-001) · **Módulos:** Identity, Requests (por confirmar, P3) · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** la ficha dice S1; `docs/PLAN_ADAPTACION.md` §D la mueve a **S2** · **Puntos:** 1 (ficha; ver §8, P9) · **Capas:** BD
> **Repositorio:** knock-backend · **Otra parte en:** — · **Rama:** `docs/KNOCK-157-KNOCK-40-specs` (spec, junto con TT-BD-07) → PR a `sprint-01`; la implementación va en su propia rama tras la aprobación (la ficha cita `chore/KNOCK-40-semillas-ficticias`)
> **Ficha:** `docs/backlog/TT-BD-03.md` (generada del adjunto `TT-BD-03_SemillasFicticiasClientesTecnicos_Knock.docx`; el `.docx` no está disponible, solo esta ficha). La ficha es anterior a ADR-002: pide `deploy/seeds/ficticios.sql` y depende de TT-BD-02; el plan de adaptación la cambia a «semillas en JSON para MongoDB».
> **Depende de:** TT-BD-06 (KNOCK-156, PR #73 abierto contra `sprint-02`): `db/seeds/catalog/colonias.json` y `oficios.json` con `_id` fijos y el patrón del cargador idempotente (`Modules/Catalog/Data/CatalogSeeder`) · HU-001 (KNOCK-43, Aprobada): modelo de `usuarios` en la base `identity` y cifrado del celular · HU-002, HU-017 y HU-024 (sin spec en el repositorio al redactar esta): perfil, técnico y solicitud · TT-BD-07 (KNOCK-157): compose local con `documentdb-local` · TT-CD-01 / TT-AZ-02: que exista Staging.
> **Fuentes:** ficha `docs/backlog/TT-BD-03.md` (pasos 1–4, V1–V2) · `docs/PLAN_ADAPTACION.md` §D · ADR-001 (regla 1) · ADR-002 (regla 2) · constitución 2.2.x (principios 3, 5 y 10; §5; §6 «Biométricos», «CURP solo como HMAC; dirección y celular cifrados», «No se usan datos personales reales») · spec HU-001 §5 · spec TT-BD-06 §3 y §5 · `db/seeds/README.md` y `db/seeds/catalog/*.json` de #73 · `docs/evidencia/KNOCK-28-firebase.md` (números de prueba)
> **Skills:** story-workflow, spec-first-writing, database-scripts, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Que Staging y las demos tengan **clientes, técnicos y solicitudes creíbles e inventados**, cargados desde JSON versionados en `db/seeds/` con el mismo formato que `db/seeds/catalog/*.json` (arreglo de documentos, `_id` fijos como `{"$oid": "…"}`, campos en camelCase), con un cargador repetible que no duplica y sin ningún dato de una persona real.

**Terminada cuando:** Staging tiene datos de demostración (criterio de Jira). En concreto: los JSON de demostración existen y pasan sus pruebas; el cargador, corrido dos veces sobre bases vacías, deja exactamente los conteos esperados con los mismos `_id`; cada técnico y solicitud referencia una colonia piloto y un oficio que existen en `colonias.json` y `oficios.json`; ningún documento tiene datos reales ni campos sensibles en claro; y el PR trae los conteos tras cargar en Staging.

**Qué pide la ficha y qué cambia con MongoDB:**

| Ficha (antes de ADR-002) | En esta spec |
|---|---|
| `deploy/seeds/ficticios.sql` o un comando de la API | JSON en `db/seeds/demo/` (ADR-002 regla 2: semillas versionadas en `db/`) y un cargador por servicio (P3). |
| 20 técnicos y 10 clientes en colonias piloto | Se conserva: 20 técnicos y 10 clientes, en colonias con `piloto: true` de `colonias.json`. |
| Solicitudes de prueba (sin número) | Número por confirmar (P5). Propuesta: 10, una por cliente. |
| Celulares con números de prueba de Firebase o con un prefijo inválido evidente | Rango ficticio evidente (P6). Los 4 números de prueba de Firebase (`+1 650 555 01xx`) no alcanzan para 30 personas y HU-001 solo acepta `+52`. |
| Nombres generados con Bogus | Nombres inventados escritos en el JSON y revisados por una persona; Bogus no se agrega salvo decisión (P7). |
| Correr el script varias veces sin duplicar | Upsert por `_id` fijo; probado (TC-TT-BD-03-05). |
| Depende de TT-BD-02 | Depende de TT-BD-06 (que reemplaza a TT-BD-02). |

## 2. Historia y casos de prueba
**TT-BD-03** — Tarea técnica. Las pruebas de carga usan `documentdb-local` con Testcontainers (`DocumentDbFixture` de TT-BD-06) y los propios JSON de demostración; ninguna usa una base real. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_BD_03_KK`. Las rutas y nombres de colecciones de esta tabla son la **propuesta** de §5 y se ajustan cuando el líder responda las preguntas de §8; los casos 09 y 10 se verifican con comandos (su salida va en el PR).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-03-01 | Dados los JSON de `db/seeds/demo/` (`identity/usuarios.json`, `identity/tecnicos.json`, `market/solicitudes.json`), cuando se leen, entonces cada uno es un arreglo JSON de documentos con el formato de `db/seeds/catalog/*.json`: `_id` como `{"$oid": "<24 hexadecimales>"}`, sin `_id` repetido entre **todos** los archivos de demostración ni con `colonias.json`/`oficios.json`, nombres de campo en camelCase, `version: 1` y `demo: true` en cada documento. | `DatabaseTest/Demo/DemoSeedFileTests.DemoFilesFollowCatalogFormatWithUniqueFixedIds_TC_TT_BD_03_01` |
| TC-TT-BD-03-02 | Dados los JSON de demostración, cuando se cuentan, entonces hay exactamente **10 clientes** y **20 técnicos** (ficha, paso 1) y el número de solicitudes acordado en P5 (propuesta: 10); cada técnico tiene un único perfil en `tecnicos.json` ligado a un usuario de `usuarios.json`, y ningún cliente tiene perfil de técnico. | `DatabaseTest/Demo/DemoSeedFileTests.DemoFilesHaveTenClientsTwentyTechniciansAndAgreedRequests_TC_TT_BD_03_02` |
| TC-TT-BD-03-03 | Dados los JSON de demostración y `db/seeds/catalog/{colonias,oficios}.json`, cuando se cruzan, entonces cada `coloniaId` de técnicos y solicitudes es el `_id` de una colonia con `piloto: true`, cada `oficioId` es el `_id` de un oficio con `activo: true`, cada técnico tiene al menos un oficio, cada solicitud referencia a un cliente de `usuarios.json` (por el identificador que fije P4) y ninguna referencia queda rota. | `DatabaseTest/Demo/DemoSeedFileTests.DemoReferencesPointToExistingPilotNeighborhoodsTradesAndClients_TC_TT_BD_03_03` |
| TC-TT-BD-03-04 | Dados los JSON de demostración, cuando se revisan, entonces **todos** los celulares cumplen el patrón ficticio acordado en P6 (propuesta: `^\+5200000\d{5}$`, que tiene el formato `+52` y 10 dígitos de HU-001 pero ninguna lada mexicana empieza con 0) y son únicos; ningún documento tiene campos de INE, selfie, CURP, dirección, correo, foto ni ubicación del dispositivo; y ningún texto libre (nombres, descripciones) contiene dígitos de un teléfono, un correo ni una URL. | `DatabaseTest/Demo/DemoSeedFileTests.DemoDataIsFictitiousAndHasNoSensitiveFields_TC_TT_BD_03_04` |
| TC-TT-BD-03-05 | Dadas bases `identity` y `market` vacías con el catálogo cargado, cuando el cargador de demostración se aplica, entonces las colecciones tienen exactamente los conteos del caso 02 con los mismos `_id` del JSON; y cuando se aplica **otra vez**, los conteos, los `_id` y `version` no cambian (ficha, V2). | `DatabaseTest/Demo/DemoSeedLoaderTests.ApplyingTwiceKeepsCountsIdsAndVersion_TC_TT_BD_03_05` |
| TC-TT-BD-03-06 | Dada la semilla aplicada, cuando se busca en la colección `usuarios` el texto de cada celular del JSON, entonces no aparece en claro en ningún campo; cada usuario tiene `celularCifrado` y `celularHash` calculados como en HU-001 (§5) con la llave del entorno, y descifrar `celularCifrado` con la llave de la prueba devuelve el celular ficticio del JSON. | `DatabaseTest/Demo/DemoSeedLoaderTests.PhonesAreStoredEncryptedAndHashedLikeHu001_TC_TT_BD_03_06` |
| TC-TT-BD-03-07 | Dado el cargador de demostración, cuando se ejecuta con el entorno `Production` o sin la marca de configuración que lo habilita (propuesta: `Seed:Demo = true`), entonces termina con error que nombra la causa sin valores de configuración y **no escribe** ningún documento (conteos en 0). | `DatabaseTest/Demo/DemoSeedLoaderTests.LoaderRefusesProductionOrMissingFlagAndWritesNothing_TC_TT_BD_03_07` |
| TC-TT-BD-03-08 | Dado el ensamblado de la API, cuando corren las pruebas de arquitectura, entonces el cargador de demostración de Identity solo usa la base `identity` y el de Mercado solo la base `market`; ninguno usa `IMongoCollection<T>` fuera de su `Data/` ni lee colecciones de otro módulo (ADR-001 regla 1; constitución, principio 10). | `UnitTest/Architecture/DemoSeedBoundaryTests.DemoLoadersOnlyTouchTheirOwnDatabase_TC_TT_BD_03_08` |
| TC-TT-BD-03-09 | Dado Staging con el catálogo cargado, cuando la persona responsable corre el cargador de demostración dos veces, entonces los conteos por colección de documentos con `demo: true` son los del caso 02 tras cada corrida; y, si la pantalla ya existe (P2), el mapa de prueba muestra técnicos (ficha, V1). | (comando) conteos tras cada carga y captura del mapa en el PR, sin cadenas ni credenciales |
| TC-TT-BD-03-10 | Dado el cambio terminado, cuando se compila y se revisa el formato, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings y `dotnet format Knock.slnx --verify-no-changes` no reporta cambios. | (comando) salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - JSON de demostración en `db/seeds/demo/<base>/` con el formato de `db/seeds/catalog/*.json`: 10 clientes y 20 técnicos (usuarios y perfiles de técnico, base `identity`) y las solicitudes acordadas (base `market`). Nombres inventados, celulares del rango ficticio de P6, colonias piloto y oficios por su `_id` fijo.
  - Un cargador por servicio (propuesta, P3), repetible: upsert por `_id`, valida referencias y formato **antes** de escribir (si algo falla no escribe nada), cifra y calcula el hash del celular como HU-001, se niega a correr en `Production` o sin la marca de configuración.
  - `db/seeds/README.md`: sección de demostración (qué contiene, cómo se carga, cómo se borra con `demo: true`).
  - Pruebas de los casos 01 a 08 y evidencia de 09 y 10.
- **Out:**
  - Datos de producción (ficha §4) y cualquier dato de una persona real.
  - Definir los modelos de perfil, técnico y solicitud: son de HU-002, HU-017 y HU-024. Esta tarea **sigue** esos modelos; si no están aprobados al implementar, ver P1.
  - Fotos (de perfil, de solicitudes o de INE/selfie), direcciones (HU-010), cotizaciones, mensajes, reseñas y verificación de identidad.
  - Cuentas de Firebase para los usuarios de demostración: con los datos sembrados no se puede iniciar sesión (P8).
  - Script `mongosh` de entrega (TT-BD-09) y respaldos (TT-BD-04).
  - Cambiar `colonias.json` u `oficios.json` (TT-BD-06; `_id` fijos que no se cambian).
  - Caché: el cargador no implementa `cache-aside` (TT-BD-08); si al implementar ya existe caché de lectura de esas colecciones, el cargador invalida sus claves (constitución, principio 10).
- **Relación con otras tareas:**
  - **TT-BD-06 (KNOCK-156, #73):** prerrequisito; aporta los `_id` de colonias y oficios y el patrón del cargador.
  - **HU-001 (KNOCK-43):** modelo de `usuarios` (`celularCifrado`, `celularHash`, `sub`, `estado`, `perfilCompleto`, `version`) y su cifrado.
  - **HU-002, HU-017, HU-024:** dueñas de los modelos de perfil, técnico y solicitud.
  - **TT-BD-07 (KNOCK-157):** compose local para cargar a mano.
  - **TT-CD-01 / TT-AZ-02:** Staging donde se cargan los datos (P2).
  - **TT-ML-01:** depende de esta tarea (ficha de TT-ML-01).

## 4. Contratos API y eventos
- **Sin endpoints, eventos ni cambios de OpenAPI.** El cargador no se expone por HTTP.
- **Configuración (propuesta, P3):**

| Clave | Valor | Nota |
|---|---|---|
| `Seed:Demo` | `true` solo donde se quiera cargar | Sin ella, o en `Production`, el cargador no escribe (TC-TT-BD-03-07). |
| Llave de cifrado y de HMAC del celular | secreto | La misma de HU-001 en ese entorno (Key Vault en Azure, user-secrets en local); nunca en el JSON ni en el repositorio. |

## 5. Modelo de datos
Formato de archivo igual a `db/seeds/catalog/*.json` (TT-BD-06): arreglo de documentos, `_id` como `{"$oid": "…"}` fijo, campos en camelCase, fechas en UTC ISO 8601. Cada documento lleva `demo: true` (para contarlos y borrarlos en Staging sin tocar otros datos) y `version: 1`. **Las formas de abajo son propuesta**: los campos marcados «por confirmar» dependen de specs que aún no existen (P1).

**`db/seeds/demo/identity/usuarios.json`** → colección `usuarios` (base `identity`, modelo de HU-001, `cache: no`)
```
{ _id, celular: "+5200000NNNNN" (solo en el JSON; el cargador lo cifra y lo descarta),
  sub (por confirmar, P4), estado: "activo", perfilCompleto: true, creadoEn, version: 1, demo: true }
```
- En la base queda lo de HU-001: `celularCifrado`, `celularCifradoVersion`, `celularHash`, `celularHashVersion`; **nunca** `celular` en claro (TC-TT-BD-03-06).
- Nombre visible del cliente o técnico: campo y colección por confirmar con HU-002 (P1).

**`db/seeds/demo/identity/tecnicos.json`** → colección por confirmar con HU-017 (propuesta `tecnicos`, `cache: 60` como perfil público, principio 10)
```
{ _id, usuarioId: ObjectId (de usuarios.json), nombre (inventado), oficios: [oficioId],
  modalidad: "domicilio" | "taller", coloniaId: ObjectId (piloto), radioKm (por confirmar),
  ubicacion?: GeoJSON Point (por confirmar, P2), version: 1, demo: true }
```

**`db/seeds/demo/market/solicitudes.json`** → colección por confirmar con HU-024 (propuesta `solicitudes`, `cache: no`)
```
{ _id, cliente (identificador por confirmar, P4), oficioId, coloniaId (piloto),
  descripcion (texto inventado, sin datos de contacto), estado (por confirmar con HU-024),
  creadaEn, version: 1, demo: true }
```
- Sin fotos, sin dirección (HU-010) y sin `Idempotency-Key` (no pasan por la API).

**Índices:** los define la historia dueña de cada colección; el cargador no crea índices nuevos salvo los que ya declare esa historia.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. El riesgo está en meter datos reales, en guardar datos personales en claro y en cargar demostración donde no toca.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** un dato de una persona real (nombre, celular, dirección, foto, INE, CURP) entra al repositorio público o a Staging. | Solo datos inventados; celulares del rango ficticio; sin campos sensibles; revisión humana de los nombres en el PR; constitución §6. | TC-TT-BD-03-04 |
| **Fuga (I):** el celular de demostración queda en claro en la base y una prueba o un log lo copia. | El cargador cifra y calcula el hash como HU-001 y descarta el valor en claro; no registra celulares. | TC-TT-BD-03-06 |
| **Fuga (I):** la llave de cifrado o la cadena de conexión de Staging aparece en el JSON, el PR o la evidencia. | Llaves y cadenas solo por Key Vault o user-secrets; la evidencia del caso 09 son conteos, sin cadenas. | TC-TT-BD-03-09 (revisión de la evidencia) |
| **Alteración (T):** se carga demostración en producción o se mezclan datos de demostración con datos reales. | Guarda de entorno y marca `Seed:Demo`; `demo: true` en cada documento. | TC-TT-BD-03-07 · TC-TT-BD-03-01 |
| **Alteración (T):** referencias rotas o duplicados tras correr el cargador varias veces. | `_id` fijos, upsert por `_id`, validación previa de referencias; si algo falla no se escribe nada. | TC-TT-BD-03-03 · TC-TT-BD-03-05 |
| **Elevación (E):** un cargador escribe en la base de otro servicio. | Un cargador por servicio con su base; prueba de arquitectura. | TC-TT-BD-03-08 |
| **Suplantación (S):** un usuario de demostración inicia sesión con un número que pertenece a alguien. | Rango sin ladas reales; sin cuentas de Firebase para la demostración (P8). | TC-TT-BD-03-04 |
| **IA (principio 5):** los datos de demostración se mandan a Azure OpenAI con celulares. | Las descripciones no tienen datos de contacto; los celulares nunca salen del módulo Identity. | TC-TT-BD-03-04 |
| **Repudio (R) / denegación (D):** no aplican: no hay endpoints ni acciones de usuario. | — | — |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Base de datos | Sí | Archivos, referencias, datos ficticios, carga repetible, cifrado y guarda de entorno con `documentdb-local` (Testcontainers). TC-TT-BD-03-01 a 07. `tests/DatabaseTest/Demo/`. |
| Arquitectura | Sí | Cada cargador solo en su base. TC-TT-BD-03-08. |
| Seguridad negativa | Sí, dentro de base de datos | Guarda de entorno y ausencia de datos sensibles (TC-TT-BD-03-04, 06 y 07). |
| Smoke en Staging | Sí, manual | Conteos y mapa de prueba (TC-TT-BD-03-09). |
| Contrato HTTP, eventos, UI, rendimiento, mutación | No | Sin endpoints ni lógica de negocio. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: build Release 0/0, `dotnet format`, Unit (con Architecture) y Database en verde con resultado real, `sdd_lint.py --trace TT-BD-03`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Límites conocidos:**
- Los modelos de perfil, técnico y solicitud no tienen spec; si cambian después de cargar, hay que regenerar los JSON y volver a cargar.
- Con `_id` fijos, un cambio de forma en los JSON necesita que el cargador suba `version` solo si cambia el contenido (como el de TT-BD-06).
- Los usuarios de demostración no pueden iniciar sesión (P8).

**Preguntas abiertas para el líder:**
- **P1 — Modelos sin spec.** HU-002 (perfil), HU-017 (técnico) y HU-024 (solicitud) no tienen spec aprobada. ¿Esta tarea espera a que existan, o se aprueba con la forma provisional de §5 y se concilia después? ¿Dónde vive el nombre del cliente y el del técnico?
- **P2 — Staging y mapa.** ¿Ya existe Staging con DocumentDB (TT-AZ-02 / TT-CD-01)? Si no, ¿«terminada» se cumple con la carga en el contenedor local y Staging queda para cuando exista? ¿Existe ya el «mapa de prueba» de V1 y qué punto usa cada técnico (centro de su colonia, un punto sintético dentro del polígono, o ninguno)?
- **P3 — Cómo se carga.** ¿Un comando de la API por servicio (necesario para cifrar el celular con la llave del entorno), un script en `db/scripts/`, o `mongosh` (TT-BD-09)? ¿Quién lo corre en Staging y con qué credencial?
- **P4 — Referencia al cliente desde Mercado.** HU-001 dice que los demás módulos solo conocen el `sub`. El `sub` sale del `uid` de Firebase (TT-API-12) y los usuarios de demostración no tienen cuenta de Firebase. ¿Se guarda un `uid` ficticio en el JSON para derivar el `sub`, o la solicitud referencia otro identificador?
- **P5 — Solicitudes.** ¿Cuántas, en qué estados y con qué fechas (relativas a la carga o fijas)? Propuesta: 10, una por cliente, en el estado inicial que defina HU-024.
- **P6 — Celulares.** ¿Se acepta el rango `+5200000NNNNN` (formato válido para HU-001, sin lada real) o se prefiere otro? Los 4 números de prueba de Firebase son `+1 650 555 01xx` y HU-001 solo acepta `+52`.
- **P7 — Bogus.** La ficha propone Bogus. ¿Se aceptan nombres escritos a mano en el JSON (sin dependencia nueva), o se evalúa Bogus para un generador de una sola vez en `db/scripts/`?
- **P8 — Inicio de sesión.** ¿Algún usuario de demostración debe poder entrar en la app (por ejemplo, para la demo de un técnico)? Si sí, ¿con qué número de prueba?
- **P9 — Puntos.** Con cifrado del celular, dos bases y cargador por servicio, 1 punto parece poco. ¿Se reestima o se recorta el alcance (por ejemplo, solo técnicos y clientes, y las solicitudes cuando exista HU-024)?

## Aprobación
- [ ] Revisor 1 — nombre: ____________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
