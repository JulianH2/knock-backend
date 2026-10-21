# AGENTS.md — knock-backend

> Manual operativo para **cualquier agente de IA** (Claude Code, Codex, Copilot, Cursor, Gemini CLI, opencode…) y para las personas del equipo.
> Rector: `.specify/memory/constitution.md` (espejo `./constitution.md`). Porqué: `docs/DECISION_TECNICA.md`. Este archivo dice **cómo**.
> Este repositorio contiene la **API** (ASP.NET Core, microservicios con gateway), los **scripts de base de datos** (MongoDB) y la **infraestructura** (Terraform). La app móvil y el panel viven en `knock-frontend`.

## 0. Antes de tocar nada
1. Lee `Memoria.md` y la spec del issue: `specs/<ID>-*/spec.md` (índice en `specs/README.md`).
2. La spec debe estar **Aprobada** y tener la clave `KNOCK-<N>` de tu rama `<tipo>/KNOCK-<N>-<english-description>`. Si no, detente: refínala con `spec-writer` y pide aprobación.
3. Sigue la skill **`story-workflow`** (leer → amenazas → plan de pruebas → implementar → verificar → informe).
4. Antes de crear algo: `python scripts/codemap.py --query "<qué necesitas>"` (skill `reuse-before-create`).

## 1. Forma de trabajo
- 1 issue de Jira = 1 rama = 1 PR hacia la rama `sprint-XX` actual. Ramas, commits y PR según `CONTRIBUTING.md`: commits en inglés (`type(scope): description (KNOCK-N)`, p. ej. `feat(requests): upload up to 5 photos with the request (KNOCK-45)`), ramas `<tipo>/KNOCK-N-english-kebab-description`.
- Orden: contrato (spec §4 + OpenAPI) → índices y colecciones → dominio/servicio + unitarias → controller + integración, seguridad negativa y contrato → eventos → `scripts/export_openapi.py` → gates → `Memoria.md` → informe.
- Si cambias el contrato, dilo en el PR: `contract-publish.yml` abrirá el PR correspondiente en knock-frontend.

## 2. Comandos
```bash
# Infraestructura local (MongoDB, Redis, Azurite, emulador de Firebase Auth)
docker compose -f deploy/docker-compose.local.yml up -d
# API
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build            # incluye Architecture/
dotnet test tests/IntegrationTest -c Release --no-build     # requiere Docker
dotnet test tests/SecurityTest tests/DatabaseTest tests/ContractTest -c Release --no-build
dotnet run --project src/Knock.Api
dotnet ef migrations add <Nombre> --project src/Knock.Api --context <Modulo>DbContext
# Calidad y contrato
python scripts/export_openapi.py             # regenera docs/openapi.v1.json
dotnet tool run dotnet-stryker -- --config-file stryker-config.json --since:sprint-XX
k6 run tests/perf/smoke.js
# Gobierno (los mismos que corre el CI)
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace <ID>
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```

## 3. Memoria
- Al terminar una tarea agrega a `Memoria.md`: estado, decisiones (con porqué) y errores a evitar. Nunca datos sensibles.
- Al cerrar un issue concilia la spec con lo implementado (rutas reales de pruebas) y déjala lista para firma humana.

## 4. Límites — no hacer
- **Nunca** agregues atribución a IA en commits o PR: ni trailers `Co-Authored-By` de agentes (Claude, Copilot, Codex, Gemini, Cursor, opencode o `noreply@anthropic.com`), ni líneas «Generated with…». El uso de IA se declara solo en la sección «Uso de IA» de la plantilla del PR. Un `Co-Authored-By` de una persona del equipo (trabajo en parejas) sí se permite. El hook `commit-msg` y el CI rechazan lo primero.
- No leer ni escribir colecciones, Domain o Services de otro servicio o módulo (usa el contrato del servicio, `I<Modulo>Api` o eventos).
- No `try/catch` ni `DbContext` en controllers. No SQL interpolado. No estado de negocio en memoria del proceso.
- No editar scripts de índices o semillas ya fusionados, fixtures de contrato a mano ni `docs/openapi.v1.json` a mano.
- No desactivar pruebas, reglas o escáneres; no bajar umbrales; no suprimir sin `EXC-NNN`.
- No agregar paquetes sin evaluación; no tocar archivos protegidos (`.github/`, `scripts/`, configuración de analizadores, constitución).
- No operar Azure, producción ni bases reales; no usar datos reales (solo ficticios e INE de muestra).
- No enviar INE, CURP, dirección ni teléfono a Azure OpenAI.

