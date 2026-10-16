# Memoria — knock-backend

> Constitución §11. Por issue: estado, decisiones con porqué, errores a evitar. Máx ~5000 líneas. Sin datos sensibles.
> **Agentes:** léela al empezar; agrega una entrada al terminar cada tarea.

## Estado actual
- Estructura SDD, guardarraíles de agentes, CI/CD y controles DevSecOps configurados (TT-SDD-01, TT-CI-01, TT-STD-01, parte de TT-SEC-01/02).
- Specs en Borrador generadas del backlog (`scripts/import_backlog.py`); falta escribir la clave KNOCK-N real y aprobarlas.
- Sin código de producto todavía. Los repos aún no tienen git; el guardián usa `KNOCK_SPEC=<ID>` para resolver la spec.

## 2026-10-12 — Configuración inicial
- Decisión: dos repositorios (knock-backend: API + scripts de BD; knock-frontend: app Flutter + panel Angular). Porqué: indicación del equipo; cada stack con su CI y versiones; el contrato OpenAPI es la frontera.
- Decisión: specs por issue (`specs/HU-NNN-…`, `TT-ÁREA-NN-…`) con casos `TC-<ID>-KK` iguales al plan de pruebas del proyecto.
- Decisión: guardián único (`scripts/hooks/knock_guard.py`) para Claude Code, Codex, Copilot, Cursor, Gemini CLI y opencode. Porqué: el equipo usa agentes distintos.
- Decisión: mapa de código propio (`scripts/codemap.py`) para evitar duplicados y violaciones de arquitectura.
- Pendiente: actualizar la ficha TT-REPO-01 (dice monorepo), configurar protección de ramas (docs/BRANCH_PROTECTION.md) y escribir los usuarios reales en CODEOWNERS.
- A evitar: empezar código sin spec Aprobada con KNOCK-N; editar archivos generados; agregar dependencias sin evaluación.

## 2026-10-12 — Verificación de integridad y spec TT-REPO-01
- Hecho: los archivos en disco coinciden con el zip (solo-archivos: 0 faltantes, 0 con otro tamaño). `sdd_lint`, `check_exceptions` y `codemap --check` en OK; `sync_agents --check` en OK con Python 3.12 e intérprete por defecto (`python3`).
- Hecho: spec TT-REPO-01 actualizada de monorepo a dos repositorios (sigue en Borrador; carpeta renombrada a `TT-REPO-01-crear-repositorios-knock-backend-y-frontend` y fila de `specs/README.md` corregida a mano).
- Decisión: `sync_agents.py` exige Python ≥ 3.12 (f-string con barra invertida, línea 135); con 3.11 falla con SyntaxError. Porqué: el CI usa 3.12.
- A evitar: correr `sync_agents.py` con `KNOCK_PYTHON=python` en local: reescribe los hooks con `python` y el CI (que espera `python3`) marcaría diferencias.
- Pendiente: acciones de GitHub sin fijar a SHA (`pin_actions.py`, lo ejecuta una persona); guardián sin git (ver informe de la Fase 0).
- Pendiente (fuente): corregir el título de TT-REPO-01 («monorepo» → dos repositorios) en `Backlog_Planeacion_Knock.xlsx`, que es la fuente de `import_backlog.py`; si no, al reimportar reaparecerán el nombre de carpeta y el título viejos.

## 2026-10-12 — Python 3.11, guardián y hooks multiplataforma
- Hecho: `sync_agents.py` ya corre en Python 3.11 (f-string de la línea 135 sin barra invertida); verificado con 3.11 y 3.12, y `py_compile` de todos los `scripts/**/*.py` en 3.11. CONTRIBUTING.md documenta el requisito de Python y Windows. Se añade `.gitattributes` (LF en `*.sh` y `.githooks/*`).
- Hallazgo (guardián, propuesto en `../_propuestas/01-knock_guard.patch`, lo aplica una persona): sin `.git` la regla «sin spec Aprobada no hay código» no se aplicaba y `stop` no verificaba nada; además `git(...).strip()` truncaba el primer archivo de `git status --porcelain` (` M Memoria.md` → `emoria.md`) incluso con git. Con `git init -b main` el estado del guardián queda por repo en `.git/knock-guard`. Haz un commit base antes de trabajar: sin él todo aparece como sin seguimiento.
- Propuesta (`../_propuestas/hooks-multiplataforma`): lanzador `run_guard.sh` que elige Python al ejecutar, para que el archivo generado sea igual en Windows y Linux. Pendiente de aplicar; el soporte real de cada agente en Windows (Codex, Gemini, Cursor, Copilot) quedó **no verificado**.
- A evitar: `codemap.py` (incluso `--check`) reescribe `docs/architecture/*` con la fecha y la ruta local; no incluyas ese ruido en un PR si no cambió el código.

