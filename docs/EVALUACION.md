# Evaluación del backend: respuestas verificadas

Fecha: 2026-11-17. Base: rama `sprint-05`. Cada respuesta indica dónde se comprueba en el repositorio. Las cifras de pruebas son **métodos de prueba** contados en el código (`[Fact]` y `[Theory]`); una `Theory` se ejecuta una vez por cada juego de datos, así que el número de casos ejecutados es mayor.

## Infraestructura y despliegue

| Pregunta | Respuesta | Dónde se comprueba |
|---|---|---|
| Nube | Azure: Container Apps (servicios y gateway), DocumentDB (MongoDB), Redis, Blob Storage, Key Vault, Container Registry y Application Insights; región West US. | `deploy/`, `specs/005`, `Memoria.md` |
| ¿Se despliega en la nube? | Sí, en Azure Container Apps: gateway, identity, messaging y market. | `.github/workflows/ci-deploy.yml`, `deploy/apps/main.tf` |
| Servicios externos fuera de Azure | Firebase Authentication de Google (verifica el token del celular; `FirebaseIdTokenVerifier`) y Firebase Cloud Messaging (avisos push, spec 037). Para el desarrollo: GitHub (código y CI) y Atlassian (Jira, solo lectura). La app se distribuye por Google Play. Azure OpenAI y la moderación de contenido son servicios de Azure. | `src/Knock.Identity.Api/Modules/Sesiones/Services/`, `specs/037` |
| Instancias del backend | Una réplica por servicio por defecto. Para la ventana de evaluación (25-nov al 5-dic) se aplica `deploy/apps/envs/evaluacion.tfvars`: **2 réplicas por servicio** (gateway, identity, messaging y market). Redis lleva una. | `deploy/apps/variables.tf`, `deploy/apps/envs/` |
| Load balancer | El ingreso de Azure Container Apps reparte entre réplicas; el gateway YARP enruta por servicio. | `specs/001`, `specs/006` |
| IaC | Terraform (`deploy/infra`, `deploy/database`, `deploy/apps` y `deploy/modules`). | `deploy/` |
| SSL | HTTPS termina en el ingreso de Container Apps; el gateway envía `Strict-Transport-Security`. Las llamadas entre servicios van por la red interna del entorno. | `specs/BUG-002` |
| API o microservicios | Microservicios con una puerta de entrada: Gateway, Identity, Messaging y Market. | `src/`, `specs/001` |
| CDN de imágenes | No hay CDN de pago. Las imágenes públicas se sirven desde un contenedor de Blob de lectura anónima con `Cache-Control` largo e inmutable; las fotos privadas viven en otra cuenta sin acceso anónimo. Motivo: Front Door cuesta 35 USD al mes y Azure CDN clásico ya no admite perfiles nuevos. | `Memoria.md` (ADR-007), `specs/032` |
| Observabilidad | OpenTelemetry con exportador a Azure Monitor (Application Insights), registros estructurados con Serilog y redacción de datos sensibles. | `src/*/Hosting/ObservabilityExtensions.cs`, `specs/011` |

## Datos y caché

| Pregunta | Respuesta | Dónde se comprueba |
|---|---|---|
| Bases de datos | MongoDB (DocumentDB) y Redis. Ambas son NoSQL; no hay base relacional. | `specs/003`, `db/` |
| Redis | Sí. Sesiones canceladas (Identity y gateway), contadores del límite de peticiones y, en la spec 026, respaldo de SignalR. | `src/Knock.Gateway/Hosting/RedisExtensions.cs`, `specs/003` |
| ¿JWT en Redis? | No se guarda el JWT. Redis guarda el identificador de las sesiones canceladas (`sid`) para que el gateway las rechace. | `src/Knock.Gateway/Hosting/RevokedSessions.cs`, `specs/016` |
| Caché | Perfil de la persona (60 s), catálogo de oficios con validador `ETag` y caché del cliente móvil en tres niveles. | `specs/010`, `specs/017` |
| Rate limit | En el gateway y en Identity y Market; el paso del contador a Redis en Identity y Market está en KNOCK-247. | `specs/006`, `docs/rate-limit-matrix.md` |
| Paginación | Sí, por cursor (`cursor` y `nextCursor`). | `specs/010`, `specs/027` |
| Índices | Scripts de índices y semillas en `db/`, incluidos los geoespaciales. | `db/`, `specs/003` |
| Serializadores JSON | `System.Text.Json`; el cliente de la app se genera desde el contrato OpenAPI. | `docs/openapi.v1.json` |
| Compresión del payload | `JsonCompressionMiddleware` con Brotli y gzip en **Identity y Market**. Nunca comprime las rutas de autenticación (riesgo BREACH). Gateway y Messaging no la llevan. | `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs` |
| Middleware | Encabezados de seguridad, correlación (`X-Correlation-Id`), redacción de registros, límite de peticiones y validación del token en el gateway. | `src/*/Hosting/` |
| Versionamiento | Versionamiento por puntos (spec 012) con etiquetas de Git `<componente>-vX.Y.Z` calculadas por `scripts/ci/version.py`; la API se versiona por ruta (`/api/v1`). | `specs/012`, `docs/VERSIONAMIENTO.md` |
| Logout | Sí: `POST /api/v1/auth/logout` cancela la sesión por `sid`. | `src/Knock.Identity.Api/Modules/Sesiones/Controllers/V1/LogoutController.cs` |