## 5. Definición de terminado (gates)
- `build -c Release` 0 errores / 0 warnings; `dotnet format` sin cambios.
- Unit (con Architecture), Integration, Security, Database y Contract **en verde** (resultado real).
- Cada caso `TC-<ID>-KK` de este repo con prueba `_TC_<ID>_KK` (`sdd_lint.py --trace`).
- Cobertura ≥ 60 % líneas / 50 % ramas; mutación del módulo en su meta (nightly).
- `codemap.py --check`, `check_exceptions.py`, `sdd_lint.py`, `sync_agents.py --check` y Semgrep en OK.
- `docs/endpoints.md`, `docs/events.md`, `docs/rate-limit-matrix.md` y `docs/openapi.v1.json` al día.
- `security-reviewer` LIBERA, `traceability-clerk` sin drift, informe de `story-workflow` en el PR, `Memoria.md` al día.

## 6. Convenciones técnicas
- Controllers delgados (`[ApiVersion("1.0")]`, `api/v{version:apiVersion}/<recurso>`), servicios con la lógica, dominio con estados, `<Modulo>DbContext` con `HasDefaultSchema`.
- Eventos en pasado vía outbox; plazos (15 min, 24 h, 48 h, 7 d) como mensajes programados.
- Geo con `geography(...,4326)` + GiST; colonia por `ST_Contains` sobre la tabla propia.
- SignalR: hub por módulo, autorización por JWT, grupos por conversación.

## 7. Seguridad
Matriz en la skill `security-core` (OWASP Top 10 + API Top 10). Toda entrada se valida en el backend. Biométricos según la constitución §6. Excepciones solo por `security/exceptions.yaml`.

## 8. Pruebas
Skill `testing-strategy-backend`. Testcontainers con MongoDB; nunca bases en memoria; `TimeProvider` falso; builders; nombres con el caso TC.

## 9. Sub-agentes
Fuente en `.agents/agents/` (`scripts/sync_agents.py` los traduce para cada herramienta).

| Agente | Edita | Cuándo |
|---|---|---|
| `spec-writer` | `specs/**`, `Memoria.md` | Antes de implementar |
| `db-builder` | Data/Domain de módulos, `db/**`, pruebas de datos | Capa [BD] |
| `api-builder` | Todo el código | Capa [Back] |
| `security-reviewer` | **Nada** | Antes del PR |
| `traceability-clerk` | **Nada** | Antes del PR |
| `docs-keeper` | `**/*.md` | Al cerrar |

## 10. Estructura
| Ruta | Contenido |
|---|---|
| `src/` | Servicios (`Knock.Gateway`, `Knock.Identity.Api`, `Knock.Messaging.Api`, `Knock.Market.Api`, `Knock.Assistant.Api`) y `Knock.Contracts`; hoy `Knock.Api` mientras se crea TT-ARQ-01 |
| `tests/` | UnitTest (+Architecture), IntegrationTest, SecurityTest, DatabaseTest, ContractTest, perf (k6), fuzz (RESTler), chaos, smoke |
| `db/` | semillas, catálogo SEPOMEX, scripts, respaldos |
| `deploy/` | docker-compose local/CI, Terraform |
| `specs/` | specs por issue · `docs/` decisión técnica, contratos vivos, arquitectura, QA · `security/` excepciones |
| `scripts/` | gates, mapa de código, guardián de agentes, CI |

