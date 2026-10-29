# TT-BD-03 — Semillas ficticias en JSON para MongoDB: clientes de prueba

> **Jira:** KNOCK-40 · **Área:** Base de datos · **Servicio:** Identidad y confianza (clientes, base `identity`) (ADR-001) · **Módulo:** Identity · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** la ficha dice S1; `docs/PLAN_ADAPTACION.md` §D la mueve a **S2** · **Puntos:** 1 (se mantiene con el alcance recortado, decisión D1) · **Capas:** BD
> **Repositorio:** knock-backend · **Otra parte en:** — · **Rama:** `docs/KNOCK-40-fake-demo-seeds-spec` (spec) → PR a `sprint-02`; la implementación va en su propia rama tras la aprobación (la ficha cita `chore/KNOCK-40-semillas-ficticias`)
> **Ficha:** `docs/backlog/TT-BD-03.md` (generada del adjunto `TT-BD-03_SemillasFicticiasClientesTecnicos_Knock.docx`; el `.docx` no está disponible, solo esta ficha). La ficha es anterior a ADR-002: pide `deploy/seeds/ficticios.sql` y depende de TT-BD-02; el plan de adaptación la cambia a «semillas en JSON para MongoDB».
> **Depende de:** TT-BD-06 (KNOCK-156, PR #73 abierto contra `sprint-02`): `db/seeds/catalog/*.json` con `_id` fijos y el patrón del cargador idempotente (`Modules/Catalog/Data/CatalogSeeder`) · **HU-001 (KNOCK-43): la implementación, no solo la spec** — modelo de `usuarios` en la base `identity` y el cifrado y hash del celular viven en el PR #75, **aún sin fusionar**; sin ese código el cargador no puede cifrar el celular (decisión D3) · TT-API-12: `UserIdDerivation.Derive(uid)` para el `sub` (decisión D4) · TT-BD-07 (KNOCK-157): compose local con el contenedor `documentdb-local`.
> **Fuentes:** ficha `docs/backlog/TT-BD-03.md` (pasos 1–4, V1–V2) · `docs/PLAN_ADAPTACION.md` §D · ADR-001 (regla 1) · ADR-002 (regla 2) · constitución 2.2.x (principios 3, 5 y 10; §5; §6 «Biométricos», «CURP solo como HMAC; dirección y celular cifrados», «No se usan datos personales reales») · spec HU-001 §5 · spec TT-API-12 · spec TT-BD-06 §3 y §5 · `db/seeds/README.md` y `db/seeds/catalog/*.json` de #73 · respuestas del líder (2026-10-28, §8 «Decisiones»)
> **Skills:** story-workflow, spec-first-writing, database-scripts, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Que el entorno local y las demos tengan **clientes creíbles e inventados**, cargados desde un JSON versionado en `db/seeds/demo/` con el mismo formato que `db/seeds/catalog/*.json` (arreglo de documentos, `_id` fijos como `{"$oid": "…"}`, campos en camelCase), con un cargador repetible dentro de la API que no duplica, cifra el celular como HU-001, se niega a correr donde no toca y no contiene ningún dato de una persona real. Técnicos y solicitudes quedan **fuera de alcance** hasta que existan las specs de HU-017 y HU-024 (decisión D1).

**Terminada cuando:** no hay Staging (se despliega desde `main`, decisión D2), así que se cumple **en el contenedor local `documentdb-local`** (compose de TT-BD-07): el JSON de clientes existe y pasa sus pruebas; el cargador, corrido dos veces sobre una base `identity` vacía en `documentdb-local`, deja exactamente 10 documentos con `demo: true` y los mismos `_id` tras cada corrida; ningún celular queda en claro; y el PR trae la salida de los comandos de conteo (`countDocuments({ demo: true })` tras cada corrida), sin cadenas de conexión ni credenciales.

**Qué pide la ficha y qué cambia con MongoDB:**

| Ficha (antes de ADR-002) | En esta spec |
|---|---|
| `deploy/seeds/ficticios.sql` o un comando de la API | JSON en `db/seeds/demo/identity/` (ADR-002 regla 2: semillas versionadas en `db/`) y un cargador por servicio dentro de la API (D3). |
| 20 técnicos y 10 clientes en colonias piloto | 10 clientes. Los 20 técnicos quedan como trabajo futuro, cuando exista la spec de HU-017 (D1). |
| Solicitudes de prueba (sin número) | Trabajo futuro: 10, una por cliente, cuando exista la spec de HU-024 (D5). |
| Celulares con números de prueba de Firebase o con un prefijo inválido evidente | Rango ficticio `+5200000NNNNN` (D6). |
| Nombres generados con Bogus | Nombres escritos a mano en el JSON, sin Bogus (D7). |
| Correr el script varias veces sin duplicar | Upsert por `_id` fijo; probado (TC-TT-BD-03-05). |
| Depende de TT-BD-02 | Depende de TT-BD-06 (que reemplaza a TT-BD-02). |
| Staging con datos de demostración | Sin Staging: contenedor local `documentdb-local` y comandos de conteo (D2). |

## 2. Historia y casos de prueba
**TT-BD-03** — Tarea técnica. Las pruebas de carga usan `documentdb-local` con Testcontainers (`DocumentDbFixture` de TT-BD-06) y el propio JSON de demostración; ninguna usa una base real. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_BD_03_KK`. Los casos 09 y 10 se verifican con comandos (su salida va en el PR).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-03-01 | Dado `db/seeds/demo/identity/usuarios.json`, cuando se lee, entonces es un arreglo JSON de documentos con el formato de `db/seeds/catalog/*.json`: `_id` como `{"$oid": "<24 hexadecimales>"}`, sin `_id` repetido dentro del archivo ni con `db/seeds/catalog/*.json`, nombres de campo en camelCase, `version: 1` y `demo: true` en cada documento. | `DatabaseTest/Demo/DemoSeedFileTests.DemoFileFollowsCatalogFormatWithUniqueFixedIds_TC_TT_BD_03_01` |
| TC-TT-BD-03-02 | Dado el JSON de demostración, cuando se cuenta, entonces hay exactamente **10 clientes** (ficha, paso 1); cada uno tiene un `uid` ficticio único con el patrón `^demo-cliente-\d{2}$` (que no corresponde a ninguna cuenta de Firebase) y un `nombre` no vacío; y no hay documentos de técnicos ni de solicitudes. | `DatabaseTest/Demo/DemoSeedFileTests.DemoFileHasTenClientsWithUniqueFictitiousUids_TC_TT_BD_03_02` |
| TC-TT-BD-03-03 | Dado el JSON de demostración, cuando se revisa, entonces **todos** los celulares cumplen `^\+5200000\d{5}$` (formato `+52` y 10 dígitos de HU-001, pero ninguna lada mexicana empieza con 0) y son únicos; ningún documento tiene campos de INE, selfie, CURP, dirección, correo, foto ni ubicación; y ningún texto libre (nombres) contiene dígitos de un teléfono, un correo ni una URL. | `DatabaseTest/Demo/DemoSeedFileTests.DemoDataIsFictitiousAndHasNoSensitiveFields_TC_TT_BD_03_03` |
| TC-TT-BD-03-04 | Dada la semilla aplicada, cuando se lee cada usuario, entonces su `sub` es `UserIdDerivation.Derive(uid)` (TT-API-12) del `uid` ficticio del JSON, el mismo valor que daría un inicio de sesión con ese `uid`, y no contiene el `uid`. | `DatabaseTest/Demo/DemoSeedLoaderTests.SubIsDerivedFromTheFictitiousUidLikeTtApi12_TC_TT_BD_03_04` |
| TC-TT-BD-03-05 | Dada una base `identity` vacía, cuando el cargador de demostración se aplica, entonces `usuarios` tiene exactamente 10 documentos con `demo: true` y los mismos `_id` del JSON; y cuando se aplica **otra vez**, el conteo, los `_id` y `version` no cambian (ficha, V2). | `DatabaseTest/Demo/DemoSeedLoaderTests.ApplyingTwiceKeepsCountsIdsAndVersion_TC_TT_BD_03_05` |
| TC-TT-BD-03-06 | Dada la semilla aplicada, cuando se busca en la colección `usuarios` el texto de cada celular del JSON, entonces no aparece en claro en ningún campo; cada usuario tiene `celularCifrado` y `celularHash` calculados con el código de HU-001 (PR #75) y la llave del entorno, y descifrar `celularCifrado` con la llave de la prueba devuelve el celular ficticio del JSON. | `DatabaseTest/Demo/DemoSeedLoaderTests.PhonesAreStoredEncryptedAndHashedLikeHu001_TC_TT_BD_03_06` |
| TC-TT-BD-03-07 | Dado el cargador de demostración, cuando se ejecuta con el entorno `Production` o sin la marca de configuración `Seed:Demo = true`, entonces termina con error que nombra la causa sin valores de configuración y **no escribe** ningún documento (conteo en 0). | `DatabaseTest/Demo/DemoSeedLoaderTests.LoaderRefusesProductionOrMissingFlagAndWritesNothing_TC_TT_BD_03_07` |
| TC-TT-BD-03-08 | Dado el ensamblado de la API, cuando corren las pruebas de arquitectura, entonces el cargador de demostración de Identity vive en `Modules/Identity/Data/`, solo usa la base `identity` y no lee colecciones de otro módulo (ADR-001 regla 1; constitución, principio 10). | `UnitTest/Architecture/DemoSeedBoundaryTests.IdentityDemoLoaderOnlyTouchesItsOwnDatabase_TC_TT_BD_03_08` |
| TC-TT-BD-03-09 | Dado el contenedor local `documentdb-local` (compose de TT-BD-07) con la base `identity` vacía, cuando la persona responsable corre el cargador de demostración dos veces, entonces `countDocuments({ demo: true })` en `usuarios` da 10 tras cada corrida (ficha, V1–V2 adaptadas sin Staging). | (comando) salida de los conteos tras cada carga en el PR, sin cadenas ni credenciales |
| TC-TT-BD-03-10 | Dado el cambio terminado, cuando se compila y se revisa el formato, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings y `dotnet format Knock.slnx --verify-no-changes` no reporta cambios. | (comando) salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `db/seeds/demo/identity/usuarios.json` con el formato de `db/seeds/catalog/*.json`: 10 clientes con nombres escritos a mano, `uid` ficticio y celulares del rango `+5200000NNNNN`.
  - Un cargador de demostración del servicio de Identidad **dentro de la API** (`Modules/Identity/Data/`, donde viven las llaves de cifrado), repetible: upsert por `_id`, valida formato **antes** de escribir (si algo falla no escribe nada), deriva el `sub` con `UserIdDerivation.Derive(uid)`, cifra y calcula el hash del celular con el código de HU-001, y se niega a correr en `Production` o sin `Seed:Demo = true`.
  - `db/seeds/README.md`: sección de demostración (qué contiene, cómo se carga en `documentdb-local`, cómo se cuenta y se borra con `demo: true`).
  - Pruebas de los casos 01 a 08 y evidencia de 09 y 10.
- **Out:**
  - **Técnicos (20, ficha paso 1):** fuera de alcance hasta que exista la spec de HU-017 (D1). Trabajo futuro.
  - **Solicitudes:** fuera de alcance hasta que exista la spec de HU-024 (D1). Trabajo futuro: 10, una por cliente (D5).
  - Mapa de prueba con técnicos (ficha, V1): depende de los técnicos; queda con ellos.
  - Staging: no existe (D2).
  - Datos de producción (ficha §4) y cualquier dato de una persona real.
  - Cuentas de Firebase para los usuarios de demostración: no inician sesión (D8).
  - Bogus u otra dependencia nueva (D7).
  - Fotos, direcciones (HU-010), INE/selfie y verificación de identidad.
  - Script `mongosh` de entrega (TT-BD-09) y respaldos (TT-BD-04).
  - Cambiar `db/seeds/catalog/*.json` (TT-BD-06; `_id` fijos que no se cambian).
  - Caché: el cargador no implementa `cache-aside` (TT-BD-08); `usuarios` es `cache: no`.
- **Relación con otras tareas:**
  - **TT-BD-06 (KNOCK-156, #73):** prerrequisito; aporta el formato de archivo y el patrón del cargador.
  - **HU-001 (KNOCK-43, PR #75 sin fusionar):** modelo de `usuarios` (`celularCifrado`, `celularHash`, `sub`, `estado`, `perfilCompleto`, `version`) y **su implementación** del cifrado; esta tarea no se implementa antes de que #75 se fusione.
  - **TT-API-12:** `UserIdDerivation.Derive(uid)` para el `sub`.
  - **HU-002:** dueña del perfil; el campo `nombre` se concilia con ella si su spec lo ubica en otra colección.
  - **HU-017 y HU-024:** cuando existan sus specs, una tarea nueva (o una ampliación de esta) agrega técnicos y solicitudes.
  - **TT-BD-07 (KNOCK-157):** compose local con `documentdb-local` para la evidencia del caso 09.
  - **TT-ML-01:** depende de esta tarea (ficha de TT-ML-01); necesitará los técnicos y solicitudes del trabajo futuro.

## 4. Contratos API y eventos
- **Sin endpoints, eventos ni cambios de OpenAPI.** El cargador no se expone por HTTP.
- **Configuración:**

| Clave | Valor | Nota |
|---|---|---|
| `Seed:Demo` | `true` solo donde se quiera cargar | Sin ella, o en `Production`, el cargador no escribe (TC-TT-BD-03-07). |
| Llave de cifrado y de HMAC del celular | secreto | La misma de HU-001 en ese entorno (user-secrets en local); nunca en el JSON ni en el repositorio. |

## 5. Modelo de datos
Formato de archivo igual a `db/seeds/catalog/*.json` (TT-BD-06): arreglo de documentos, `_id` como `{"$oid": "…"}` fijo, campos en camelCase, fechas en UTC ISO 8601. Cada documento lleva `demo: true` (para contarlos y borrarlos sin tocar otros datos) y `version: 1`.

**`db/seeds/demo/identity/usuarios.json`** → colección `usuarios` (base `identity`, modelo de HU-001, `cache: no`)
```
{ _id, uid: "demo-cliente-NN" (ficticio; solo en el JSON, el cargador deriva el sub y lo descarta),
  celular: "+5200000NNNNN" (solo en el JSON; el cargador lo cifra y lo descarta),
  nombre (escrito a mano), estado: "activo", perfilCompleto: true, creadoEn, version: 1, demo: true }
```
- En la base queda lo de HU-001: `sub` = `UserIdDerivation.Derive(uid)` (TT-API-12), `celularCifrado`, `celularCifradoVersion`, `celularHash`, `celularHashVersion`; **nunca** `celular` en claro (TC-TT-BD-03-06) ni el `uid`.
- `nombre`: campo propuesto en `usuarios`; si la spec de HU-002 lo ubica en otra colección, se concilia al implementar.

**Índices:** los define HU-001; el cargador no crea índices nuevos.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. El riesgo está en meter datos reales, en guardar datos personales en claro y en cargar demostración donde no toca.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** un dato de una persona real (nombre, celular, dirección, foto, INE, CURP) entra al repositorio público o a una base. | Solo datos inventados; celulares del rango ficticio; sin campos sensibles; revisión humana de los nombres en el PR; constitución §6. | TC-TT-BD-03-03 |
| **Fuga (I):** el celular de demostración queda en claro en la base y una prueba o un log lo copia. | El cargador cifra y calcula el hash con el código de HU-001 y descarta el valor en claro; no registra celulares. | TC-TT-BD-03-06 |
| **Fuga (I):** la llave de cifrado o una cadena de conexión aparece en el JSON, el PR o la evidencia. | Llaves solo por user-secrets o Key Vault; la evidencia del caso 09 son conteos, sin cadenas. | TC-TT-BD-03-09 (revisión de la evidencia) |
| **Alteración (T):** se carga demostración en producción o se mezclan datos de demostración con datos reales. | Guarda de entorno y marca `Seed:Demo`; `demo: true` en cada documento. | TC-TT-BD-03-07 · TC-TT-BD-03-01 |
| **Alteración (T):** duplicados tras correr el cargador varias veces. | `_id` fijos, upsert por `_id`, validación previa; si algo falla no se escribe nada. | TC-TT-BD-03-05 |
| **Elevación (E):** el cargador escribe en la base de otro servicio. | Cargador en `Modules/Identity/Data/` con su base; prueba de arquitectura. | TC-TT-BD-03-08 |
| **Suplantación (S):** un usuario de demostración inicia sesión, o su `sub` coincide con el de una cuenta real. | Rango sin ladas reales; sin cuentas de Firebase (D8); `uid` con prefijo `demo-cliente-` que Firebase no emite; `sub` derivado con la misma función que la sesión. | TC-TT-BD-03-02 · TC-TT-BD-03-03 · TC-TT-BD-03-04 |
| **IA (principio 5):** los datos de demostración se mandan a Azure OpenAI con celulares. | Los celulares nunca salen del módulo Identity; no hay textos libres más allá del nombre. | TC-TT-BD-03-03 |
| **Repudio (R) / denegación (D):** no aplican: no hay endpoints ni acciones de usuario. | — | — |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Base de datos | Sí | Archivo, datos ficticios, `sub`, carga repetible, cifrado y guarda de entorno con `documentdb-local` (Testcontainers). TC-TT-BD-03-01 a 07. `tests/DatabaseTest/Demo/`. |
| Arquitectura | Sí | El cargador solo en su base. TC-TT-BD-03-08. |
| Seguridad negativa | Sí, dentro de base de datos | Guarda de entorno y ausencia de datos sensibles (TC-TT-BD-03-03, 06 y 07). |
| Smoke local | Sí, manual | Conteos en `documentdb-local` tras dos cargas (TC-TT-BD-03-09). |
| Contrato HTTP, eventos, UI, rendimiento, mutación | No | Sin endpoints ni lógica de negocio. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: build Release 0/0, `dotnet format`, Unit (con Architecture) y Database en verde con resultado real, `sdd_lint.py --trace TT-BD-03`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Límites conocidos:**
- El cifrado del celular depende de la **implementación** de HU-001 (PR #75, aún sin fusionar); si #75 cambia el servicio de cifrado o los campos, el cargador lo sigue.
- Con `_id` fijos, un cambio de forma en el JSON necesita que el cargador suba `version` solo si cambia el contenido (como el de TT-BD-06).
- Los usuarios de demostración no pueden iniciar sesión (D8).
- Sin técnicos ni solicitudes, la demostración no cubre el mapa ni el flujo de solicitud hasta el trabajo futuro de HU-017 y HU-024.

**Decisiones del líder (2026-10-28):**
- **D1 (P1 y P9) — Alcance y puntos.** Se recorta a usuarios clientes, el cargador y las guardas de entorno. Técnicos y solicitudes quedan fuera de alcance hasta que existan las specs de HU-017 y HU-024. Se mantiene 1 punto.
- **D2 (P2) — Staging.** No hay Staging (se despliega desde `main`). «Terminada cuando» se cumple con el contenedor local `documentdb-local` y comandos de conteo.
- **D3 (P3) — Cómo se carga.** Un cargador por servicio dentro de la API, porque ahí viven las llaves de cifrado.
- **D4 (P4) — Referencia al cliente.** El JSON guarda un `uid` ficticio y el `sub` se deriva con la misma función de TT-API-12 (`UserIdDerivation.Derive`).
- **D5 (P5) — Solicitudes.** 10, una por cliente, cuando exista HU-024 (trabajo futuro).
- **D6 (P6) — Celulares.** Rango `+5200000NNNNN`.
- **D7 (P7) — Nombres.** Escritos a mano, sin Bogus.
- **D8 (P8) — Inicio de sesión.** Los usuarios de demostración no inician sesión.

**Preguntas abiertas para el líder:** ninguna.

## Aprobación
- [ ] Revisor 1 — nombre: ____________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
