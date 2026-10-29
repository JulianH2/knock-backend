# plan.md — TT-API-12 — `sub` v8 con SHA-256

> Spec Aprobada (2026-10-26) e implementada en el PR #71. Decisiones previas en spec §8 (decisión 1 de HU-001, D8 y D10); sin dudas de diseño abiertas.

## 0. Cuándo y en qué orden
Va **primero**: fusiona antes que HU-001 (KNOCK-43) y antes de que exista ningún dato guardado. No depende de TT-BD-06 ni de MongoDB. Implementación en Sprint 2.

## 1. Mapeo de archivos
- **Reutilizar** (buscado con `python scripts/codemap.py --query "UserIdDerivation"`; único consumidor en `src/`: `FirebaseSessionService`):
  - `src/Knock.Api/Modules/Identity/Services/UserIdDerivation.cs` (se modifica el algoritmo; mismo nombre, firma y `Namespace`).
  - `Services/FirebaseSessionService.cs` y `Services/SessionTokenIssuer.cs`: sin cambios.
  - `tests/UnitTest/Identity/UserIdDerivationTests.cs` (molde), `tests/IntegrationTest/Identity/FirebaseSessionTests.cs`, `tests/IntegrationTest/Support/{KnockApiFactory,FirebaseEmulator,ConsoleCapture}` y `tests/SecurityTest/Identity/SensitiveDataLeakTests.cs`.
- **Crear:** solo métodos nuevos de prueba: en `UserIdDerivationTests` (casos 02, 03, 04 y 07), `FirebaseSessionTests.IssuedSubIsTheUuidV8OfTheUid_TC_TT_API_12_05` y `SensitiveDataLeakTests.ExchangeDoesNotLogOrReturnUidOrSub_TC_TT_API_12_06`. Ningún archivo ni paquete nuevo.
- **Modificar:** `UserIdDerivation.cs` (SHA-256 del framework, versión 8; se borra `Sha1` y su documentación; se actualiza el `<summary>`) y `UserIdDerivationTests.cs` (vectores v8 y «versión 8»; los métodos conservan `_TC_TT_API_02_10` además de `_TC_TT_API_12_KK`).
- **No tocar:** la spec ni la evidencia de TT-API-02, `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `ModuleSurfaceTests`, `ModuleBoundaryTests`, `docs/openapi.v1.json`, `SessionTokenIssuerTests`, `MeEndpointTests` y las pruebas de TT-API-02 no listadas (deben pasar sin cambios), cualquier archivo de MongoDB o de HU-001.

## 2. Riesgos y efectos secundarios
- **Valor del `sub`:** cambia para todo `uid`; no hay cuentas guardadas, por eso esta tarea va antes. Si un `sub` v5 llegara a persistirse (p. ej. HU-001 fusionada antes), habría que migrarlo: HU-001 depende de esta tarea.
- **Analizadores:** CA5350 y S4790 prohíben SHA-1; SHA-256 de `System.Security.Cryptography` no necesita excepción. No se agrega `EXC-NNN` ni supresión.
- **Orden de bytes del namespace:** un error de endianness da un UUID válido pero distinto; lo cubren los vectores independientes (caso 01).
- **Pruebas de TT-API-02:** solo `UserIdDerivationTests` cambia; el resto compara el `sub` entre sí o con `Derive` y no debería romperse (caso 07). El sufijo `_TC_TT_API_02_10` se conserva para que `sdd_lint --trace TT-API-02` no marque drift (spec firmada, no se edita).
- **Emulador de Firebase:** los casos 05, 06 y la regresión de integración y seguridad lo necesitan en marcha; si no está, fallan con mensaje claro (no se saltan). Docker no se necesita.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-API-12-01 | `UnitTest/Identity/UserIdDerivationTests` (vectores; conserva `_TC_TT_API_02_10`) | unitaria |
| TC-TT-API-12-02 | `UnitTest/Identity/UserIdDerivationTests` | unitaria |
| TC-TT-API-12-03 | `UnitTest/Identity/UserIdDerivationTests` | unitaria |
| TC-TT-API-12-04 | `UnitTest/Identity/UserIdDerivationTests` | unitaria |
| TC-TT-API-12-05 | `IntegrationTest/Identity/FirebaseSessionTests` | integración |
| TC-TT-API-12-06 | `SecurityTest/Identity/SensitiveDataLeakTests` | seguridad negativa |
| TC-TT-API-12-07 | `UnitTest/Identity/UserIdDerivationTests` (firma) y regresión de TC-TT-API-02-01..14 | unitaria y regresión |
| TC-TT-API-12-08 | `grep` de SHA-1 en `src/` y `check_exceptions.py` | comando (evidencia en el PR) |
| TC-TT-API-12-09 | `dotnet list package --vulnerable`, `gitleaks` | comando (evidencia en el PR) |
| TC-TT-API-12-10 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Spec aprobada (2026-10-26); vectores recalculados con una implementación independiente.
2. Pruebas que fallan primero (casos 01 a 04 con vectores v8).
3. Reemplazar el algoritmo en `UserIdDerivation`; borrar el SHA-1.
4. Casos 05 y 06; regresión de TT-API-02.
5. Gates, informe y evidencia en el PR (casos 08 a 10); `Memoria.md` (la escribe quien hace el cambio).

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
# emulador de Firebase Auth en otra terminal: npx firebase-tools@15.34.0 emulators:start --only auth --project demo-knock
# y FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099
dotnet test tests/IntegrationTest tests/SecurityTest -c Release --no-build
grep -rniE "sha-?1\b|0x67452301|0xC3D2E1F0" src --include=*.cs
dotnet list Knock.slnx package --include-transitive --vulnerable
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-12 && python scripts/sdd_lint.py --trace TT-API-02
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
