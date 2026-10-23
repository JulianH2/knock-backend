# plan.md — TT-BD-06 — Migración del catálogo a MongoDB: oficios, colonias y consulta de colonia por punto

> Borrador. Servicio afectado: Mercado. Las dudas (a)–(i) de spec §8 siguen abiertas; este plan asume las recomendaciones y se ajusta cuando el líder decida.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query "neighborhood colonia postgis health"`):**
  - `src/Knock.Api/Modules/Catalog/ICatalogApi.cs` y `CatalogModuleExtensions.cs`: el contrato no cambia; el registro cambia de `AddDbContext` a la conexión de MongoDB.
  - `src/Knock.Api/Modules/Catalog/Domain/Oficio.cs`, `Colonia.cs`: se adaptan (sin `Guid` ni tipos de NetTopologySuite).
  - Validación de `Domain/CatalogCoordinates.ToPoint` (rangos y mensajes sin el valor): se conserva la regla y se reescribe sin NetTopologySuite.
  - `src/Knock.Api/Hosting/HealthExtensions.cs` (`AddKnockHealth`, etiqueta `ready`): se cambia la comprobación de PostgreSQL por la de MongoDB.
  - `tests/Spike.DocumentDb/Support/{SpikeTargets,SpikeSeeds,SpikeQueries}.cs` (rama del spike): molde para la fixture, la semilla y la medición con `explain` (P3).
  - `tests/IntegrationTest/Support/{KnockApiFactory,ConsoleCapture}.cs`, `tests/UnitTest/Architecture/{ModuleCatalog,ModuleSurfaceTests,ModuleBoundaryTests}.cs`.
  - Datos fuente: `db/seeds/colonias_tula.geojson`, `db/sepomex/tula_colonias.csv`, `db/scripts/validate_colonias_tula.py` (sin cambios).
- **Crear (por confirmar al implementar):**
  - `src/Knock.Api/Hosting/MongoExtensions.cs` (o en `Common/`): `MongoClient` único, `Mongo:Database`, `Mongo:MaxPoolSize` validado, cadena desde `ConnectionStrings:Mongo`.
  - `src/Knock.Api/Modules/Catalog/Data/`: repositorio de oficios y colonias, creación idempotente de índices, cargador de la semilla.
  - `src/Knock.Api/Modules/Catalog/Domain/SearchNameNormalizer.cs`.
  - `db/seeds/catalog/colonias.json` y `db/seeds/catalog/oficios.json` (si se aprueba la duda (c)), con un generador en `db/scripts/` (no en `scripts/`, que es protegido).
  - Pruebas: `tests/DatabaseTest/Catalog/*`, `tests/DatabaseTest/Support/MongoFixture.cs`, `tests/IntegrationTest/Catalog/*`, `tests/SecurityTest/Catalog/MongoConnectionSecretTests.cs`, `tests/UnitTest/Catalog/*`, `tests/UnitTest/Architecture/MongoAccessTests.cs`.
- **Modificar:** `src/Knock.Api/Knock.Api.csproj` y los `.csproj` de `DatabaseTest`/`IntegrationTest`/`SecurityTest` con sus `packages.lock.json` (agregar `MongoDB.Driver` y `Testcontainers.MongoDb` **tras evaluarlos y registrarlos en `Memoria.md`**; quitar EF Core, Npgsql, NetTopologySuite y `Testcontainers.PostgreSql`); `appsettings.json` (solo `Mongo:Database` y `Mongo:MaxPoolSize`, nunca la cadena); `CatalogApi.cs` (reimplementación).
- **Borrar:** `Modules/Catalog/Migrations/`, `Data/{CatalogDbContext,CatalogDbContextFactory,LocalMigrationConnectionGuard,CatalogSeeder}.cs`, `PostgresReadinessHealthCheck`, `tests/DatabaseTest/{CatalogCoordinatesTests,CatalogNeighborhoodQueriesTests,CatalogTradeSeedTests}.cs` y `Support/CatalogDatabaseFixture.cs`, `tests/IntegrationTest/Data/{PostgisFixture,PostgisInfrastructureTests,CatalogLocationPrivacyTests}.cs`, `tests/IntegrationTest/Support/{CatalogTestDatabase,PostgisReadinessChecks}.cs`. `db/seeds/catalog/oficios.sql` solo con la decisión (e).
- **No tocar:** `.github/`, `scripts/` (incluido `sdd_lint.py`, duda (h)), `.agents/`, configuración de analizadores, `constitution.md`, `deploy/docker-compose.local.yml` (TT-ARQ-01), `db/seeds/colonias_tula.geojson` (duda (d)), `docs/openapi.v1.json`, specs de otras tareas, el módulo `Media` (duda (b)), el clúster `knock-docdb`.

## 2. Riesgos y efectos secundarios
- **Comportamiento del contrato:** el desempate interior/borde de PostGIS no existe en `$geoIntersects`; con los traslapes del seed el resultado puede cambiar en esas zonas (spec §5, duda (d)). Se prueba con TC-TT-BD-06-05.
- **Paridad con Azure:** el contenedor local no reproduce todo (P7 conexiones, P8 restauración, planificador de P5); por eso el humo contra `knock-docdb` (TT-BD-07) y la recomendación de una corrida manual.
- **Medición de rendimiento:** documentdb-local reporta `executionTimeMillis` en 0 ms y otro formato de `explain` (`explainVersion` 2, P9); la prueba debe leer ambos formatos y no tomar el total de ida y vuelta como criterio.
- **Fuga de secretos:** las excepciones del driver pueden traer el host; no registrar su mensaje crudo (lección del spike).
- **Pruebas existentes:** TC-TT-API-03-*, TC-TT-API-02-* y TC-TT-API-01-* usan `KnockApiFactory`; al quitar PostgreSQL, la fábrica debe configurar MongoDB y `/health/ready` sin romper sus casos (TC-TT-API-01-* de health).
- **TT-PRU-01 (#31):** si se fusiona antes, su lectura de oficios y su fixture pasan a MongoDB en esta tarea.
- **CI:** el job `sqlfluff` seguirá revisando `db/`; si se borra `oficios.sql` puede quedarse sin archivos (TT-BD-07, duda (e)).
- **ADR-001:** no separar el servicio ni mover `ICatalogApi` a `Knock.Contracts` aquí.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-BD-06-01 | `DatabaseTest/Catalog/CatalogSeedTests` | base de datos |
| TC-TT-BD-06-02 | `DatabaseTest/Catalog/CatalogSeedTests` | base de datos |
| TC-TT-BD-06-03 | `DatabaseTest/Catalog/CatalogIndexTests` | base de datos |
| TC-TT-BD-06-04 | `DatabaseTest/Catalog/NeighborhoodAtPointTests` | base de datos |
| TC-TT-BD-06-05 | `DatabaseTest/Catalog/NeighborhoodAtPointTests` | base de datos |
| TC-TT-BD-06-06 | `DatabaseTest/Catalog/NeighborhoodAtPointTests` | base de datos |
| TC-TT-BD-06-07 | `DatabaseTest/Catalog/CatalogCoordinatesTests` | base de datos |
| TC-TT-BD-06-08 | `DatabaseTest/Catalog/NeighborhoodsByPostalCodeTests` | base de datos |
| TC-TT-BD-06-09 | `IntegrationTest/Catalog/CatalogLocationPrivacyTests` | integración |
| TC-TT-BD-06-10 | `UnitTest/Catalog/SearchNameNormalizerTests` | unitaria |
| TC-TT-BD-06-11 | `DatabaseTest/Catalog/TradePrefixSearchTests` | base de datos (duda (g)) |
| TC-TT-BD-06-12 | `DatabaseTest/Catalog/NeighborhoodLookupPerformanceTests` | rendimiento |
| TC-TT-BD-06-13 | `UnitTest/Catalog/MongoConnectionOptionsTests` | unitaria |
| TC-TT-BD-06-14 | `SecurityTest/Catalog/MongoConnectionSecretTests` | seguridad negativa |
| TC-TT-BD-06-15 | `IntegrationTest/Catalog/MongoReadinessTests` | integración |
| TC-TT-BD-06-16 | `UnitTest/Architecture/MongoAccessTests` | arquitectura |
| TC-TT-BD-06-17 | `UnitTest/Catalog/CatalogContractTests` | arquitectura |
| TC-TT-BD-06-18 | `dotnet list package`, `--vulnerable`, gitleaks, ausencia de `Migrations/` | comando (evidencia en el PR) |
| TC-TT-BD-06-19 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec con las dudas (a)–(i) resueltas; evaluar `MongoDB.Driver` y `Testcontainers.MongoDb` y registrarlos en `Memoria.md`.
2. Fixture de Testcontainers y pruebas que fallan primero (casos 01 a 08, 10, 12, 16 y 17).
3. Conexión (`MongoExtensions`, `maxPoolSize`, secretos) y readiness (casos 13, 14 y 15).
4. Modelo, normalizador, índices y cargador de la semilla (casos 01, 02, 03 y 10); si (c)-1, generar `colonias.json` exportando el centro desde PostGIS **antes** de retirarlo.
5. Reimplementar `CatalogApi` sobre el repositorio (casos 04 a 09, 11, 12 y 17).
6. Retiro de EF Core, Npgsql, NetTopologySuite, migraciones, fixtures y pruebas de PostGIS (casos 16 y 18); si #31 ya está en `sprint-01`, migrar su lectura de oficios.
7. Gates, `Memoria.md` (decisiones y errores a evitar), informe y evidencia en el PR (casos 18 y 19).

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/DatabaseTest tests/IntegrationTest tests/SecurityTest -c Release --no-build   # requiere Docker
dotnet test tests/DatabaseTest -c Release --no-build --filter "FullyQualifiedName~NeighborhoodLookupPerformanceTests" --logger "console;verbosity=detailed"
dotnet list Knock.slnx package --include-transitive
dotnet list Knock.slnx package --vulnerable --include-transitive
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-BD-06
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
