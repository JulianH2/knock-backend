# plan.md — Spec 056 Autenticación entre servicios (KNOCK-162)

> **Estado:** Borrador. El plan se confirma al aprobar la spec; lo marcado «a confirmar» depende de §8 P-01.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `codemap.py --query`):**
  - `src/Knock.Market.Api/Hosting/InternalServiceAuthentication.cs` (única implementación del esquema; se toma como molde).
  - `src/Knock.Identity.Api/Modules/HomeAndCar/Services/MarketHistoryClient.cs` (emisión de la credencial de Identidad hacia Mercado).
  - Redactor de registros y respuestas ProblemDetails ya existentes en cada servicio.
- **Crear (a confirmar según P-01):**
  - Pieza compartida de validación y emisión de la credencial de servicio, si el líder la acepta en `Knock.Hosting` (spec 055) o una por servicio.
  - Receptor de `POST /api/v1/interno/avisos` en Mensajería con la regla común (su contrato es de la spec 037).
  - Pruebas de §3 en `tests/UnitTest/Hosting/`, `tests/SecurityTest/InternalAuth/`, `tests/IntegrationTest/Gateway/` y reglas de arquitectura en `tests/UnitTest/Architecture/`.
- **Modificar:** `InternalServiceAuthentication.cs` solo si P-01 lo extrae; `docs/endpoints.md` si cambia el esquema declarado; `Memoria.md`.
- **No tocar:** `.github/`, `scripts/`, `deploy/**` (Terraform y Key Vault son del líder, P-04), `jwt-signing-key` y la autenticación de usuario (spec 009), contratos de las specs 037 y 041, `docs/openapi.v1.json` a mano.

## 2. Riesgos y efectos secundarios
- **Convivencia con la 037 R-11 y la 041 R-17:** cambiar nombres de secretos o de claims rompe a Mensajería, Identidad y Mercado a la vez (P-06). Mitigación: regresión CA-09.
- **Rotación sin procedimiento (P-02):** cambiar una clave puede cortar las llamadas internas. No se implementa hasta decidirla.
- **Dependencia con la spec 055:** `Knock.Contracts` no admite lógica; compartir código exige el catálogo de `Knock.Hosting`.
- **Datos:** sin migraciones ni colecciones nuevas (salvo P-03). **Permisos:** solo se agregan restricciones, no se amplían. **Dependencias:** ningún paquete nuevo (se usan `Microsoft.IdentityModel.Tokens` y `JwtBearer`, ya presentes).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| CA-01 | `InvalidCredential_Returns401_CA_056_01` | SecurityTest |
| CA-02 | `Claims_AreValidated_CA_056_02` | UnitTest |
| CA-03 | `MissingPermission_Returns403_CA_056_03` | SecurityTest |
| CA-04 | `SchemesAreNotInterchangeable_CA_056_04` | SecurityTest |
| CA-05 | `IssuerKeyMismatch_Returns401_CA_056_05` | SecurityTest |
| CA-06 | `InvalidKey_FailsClosed_CA_056_06` | UnitTest |
| CA-07 | `InternalRoutes_AreNotRouted_CA_056_07` | IntegrationTest |
| CA-08 | `Secrets_DoNotAppear_CA_056_08` y `gitleaks` | SecurityTest · (comando) |
| CA-09 | Suites de las specs 037 y 041 sin editar | IntegrationTest · (comando) |
| CA-10, CA-11 | Pendientes de P-03 y P-02 | — |

## 4. Secuencia
1. Resolver §8 P-01 a P-06 con el líder y aprobar la spec (nada de código antes).
2. Contrato: confirmar propiedades de la credencial; actualizar `docs/endpoints.md` si cambia.
3. Dominio y servicio de validación y emisión + unitarias (CA-02, CA-06).
4. Receptores (Mensajería y ajuste de Mercado) + integración (CA-07, CA-09).
5. Seguridad negativa y arquitectura (CA-01, CA-03, CA-04, CA-05, CA-08).
6. Gates + `Memoria.md` + informe final.

## 5. Verificación (comandos exactos)
```bash
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest tests/DatabaseTest tests/ContractTest -c Release --no-build
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace 056
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
semgrep scan --config .semgrep/semgrep.yaml --error
```