## 2026-10-12 — Claves de Jira en las specs
- Hecho: la clave `KNOCK-N` de cada spec se escribió desde `mapa_specs_jira.csv` (cruce por ID; 194 specs entre los dos repos; tabla en `../_propuestas/spec_clave_jira.md`). Ninguna spec está Aprobada todavía salvo decisión de una persona.
- Alerta: el título de TT-REPO-01 (KNOCK-15) en Jira sigue diciendo «monorepo»; corregirlo en Jira y en `Backlog_Planeacion_Knock.xlsx`.
- Nota: TT-GES-01/02 y TT-DOC-01/02 están en el CSV pero no tienen spec en ningún repo.
- TT-API-01 (KNOCK-20): revisor Julián; decisiones del revisor registradas en su spec §8 (16 casos, AwesomeAssertions, NetArchTest.eNhancedEdition, xunit v2, Blob a TT-API-05, `check_endpoints.py` fuera de esta tarea, Docker lo inicia la persona, JWT con clave efímera). Rama `chore/KNOCK-20-aspnet-core-project` desde `sprint-00`, tras el commit base de la persona.
- Decisión: skill `testing-strategy-backend` y `docs/QA_DEVSECOPS.md` pasan a AwesomeAssertions y NetArchTest.eNhancedEdition (FluentAssertions 8.x tiene licencia propia; NetArchTest.Rules sin publicar desde 2021). KNOCK-41 (TT-QA-01, Sprint 1) lo formaliza.
- Pendiente (persona): `.github/dependabot.yml` agrupa `FluentAssertions`; añadir `AwesomeAssertions` (archivo protegido). `NetArchTest*` ya cubre el paquete nuevo.

## 2026-10-12 — Nueva regla de commits y atribución a IA (reemplaza la anterior)
- Decisión: commits **en inglés**, Conventional Commits `type(scope): description in english, imperative, lowercase (KNOCK-N)`; types feat, fix, docs, refactor, test, chore, ci, build, perf; scopes identity, catalog, requests, messaging, reputation, trust, notifications, assistant, app, panel, ci, infra, docs; clave obligatoria y igual a la de la rama. Ejemplo: `feat(requests): upload up to 5 photos with the request (KNOCK-45)`. Antes: descripción en español y `style|revert|sec` permitidos.
- Decisión: prohibido `Co-Authored-By` y cualquier atribución a IA en commits y PR; la IA se declara solo en «Uso de IA» del PR. Aplicado en: CONTRIBUTING, AGENTS.md, constitución (v2.0.1), plantilla de PR, hook `commit-msg`, `branch_rules.py` (CI), `.agents/knock.json`, punteros de Copilot/Cursor y `attribution` de Claude Code (`commit: ""`, `pr: ""`, `sessionUrl: false`; `includeCoAuthoredBy` está obsoleta).
- Decisión: ramas `<tipo>/KNOCK-N-english-kebab-description` (p. ej. `chore/KNOCK-20-aspnet-core-project`) desde el `sprint-XX` actual.
- Límite: ninguna herramienta puede verificar que la descripción esté en inglés ni que sea imperativa; se comprueba formato, minúscula inicial, longitud, clave y trailers prohibidos. Config de atribución de Codex, Gemini, Cursor y Copilot: no verificada; para ellos rige la instrucción en AGENTS.md/punteros y el hook.
- Pendiente (persona, `.github/**`): `contract-publish.yml` crea un commit en español `chore(deps): actualizar contrato…`; cambiarlo a inglés. `ci-pr.yml` puede pasar `--head "$HEAD_REF"` a `branch_rules.py commits` (por defecto usa `$GITHUB_HEAD_REF`).

## 2026-10-12 — Precisión: solo se bloquea la atribución a IA; hooks con selector de Python
- Decisión: el hook `commit-msg` y el CI bloquean solo atribución a IA (trailers `Co-Authored-By` con nombre o correo de Claude, Anthropic, Copilot, Codex, Gemini, Cursor, opencode, ChatGPT/OpenAI y líneas «Generated with…»). Un `Co-Authored-By` de una persona del equipo se permite (trabajo en parejas). Constitución v2.0.2.
- Decisión: `.githooks/lib-python.sh` es el selector común de Python (KNOCK_PYTHON, python3, python, `py -3`; descarta el alias de la Store) para `pre-commit`, `pre-push` y `commit-msg`.
- Excepción de bots (sin KNOCK-N), documentada en `branch_rules.py` y CONTRIBUTING: release-please `chore(main|release)`, Dependabot `build(deps)` y el commit automático del contrato de la API `chore(deps): update api contract to <sha>`.