## 11. Archivos generados
`constitution.md`, `CLAUDE.md`, `GEMINI.md`, `.claude/`, `.opencode/`, `.codex/`, `.gemini/`, `.cursor/`, `.github/copilot-instructions.md`, `.github/agents/`, `.github/instructions/`, `.github/hooks/` se generan con `python scripts/sync_agents.py` desde `AGENTS.md`, la constitución y `.agents/`. `docs/architecture/*` lo genera `scripts/codemap.py`.

<!-- skills-index:start -->
## 12. Índice de skills, agentes y reglas (generado)

> Si tu herramienta no carga skills por sí sola, abre el `SKILL.md` indicado cuando la tarea coincida con su descripción.

| Skill | Cuándo usarla | Archivo |
|---|---|---|
| `ai-features` | Reglas para las funciones de IA de Knock (asistente Knock, resumen del chat, bitácora, borrador de cotización con Azure OpenAI y ML.NET para «Recomendadas» y rango de precio). Úsala en el módulo Assistant y en Reputation (ML). | `.agents/skills/ai-features/SKILL.md` |
| `api-contract-openapi` | Mantiene el contrato REST de Knock (OpenAPI 3.1, docs/endpoints.md, ProblemDetails, versionado) y regenera el cliente Dart. Úsala cuando agregues o cambies un endpoint. | `.agents/skills/api-contract-openapi/SKILL.md` |
| `auth-firebase-jwt` | Inicio de sesión de Knock con Firebase Phone Auth en la app y JWT propios en el backend (refresh rotativo, detección de reuso, blacklist en Redis). Úsala en el módulo Identity y al proteger endpoints. | `.agents/skills/auth-firebase-jwt/SKILL.md` |
| `database-scripts` | Modelo de datos, migraciones EF Core, semillas, catálogo SEPOMEX con polígonos PostGIS, respaldos y pruebas de base de datos de knock-backend. Úsala en tareas con capa [BD] (TT-BD-01..04, HU con tablas nuevas) y al escribir SQL. | `.agents/skills/database-scripts/SKILL.md` |
| `devsecops-audit` | Auditoría integral de calidad, pruebas, seguridad y DevSecOps de un repositorio de Knock por fases (inventario, propuesta priorizada, implementación incremental, verificación, informe). Úsala cuando pidan auditar el repositorio o revisar que los controles existen y funcionan. | `.agents/skills/devsecops-audit/SKILL.md` |
| `events-outbox` | Eventos, saga y plazos programados en Knock con MassTransit y outbox transaccional de EF Core (oleadas a 15 min, vencimiento a 48 h, gracia de 7 días, borrados programados). Úsala cuando una HU tenga pasos asíncronos o plazos. | `.agents/skills/events-outbox/SKILL.md` |
| `foundation-backend` | Estructura base de knock-backend (solución .NET 10, módulos vacíos, pipeline de seguridad fijo, health, docker-compose con PostGIS, Redis y Azurite, CI). Úsala en TT-API-01, TT-API-03, TT-BD-01, TT-QA-01 y cuando toques Program.cs, Hosting/ o la configuración base. | `.agents/skills/foundation-backend/SKILL.md` |
| `postgis-geo` | Búsqueda geográfica en Knock con PostgreSQL + PostGIS y EF Core (técnicos cercanos, radio de atención, colonia desde la tabla propia SEPOMEX). Úsala en Catalog, Requests (oleadas) y en el mapa. | `.agents/skills/postgis-geo/SKILL.md` |
| `realtime-signalr` | Chat y avisos en tiempo real de Knock con SignalR (+ Redis backplane) y FCM cuando el destinatario no está conectado. Úsala en el módulo Messaging y para avisos en vivo. | `.agents/skills/realtime-signalr/SKILL.md` |
| `reuse-before-create` | Obliga a buscar en el mapa de código de Knock antes de crear una clase, función, servicio, widget, componente o archivo nuevo, para reutilizar o extender lo existente y evitar duplicados. Úsala siempre que vayas a crear algo nuevo. | `.agents/skills/reuse-before-create/SKILL.md` |
| `security-core` | Matriz de seguridad y privacidad de Knock basada en OWASP Top 10 y OWASP API Security Top 10 (autenticación, autorización por recurso, entradas, datos sensibles y biométricos, archivos, sesiones, límites, secretos, cadena de suministro). Úsala al construir endpoints o pantallas y al revisar antes del PR. | `.agents/skills/security-core/SKILL.md` |
| `spec-first-writing` | Refina el borrador de spec de una historia (HU-NNN), tarea técnica (TT-ÁREA-NN) o defecto (BUG-NNN) de Knock antes de escribir código - alcance por capa, contrato, amenazas, verificación por caso y plan de pruebas. Úsala al iniciar cualquier issue o cuando pidan algo sin spec. | `.agents/skills/spec-first-writing/SKILL.md` |
| `story-workflow` | Flujo obligatorio de Knock para implementar cualquier historia, tarea técnica, bug, refactor o cambio de arquitectura - leer, analizar amenazas, planear pruebas por criterio, implementar, verificar de verdad e informar con honestidad. Úsala siempre antes de cambiar código. | `.agents/skills/story-workflow/SKILL.md` |
| `testing-strategy-backend` | Estrategia de pruebas de knock-backend - unitarias, arquitectura, integración con Testcontainers, seguridad negativa, base de datos, contrato, aceptación por caso TC, mutación con Stryker, fuzzing con RESTler, carga con k6, smoke y caos. Úsala al planear y escribir pruebas. | `.agents/skills/testing-strategy-backend/SKILL.md` |
| `traceability` | Verifica la trazabilidad spec ↔ código ↔ prueba de un issue de Knock (casos TC-<ID>-KK, endpoints, eventos, alcance) y reporta drift sin editar. Úsala antes de cada PR. | `.agents/skills/traceability/SKILL.md` |
| `vertical-slice-api` | Cómo construir una rebanada de backend en un módulo de Knock (migración, dominio, servicio, controller, DTOs, validadores, eventos y pruebas) respetando las fronteras del monolito modular. Úsala en cada historia o tarea con capa [Back]. | `.agents/skills/vertical-slice-api/SKILL.md` |

