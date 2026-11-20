# BUG-001 — El job `dast` del CI nunca se ejecuta por archivos faltantes

> **Jira:** KNOCK-238 · **Área:** CI · **Servicio:** Transversal (CI del repositorio) · **Responsable:** Alexis Blas · **Sprint:** S4 · **Puntos:** 1 (corrección) · **Prioridad:** Alta · **Capas:** CI
> **Repositorio:** knock-backend · **Otra parte en:** —
> **Encontrado en:** TT-SEC-03 (KNOCK-78), `docs/modelo-de-amenazas.md` §3, brecha **CI-6 / H-06** (2026-11-06).
> **Relacionadas:** spec 007 (CI y despliegue; deja ZAP fuera de su alcance hasta tener un servicio desplegable), KNOCK-233 (comprobaciones obligatorias de los rulesets, spec 007 T-05), KNOCK-239 y KNOCK-240 (encabezados del gateway, de otra persona).
> **Fuentes:** `.github/workflows/ci-pr.yml` (jobs `detect`, `dast` y `gate`) · `docs/QA_DEVSECOPS.md` (filas DAST y Smoke) · `docs/modelo-de-amenazas.md` · `deploy/docker-compose.local.yml` · `src/*/Dockerfile` · `security/exceptions.yaml`
> **Skills:** story-workflow, spec-first-writing, foundation-backend, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Aprobada

## 1. Objetivo
Que el escaneo dinámico (ZAP baseline) y la prueba de humo que `docs/QA_DEVSECOPS.md` promete en cada PR **se ejecuten de verdad**. La pila de servicios corre dentro del runner, sin costo y sin Azure. Si el escaneo deja de poder ejecutarse, el `gate` debe fallar en vez de darlo por bueno.

**Qué pasa hoy.** El job `detect` calcula `dast=true` solo si existen `deploy/docker-compose.ci.yml` y `tests/smoke/smoke.sh`. Ninguno de los dos existe, y el job `dast` también lee `security/zap-rules.tsv`, que tampoco existe. Por eso `dast` sale como `skipped` en todas las ejecuciones (por ejemplo, la ejecución 42664316144 de `sprint-04`). El `gate` acepta `skipped` como bueno, así que la brecha no se ve en ningún PR. Además, el job todavía dice «PostgreSQL», aunque la base es MongoDB (`documentdb-local`) desde TT-BD-06.

