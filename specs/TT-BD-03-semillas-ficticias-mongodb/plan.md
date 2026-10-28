# plan.md — TT-BD-03 — Semillas ficticias en JSON para MongoDB: clientes, técnicos y solicitudes de prueba

> Borrador. Servicios afectados: Identidad y confianza y Mercado. Falta la aprobación de la spec y las respuestas P1–P9 de spec §8; las rutas y colecciones de abajo son propuesta.

## 1. Mapeo de archivos
- **Reutilizar (buscar con `python scripts/codemap.py --query "seed loader json catalog encrypt phone"`):**
  - Patrón de `Modules/Catalog/Data/CatalogSeeder` (#73): lectura de JSON con `_id` fijos, validación previa, upsert y `version` solo si cambia el contenido.
  - `db/seeds/catalog/colonias.json` y `oficios.json` (#73): `_id` de colonias piloto y oficios activos; no se editan.
  - Cifrado y hash del celular del módulo Identity (HU-001): mismo servicio, mismas llaves, mismos campos.
  - `tests/IntegrationTest/Support/DocumentDbFixture.cs` y `tests/DatabaseTest/Support/` (#73) para las pruebas de carga.
  - `AddKnockMongo` (`Hosting/`, #73) para pedir las bases `identity` y `market`.
- **Crear (por confirmar al implementar):**
  - `db/seeds/demo/identity/usuarios.json`, `db/seeds/demo/identity/tecnicos.json`, `db/seeds/demo/market/solicitudes.json`.
  - Cargador de demostración en `Modules/Identity/Data/` y en el `Data/` del módulo de solicitudes (P3), con la guarda de entorno.
  - Pruebas: `tests/DatabaseTest/Demo/{DemoSeedFileTests,DemoSeedLoaderTests}.cs`, `tests/UnitTest/Architecture/DemoSeedBoundaryTests.cs`.
- **Modificar:** `db/seeds/README.md` (sección de demostración).
- **No tocar:** `db/seeds/catalog/*.json`, `db/seeds/colonias_tula.geojson`, `db/sepomex/`, archivos protegidos (`.github/`, `scripts/`, `security/`, `constitution.md`, `*.csproj` y `packages.lock.json` salvo dependencia aprobada en P7), specs de otras tareas.

## 2. Riesgos y efectos secundarios
- **Modelos sin spec (P1):** si HU-002, HU-017 o HU-024 cambian la forma, los JSON se rehacen.
- **Orden:** sin #73 fusionado no hay `colonias.json` ni `oficios.json` que referenciar.
- **Cifrado del celular:** un cargador que escriba el JSON tal cual dejaría el celular en claro; el caso 06 lo impide.
- **Staging (P2):** si no existe, el caso 09 no se puede cerrar; no se declara terminado sin esa evidencia o sin la decisión del líder.
- **`_id` repetidos entre archivos:** un `_id` de demostración igual a uno del catálogo haría que un upsert pisara el otro; el caso 01 lo prohíbe.
- **Nombres:** inventados no significa que no coincidan con alguien conocido; los revisa una persona en el PR.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-BD-03-01 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos |
| TC-TT-BD-03-02 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos |
| TC-TT-BD-03-03 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos |
| TC-TT-BD-03-04 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos (seguridad negativa) |
| TC-TT-BD-03-05 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos |
| TC-TT-BD-03-06 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos (seguridad negativa) |
| TC-TT-BD-03-07 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos (seguridad negativa) |
| TC-TT-BD-03-08 | `UnitTest/Architecture/DemoSeedBoundaryTests` | arquitectura |
| TC-TT-BD-03-09 | conteos en Staging y captura del mapa | comando (evidencia en el PR) |
| TC-TT-BD-03-10 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec y respuestas P1–P9; confirmar que #73 está fusionado.
2. Pruebas de archivos (casos 01 a 04) que fallan primero; luego los JSON.
3. Cargadores con guarda de entorno y cifrado (casos 05 a 08).
4. `db/seeds/README.md`.
5. Carga en Staging (o en el entorno que decida P2) y evidencia (caso 09); gates (caso 10); `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/DatabaseTest -c Release --no-build --filter "FullyQualifiedName~Demo"   # requiere Docker
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-BD-03
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
