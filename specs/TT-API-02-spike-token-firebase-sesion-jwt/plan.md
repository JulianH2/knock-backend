# plan.md — TT-API-02 — Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT)

## 1. Mapeo de archivos
- **Reutilizar:** `src/Knock.Api/Modules/Identity/` (`IdentityModuleExtensions`, `IIdentityApi`), `Hosting/*Extensions.cs` (patrón de registro), `ProblemDetailsExtensions`, `tests/IntegrationTest/Support/KnockApiFactory.cs`, `tests/SecurityTest/Secrets/AppSettingsSecretsTests`. Antes de crear algo: `python scripts/codemap.py --query "auth jwt firebase"` (reuse-before-create). Respetar `Directory.Build.props` y `.editorconfig`.
- **Archivos de la tarea (por confirmar al implementar):**
  - `src/Knock.Api/Modules/Identity/Services/` (verificador del ID token con el Admin SDK, derivación del `sub` con GUID v5, emisor del JWT y del refresh token), todo `internal`.
  - Controllers `internal` de Identity (`POST /api/v1/auth/firebase`, `GET /api/v1/me`) con DTO y validador.
  - `src/Knock.Api/Hosting/AuthenticationExtensions.cs` (JwtBearer, `ClockSkew` cero, HS256, `iss`/`aud`), `ControllerFeatureProvider` que descubre controllers `internal`, límite del cuerpo de 8 KiB, constante del namespace del GUID v5 (`bcfea58b-95dd-4c0e-80d1-baf839f2e36e`, inmutable), y `UseAuthentication`/`UseAuthorization` en `KnockApplicationExtensions`.
  - `src/Knock.Api/Knock.Api.csproj` y `packages.lock.json`: `FirebaseAdmin` y `Microsoft.AspNetCore.Authentication.JwtBearer`, **solo tras evaluarlos y registrarlos** en `Memoria.md`.
  - Emulador de Firebase Auth: `npx firebase-tools` con proyecto `demo-` y `firebase-tools` 15.32.1 fijada (por evaluar y registrar; Java del emulador no verificado; el paso del CI lo agrega el revisor); las pruebas fallan con mensaje claro si el emulador no está disponible; soporte de pruebas en `tests/IntegrationTest/Support/`.
  - `tests/UnitTest/Identity/`, `tests/IntegrationTest/Identity/`, `tests/SecurityTest/Identity/` y `tests/SecurityTest/Secrets/JwtKeyConfigurationTests`.
  - `docs/evidencia/KNOCK-21-spike-firebase.md`.
- **No tocar:** `.github/` (el paso del CI para el emulador lo agrega una persona), `scripts/`, `.agents/`, configuración de analizadores, constitución, `ModuleSurfaceTests` y `ModuleBoundaryTests`, `docs/openapi.v1.json` a mano; pipeline de seguridad §4 (otra tarea); módulos distintos de Identity.

## 2. Riesgos y efectos secundarios
- Paquetes y herramienta nuevos (`FirebaseAdmin`, `JwtBearer`, `firebase-tools`): evaluar y registrar; `NuGetAudit` rompe el restore con vulnerabilidades (como pasó con Microsoft.OpenApi en TT-API-01).
- Endpoint público de canje sin rate limit: no desplegar hasta la tarea del pipeline de seguridad (condición en la nota del spike).
- El modo emulador acepta tokens sin firma: limitado a Development y pruebas (caso 14).
- Un `ControllerFeatureProvider` que descubre controllers `internal` cambia el descubrimiento de MVC para todo el ensamblado: el caso 06 lo comprueba y las pruebas de arquitectura de TT-API-01 deben seguir en verde.
- Secretos: la cuenta de servicio y la clave del JWT solo en user-secrets o variables; códigos de números de prueba y llaves nunca al repositorio, a la spec ni a la IA.
- Docker, Firebase y Azure los opera una persona.
- **Contingencia del emulador (tope de medio día):** si montar `npx firebase-tools` pasa de medio día, se cambia a un validador falso detrás de una interfaz (opción C), el caso 15 queda como evidencia contra `knock-0` y se avisa al revisor para ajustar los casos 01 a 04 y quitar el 14.

## 3. Pruebas por caso
| Caso | Prueba | Tipo |
|---|---|---|
| TC-TT-API-02-01 | `IntegrationTest/Identity/FirebaseSessionTests` | integración (emulador) |
| TC-TT-API-02-02 | `SecurityTest/Identity/FirebaseTokenRejectionTests` | seguridad negativa |
| TC-TT-API-02-03 | `SecurityTest/Identity/FirebaseTokenRejectionTests` | seguridad negativa |
| TC-TT-API-02-04 | `SecurityTest/Identity/FirebaseTokenRejectionTests` | seguridad negativa |
| TC-TT-API-02-05 | `IntegrationTest/Identity/MeEndpointTests` | integración |
| TC-TT-API-02-06 | `IntegrationTest/Identity/MeEndpointTests` (incluye reflexión: controller `internal` descubierto) | integración |
| TC-TT-API-02-07 | `UnitTest/Identity/SessionTokenIssuerTests` | unitaria |
| TC-TT-API-02-08 | `SecurityTest/Identity/SessionJwtValidationTests` | seguridad negativa |
| TC-TT-API-02-09 | `IntegrationTest/Identity/FirebaseSessionTests` | integración |
| TC-TT-API-02-10 | `UnitTest/Identity/UserIdDerivationTests` (vector conocido GUID v5) | unitaria |
| TC-TT-API-02-11 | `SecurityTest/Identity/FirebaseRequestValidationTests` | seguridad negativa |
| TC-TT-API-02-12 | `SecurityTest/Secrets/JwtKeyConfigurationTests` | seguridad negativa |
| TC-TT-API-02-13 | `SecurityTest/Identity/SensitiveDataLeakTests` | seguridad negativa |
| TC-TT-API-02-14 | `SecurityTest/Identity/FirebaseEmulatorGuardTests` | seguridad negativa |
| TC-TT-API-02-15 | canje real con número de prueba contra `knock-0` | comando (evidencia en el PR) |
| TC-TT-API-02-16 | nota `docs/evidencia/KNOCK-21-spike-firebase.md` | comando (evidencia en el PR) |
| TC-TT-API-02-17 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Obtener la aprobación; evaluar y registrar paquetes y `firebase-tools`; una persona agrega el paso del emulador al CI.
2. Contrato (spec §4) y pruebas que fallan primero (derivación del `sub`, JWT, validación de entrada).
3. Emisor del JWT, JwtBearer y `ControllerFeatureProvider`; `/me` protegido.
4. Verificador del ID token y endpoint de canje contra el emulador.
5. Pruebas de integración y seguridad negativa; caso 15 manual por una persona.
6. Nota del spike, gates, `Memoria.md`, informe y evidencia en el PR.

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
