# plan.md — TT-API-03 — Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query "ProblemDetails"` y `"rate limit correlation validation exception handler"`; sin coincidencias para correlación, límite ni validación):**
  - `src/Knock.Api/Hosting/ProblemDetailsExtensions.cs` (`AddKnockProblemDetails`) y `ProblemResponses` (en `RequestBodyLimitMiddleware.cs`): formato único de error; no se duplica.
  - `src/Knock.Api/Hosting/LoggingExtensions.cs` (Serilog JSON, `Enrich.FromLogContext`) y `UseSerilogRequestLogging`: solo se agrega la propiedad `CorrelationId`.
  - `src/Knock.Api/Hosting/KnockApplicationExtensions.cs` (`AddKnockApi`/`UseKnockApi`): aquí se registra y se ordena todo.
  - `RequestBodyLimitMiddleware` y `AuthenticationExtensions`: sin cambios.
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `TestController`, `ConsoleCapture`) y `tests/IntegrationTest/Errors/ProblemDetailsTests`, `tests/SecurityTest/Errors/ErrorDetailTests` como molde.
- **Crear (por confirmar al implementar):**
  - `src/Knock.Api/Hosting/CorrelationIdMiddleware.cs` (+ extensión de registro).
  - `src/Knock.Api/Hosting/DomainExceptionHandler.cs` (`IExceptionHandler`) y los tipos mínimos de excepción de dominio en `src/Knock.Api/Common/`.
  - `src/Knock.Api/Hosting/ValidationExtensions.cs` (filtro de validación a 422).
  - `src/Knock.Api/Hosting/RateLimitingExtensions.cs` (políticas global y `/auth`, 429 por `ProblemResponses`) y, si el revisor lo acepta, `ForwardedHeaders` (duda 2).
  - Documento del orden de middlewares en `docs/` (nombre por decidir, duda 7) y, si se acepta, `docs/rate-limit-matrix.md`.
  - Pruebas: `tests/UnitTest/Errors/`, `tests/IntegrationTest/{Validation,RateLimiting,Correlation,Logging,Errors,Hosting}/`, `tests/SecurityTest/{Validation,RateLimiting,Correlation,Errors,Logging}/` y controllers de prueba en `tests/IntegrationTest/Support/`.
- **Modificar:** `KnockApplicationExtensions.cs` (orden del pipeline), `appsettings.json` (valores del límite y proxies de confianza, sin secretos), `Knock.Api.csproj` y `packages.lock.json` **solo si el revisor aprueba paquetes** (duda 5, y tras evaluarlos y registrarlos en `Memoria.md`), `KnockApiFactory` solo si hace falta para no recibir 429 en las suites existentes (duda 9).
- **No tocar:** `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `ModuleSurfaceTests`, `ModuleBoundaryTests`, pruebas de TT-API-01/02 (siguen en verde sin editar), `docs/openapi.v1.json` a mano, specs ajenas, módulos de negocio.

## 2. Riesgos y efectos secundarios
- **Orden del pipeline:** un cambio de orden afecta todas las respuestas (correlación primero, manejador de excepciones antes del limitador, limitador antes de autenticar). Se prueba por comportamiento (caso 16).
- **Conflicto «por usuario» frente a §4:** el limitador va antes de `Authentication`, así que no conoce el `sub` (duda 3).
- **Rate limiting por proceso:** con 2 réplicas el límite efectivo se duplica; sin Redis (ADR-006). Riesgo aceptado o mover al gateway (duda 8).
- **IP real detrás de proxy:** `ForwardedHeaders` mal configurado deja evadir el límite (caso 06) o limita por la IP del proxy (todos los usuarios comparten partición).
- **Pruebas existentes:** el límite nuevo no debe dar 429 a las suites de TT-API-01/02 (duda 9). 422 frente a 400 en `/auth/firebase` puede romper TC-TT-API-02-11 si cambia (duda 6).
- **Paquetes:** evaluar y registrar antes de agregar; `NuGetAudit` rompe el restore con vulnerabilidades; `FluentValidation.AspNetCore` posiblemente sin mantenimiento (no consultado).
- **Fuga en logs:** el log de petición y las excepciones no deben registrar tokens, celulares ni coordenadas (caso 15).
- **ADR-001:** no separar servicios ni crear el gateway en esta tarea.
- Docker, Azure y Firebase los opera una persona; esta tarea no los necesita (los casos con `/auth/firebase` usan cuerpo vacío).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-API-03-01 | `IntegrationTest/Validation/ValidationFilterTests` | integración |
| TC-TT-API-03-02 | `SecurityTest/Validation/ValidationErrorLeakTests` | seguridad negativa |
| TC-TT-API-03-03 | `IntegrationTest/RateLimiting/AuthRateLimitTests` | integración |
| TC-TT-API-03-04 | `IntegrationTest/RateLimiting/GlobalRateLimitTests` | integración |
| TC-TT-API-03-05 | `IntegrationTest/RateLimiting/UserRateLimitTests` | integración |
| TC-TT-API-03-06 | `SecurityTest/RateLimiting/ForwardedHeadersEvasionTests` | seguridad negativa |
| TC-TT-API-03-07 | `SecurityTest/RateLimiting/RateLimitResponseLeakTests` | seguridad negativa |
| TC-TT-API-03-08 | `IntegrationTest/Correlation/CorrelationIdTests` | integración |
| TC-TT-API-03-09 | `IntegrationTest/Correlation/CorrelationIdTests` | integración |
| TC-TT-API-03-10 | `SecurityTest/Correlation/CorrelationIdInjectionTests` | seguridad negativa |
| TC-TT-API-03-11 | `IntegrationTest/Logging/CorrelationLoggingTests` | integración |
| TC-TT-API-03-12 | `IntegrationTest/Errors/ErrorResponseCorrelationTests` | integración |
| TC-TT-API-03-13 | `IntegrationTest/Errors/DomainExceptionHandlerTests` y `UnitTest/Errors/DomainExceptionMappingTests` | integración y unitaria |
| TC-TT-API-03-14 | `SecurityTest/Errors/DomainErrorDetailTests` | seguridad negativa |
| TC-TT-API-03-15 | `SecurityTest/Logging/SensitiveLogTests` | seguridad negativa |
| TC-TT-API-03-16 | `IntegrationTest/Hosting/PipelineOrderTests` | integración |
| TC-TT-API-03-17 | paquetes evaluados, documento del orden, `dotnet list package --vulnerable`, `gitleaks` | comando (evidencia en el PR) |
| TC-TT-API-03-18 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec y respuesta a las dudas 2, 3, 5, 6 y 8; evaluar y registrar paquetes (si se aprueban) en `Memoria.md`.
2. Contrato (spec §4) y pruebas que fallan primero (correlación, catálogo, validación, límite).
3. Middleware de correlación y propiedad en logs (casos 08 a 12, 15).
4. `IExceptionHandler` y excepciones de dominio (casos 13 y 14); regresión de TC-TT-API-01-04..06.
5. Filtro de validación a 422 (casos 01 y 02).
6. Rate limiter y `ForwardedHeaders` (casos 03 a 07); orden del pipeline y documento (casos 16 y 17).
7. Gates, `Memoria.md`, informe y evidencia en el PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
dotnet list Knock.slnx package --vulnerable --include-transitive
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-03
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
