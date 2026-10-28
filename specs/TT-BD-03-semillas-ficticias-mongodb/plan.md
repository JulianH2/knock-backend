# plan.md — TT-BD-03 — Semillas ficticias en JSON para MongoDB: clientes de prueba

> Borrador. Servicio afectado: Identidad y confianza. Falta la aprobación de la spec; las preguntas P1–P9 ya tienen respuesta (spec §8, decisiones D1–D8 del 2026-10-28). Técnicos y solicitudes quedan fuera de alcance hasta que existan las specs de HU-017 y HU-024.

## 1. Mapeo de archivos
- **Reutilizar (buscar con `python scripts/codemap.py --query "seed loader json catalog encrypt phone"`):**
  - Patrón de `Modules/Catalog/Data/CatalogSeeder` (#73): lectura de JSON con `_id` fijos, validación previa, upsert y `version` solo si cambia el contenido.
  - Cifrado y hash del celular del módulo Identity (**implementación de HU-001, PR #75, sin fusionar**): mismo servicio, mismas llaves, mismos campos.
  - `UserIdDerivation.Derive(uid)` (`Modules/Identity/Services/`, TT-API-12) para el `sub`.
  - `tests/IntegrationTest/Support/DocumentDbFixture.cs` y `tests/DatabaseTest/Support/` (#73) para las pruebas de carga.
  - `AddKnockMongo` (`Hosting/`, #73) para pedir la base `identity`.
- **Crear (al implementar):**
  - `db/seeds/demo/identity/usuarios.json` (10 clientes).
  - Cargador de demostración en `Modules/Identity/Data/`, con la guarda de entorno (D3).
  - Pruebas: `tests/DatabaseTest/Demo/{DemoSeedFileTests,DemoSeedLoaderTests}.cs`, `tests/UnitTest/Architecture/DemoSeedBoundaryTests.cs`.
- **Modificar:** `db/seeds/README.md` (sección de demostración).
- **No tocar:** `db/seeds/catalog/*.json`, `db/seeds/colonias_tula.geojson`, `db/sepomex/`, archivos protegidos (`.github/`, `scripts/`, `security/`, `constitution.md`, `*.csproj` y `packages.lock.json`: sin dependencias nuevas, D7), specs de otras tareas.

## 2. Riesgos y efectos secundarios
- **HU-001 sin fusionar:** el cifrado del celular depende del código del PR #75, no solo de su spec; sin #75 fusionado no se empieza el cargador.
- **Orden:** sin #73 fusionado no hay patrón de cargador ni `DocumentDbFixture`.
- **Cifrado del celular:** un cargador que escriba el JSON tal cual dejaría el celular en claro; el caso 06 lo impide.
- **`sub`:** si se calculara distinto de TT-API-12, no coincidiría con el de una sesión; el caso 04 lo impide.
- **`_id` repetidos con el catálogo:** un upsert pisaría otro documento; el caso 01 lo prohíbe.
- **Nombres:** inventados no significa que no coincidan con alguien conocido; los revisa una persona en el PR.
- **`nombre`:** si HU-002 lo ubica en otra colección, se ajusta el JSON.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-BD-03-01 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos |
| TC-TT-BD-03-02 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos |
| TC-TT-BD-03-03 | `DatabaseTest/Demo/DemoSeedFileTests` | base de datos (seguridad negativa) |
| TC-TT-BD-03-04 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos |
| TC-TT-BD-03-05 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos |
| TC-TT-BD-03-06 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos (seguridad negativa) |
| TC-TT-BD-03-07 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos (seguridad negativa) |
| TC-TT-BD-03-08 | `UnitTest/Architecture/DemoSeedBoundaryTests` | arquitectura |
| TC-TT-BD-03-09 | conteos en `documentdb-local` tras dos cargas | comando (evidencia en el PR) |
| TC-TT-BD-03-10 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec; confirmar que #73 (TT-BD-06) y #75 (HU-001) están fusionados.
2. Pruebas de archivo (casos 01 a 03) que fallan primero; luego el JSON.
3. Cargador con `sub`, cifrado y guarda de entorno (casos 04 a 08).
4. `db/seeds/README.md`.
5. Carga dos veces en `documentdb-local` y conteos (caso 09); gates (caso 10); `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
docker compose -f deploy/docker-compose.local.yml up -d   # incluye documentdb-local (TT-BD-07)
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