## 2026-10-12 — KNOCK-19: estructura SDD, specs del Sprint 0 y mapa de código
- Hecho: estructura SDD (constitución v2.0.2, AGENTS.md, skills, agentes, guardián, scripts de gobierno) y las specs de gestión del Sprint 0 (KNOCK-15, 16, 17, 18, 19, 26, 27 y 28) refinadas y en Borrador. KNOCK-14 y KNOCK-33 son de gestión y no llevan spec. El resto de specs se publica por sprint (copias en `backup/`); `specs/README.md` lista solo las publicadas.
- Decisión: `codemap.py --check` ya no escribe; compara el mapa de `docs/architecture/` con el código sin contar fecha ni ruta y falla si está desactualizado, hay violaciones o clones nuevos. Sin `--check`, solo reescribe lo que cambió (LF). Porqué: el paso «Mapa de código actualizado» del CI fallaba siempre porque `--check` reescribía la fecha y la ruta local. El hook `pre-commit` ahora regenera y luego valida.
- A evitar: aprobar o firmar specs desde un agente; la aprobación (Estado, revisor y fecha) la escribe una persona.
- Pendiente: la condición de repos privados (dependency-review y SARIF) va en KNOCK-26.

## 2026-10-13 — KNOCK-27: grupo de recursos y presupuesto de Azure
- Hecho (lo ejecutó una persona; evidencia en `docs/evidencia/KNOCK-27-azure.md`): grupo `rg-knock-dev` en `northcentralus` y presupuesto `budget-knock-dev` de 10 USD al mes con alertas al 50, 80 y 100 % de lo gastado y al 100 % de lo previsto. Pendiente: roles mínimos (caso 3) y comprobar que llega el correo.
- Decisión: región `northcentralus`. Porqué: la suscripción Azure for Students solo permite `chilecentral`, `norwayeast`, `westus`, `francecentral` y `northcentralus` (ni `eastus2` ni `mexicocentral`); `northcentralus` ofrece Container Apps, PostgreSQL Flexible y Redis. Los datos quedan fuera de México: reflejarlo en el aviso de privacidad y consultarlo con el abogado.
- Decisión: PostgreSQL de desarrollo con `Burstable B1ms`; los tamaños General Purpose cuestan mucho más que el presupuesto. Se confirma al crear el servidor en KNOCK-34.
- A evitar: en PowerShell, `az ... --query "length(@)"` rompe el wrapper `az.cmd`; cuenta los resultados con `ConvertFrom-Json`. Si `$sub` está vacío, el `scope` sale como `/subscriptions//resourceGroups/...` y Azure responde «InvalidSubscriptionId».
- A evitar: subir IDs de suscripción, tenant o correos a los repositorios; van en variables de GitHub o en un `.env` local ignorado por git.

## 2026-10-13 — Borradores TT-BD-01 (KNOCK-22) y TT-BD-02 (KNOCK-23)
- Estado: specs TT-BD-01 y TT-BD-02 aprobadas por persona; aclaraciones posteriores de sus PRs documentadas sin modificar las aprobaciones ni firmas.
- Decisión del equipo (2026-10-13): Tula de Allende aplica al catálogo de KNOCK-23; la actualización de la decisión general del producto y de la ficha queda en un PR separado.
- Decisión técnica para TT-BD-02: polígonos piloto aproximados, etiquetados como no oficiales y documentados con autor/fecha; fallback por centro hasta 2 km; geometría `geography(...,4326)` y GiST conforme a las convenciones geoespaciales del repo; no persistir/loggear coordenadas de usuarios.
- Pendiente: verificar extracto SEPOMEX, CP de Tula, fecha/licencia y puntos de prueba. No se afirma haber encontrado ni verificado esos datos.
- A evitar: inventar CP, geometrías, coordenadas de usuarios, comandos de migración o detalles de esquema; no aprobar ni firmar las specs desde un agente.

