# TT-API-01 — Proyecto ASP.NET Core 10 con controllers, módulos vacíos, ProblemDetails, versionado /api/v1, health y Scalar

> **Jira:** KNOCK-20 · **Área:** API · Backend base · **Épica:** EP-13 · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S0 · **Puntos:** 3
> **Repositorio:** knock-backend · **Ficha:** TT-API-01 · **Rama:** `chore/KNOCK-20-aspnet-core-project` → PR a `sprint-00`
> **Depende de:** TT-REPO-01, TT-STD-01
> **Skills:** foundation-backend, vertical-slice-api, testing-strategy-backend, security-core, story-workflow
> **Estado:** Aprobada

## 1. Objetivo
Dejar la base del backend con los módulos ya separados para que cada historia se agregue en su módulo sin reorganizar nada. Terminada cuando la API compila, responde `/health` y tiene pruebas base.

## 2. Historia y casos de prueba
**TT-API-01** — Tarea técnica (no es historia de usuario). Los casos 01 a 14 se verifican con pruebas de código; el 15 y el 16 con comandos. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_01_KK`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-01-01 | Dada la API en ejecución, cuando se hace `GET /health`, entonces responde 200 (liveness) sin autenticación (V1 de la ficha). | `IntegrationTest/Health/HealthEndpointTests.Get_Health_Returns200_TC_TT_API_01_01` |
| TC-TT-API-01-02 | Dada la API sin dependencias externas registradas aún, cuando se hace `GET /health/ready`, entonces responde 200 (sin comprobación de Blob: pasa a TT-API-05). | `IntegrationTest/Health/HealthEndpointTests.Get_HealthReady_Returns200_TC_TT_API_01_02` |
| TC-TT-API-01-03 | Dada una comprobación de readiness de prueba que falla, cuando se hace `GET /health/ready`, entonces responde 503 mientras `/health` sigue en 200. | `IntegrationTest/Health/HealthEndpointTests.Get_HealthReady_FailingCheck_Returns503_TC_TT_API_01_03` |
| TC-TT-API-01-04 | Dado un endpoint de prueba que lanza una excepción no controlada, cuando se invoca, entonces responde 500 con `application/problem+json` (RFC 9457: `type`, `title`, `status`), sin `try/catch` en el controller (V2). | `IntegrationTest/Errors/ProblemDetailsTests.UnhandledException_ReturnsProblemDetails_TC_TT_API_01_04` |
| TC-TT-API-01-05 | Dado un entorno distinto de Development, cuando ocurre una excepción no controlada, entonces el ProblemDetails no incluye stack trace ni el mensaje interno de la excepción. | `SecurityTest/Errors/ErrorDetailTests.NonDevelopment_UnhandledException_HidesDetail_TC_TT_API_01_05` |
| TC-TT-API-01-06 | Dado el entorno Development, cuando ocurre una excepción no controlada, entonces el ProblemDetails incluye el detalle de la excepción. | `IntegrationTest/Errors/ProblemDetailsTests.Development_UnhandledException_IncludesDetail_TC_TT_API_01_06` |
| TC-TT-API-01-07 | Dada la regla «un módulo no referencia los internos de otro» (NetArchTest), cuando se evalúa el ensamblado de la API, entonces pasa para los 8 módulos. | `UnitTest/Architecture/ModuleBoundaryTests.Modules_DoNotReferenceOtherModulesInternals_TC_TT_API_01_07` |
| TC-TT-API-01-08 | Dado un tipo de prueba que viola la frontera (un módulo usa un interno de otro), cuando se aplica la misma regla, entonces la regla falla (V3: caso negativo, la regla se rompe a propósito en la prueba). | `UnitTest/Architecture/ModuleBoundaryTests.BoundaryRule_WithViolatingFixture_Fails_TC_TT_API_01_08` |
| TC-TT-API-01-09 | Dados Identity, Catalog, Requests, Messaging, Reputation, Trust, Notifications y Assistant, entonces cada uno expone una clase pública `AddXxxModule` y una interfaz pública, y el resto de sus tipos es `internal`. | `UnitTest/Architecture/ModuleSurfaceTests.EachModule_ExposesOnlyAddModuleAndInterface_TC_TT_API_01_09` |
| TC-TT-API-01-10 | Dado el versionado de Asp.Versioning, cuando se llama a un endpoint de prueba en `/api/v1/...`, entonces responde 200 y declara la versión 1.0 como soportada (`api-supported-versions`). | `IntegrationTest/Versioning/ApiVersioningTests.V1Route_ResolvesVersion1_TC_TT_API_01_10` |
| TC-TT-API-01-11 | Dado el entorno Development, cuando se pide el documento OpenAPI y `/scalar`, entonces ambos responden 200 (V4: además se abre Scalar en `https://localhost:<puerto>/scalar` en el navegador, con captura para el PR). | `IntegrationTest/OpenApi/ScalarTests.Development_ExposesOpenApiAndScalar_TC_TT_API_01_11` + captura (navegador) |
| TC-TT-API-01-12 | Dado un entorno distinto de Development, cuando se pide el documento OpenAPI o `/scalar`, entonces responde 404. | `SecurityTest/OpenApi/ScalarExposureTests.NonDevelopment_OpenApiAndScalar_Return404_TC_TT_API_01_12` |
| TC-TT-API-01-13 | Dada la API en ejecución, cuando atiende una petición, entonces Serilog escribe a consola una línea JSON válida. | `IntegrationTest/Logging/ConsoleJsonLoggingTests.Request_WritesJsonLineToConsole_TC_TT_API_01_13` |
| TC-TT-API-01-14 | Dados los `appsettings*.json` del proyecto, entonces no contienen secretos, cadenas de conexión con credenciales ni claves (los secretos van a user-secrets). | `SecurityTest/Secrets/AppSettingsSecretsTests.AppSettings_ContainNoSecrets_TC_TT_API_01_14` |
| TC-TT-API-01-15 | Dado el `Dockerfile`, cuando se construye, entonces es multi-etapa (compilación y ejecución separadas) y el contenedor arranca y responde `/health`. | `docker build` y `docker run` + `curl /health` (manual: la persona inicia Docker); evidencia en el PR |
| TC-TT-API-01-16 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios (cierra TC-TT-STD-01-04 en backend). | `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes` (comando); salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `Knock.slnx` y `src/Knock.Api` con `Program.cs` corto (registro en `Hosting/*Extensions.cs`), `Common/` y `Modules/<Modulo>/` para Identity, Catalog, Requests, Messaging, Reputation, Trust, Notifications y Assistant; cada módulo con clase pública `AddXxxModule` e interfaz pública, lo demás `internal`.
  - `AddControllers`, `AddProblemDetails` + `UseExceptionHandler` (sin `try/catch` en controllers), Asp.Versioning `/api/v{version}`, `AddOpenApi` + `MapOpenApi` y `MapScalarApiReference` solo en Development.
  - `/health` (vivo) y `/health/ready`; Serilog a consola en JSON.
  - Proyectos `tests/UnitTest` (con `Architecture/`), `tests/IntegrationTest` y `tests/SecurityTest`; xUnit v2 + `WebApplicationFactory`.
  - `Dockerfile` multi-etapa.
  - Paquetes: Asp.Versioning.Mvc, Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, Serilog.AspNetCore y, para pruebas, NetArchTest.eNhancedEdition y AwesomeAssertions (ver §8).