| Agente | Para qué | Archivo |
|---|---|---|
| `api-builder` | Implementa la capa [Back] de un issue aprobado de knock-backend (contrato, dominio, servicio, controller, eventos y pruebas) dentro de su módulo, siguiendo story-workflow. | `.agents/agents/api-builder.md` |
| `db-builder` | Implementa la capa [BD] de un issue aprobado de knock-backend (modelo, migraciones EF Core, semillas, catálogo SEPOMEX/PostGIS, scripts y respaldos) con sus pruebas de base de datos. | `.agents/agents/db-builder.md` |
| `docs-keeper` | Mantiene vivos los Markdown de Knock (README, docs, índice de specs y Memoria) al cerrar un issue, sin tocar código. | `.agents/agents/docs-keeper.md` |
| `security-reviewer` | Audita sin editar la seguridad y privacidad del cambio de un issue de Knock (OWASP Top 10 y API Top 10, amenazas de la spec, datos sensibles, cadena de suministro) y entrega PASS/FAIL; un FAIL bloquea. | `.agents/agents/security-reviewer.md` |
| `spec-writer` | Refina el borrador de spec de un issue de Knock (HU, TT o BUG) - clave de Jira, verificación por caso, alcance por capa, contrato, amenazas y plan de pruebas - sin tocar código. Úsalo antes de implementar. | `.agents/agents/spec-writer.md` |
| `traceability-clerk` | Reporta sin editar el drift entre la spec, el código y las pruebas de un issue de Knock antes del PR. | `.agents/agents/traceability-clerk.md` |

| Regla | Aplica a | Archivo |
|---|---|---|
| `backend-csharp` | `src/**/*.cs`, `src/**/*.csproj` | `.agents/rules/backend-csharp.md` |
| `database` | `db/**`, `src/**/Migrations/**`, `src/**/Data/**` | `.agents/rules/database.md` |
| `specs` | `specs/**` | `.agents/rules/specs.md` |
| `tests-backend` | `tests/**` | `.agents/rules/tests-backend.md` |
<!-- skills-index:end -->