## 2026-10-14 — KNOCK-26: CI de cada PR
- Hecho: workflows `ci-pr`, `_governance` y `ai-review` con su configuración (`.gitleaks.toml`, `.semgrep/`, `.semgrepignore`), `pin_actions.py`, `check_coverage.py` y `docs/QA_DEVSECOPS.md`. `docker-compose.ci.yml` y `nginx.ci.conf` quedan fuera hasta que exista la API: solo los usa el job `dast`, que se salta sin Dockerfile.
- Decisión: `ai-review` se omite (skipped) si falta el secreto `ANTHROPIC_API_KEY` o la variable `AI_REVIEW=true`; no falla ni afecta a `gate`. Dónde crearlos: CONTRIBUTING §8 (secreto y variable de repositorio, en cada repo). La cuenta está pendiente (unas 72 h).
- Decisión: en repos privados sin GitHub Advanced Security, `dependency-review` y la subida de SARIF se omiten; la variable `GHAS_ENABLED=true` las activa. Los escáneres no se quitan.
- Decisión: `codemap.py --check` ya no escribe (KNOCK-19), por eso el paso «Mapa de código actualizado» del CI deja de fallar siempre.
- Riesgo: la protección de ramas con `gate` como check requerido no se puede aplicar en repos privados de una cuenta gratuita (GitHub pide Pro o repo público).
- Riesgo (no verificado): OSV-Scanner puede devolver error en un repositorio sin archivos de dependencias; se confirma en la primera ejecución real.
- A evitar: dar por válido un workflow sin correrlo en GitHub; `actionlint` y `jscpd` locales no sustituyen la primera ejecución.
- Primera ejecución real del CI (PR de KNOCK-26): fallaron 3 jobs. Gitleaks por falta de permiso `pull-requests: read` (403 al listar los commits del PR); governance porque `actionlint` con `shellcheck` en el runner marcó SC2129 y SC2034 en `ci-pr.yml`; Semgrep con 4 hallazgos bloqueantes cuyo detalle no salía en el log (solo iba al SARIF). Arreglado: permisos del job `secrets`, redirecciones agrupadas, variable de bucle `_`, y Semgrep ahora escribe el SARIF con `--sarif-output` y deja el texto en el log.
- A evitar: `actionlint` local sin `shellcheck` no reproduce lo del CI; instala `shellcheck` (por ejemplo `pip install shellcheck-py`) antes de dar un workflow por válido.
- Semgrep (SAST) dio 4 hallazgos bloqueantes en el CI; 3 corregidos sin silenciar nada: `_governance.yml` y `ci-pr.yml` pasaban `${{ ... }}` directo a `run:` (ahora van por `env:` y se usa `"$VAR"`), y `check_coverage.py` ya no usa `xml.etree` (lee `line-rate` y `branch-rate` del elemento raíz con una expresión regular, sin dependencias). El cuarto está en `scripts/hooks/knock_guard.py` (subprocess con lista de argumentos, sin shell): pendiente de decidir entre excepción EXC-001 justificada o cambio de código.

## 2026-10-14 — KNOCK-28: proyecto de Firebase y autenticación por teléfono
- Hecho (lo configuró una persona; evidencia en `docs/evidencia/KNOCK-28-firebase.md`): proyecto `knock` (ID `knock-0`) en plan Spark, Analytics desactivado, proveedor Teléfono habilitado con 4 números de prueba ficticios, FCM V1 habilitada y acceso del equipo configurado.
- Pendiente: probar el inicio de sesión de punta a punta con un número de prueba (app o spike TT-API-02); confirmar cuota y costo de SMS reales.
- Decisión: los números de prueba existen solo en este proyecto de desarrollo; para producción se crea otro proyecto sin ellos. Los códigos de prueba no van al repositorio ni a tickets.
- A evitar: descargar llaves de cuenta de servicio o subir `google-services.json`, `GoogleService-Info.plist` o `firebase_options.dart` sin decidirlo (este último puede disparar gitleaks); usar teléfonos reales como números de prueba.