- **Out:**
  - Base de datos y `DbContext` (TT-BD-01).
  - Autenticación (TT-API-02); comprobación de Blob en `/health/ready` (TT-API-05).
  - `scripts/check_endpoints.py` (fuera de esta tarea por decisión del revisor).
  - Cualquier endpoint de negocio, evento u outbox.

## 4. Contratos API y eventos
| Método y ruta | Auth | Respuesta | Notas |
|---|---|---|---|
| `GET /health` | pública | 200 | Liveness; no depende de servicios externos. Fuera de `/api/v1`. |
| `GET /health/ready` | pública | 200 / 503 | Readiness; sin Blob en esta tarea (TT-API-05). Fuera de `/api/v1`. |
| `/api/v1/...` | — | — | Solo la base de versionado (`[ApiVersion("1.0")]`, `api/v{version:apiVersion}/<recurso>`); sin endpoints de negocio. |
| `GET /openapi/v1.json` y `GET /scalar` | pública | 200 en Development / 404 fuera | Ruta exacta del documento OpenAPI por confirmar al implementar. |

Errores: `application/problem+json` (RFC 9457). El endpoint que lanza excepción para los casos 04 a 06 existe solo en el host de pruebas, no en la API publicada. Eventos: ninguno.

## 5. Modelo de datos
— (sin tablas; la base de datos es TT-BD-01)

