# plan.md — TT-SEC-01 — Secretos en Key Vault con identidad administrada; ningún secreto en el repositorio

> Borrador. Servicio afectado: Transversal (configuración, documentación y CI). Falta la aprobación de la spec y la respuesta del líder a las dudas de spec §8 (sobre todo la 1). Los comandos de Azure los ejecuta una persona; los agentes no operan Azure ni ven valores.

## 1. Mapeo de archivos
- **Reutilizar:**
  - `src/Knock.Api/Common/Security/JwtSettingsFactory.cs`, `src/Knock.Api/Hosting/MongoExtensions.cs` y la configuración de `Media`: ya leen los secretos de `IConfiguration`; con la opción A no se tocan.
  - `tests/SecurityTest/Secrets/` (`AppSettingsSecretsTests`, `JwtKeyConfigurationTests`) y `tests/SecurityTest/Hosting/KnockMongoSecretTests.cs`: patrón y ayudantes (`ApiProjectDirectory`).
  - `tests/UnitTest/Architecture/` y su ayudante para ubicar la raíz del repositorio (como `SecurityPipelineOrderTests`, TT-API-06).
  - `.gitleaks.toml` y el job «Secretos (Gitleaks)» de `ci-pr.yml` (solo se ejecutan; son protegidos).
  - `python scripts/codemap.py --query "secrets configuration jwt connection string"` antes de crear nada.
- **Crear:** `docs/SECRETOS.md`; `tests/SecurityTest/Secrets/SecretInventoryTests.cs` y `SecretsFailClosedTests.cs`; `tests/UnitTest/Architecture/SecretsDocumentationTests.cs`.
- **Modificar (no protegidos):** `README.md` y `CONTRIBUTING.md` (enlace a `docs/SECRETOS.md` y el uso de `dotnet user-secrets` con marcadores), `.env.example` (solo si falta alguna clave del inventario, con valor vacío), `Memoria.md`. Con la opción B de la duda 1, además `src/Knock.Api/Hosting/` (proveedor de Key Vault fuera de Development) y los `*.csproj` y `packages.lock.json` con su evaluación.
- **No tocar:** `.github/`, `scripts/`, `.gitleaks.toml` (si hace falta una regla, la agrega el Tech Lead), `deploy/` (los módulos son de TT-AZ-02; esta tarea solo fija el inventario de nombres), configuración de analizadores, constitución, `docs/openapi.v1.json`.

## 2. Riesgos y efectos secundarios
- **Orden:** el despliegue real (caso 07) necesita la implementación de TT-AZ-02 (Key Vault, identidades y roles); sin ella, los casos 01 a 06 y 09 se pueden cerrar en el repositorio y el 07 y 08 quedan para cuando la bóveda exista.
- **Nombres:** el inventario (§5 de la spec) es el contrato con el Terraform de TT-AZ-02: si ahí se escribe otro nombre de secreto, la aplicación no arranca. El caso 03 compara el documento con las claves que lee el código; el Tech Lead revisa que el Terraform use los mismos nombres.
- **Llaves del celular:** llegan con #75 (HU-001). El caso 02 las exige solo cuando #75 esté fusionada.
- **Historial:** el caso 06 puede encontrar hallazgos antiguos; cada uno se rota (si era real) o se registra con huella en `security/exceptions.yaml`. No se reescribe historia.
- **Evidencia sin valores:** las capturas de Azure se toman con `--query` sobre nombres y URL de secreto, nunca valores, y con la suscripción, el tenant y los correos ocultos.
- **Pruebas que leen archivos:** deben fallar con un mensaje claro si el archivo no existe (no pasar en vacío).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-SEC-01-01 | `SecurityTest/Secrets/SecretInventoryTests.NoInventoryKeyHasAValueInRepositoryFiles_TC_TT_SEC_01_01` | seguridad |
| TC-TT-SEC-01-02 | `SecurityTest/Secrets/SecretsFailClosedTests.ProductionWithoutAnySecretFailsNamingTheKey_TC_TT_SEC_01_02` | seguridad |
| TC-TT-SEC-01-03 | `UnitTest/Architecture/SecretsDocumentationTests.InventoryMatchesTheCodeKeysAndHasNoValues_TC_TT_SEC_01_03` | arquitectura |
| TC-TT-SEC-01-04 | `UnitTest/Architecture/SecretsDocumentationTests.SecretsAreReadOnlyFromConfiguration_TC_TT_SEC_01_04` | arquitectura |
| TC-TT-SEC-01-05 | gitleaks sobre una muestra y PR de prueba | comando (evidencia en el PR) |
| TC-TT-SEC-01-06 | gitleaks sobre todo el historial | comando (evidencia en el PR) |
| TC-TT-SEC-01-07 | `az containerapp show` de las referencias, `/health/ready` y logs de arranque | comando (una persona) |
| TC-TT-SEC-01-08 | ensayo de rotación en desarrollo | comando (una persona) |
| TC-TT-SEC-01-09 | build, formato y gobierno | comando (evidencia en el PR) |

## 4. Secuencia
1. Aprobación de la spec y respuesta del líder a las dudas (1 a 6); confirmar el estado de la implementación de TT-AZ-02.
2. `docs/SECRETOS.md` con el inventario, la rotación y la respuesta a una filtración; enlaces desde el README y `CONTRIBUTING.md`.
3. Pruebas de los casos 01 a 04 (ver fallar contra el estado actual lo que corresponda: 03 y 04 fallan hasta que existe el documento) y luego hacerlas pasar.
4. Casos 05 y 06: gitleaks sobre la muestra y sobre el historial; resolver hallazgos.
5. Con TT-AZ-02 desplegado: casos 07 y 08 con una persona.
6. Gates, `Memoria.md` y PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/SecurityTest -c Release --no-build --filter "FullyQualifiedName~Secrets"
dotnet test tests/UnitTest -c Release --no-build --filter "FullyQualifiedName~SecretsDocumentation"
dotnet test tests/UnitTest tests/IntegrationTest tests/SecurityTest tests/DatabaseTest -c Release --no-build   # requiere Docker
docker run --rm -v "$PWD:/repo" zricethezav/gitleaks:latest detect --source /repo --config /repo/.gitleaks.toml --log-opts "--all" --redact --no-banner
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-SEC-01
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
