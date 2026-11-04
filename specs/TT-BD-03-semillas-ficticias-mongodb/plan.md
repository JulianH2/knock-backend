# plan.md — TT-BD-03 — Semillas ficticias en JSON para MongoDB: clientes de prueba

> Aprobada (spec «Aprobación»: José Julián Hernández Hernández, 2026-11-02). Servicio afectado: Identidad y confianza. Decisiones en spec §8: D1–D8 (2026-10-28), D9 (2026-11-02) y D10–D16 (revisión de pares, 2026-11-03); D11 (punto de entrada) y D16 (Sprint 3) están pendientes de confirmar por el líder. Técnicos y solicitudes quedan fuera de alcance hasta que existan las specs de HU-017 y HU-024.

## 1. Mapeo de archivos
- **Reutilizar (buscar con `python scripts/codemap.py --query "seed loader json catalog encrypt phone"`):**
  - Patrón de `Modules/Catalog/Data/CatalogSeeder` (#73): lectura de JSON con `_id` fijos, validación previa, upsert y `version` solo si cambia el contenido. Diferencias (D10, D12): aquí el upsert es por `_id` y no por clave natural, y la comparación excluye `celularCifrado`. Tampoco hay precedente de arranque: `CatalogSeeder` solo se registra en el contenedor y lo invocan las pruebas.
  - Cifrado y hash del celular de HU-001 (**PR #75, sin fusionar**): `Modules/Identity/Services/PhoneProtector` con las llaves de `Identity:PhoneProtection` (`PhoneProtectionOptions`); `IdentityStore` (`Modules/Identity/Data/`) como único tipo con `IMongoCollection` (TC-HU-001-23).
  - `UserIdDerivation.Derive(uid)` (`Modules/Identity/Services/`, TT-API-12) para el `sub`.
  - `tests/IntegrationTest/Support/DocumentDbFixture.cs` y `tests/DatabaseTest/Support/` (#73) para las pruebas de carga.
  - `AddKnockMongo` (`Hosting/`, #73) para pedir la base `identity`.
- **Crear (al implementar):**
  - `db/seeds/demo/identity/usuarios.json` (10 clientes).
  - Cargador de demostración en `Modules/Identity/Data/` (D9), con la guarda de entorno (D3) y ejecutado al arrancar la API solo con `Seed:Demo = true` fuera de `Production` (D11, pendiente de confirmar).
  - Pruebas: `tests/DatabaseTest/Demo/{DemoSeedFileTests,DemoSeedLoaderTests}.cs` (casos 01–07 y 11), `tests/UnitTest/Architecture/DemoSeedBoundaryTests.cs`.
- **Modificar:** `db/seeds/README.md` (sección de demostración); `IdentityStore` (`Modules/Identity/Data/`, #75): método nuevo de upsert por `_id` fijo, coordinado con Guillermo y agregado cuando #75 esté fusionado (D9).
- **No tocar:** `db/seeds/catalog/*.json`, `db/seeds/colonias_tula.geojson`, `db/sepomex/`, archivos protegidos (`.github/`, `scripts/`, `security/`, `constitution.md`, `*.csproj` y `packages.lock.json`: sin dependencias nuevas, D7), specs de otras tareas.

## 2. Riesgos y efectos secundarios
- **HU-001 sin fusionar:** el cifrado del celular depende del código del PR #75, no solo de su spec; sin #75 fusionado no se empieza el cargador.
- **Calendario:** si #75 no se fusiona antes del 30-oct-2026, la tarea pasa al siguiente sprint (lo decide el equipo). #75 no se fusionó a tiempo: la implementación pasa al Sprint 3 si el líder lo confirma (D16).
- **Nonce aleatorio:** `PhoneProtector.Protect` cambia `celularCifrado` en cada corrida; comparar ese campo rompería el caso 05 o reescribiría cada documento. Se compara `celularHash` (D10).
- **Índices únicos de `celularHash` y `sub`:** un choque bajo otro `_id` falla antes de escribir, sin escrituras parciales (D12, caso 11).
- **Frontera de colecciones:** TC-HU-001-23 solo admite `IdentityStore` con `IMongoCollection`; el cargador escribe por él (D9), con el upsert dentro de `IdentityStore`.
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
| TC-TT-BD-03-09 | conteos en `documentdb-local` tras dos arranques de la API con `Seed:Demo = true` (D11; compose de TT-BD-07 o contenedor a mano, spec §8, fila «Smoke local») | comando (evidencia en el PR) |
| TC-TT-BD-03-10 | `dotnet build` + `dotnet format --verify-no-changes` | comando |
| TC-TT-BD-03-11 | `DatabaseTest/Demo/DemoSeedLoaderTests` | base de datos (seguridad negativa) |

## 4. Secuencia
1. Confirmación del líder de D11 y D16; confirmar que #75 (HU-001) está fusionado (#73 ya lo está) y coordinar con Guillermo el método nuevo de `IdentityStore` (D9).
2. Pruebas de archivo (casos 01 a 03) que fallan primero; luego el JSON.
3. Método nuevo de `IdentityStore` y cargador con `sub`, cifrado, guarda de entorno, choque de índices y arranque con `Seed:Demo` (casos 04 a 08 y 11).
4. `db/seeds/README.md`.
5. Dos arranques de la API contra `documentdb-local` y conteos (caso 09); gates (caso 10); `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
docker compose -f deploy/docker-compose.local.yml up -d   # documentdb-local cuando TT-BD-07 esté implementado (hoy es PostgreSQL)
# mientras tanto, a mano (misma imagen y digest que DocumentDbFixture):
docker run -d --rm --name documentdb-local -p 127.0.0.1:10260:10260 ghcr.io/microsoft/documentdb/documentdb-local@sha256:0fcf634531c1917ad0855ff9f4354aca0a5c5d8e435f08250568deb37eeb0ad5 --username knock --password "$KNOCK_DEMO_DB_PASSWORD"
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