## 6. Seguridad, privacidad y amenazas
STRIDE breve (sin datos personales ni autenticación en esta tarea):

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Fuga de información: stack trace o detalle de errores fuera de Development | ProblemDetails sin detalle fuera de Development | TC-TT-API-01-05 |
| Fuga de información: OpenAPI y Scalar expuestos fuera de Development | `MapOpenApi` y `MapScalarApiReference` solo en Development | TC-TT-API-01-12 |
| Fuga de secretos en el repositorio | Nada de secretos en `appsettings.json`; user-secrets en local; gitleaks en CI | TC-TT-API-01-14 |
| Elevación / alteración: un módulo usa internos de otro y salta sus reglas | Prueba de arquitectura NetArchTest con caso negativo | TC-TT-API-01-07, TC-TT-API-01-08 |
| Denegación: `/health/ready` reporta sano estando caído | Readiness devuelve 503 si una comprobación falla | TC-TT-API-01-03 |
| Fuga hacia la IA: secretos, cadenas de conexión o datos reales en prompts | Regla de proceso (AGENTS.md §4); no pasar nada de eso a la IA | revisión de `security-reviewer` |

No aplican en esta tarea (sin datos ni sesión): autorización por recurso, rate limit, cifrado de campos, biométricos y archivos.

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**
- **Unitarias y arquitectura (`tests/UnitTest`, `Architecture/`):** Sí (casos 07, 08, 09), con caso negativo.
- **Integración (`tests/IntegrationTest`):** Sí, con `WebApplicationFactory` (casos 01-04, 06, 10, 11, 13). Sin Testcontainers: aún no hay base de datos.
- **Seguridad negativa (`tests/SecurityTest`):** Sí (casos 05, 12, 14).
- **Contrato (`tests/ContractTest`):** No aplica: no hay endpoints de negocio ni `docs/openapi.v1.json` que comparar; llega con la primera HU.
- **Base de datos:** No aplica (TT-BD-01).
- **Mutación:** No aplica: ningún módulo crítico tiene lógica todavía (metas futuras: Identity ≥ 80 %, Requests/Trust/Reputation ≥ 70 %).
- **Rendimiento, fuzzing, caos, E2E, UI:** No aplican (sin lógica ni endpoints de negocio).
- **Smoke:** el caso 15 hace de humo manual del contenedor.
- Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit, Integration y Security en verde, `sdd_lint.py --trace TT-API-01`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Evidencia en el PR: captura de Scalar y salida de `dotnet test`.

**Decisiones del revisor (Julián), registradas en `Memoria.md`:**
1. Son 16 casos TC (los de la tabla de §2).
2. AwesomeAssertions para las aserciones (no FluentAssertions).
3. NetArchTest.eNhancedEdition para arquitectura (no NetArchTest.Rules).
4. xunit v2.
5. La comprobación de Blob en `/health/ready` pasa a TT-API-05; aquí no se incluye.
6. `check_endpoints.py` queda fuera de esta tarea.
7. Docker lo inicia la persona.

La decisión «JWT con clave efímera» se traslada a TT-API-02, donde está la autenticación; no forma parte de esta spec.

**Límites y dudas (resueltas por el revisor en la revisión del PR):**
- **Estructura vs. ficha — Resuelto:** se acepta la de la spec (`Knock.slnx`, `src/Knock.Api`, `tests/UnitTest`, `IntegrationTest`, `SecurityTest`), no la de la ficha (`api/Knock.sln`, `tests/Knock.Api.Tests`).
- **Proyecto por módulo vs. carpeta — Resuelto:** carpetas en `src/Knock.Api/Modules/` y `Common/`, no un proyecto por módulo ni `SharedKernel`.
- **Contenido de `/health/ready` hoy — Resuelto:** sin comprobaciones por ahora (Blob pasa a TT-API-05 y la base de datos a TT-BD-01). No se agrega Redis.
- **Endpoint de prueba (casos 04 a 06 y 10) — Resuelto:** se acepta el controller que existe solo en el host de pruebas.
- **Sin `try/catch` en controllers — Resuelto:** se acepta sin caso propio; se cubre con Semgrep y con la revisión.
- **Dependabot (AwesomeAssertions) — Fuera de esta spec:** el revisor lo hace en un PR aparte (`.github/dependabot.yml` es archivo protegido); no bloquea esta spec.
- **Ruta del Dockerfile y del documento OpenAPI (`/openapi/v1.json`):** se confirman al implementar.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
