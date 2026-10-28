# TT-BD-07 — Adaptar CI y pruebas a MongoDB (Testcontainers); quitar sqlfluff y PostgreSQL

> **Jira:** KNOCK-157 · **Área:** Base de datos · Integración continua · **Servicio:** Transversal (CI, pruebas y entorno local; ADR-001) · **Módulo:** — · **Responsable:** José Guillermo Rangel Hernández (como en Jira; el Tech Lead hace los cambios de `.github/` y `.agents/knock.json`) · **Sprint:** S2 · **Puntos:** 3 · **Capas:** CI, BD (pruebas y entorno local)
> **Repositorio:** knock-backend · **Otra parte en:** — · **Rama:** `docs/KNOCK-157-mongo-ci-tests-spec` (solo esta spec) → PR a `sprint-02`; la implementación va en su propia rama, también a `sprint-02`, después de #73 y de la aprobación
> **Ficha:** no existe `docs/backlog/TT-BD-07.md` (la tarea nace en `docs/PLAN_ADAPTACION.md` §E, «Datos», y en los puntos C3, C4 y C5).
> **Depende de:** TT-BD-06 (KNOCK-156, PR #73, **fusionado** en `sprint-02` el 2026-10-28): retira EF Core, Npgsql, NetTopologySuite, `Testcontainers.PostgreSql` y `db/seeds/catalog/oficios.sql`, y crea `tests/IntegrationTest/Support/DocumentDbFixture.cs` con `documentdb-local` fijada por digest. Con #73 ya fusionado, la tarea empieza cuando esta spec esté Aprobada.
> **Fuentes:** `docs/PLAN_ADAPTACION.md` (C3, C4, C5, D «TT-CI-01» y «TT-REPO-06», E) · ADR-002 («Qué se retira») · constitución 2.2.x (principio 9; §3 «Calidad»; §4 «Transición de datos»; §6 «Secretos»; §7) · spec TT-BD-06 (§3 «Out», §8 decisión (a) y «Límites conocidos») · lectura de `.github/workflows/ci-pr.yml`, `.github/dependabot.yml`, `deploy/docker-compose.local.yml`, `README.md`, `.agents/` y de la rama de #73 (2026-10-27)
> **Skills:** story-workflow, spec-first-writing, testing-strategy-backend, database-scripts, security-core, reuse-before-create
> **Estado:** Aprobada

## 1. Objetivo
Que el CI, el entorno local y las instrucciones de los agentes dejen de suponer PostgreSQL + PostGIS + sqlfluff, y que **todas** las suites de .NET corran en el CI contra MongoDB (`documentdb-local` con Testcontainers, la imagen fijada por TT-BD-06), sin bajar ningún umbral ni quitar ninguna verificación que siga teniendo objeto.

**Terminada cuando:** el CI corre sin PostgreSQL y todas las pruebas pasan con MongoDB (criterio de Jira). En concreto: ningún job de `ci-pr.yml` instala sqlfluff ni levanta o nombra PostgreSQL; Dependabot no agrupa Npgsql ni EF Core y sí el driver de MongoDB; `deploy/docker-compose.local.yml` levanta `documentdb-local` con el mismo digest que la fixture; el README y las instrucciones de los agentes ya no piden PostgreSQL; y en el PR de implementación las suites Unit, Integration, Security y Database terminan en verde con pruebas ejecutadas (no saltadas).

**Archivos protegidos — implementa o autoriza el Tech Lead.** Casi todo lo que cambia esta tarea está en rutas protegidas por `AGENTS.md` §4 y por `scripts/hooks/knock_guard.py`: `.github/workflows/ci-pr.yml`, `.github/dependabot.yml`, `.github/agents/*` y `.github/instructions/*` (generados), y `.agents/knock.json`. **Esos cambios los hace el Tech Lead (José Julián Hernández Hernández) o una persona a quien él autorice por escrito en el PR.** Ningún agente los edita ni usa `KNOCK_GUARD_ALLOW_PROTECTED`. Los archivos no protegidos (`deploy/docker-compose.local.yml`, `README.md`, las fuentes `.agents/agents/*.md` y `.agents/rules/*.md`, y las pruebas) puede editarlos quien implemente.

**Qué hay hoy y qué cambia** (leído en `sprint-01` y en la rama de #73 el 2026-10-27; los números de línea son de `sprint-01` y pueden moverse):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| `ci-pr.yml`, job `build-test` (línea ~157) | Paso «Integración (PostgreSQL + PostGIS, Redis y Azurite con Testcontainers)». | Se renombra a MongoDB (`documentdb-local`), Redis y Azurite. El comando no cambia. |
| `ci-pr.yml`, job `build-test` (línea ~161) | Paso «Base de datos (migraciones, semillas, restricciones, PostGIS)» con `if: hashFiles('tests/DatabaseTest/*.csproj') != ''`. | Se renombra (semillas, índices y consultas de MongoDB). La condición `if` se quita: si `tests/DatabaseTest` desapareciera, el CI debe fallar, no saltar el paso en silencio. |
| `ci-pr.yml`, job `sql-lint` (líneas ~307–320) | `pip install sqlfluff && sqlfluff lint db --dialect postgres`, activado por la salida `sql` de `detect` (línea ~59). | Se **borra** el job, la salida `sql` de `detect` y `sql-lint` de `gate.needs` (línea ~408). Tras #73 no queda SQL en `db/`. |
| `ci-pr.yml`, job `dast` (línea ~388) | Paso «Levantar API + PostgreSQL + Redis + Azurite» con `CI_DB_PASSWORD`; usa `deploy/docker-compose.ci.yml`, que **no existe**, por lo que el job hoy se salta (`dast=false`). | Se renombra a MongoDB y la variable a un nombre neutro. Crear `docker-compose.ci.yml` **no** es de esta tarea (ver pregunta P3). |
| `dependabot.yml` (línea 16) | Grupo `efcore` con `Microsoft.EntityFrameworkCore*` y `Npgsql*`. | Se quita el grupo y se agrega uno para `MongoDB.*` (C5). |
| `deploy/docker-compose.local.yml` | Solo un servicio `postgres` con `postgis/postgis:16-3.4` y volumen `postgres-data`. | El servicio de base pasa a `documentdb-local` con **el mismo digest** que `DocumentDbFixture.Image`; sin PostgreSQL. El compose por servicio (Redis, Azurite, emulador, APIs) sigue siendo de TT-ARQ-01. |
| `README.md` (líneas 4, 23, 28–29) | Stack «PostgreSQL 16 + PostGIS», `POSTGRES_PASSWORD`, `psql … postgis_version()` y reinicio de `postgres`. | Stack con MongoDB (Azure DocumentDB); arranque y verificación del contenedor local; `ConnectionStrings:KnockMongo` por user-secrets (#73 ya cambia el párrafo de la cadena). |
| Instrucciones de agentes | `.github/agents/db-builder.agent.md` (migraciones EF Core, consultas PostGIS, skill `postgis-geo`) y `.github/instructions/tests-backend.instructions.md` («Integración contra PostgreSQL+PostGIS…»); son **generados** por `scripts/sync_agents.py` desde `.agents/agents/db-builder.md` y `.agents/rules/tests-backend.md`. `.agents/rules/database.md` pide sqlfluff y `.agents/knock.json` tiene una verificación de cierre con sqlfluff. | Se corrigen las **fuentes** en `.agents/` y se regeneran los adaptadores con `python scripts/sync_agents.py` (no se editan a mano). `.agents/knock.json` es protegido: lo cambia el Tech Lead. |
| Pruebas | Tras #73: `DatabaseTest`, `IntegrationTest` y `SecurityTest` usan `DocumentDbFixture` (contenedor genérico de Testcontainers, digest del spike); ningún `.csproj` de la solución referencia `Testcontainers.PostgreSql`. | Se comprueba con pruebas que eso siga así (una sola imagen, por digest) y que las cuatro suites corren y pasan en el CI. |

## 2. Historia y casos de prueba
**TT-BD-07** — Tarea técnica. Las comprobaciones sobre archivos del repositorio (workflow, Dependabot, compose, README, fuentes de agentes, paquetes) se escriben como pruebas que leen esos archivos como texto, en `tests/UnitTest/Architecture/` (nombre de clase por confirmar al implementar), para que una regresión las haga fallar en cada PR. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_BD_07_KK`. Solo datos ficticios: ninguna prueba usa una cadena real. Los casos 08 y 09 se verifican con comandos (su salida va en el PR).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-07-01 | Dado `.github/workflows/ci-pr.yml`, cuando se lee, entonces no contiene `sqlfluff`, no tiene el job `sql-lint` ni la salida `sql` en `detect`, `gate.needs` no nombra `sql-lint`, y no contiene `postgres`, `postgis` ni `npgsql` (sin distinguir mayúsculas). | `UnitTest/Architecture/PostgresRetirementTests.CiWorkflowHasNoSqlfluffNorPostgres_TC_TT_BD_07_01` |
| TC-TT-BD-07-02 | Dado `ci-pr.yml`, cuando se leen los pasos de `build-test`, entonces existen los pasos de `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest` y `tests/DatabaseTest`, **ninguno** con condición `if` que lo salte por ausencia del proyecto, y las variables `MIN_LINES` y `MIN_BRANCHES` conservan los valores de `sprint-01` (60 y 50). | `UnitTest/Architecture/PostgresRetirementTests.CiRunsAllDotnetSuitesWithoutSkipAndKeepsCoverageThresholds_TC_TT_BD_07_02` |
| TC-TT-BD-07-03 | Dado `.github/dependabot.yml`, cuando se lee, entonces no tiene el grupo `efcore` ni los patrones `Npgsql*`, `Microsoft.EntityFrameworkCore*` o `NetTopologySuite*`, y el ecosistema `nuget` tiene un grupo cuyo patrón cubre `MongoDB.Driver` (`MongoDB.*`). | `UnitTest/Architecture/PostgresRetirementTests.DependabotGroupsMongoAndNotEfCoreOrNpgsql_TC_TT_BD_07_03` |
| TC-TT-BD-07-04 | Dados todos los `*.csproj` y `packages.lock.json` del repositorio (incluido `tests/Spike.DocumentDb`), cuando se leen, entonces ninguno referencia `Npgsql`, `Microsoft.EntityFrameworkCore`, `NetTopologySuite` ni `Testcontainers.PostgreSql`, ni como dependencia directa ni transitiva. | `UnitTest/Architecture/PostgresRetirementTests.NoProjectOrLockFileReferencesPostgresPackages_TC_TT_BD_07_04` |
| TC-TT-BD-07-05 | Dado el código de `tests/` (sin `tests/Spike.DocumentDb`, que conserva su propio digest de evidencia), cuando se buscan imágenes de contenedor, entonces la única imagen de MongoDB/DocumentDB es la constante `DocumentDbFixture.Image`, que está fijada por digest (`@sha256:` de 64 hexadecimales) y no por etiqueta; y no aparece `postgis/postgis` ni `postgres:`. | `UnitTest/Architecture/PostgresRetirementTests.TestsUseOnlyTheDigestPinnedDocumentDbImage_TC_TT_BD_07_05` |
| TC-TT-BD-07-06 | Dado `deploy/docker-compose.local.yml`, cuando se lee, entonces no tiene servicios ni volúmenes de PostgreSQL; tiene un servicio cuya imagen es **exactamente** el valor de `DocumentDbFixture.Image`; su puerto se publica solo en `127.0.0.1`; la contraseña llega de una variable **obligatoria** (`${…:?…}`, sin valor por defecto) y no aparece escrita en el archivo; y tiene `healthcheck`. | `UnitTest/Architecture/PostgresRetirementTests.LocalComposeRunsDigestPinnedDocumentDbWithoutPostgres_TC_TT_BD_07_06` |
| TC-TT-BD-07-07 | Dados `README.md`, `.agents/agents/db-builder.md`, `.agents/rules/tests-backend.md`, `.agents/rules/database.md` y `.agents/knock.json`, cuando se leen, entonces no contienen `PostgreSQL`, `PostGIS`, `postgres`, `psql`, `sqlfluff`, `dotnet ef` ni `EF InMemory`; el README nombra `ConnectionStrings:KnockMongo` y el comando de `deploy/docker-compose.local.yml`; y `python scripts/sync_agents.py --check` termina en OK (los adaptadores de `.github/agents/` y `.github/instructions/` coinciden con sus fuentes). | `UnitTest/Architecture/PostgresRetirementTests.ReadmeAndAgentSourcesHaveNoPostgresOrSqlfluff_TC_TT_BD_07_07` |
| TC-TT-BD-07-08 | Dado el PR de implementación, cuando corre `ci-pr.yml`, entonces el job `build-test` termina en verde; los reportes `trx` de Unit, Integration, Security y Database tienen **más de 0 pruebas ejecutadas** y 0 fallidas cada uno; el registro del job no muestra la descarga de `postgis/postgis` ni de `postgres`; el job `sql-lint` ya no aparece; y `gate` termina en verde. | (comando) enlace a la corrida del CI y conteos de los `trx` en el PR |
| TC-TT-BD-07-09 | Dado el cambio terminado, cuando se compila y se revisan formato y gobierno, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings, `dotnet format Knock.slnx --verify-no-changes` no reporta cambios, y `python scripts/sdd_lint.py`, `python scripts/sync_agents.py --check`, `python scripts/codemap.py --check` y `python scripts/ci/check_exceptions.py` terminan en OK. | (comando) salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - **CI (`.github/workflows/ci-pr.yml`, protegido: Tech Lead):** renombrar los pasos de integración y base de datos; quitar la condición `if` del paso de `DatabaseTest`; borrar el job `sql-lint`, la salida `sql` de `detect` y `sql-lint` de `gate.needs`; renombrar el paso y la variable de credencial del job `dast` sin crear `docker-compose.ci.yml`. Los umbrales de cobertura no cambian.
  - **Dependabot (`.github/dependabot.yml`, protegido: Tech Lead):** quitar el grupo `efcore`; agregar un grupo `MongoDB.*` en `nuget`.
  - **Compose local (`deploy/docker-compose.local.yml`):** cambiar el servicio de base por `documentdb-local` fijado con el digest de `DocumentDbFixture.Image`, puerto solo en `127.0.0.1`, usuario y contraseña desde variables (contraseña obligatoria), `healthcheck` y volumen propio; quitar `postgres` y `postgres-data`. Ajustar `.env.example` a las variables nuevas sin valores secretos.
  - **README:** stack, arranque y verificación del contenedor local, cadena por user-secrets; quitar `psql`, `postgis_version()` y `POSTGRES_PASSWORD`.
  - **Instrucciones de agentes:** corregir las fuentes `.agents/agents/db-builder.md`, `.agents/rules/tests-backend.md` y `.agents/rules/database.md` (sin PostgreSQL, PostGIS, migraciones EF Core ni sqlfluff; integración con `documentdb-local` y Testcontainers), y regenerar con `python scripts/sync_agents.py` los adaptadores `.github/agents/db-builder.agent.md` y `.github/instructions/tests-backend.instructions.md` (y los demás que el script regenere). `.agents/knock.json` (protegido): el Tech Lead quita la verificación de cierre «SQL (sqlfluff)».
  - **Pruebas de regresión** de los casos 01 a 07 en `tests/UnitTest/Architecture/`.
- **Out:**
  - Todo lo que hace TT-BD-06 (#73): retirar paquetes, migraciones, fixtures de PostGIS y `oficios.sql`; crear `DocumentDbFixture`.
  - **CI por servicio** con filtros por ruta (TT-ARQ-04) y **compose por servicio** con Redis, Azurite, emulador y APIs (TT-ARQ-01). Crear `deploy/docker-compose.ci.yml` y reactivar el job `dast` (pregunta P3).
  - Dependabot para Terraform y Docker por servicio (C5, segunda mitad): TT-AZ-02 y TT-ARQ-01.
  - Reescribir las skills que todavía describen PostgreSQL o migraciones de EF Core (`postgis-geo`, `database-scripts`, `foundation-backend`, `testing-strategy-backend`, `vertical-slice-api`) y `docs/DECISION_TECNICA.md` (pregunta P4).
  - Checkov/Trivy sobre Terraform (C6), Key Vault (C8) y el script `mongosh` de entrega (TT-BD-09).
  - Cambiar el estado de TT-BD-01 o TT-BD-02.
  - **Prueba de humo contra el DocumentDB real** (principio 9) con el validador `$jsonSchema` de `modalidad`, y pasar la imagen a `1.0` tras repetir P5 y P9: TT-BD-06 los dejó para «TT-BD-07 o antes», pero la ficha de Jira no los nombra y no caben en 3 puntos sin decidir dónde vive la credencial. Quedan **fuera hasta que el líder responda P1 y P2**; si entran, se agregan casos y se reestiman los puntos.
- **Relación con otras tareas:**
  - **TT-BD-06 (KNOCK-156, #73):** prerrequisito. Esta tarea comprueba y documenta lo que #73 retira del código; no repite su retiro.
  - **TT-QA-01:** sus plantillas de prueba con MongoDB reutilizan `DocumentDbFixture` (caso 05).
  - **TT-ARQ-01 / TT-ARQ-04:** heredan un CI y un compose ya sin PostgreSQL.
  - **TT-BD-03 (KNOCK-40):** usa el compose local de esta tarea para cargar sus semillas a mano.

## 4. Contratos API y eventos
- **Sin endpoints, eventos ni cambios de OpenAPI.** No cambia `docs/openapi.v1.json`, `docs/endpoints.md` ni `docs/events.md`.
- **Configuración del entorno local (no es contrato HTTP):**

| Variable | Valor | Nota |
|---|---|---|
| Usuario del contenedor local | por defecto `knock` | En `deploy/docker-compose.local.yml`; nombre de variable por confirmar al implementar. |
| Contraseña del contenedor local | **obligatoria**, sin valor por defecto | Solo en `.env` local (ignorado por git); `.env.example` la lista vacía. |
| `ConnectionStrings:KnockMongo` | secreto | La API la lee por user-secrets o `ConnectionStrings__KnockMongo` (TT-BD-06); nunca en `appsettings*.json`. |

## 5. Modelo de datos
Sin colecciones, índices ni semillas nuevas. El contenedor local usa la imagen fijada por TT-BD-06, decisión (a): `ghcr.io/microsoft/documentdb/documentdb-local@sha256:0fcf634531c1917ad0855ff9f4354aca0a5c5d8e435f08250568deb37eeb0ad5`. Una sola fuente del digest: `DocumentDbFixture.Image`; el compose debe coincidir con ella (TC-TT-BD-07-06).

## 6. Seguridad, privacidad y amenazas
STRIDE breve. El riesgo está en la configuración del CI, la cadena de suministro y los secretos locales.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Alteración (T):** al quitar el job `sql-lint`, `gate` se rompe o deja de exigir otro job, o al quitar PostgreSQL se salta una suite y el CI queda verde sin probar nada. | Se quita `sql-lint` también de `gate.needs`; la suite de base de datos no tiene `if` que la salte; se exige que cada `trx` tenga más de 0 pruebas. | TC-TT-BD-07-01 · TC-TT-BD-07-02 · TC-TT-BD-07-08 |
| **Alteración (T):** bajar los umbrales de cobertura para que el CI pase tras el cambio de base. | `MIN_LINES` 60 y `MIN_BRANCHES` 50 sin cambio (constitución §7). | TC-TT-BD-07-02 |
| **Cadena de suministro:** imagen de pruebas o del compose que cambia sin aviso, o dos imágenes distintas entre pruebas y entorno local. | Una sola imagen, por digest, en `DocumentDbFixture.Image`; el compose usa el mismo valor. | TC-TT-BD-07-05 · TC-TT-BD-07-06 |
| **Cadena de suministro:** paquetes retirados que vuelven por una dependencia transitiva o por Dependabot. | Sin Npgsql/EF Core/NetTopologySuite en `.csproj` ni `packages.lock.json`; Dependabot sin su grupo. | TC-TT-BD-07-03 · TC-TT-BD-07-04 |
| **Fuga (I):** la contraseña del contenedor local o una cadena de conexión queda escrita en el compose, el README o `.env.example`, o el puerto queda expuesto en la red. | Contraseña por variable obligatoria sin valor por defecto; `.env` ignorado; puerto solo en `127.0.0.1`; gitleaks en el CI. La cadena con `AllowInsecureTls` de la fixture es solo para el contenedor local y no se copia al README como valor de ejemplo. | TC-TT-BD-07-06 · TC-TT-BD-07-07 |
| **Elevación (E):** un agente edita un workflow o un archivo protegido para «arreglar» el CI. | Los cambios en `.github/` y `.agents/knock.json` los hace o autoriza el Tech Lead (CODEOWNERS y `knock_guard`). | Revisión del PR (sin caso automático) |
| **Repudio (R):** instrucciones de agentes que siguen pidiendo PostgreSQL generan código fuera de la transición (constitución §4). | Fuentes de `.agents/` corregidas y adaptadores regenerados. | TC-TT-BD-07-07 |
| **Suplantación (S) / denegación (D):** no aplican: no hay endpoints ni identidad nueva. | — | — |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Arquitectura (regresión de configuración) | Sí | Pruebas que leen el workflow, Dependabot, compose, README, fuentes de agentes y paquetes como texto. TC-TT-BD-07-01 a 07. `tests/UnitTest/Architecture/`. |
| Integración y base de datos | Sí, sin pruebas nuevas | Las de TT-BD-06 con `DocumentDbFixture` deben correr y pasar en el CI (TC-TT-BD-07-08). |
| Unitarias, seguridad negativa | Sí, sin pruebas nuevas | Siguen en verde (TC-TT-BD-07-08). |
| Contrato HTTP, eventos, UI, rendimiento, mutación | No | Sin endpoints ni lógica de negocio. |
| Humo contra DocumentDB real | Por decidir | Pregunta P1. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: build Release 0/0, `dotnet format`, Unit (con Architecture), Integration, Security y Database en verde con resultado real, `sdd_lint.py --trace TT-BD-07`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Límites conocidos:**
- documentdb-local agota conexiones entre 64 y 96 (spike P7): si las suites corren en paralelo contra un solo contenedor, el CI puede fallar por conexiones y no por lógica; la fixture compartida de #73 lo mitiga.
- El contenedor local no reproduce todo DocumentDB (P5, P7, P8, P9; principio 9): que el CI pase con `documentdb-local` no prueba el comportamiento en Azure.
- Los números de línea de §1 son de `sprint-01` al 2026-10-27; el PR de implementación los vuelve a leer.

**Puntos:** 3 (Jira). Son cambios de configuración pequeños pero repartidos en muchos archivos, la mitad protegidos, más 7 pruebas de regresión. Si entran P1 o P2, no caben en 3.

**Preguntas abiertas para el líder:**
- **P1 — Humo contra DocumentDB real.** **Resuelto (líder, 2026-10-28): queda fuera de esta tarea.** No cabe en 3 puntos sin decidir dónde vive la credencial; el líder abre una tarea aparte para no perder el requisito del principio 9. *Pregunta original:* TT-BD-06 dejó para esta tarea la prueba de humo del principio 9 (incluido el `$jsonSchema` de `modalidad`). ¿Entra aquí, y dónde vive la cadena (secreto de GitHub de un entorno protegido, ejecución manual con `workflow_dispatch`, o corrida local con la variable como en el spike)? Si entra, ¿se reestima?
- **P2 — Imagen `1.0`.** **Resuelto (líder, 2026-10-28): se mantiene el digest del spike por ahora.** *Pregunta original:* ¿Se repiten P5 y P9 con `ghcr.io/documentdb/documentdb/documentdb-local:1.0` en esta tarea, o se mantiene el digest del spike hasta otra tarea?
- **P3 — Job `dast`.** **Resuelto (líder, 2026-10-28): se renombran aquí los pasos del job `dast`; `docker-compose.ci.yml` va en TT-ARQ-01 o TT-CD-01.** *Pregunta original:* Hoy se salta porque `deploy/docker-compose.ci.yml` no existe. ¿Basta con renombrar sus pasos aquí y crear el compose de CI en TT-ARQ-01/TT-CD-01, o se espera que esta tarea lo reactive con MongoDB?
- **P4 — Skills con PostgreSQL.** **Resuelto (líder, 2026-10-28): van en otra tarea de documentación de agentes.** Mientras tanto, quien implemente revisa lo que un agente genere en esa zona. *Pregunta original:* `postgis-geo`, `database-scripts`, `foundation-backend` y `testing-strategy-backend` siguen describiendo PostgreSQL, y `vertical-slice-api` pide migraciones con `dotnet ef` y PostGIS. ¿Se reescriben aquí (más puntos) o en una tarea de documentación de agentes?
- **P5 — Rama destino.** **Resuelto (líder, 2026-10-28): la spec va a `sprint-02` y la implementación también, después de #73.** *Pregunta original:* #73 apunta a `sprint-02`. ¿El PR de implementación de esta tarea va a `sprint-02` (después de #73) y esta spec a `sprint-01`?
- **P6 — Autorización.** **Resuelto (líder, 2026-10-28): los cambios de `.github/` y `.agents/knock.json` los hace el Tech Lead; para el compose local, el README y las pruebas autoriza por escrito en el PR de implementación.** *Pregunta original:* ¿Implementa el Tech Lead los cambios de `.github/` y `.agents/knock.json`, o autoriza por escrito en el PR a quien implemente el resto?

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-28
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
