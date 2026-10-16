# plan.md — TT-API-02 — Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT)

## 1. Mapeo de archivos
- **Reutilizar:** `src/Knock.Api/Modules/Identity/` (`IdentityModuleExtensions`, `IIdentityApi`), `Hosting/*Extensions.cs` (patrón de registro), `ProblemDetailsExtensions`, `tests/IntegrationTest/Support/KnockApiFactory.cs`, `tests/SecurityTest/Secrets/AppSettingsSecretsTests`. Antes de crear algo: `python scripts/codemap.py --query "auth jwt firebase"` (reuse-before-create). Respetar `Directory.Build.props` y `.editorconfig`.
- **Archivos de la tarea (por confirmar al implementar; sujetos a las dudas 1, 2 y 12 de la spec):**
  - `src/Knock.Api/Modules/Identity/Services/` (verificador del ID token con el Admin SDK, emisor del JWT, almacén de usuarios del spike), todo `internal`.
  - Endpoints `POST /api/v1/auth/firebase` y `GET /api/v1/me` (ubicación según duda 12) con sus DTO y validador.
  - `src/Knock.Api/Hosting/AuthenticationExtensions.cs` (JwtBearer, `ClockSkew` cero, HS256) y `UseAuthentication`/`UseAuthorization` en `KnockApplicationExtensions`.
  - `src/Knock.Api/Knock.Api.csproj` y `packages.lock.json`: `FirebaseAdmin` y `Microsoft.AspNetCore.Authentication.JwtBearer`, **solo tras evaluarlos y registrarlos** en `Memoria.md`.
  - `tests/UnitTest/Identity/`, `tests/IntegrationTest/Identity/`, `tests/SecurityTest/Identity/` y `tests/SecurityTest/Secrets/JwtKeyConfigurationTests`.
  - La nota del spike (ruta según duda 10).
- **No tocar:** `.github/`, `scripts/`, configuración de analizadores, constitución, `docs/openapi.v1.json` a mano (`AGENTS.md §4`); pipeline de seguridad §4 (otra tarea); módulos distintos de Identity (usar `IIdentityApi` o eventos).

## 2. Riesgos y efectos secundarios
- Paquetes nuevos: evaluar y registrar; `NuGetAudit` rompe el restore con vulnerabilidades (como pasó con Microsoft.OpenApi en TT-API-01).
- Endpoint público de canje sin rate limit: no desplegar hasta la tarea del pipeline de seguridad.
- El modo emulador de Firebase acepta tokens sin firma: debe quedar limitado a Development y pruebas.
- Un controller público en Identity puede romper `ModuleSurfaceTests` (duda 12).
- Secretos: la cuenta de servicio y la clave del JWT solo en user-secrets o variables; el código de los números de prueba y las llaves nunca al repositorio, a la spec ni a la IA.
- Docker, Firebase y Azure los opera una persona.

## 3. Pruebas por caso
| Caso | Prueba | Tipo |
|---|---|---|
| TC-TT-API-02-01 | `IntegrationTest/Identity/FirebaseSessionTests` | integración |
| TC-TT-API-02-02 | `SecurityTest/Identity/FirebaseTokenRejectionTests` | seguridad negativa |
| TC-TT-API-02-03 | `SecurityTest/Identity/FirebaseTokenRejectionTests` | seguridad negativa |
| TC-TT-API-02-04 | `SecurityTest/Identity/FirebaseTokenRejectionTests` | seguridad negativa |
| TC-TT-API-02-05 | `IntegrationTest/Identity/MeEndpointTests` | integración |
| TC-TT-API-02-06 | `IntegrationTest/Identity/MeEndpointTests` | integración |
| TC-TT-API-02-07 | `UnitTest/Identity/SessionTokenIssuerTests` | unitaria |
| TC-TT-API-02-08 | `SecurityTest/Identity/SessionJwtValidationTests` | seguridad negativa |
| TC-TT-API-02-09 | `IntegrationTest/Identity/FirebaseSessionTests` | integración |
| TC-TT-API-02-10 | `SecurityTest/Identity/FirebaseRequestValidationTests` | seguridad negativa |
| TC-TT-API-02-11 | `SecurityTest/Secrets/JwtKeyConfigurationTests` | seguridad negativa |
| TC-TT-API-02-12 | `SecurityTest/Identity/SensitiveDataLeakTests` | seguridad negativa |
| TC-TT-API-02-13 | `SecurityTest/Identity/FirebaseEmulatorGuardTests` | seguridad negativa (condicional, duda 1) |
| TC-TT-API-02-14 | canje real con número de prueba | manual, evidencia en el PR |
| TC-TT-API-02-15 | nota del spike | revisión del PR |
| TC-TT-API-02-16 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Resolver las dudas de la spec y obtener la aprobación; evaluar y registrar los paquetes.
2. Contrato (spec §4) y pruebas que fallan primero (JWT y validación de entrada).
3. Emisor del JWT y autenticación JwtBearer; `/me` protegido.
4. Verificador del ID token y endpoint de canje; alta del usuario según la duda 2.
5. Pruebas de integración y seguridad negativa; caso 14 manual por una persona.
6. Nota del spike, contrato vivo (duda 9), gates, `Memoria.md`, informe y evidencia en el PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-02
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py && python scripts/sync_agents.py --check
```
