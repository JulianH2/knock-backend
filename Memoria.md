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
- Estado: preparadas ambas specs con `plan.md`, `task.md` y `checklists/gates.md`, en Borrador y para revisión en PRs separados hacia `sprint-00`. El PR de KNOCK-22 es #5.
- Decisión confirmada por el usuario: Tula de Allende reemplaza Pachuca solo para el catálogo de KNOCK-23; no se modificaron documentos de referencia ni el alcance de la ciudad piloto general.
- Pendiente: determinar puntos, Skills y detalles técnicos abiertos en las specs; confirmar disponibilidad, procedencia/licencia y cobertura de polígonos SEPOMEX para Tula, sin inventar geometrías.
- A evitar: inventar puntos, geometrías, comandos de migración o detalles de esquema; no aprobar ni firmar las specs desde un agente.