## 2. Defecto y casos de prueba
**BUG-001** — Como equipo, queremos que el escaneo dinámico y la prueba de humo corran en cada PR que cambia un servicio, para detectar configuraciones inseguras antes de fusionar.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-BUG-001-01 | Dado un PR contra la rama de sprint vigente (hoy `sprint-05`) que toca `src/`, cuando corre `ci-pr`, entonces `detect` da `dast=true` y el job «Smoke + DAST (ZAP baseline)» termina en `success` o `failure`, nunca en `skipped`. | (comando) `gh run view <id> --json jobs`; la salida va en el PR |
| TC-BUG-001-02 | Dado `deploy/docker-compose.ci.yml` con credenciales efímeras del job, cuando se levanta en el runner, entonces el gateway, Identidad, Mercado, Mensajería, `documentdb-local`, Redis y Azurite arrancan, y `GET /health/ready` de cada uno de los cuatro servicios responde 200 en 120 s o menos; los servicios internos se comprueban con `docker compose exec` o por la red interna. | (comando) paso «Levantar» del job; la salida va en el PR |
| TC-BUG-001-03 | Dada la pila levantada, cuando corre `tests/smoke/smoke.sh <url>`, entonces comprueba que `/health` responde 200, que una ruta protegida sin token responde 401 con `application/problem+json`, que están `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` y `Content-Security-Policy`, y que no se envía `Server: Kestrel`; termina con 0 solo si todo se cumple. HSTS no se exige en este smoke HTTP local. | (comando) paso «Smoke» del job |
| TC-BUG-001-04 | Dado un servidor de prueba que omite por separado cada uno de `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` y `Content-Security-Policy`, o envía `Server: Kestrel`, o responde 200 sin token, cuando corre `smoke.sh` contra él, entonces termina con un código distinto de 0 y nombra la comprobación que falló, sin imprimir cuerpos ni encabezados con valores sensibles. | `tests/smoke/smoke_test.sh` → `TC-BUG-001-04` |
| TC-BUG-001-05 | Dado `security/zap-rules.tsv`, cuando ZAP baseline encuentra una alerta de riesgo alto (o media sin regla), entonces el job falla; cada regla en `IGNORE` o `WARN` cita su `EXC-NNN` de `security/exceptions.yaml` con motivo y vencimiento. | (comando) ejecución del job · revisión de `zap-rules.tsv` en el PR |
| TC-BUG-001-06 | Dado un PR donde `detect` da `docker=true` pero `dast=false` (falta un archivo de la pila), cuando corre `gate`, entonces `gate` falla con un mensaje que nombra el archivo que falta, en lugar de aceptar el `skipped`. | (comando) PR de prueba que borra `tests/smoke/smoke.sh`; la salida va en el PR |
| TC-BUG-001-07 | Dados el archivo de compose, `smoke.sh`, `zap-rules.tsv` y los registros del job, cuando se revisan, entonces no contienen contraseñas, llaves ni cadenas de conexión con credenciales (solo `${CI_*}` del entorno del job, enmascaradas), y Gitleaks no reporta hallazgos nuevos. | (comando) job «Secretos (Gitleaks)» en verde |
| TC-BUG-001-08 | Dada la ejecución del job, cuando se revisa, entonces no llama a Azure ni a servicios de pago, las imágenes de `documentdb-local`, Redis y Azurite están fijadas por digest como en el compose local actualizado, Postgres queda fuera de la pila y el job termina dentro de su `timeout-minutes` (25). | (comando) revisión del compose y duración de la ejecución en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Primero agregar a `deploy/docker-compose.local.yml` Azurite `mcr.microsoft.com/azure-storage/azurite:3.37.0` fijado por digest, el mismo de los PR de Media; actualmente no está. `documentdb-local` y Redis ya están fijados por digest.
  - Crear `deploy/docker-compose.ci.yml`: los cuatro servicios construidos desde sus `Dockerfile`, más `documentdb-local`, Redis y Azurite con las mismas imágenes fijadas por digest del compose local actualizado. Sin puertos publicados salvo el 8080 del gateway, y con la configuración de cada servicio tomada de variables `CI_*`.
  - Crear `tests/smoke/smoke.sh` (bash, sin dependencias además de `curl`) y su autoprueba `tests/smoke/smoke_test.sh`.
  - Crear `security/zap-rules.tsv` (al principio vacío o con reglas justificadas por `EXC-NNN`).
  - Cambios en `.github/workflows/ci-pr.yml`, que **los aplica una persona**, porque `.github/` está protegido para el agente:
    1. El `gate` falla si `docker=true` y `dast=false` (TC-06).
    2. Los nombres de los pasos dicen MongoDB (`documentdb-local`) en lugar de PostgreSQL.
    3. Si hace falta, el paso de credenciales exporta las variables que pida el compose.
  - Actualizar `docs/QA_DEVSECOPS.md`, cerrar la brecha CI-6 / H-06 en `docs/modelo-de-amenazas.md` y registrar en `Memoria.md`.
- **Out:**
  - Postgres (`postgis/postgis:16-3.4`, sin digest en el compose local): ningún servicio actual en `src/` lo usa; queda fuera de la pila del CI.
  - El escaneo de API o activo de ZAP con OpenAPI y un token de prueba (`docs/modelo-de-amenazas.md` §4; corre de noche y no bloquea el PR).
  - Infraestructura temporal en Azure.
  - Corregir los hallazgos de encabezados del gateway: KNOCK-239 (HSTS) y KNOCK-240 (`Server: Kestrel`) son de otra persona. Esta spec no los arregla; ver §8 sobre cómo los trata el smoke.
  - Seguridad propia de Mensajería (KNOCK-241).
  - Llenar las comprobaciones obligatorias de los rulesets (KNOCK-233, spec 007 T-05).

## 4. Contratos API y eventos
No agrega ni cambia endpoints. Usa los existentes: `GET /health` y `GET /health/ready` del gateway, `GET /health/ready` de Identidad, Mercado y Mensajería por la red interna, y una ruta protegida del gateway para la comprobación de 401 (por ejemplo `GET /api/v1/me`). Interfaz que fija: `smoke.sh <url-base>` devuelve 0 si todas las comprobaciones pasan, y si no, un código distinto de 0 con una línea por comprobación fallida.