## Seguridad y flujo de trabajo

| Pregunta | Respuesta | Dónde se comprueba |
|---|---|---|
| Herramientas en CI | Gitleaks (secretos), Semgrep (SAST), Trivy y Dockle (imágenes), Checkov (IaC y Dockerfiles), OSV y `dotnet list package --vulnerable` (dependencias), oasdiff (cambios incompatibles del contrato), Stryker (mutación, informativa) y Dependabot. | `.github/workflows/ci-pr.yml`, `.github/dependabot.yml` |
| Herramientas en CD | Trivy y Dockle sobre cada imagen antes de publicarla, `az containerapp update` y pruebas de humo contra `/health`. | `.github/workflows/ci-deploy.yml` |
| Vulnerabilidades conocidas | BUG-002, BUG-003 y BUG-004 (encabezados del gateway y seguridad de Messaging) y BUG-005 (registros de Market) están corregidos en el código. BUG-001 (el job de DAST no se ejecuta) sigue abierto. La evidencia en vivo de BUG-002 a BUG-004 se captura tras el despliegue. Hay una sola llave de firma del JWT compartida entre servicios y sin autenticación entre ellos. | `specs/BUG-001` a `BUG-005`, `Memoria.md` |
| Excepciones de seguridad | Registradas con vencimiento en `security/exceptions.yaml`. | `security/exceptions.yaml` |
| MCP | Atlassian (Jira, lectura, OAuth por persona) y GitHub CLI. Configurado en `.mcp.json`. | `docs/mcp.md`, `specs/019` |
| Agentes de IA | 10 definiciones: api-builder, constitution-reviewer, db-builder, docs-keeper, implementer, plan-writer, security-reviewer, spec-writer, task-writer y traceability-clerk. | `.opencode/agents/` |
| Skills | 16: ai-features, api-contract-openapi, auth-firebase-jwt, database-scripts, devsecops-audit, events-outbox, foundation-backend, postgis-geo, realtime-signalr, reuse-before-create, security-core, spec-first-writing, story-workflow, testing-strategy-backend, traceability y vertical-slice-api. | `.agents/skills/` |
| Aprendizaje del proyecto | `Memoria.md` con una entrada por decisión, su porqué y qué evitar. | `Memoria.md` |

## Endpoints

- **Contrato:** 130 endpoints en `Knock_Endpoints_CRUD.xlsx`. Las 54 specs (`specs/001` a `054`) están aprobadas; la spec BUG-001 está en borrador.
- **Implementados al 2026-11-17:** 21 acciones de controlador (Identity 7 y Market 14) y 3 `/health`. El hub `/hubs/chat` de SignalR aún no existe.
- **Sin pruebas ni código:** los endpoints con spec que todavía no tienen controlador (Messaging, verificación, archivos, técnicos, opiniones, asistente y otros) no tienen pruebas, porque no existe el código que probar.

### Endpoints del Excel que las specs citan con otra ruta o ya no llevan

| ID del Excel | Ruta en el Excel | En las specs | Estado |
|---|---|---|---|
| COL-02 | `GET /colonias/por-punto` | `GET /api/v1/colonias/en-punto` (TT-API-07) | Implementado |
| COL-03 | `GET /colonias/por-codigo-postal/{cp}` | `GET /api/v1/zonas/{cp?}` (spec 023) | Implementado |
| LIS-01 | `POST /lista-espera` | `POST /api/v1/zonas/lista-espera` (spec 023) | Implementado |
| COT-04 | `POST /cotizaciones/{id}/elegir` | `POST /api/v1/solicitudes/{id}/elegir` (spec 021) | Implementado |
| VIS-08 | `POST /visitas/{id}/aviso-confianza` | `POST /api/v1/visitas/{id}/aviso` (spec 022) | Implementado |
| MSJ-03 | `GET /hubs/chat` | `/hubs/chat` (spec 026) | Con spec, sin código |
| VER-01, VER-02 | `POST /verificaciones`, `GET /verificaciones/me` | `POST` y `GET /api/v1/me/verificacion` (spec 025) | Con spec, sin código |
| ARC-01 | `POST /archivos/subida` | `POST /api/v1/archivos/subidas` (specs 013 y 020) | Con spec, sin código |
| CON-02 | `POST /me/contactos-confianza` | Se eliminó: los contactos de confianza no se guardan en el servidor (spec 042, D-04) | No aplica |
| COL-01 | `GET /colonias` | Sin spec que lo defina | Pendiente de decisión |

## Pruebas

| Proyecto | Métodos de prueba | Qué cubre |
|---|---|---|
| `tests/UnitTest` | 452 | Reglas de negocio, validadores y servicios |
| `tests/IntegrationTest` | 201 | Servicios completos con MongoDB, Redis y Azurite en Testcontainers |
| `tests/DatabaseTest` | 118 | Scripts de base de datos, índices y semillas |
| `tests/SecurityTest` | 97 | Pruebas de seguridad negativas |
| `tests/ArchitectureTest` | 5 | Fronteras entre módulos |
| `tests/ContractTest` | 3 | Contrato OpenAPI |

Otras pruebas: rendimiento con k6 en `tests/perf/` (caché de oficios y dos réplicas), mutación con Stryker (`stryker-config.json`, informativa; está en revisión por la actualización de la herramienta a 5.1.0, KNOCK-282) y humo contra `/health` después de cada despliegue.