## 2026-10-14 — KNOCK-22: Compose local PostgreSQL/PostGIS
- Estado: entregable A completado en `feature/KNOCK-22-postgis-compose`; `src/Knock.Api` todavía no existe en `sprint-00`, por lo que EF Core, migración inicial, guardia de host remoto, readiness y Testcontainers quedan pendientes de TT-API-01 (KNOCK-20).
- Hecho: se creó `deploy/docker-compose.local.yml` con `postgis/postgis:16-3.4`, contraseña requerida por variable, puerto ligado a `127.0.0.1`, healthcheck y volumen nombrado; `.env.example` usa una contraseña vacía y `.env` ya está ignorado por Git. README documenta arranque, consulta de versión y reinicio que conserva los datos.
- Verificación: Docker Compose inició el servicio `postgres` saludable; `SELECT postgis_version()` devolvió `3.4 USE_GEOS=1 USE_PROJ=1 USE_STATS=1`. La comprobación usó un valor sintético temporal proporcionado solo al proceso; no se guardó en archivos.
- Decisión: dejar Redis fuera y no crear una configuración/Compose para TT-API-01; el alcance independiente del issue es solo PostgreSQL/PostGIS.
- A evitar: no usar el `.env.example` como archivo de entorno sin copiarlo a `.env` y completar la contraseña; Compose usa la variable obligatoria y falla si está ausente o vacía.

## 2026-10-14 — KNOCK-23: datos SEPOMEX y catálogo Tula
- Estado (actualizado 2026-10-15): entrega parcial en `feature/KNOCK-23-tula-catalog-data`; se versionaron el extracto SEPOMEX, el seed canónico de oficios, el validador y el GeoJSON piloto con ocho polígonos aproximados, no oficiales.
- Fuente consultada: exportación oficial Correos de México en TXT (ZIP), actualizada el 2026-10-13; contiene registros públicos de nombre de asentamiento y CP. El sitio no declara licencia abierta y señala que no se permite comercializar el catálogo, total o parcialmente; no asumir redistribución irrestricta del extracto.
- Verificado para evaluar candidatos piloto: 12 registros con tipo `Colonia` para Tula de Allende, Hidalgo, CP 42800 (Centro) y 42803 (El Cielito, San Lorenzo, El Llano 1a Sección, Iturbe, Tultengo, El Llano 2a Sección, Mastrantos, Bugambilias, Del Llano, Tierra Blanca y La Huerta). Estos registros no proporcionan límites geográficos.
- Hecho: `db/sepomex/tula_colonias.csv` contiene exactamente los 12 nombres/CP confirmados, sin importar el catálogo nacional. El README conserva la fuente/fecha y registra que la fuente no declara licencia abierta, prohíbe la comercialización total o parcial y deja pendiente la autorización de redistribución/revisión jurídica.
- Decisión: se fijan las claves estables `mec`, `plo`, `ele` y los registros canónicos activos Mecánica/taller, Plomería/domicilio y Electricidad/domicilio. `db/seeds/catalog/oficios.sql` hace upsert por `clave` para que reejecutarlo conserve esos valores activos.
- Hecho: `db/scripts/validate_colonias_tula.py` valida el contrato GeoJSON documentado en `db/seeds/README.md`, incluyendo propiedades, Polygon/MultiPolygon, cierre de anillos, coordenadas `[longitud, latitud]`, rangos y hemisferios. Por defecto no modifica la entrada; `--normalize-output` escribe una copia con Polygon convertido a MultiPolygon.
- Verificación del GeoJSON piloto: el validador aceptó 8 features y normalizó 8 Polygon a MultiPolygon; los ocho pares nombre/CP coinciden exactamente con SEPOMEX. En PostGIS temporal, un punto interior devolvió `Centro`, un punto exterior devolvió vacío y `ST_IsValid` fue verdadero para las ocho geometrías. No se creó migración. `sdd_lint --trace TT-BD-02` avisó que los nueve casos aún no tienen pruebas; `codemap --check`, `check_exceptions.py` y `git diff --check` pasaron.
- Decisión: no se implementó esquema ni migración. `src/Knock.Api` está ausente y la ruta prescrita para el modelo/migración es `src/Knock.Api/Modules/Catalog/Data/Migrations/`; crear SQL alternativo fuera de esa ruta arriesgaría una migración divergente. El seed SQL queda explícitamente condicionado a la futura migración de `catalog.oficios` con `id` generado y `clave` única.
- Bloqueos exactos: (1) se necesita `src/Knock.Api`/infraestructura de TT-API-01 (KNOCK-20) para modelo Catalog, migraciones EF Core, consultas PostGIS y pruebas DatabaseTest/Testcontainers; (2) equipo/revisión jurídica debe resolver el riesgo de licencia y redistribución antes de distribución comercial o pública. Las geometrías son aproximadas y no certifican límites oficiales; el validador comprueba estructura y hemisferios, no exactitud geográfica.
- A evitar: no sustituir la ausencia del proyecto API con una migración SQL paralela, no presentar los polígonos piloto como oficiales, ni presentar el extracto como de licencia abierta o redistribución irrestricta.