## 5. Modelo de datos
No aplica: la pila del CI arranca con bases vacías y se destruye con `down -v` al terminar.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Fuga de información: credenciales en el compose o en los registros del job | Credenciales generadas por ejecución con `openssl rand` y `::add-mask::`; el compose solo lee `${CI_*}`; Gitleaks sin hallazgos | TC-BUG-001-07 |
| Alteración: imagen de terceros cambiada (cadena de suministro) | Imágenes por digest, la acción de ZAP fijada por SHA (ya lo está) | TC-BUG-001-08 |
| Repudio o control silenciado: el escaneo deja de correr y nadie lo nota | `gate` falla si `docker=true` y `dast=false` | TC-BUG-001-06 |
| Control debilitado: ignorar alertas de ZAP para «pasar» | Toda regla `IGNORE`/`WARN` necesita su `EXC-NNN` con vencimiento, revisada en el PR | TC-BUG-001-05 |
| Falso verde: un smoke que nunca falla | Autoprueba negativa de `smoke.sh` | TC-BUG-001-04 |
| Denegación del CI: la pila no arranca y bloquea todos los PR | Espera acotada (120 s) con mensaje claro y `docker compose logs` de los servicios (sin variables de entorno) al fallar | TC-BUG-001-02 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | No | No hay código de producción nuevo |
| Integración | Sí | La pila completa en el runner (TC-02) |
| Humo | Sí | `smoke.sh` (TC-03) |
| Seguridad negativa | Sí | Autoprueba de `smoke.sh` (TC-04) y `gate` con un archivo faltante (TC-06) |
| DAST | Sí | ZAP baseline (TC-05) |
| Contrato | No | No cambia rutas |
| Arquitectura | No | No cambia código |
| Mutación | No | No hay lógica de dominio |
| Regresión | Sí | El resto de los jobs de `ci-pr` sigue igual |
| CI y análisis | Sí | Checkov sobre el compose y los workflows, y Gitleaks |

**Gates:** ver `AGENTS.md` (Definición de terminado); además, una ejecución real del job en un PR de prueba con la salida en el PR.

**Límites:**
- **Archivos protegidos:** los cambios a `.github/workflows/ci-pr.yml` (TC-06 y los nombres de los pasos) los aplica una persona. Sin ellos, TC-06 no se puede cerrar.
- **HSTS y `Server`:** HSTS no se exige en el smoke del CI: corre por `http://localhost` y ASP.NET Core no envía `Strict-Transport-Security` por HTTP ni a localhost. Se comprueba en el smoke posterior al despliegue por HTTPS real cuando BUG-002 esté desplegado. KNOCK-240 ya está fusionado: el smoke sí exige que no se envíe `Server: Kestrel`. Las alertas de ZAP se tratan conforme a TC-05.
- **Configuración mínima de cada servicio en el CI:** decisión B-01 de abajo. No se agrega ningún modo inseguro que pueda activarse fuera del CI.
- **Readiness:** `/health/ready` del gateway existe, pero su única comprobación `ready` es Redis; no demuestra que los demás servicios arrancaron. TC-02 espera también la ruta de cada servicio interno.
- **Tiempo del runner:** se construyen las cuatro imágenes y se mide en el primer PR de prueba; si la duración supera 20 minutos de los 25 del `timeout-minutes`, se reutilizan las imágenes del job `container`. Es un criterio de implementación, no una decisión.

**Decisiones pendientes del Tech Lead (sí/no).** Cada una se contesta sí o no sobre la recomendación del autor; responder «sí» la deja decidida tal como está escrita y «no» obliga a ajustar la spec antes de aprobar. Ninguna se inventó como bloqueo: se anota qué parte del trabajo depende de ella.

| N.º | Decisión (recomendación del autor: sí) | Contexto y alternativa | Si es «no» |
|---|---|---|---|
| B-01 | **No se modifica `src/`: Identidad, Mercado y Mensajería arrancan en el CI con valores sintéticos por variables `CI_*` (por ejemplo un `ProjectId` de Firebase inventado), sin perfil `Ci` ni banderas que desactiven controles.** El smoke solo prueba rutas sin token (401), que no necesitan llamar a Firebase. | Identidad ya lee `FIREBASE_AUTH_EMULATOR_HOST`. Si el arranque de T1 demuestra que algún servicio no arranca sin un valor real, se vuelve con ese dato y la corrección se pide en una spec propia. | Se autoriza un perfil `Ci` en `src/` acotado (más puntos y revisión de seguridad). |
| B-02 | **Los cambios a `.github/workflows/ci-pr.yml` (TC-01 y TC-06, nombres de los pasos) los aplica Julián (Tech Lead) en el mismo PR de implementación, una vez aprobada la spec, como ya dice `task.md` T4.** | Sin esos cambios TC-01 y TC-06 no se pueden cerrar; el agente no toca `.github/`. | Se indica otra persona. |
| B-03 | **Se acepta la revisión de par con los cuatro puntos de Erick atendidos (commit `ff0848c`), sin una nueva respuesta suya.** | En el PR #59 Erick dejó «cambios solicitados» el 2026-11-11, Alexis los atendió el 2026-11-12 y pidió el 2026-11-13 que los revisara; no hay respuesta posterior en el hilo. | Se pide la re-revisión de Erick antes de aprobar. |

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-20
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
