# plan.md — TT-API-01 — Proyecto ASP.NET Core 10 con controllers, módulos vacíos, ProblemDetails, versionado /api/v1, health y Scalar

## 1. Mapeo de archivos
- **Reutilizar:** nada previo; aún no hay código (`python scripts/codemap.py --query "<tema>"` vacío). Respetar `Directory.Build.props` y `.editorconfig` de TT-STD-01.
- **Archivos de la tarea:**
  - `Knock.slnx`, `global.json`, `src/Knock.Api/Knock.Api.csproj`, `src/Knock.Api/Program.cs`.
  - `src/Knock.Api/Hosting/*Extensions.cs` (controllers, ProblemDetails, versionado, OpenAPI/Scalar, health, Serilog) y `src/Knock.Api/Common/`.
  - `src/Knock.Api/Modules/{Identity,Catalog,Requests,Messaging,Reputation,Trust,Notifications,Assistant}/` con `AddXxxModule` pública e interfaz pública (resto `internal`).
  - `tests/UnitTest/Architecture/`, `tests/IntegrationTest/`, `tests/SecurityTest/`.
  - `Dockerfile` multi-etapa (ruta por confirmar) y `appsettings.json` sin secretos.
- **No tocar:** `.github/`, `scripts/`, configuración de analizadores, constitución, `docs/openapi.v1.json` a mano (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Paquetes nuevos: evaluar licencia, mantenimiento y vulnerabilidades y registrarlos en `Memoria.md` (AwesomeAssertions y NetArchTest.eNhancedEdition ya decididos por el revisor).
- Las advertencias son errores: los analizadores de TT-STD-01 pueden fallar el primer build.
- `internal` por defecto oculta tipos a las pruebas: usar la interfaz pública o `InternalsVisibleTo` solo si se justifica.
- No subir secretos; user-secrets en local; nada de datos reales ni secretos a la IA.

## 3. Pruebas por caso
| Caso | Prueba | Tipo |
|---|---|---|
| TC-TT-API-01-01 | `IntegrationTest/Health/HealthEndpointTests` | integración |
| TC-TT-API-01-02 | `IntegrationTest/Health/HealthEndpointTests` | integración |
| TC-TT-API-01-03 | `IntegrationTest/Health/HealthEndpointTests` | integración |
| TC-TT-API-01-04 | `IntegrationTest/Errors/ProblemDetailsTests` | integración |
| TC-TT-API-01-05 | `SecurityTest/Errors/ErrorDetailTests` | seguridad negativa |
| TC-TT-API-01-06 | `IntegrationTest/Errors/ProblemDetailsTests` | integración |
| TC-TT-API-01-07 | `UnitTest/Architecture/ModuleBoundaryTests` | arquitectura |
| TC-TT-API-01-08 | `UnitTest/Architecture/ModuleBoundaryTests` | arquitectura (negativo) |
| TC-TT-API-01-09 | `UnitTest/Architecture/ModuleSurfaceTests` | arquitectura |
| TC-TT-API-01-10 | `IntegrationTest/Versioning/ApiVersioningTests` | integración |
| TC-TT-API-01-11 | `IntegrationTest/OpenApi/ScalarTests` + captura | integración + navegador |
| TC-TT-API-01-12 | `SecurityTest/OpenApi/ScalarExposureTests` | seguridad negativa |
| TC-TT-API-01-13 | `IntegrationTest/Logging/ConsoleJsonLoggingTests` | integración |
| TC-TT-API-01-14 | `SecurityTest/Secrets/AppSettingsSecretsTests` | seguridad negativa |
| TC-TT-API-01-15 | `docker build` + `docker run` + `curl /health` | manual / comando |
| TC-TT-API-01-16 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Contrato (spec §4) y estructura de la solución; módulos vacíos con `AddXxxModule`.
2. Pruebas de arquitectura (con caso negativo) y de `/health`, antes de la implementación.
3. `Program.cs` y `Hosting/`: ProblemDetails, versionado, health, Serilog, OpenAPI y Scalar solo en Development.
4. Pruebas de integración y seguridad negativa; Dockerfile.
5. Gates, `Memoria.md`, informe y evidencia (captura de Scalar y salida de `dotnet test`) en el PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-01
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py && python scripts/sync_agents.py --check
```
