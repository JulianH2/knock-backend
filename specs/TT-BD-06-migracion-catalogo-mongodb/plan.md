# plan.md — TT-BD-06 — Migración del catálogo a MongoDB: oficios, colonias y consulta de colonia por punto

> Borrador. Servicio afectado: Mercado. Dudas (a)–(j) de spec §8 resueltas por el líder en #53 (2026-10-24); queda por confirmar la propuesta de imagen de (a). Incluye la revisión de #53 (Guillermo y el líder): cliente de Mongo compartido en `Hosting/`, consulta interna con `{ id, nombre, cp, municipio }` y `NeighborhoodExistsAsync`. Falta la aprobación de la spec.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query "neighborhood colonia postgis health"`):**
  - `src/Knock.Api/Modules/Catalog/ICatalogApi.cs` y `CatalogModuleExtensions.cs`: las tres firmas no cambian y se agrega `NeighborhoodExistsAsync` (decisión (j)); el registro cambia de `AddDbContext` a pedir la base `market` a `AddKnockMongo`.
  - `src/Knock.Api/Modules/Catalog/Domain/Oficio.cs`, `Colonia.cs`: se adaptan (sin `Guid` ni tipos de NetTopologySuite).
  - Validación de `Domain/CatalogCoordinates.ToPoint` (rangos y mensajes sin el valor): se conserva la regla y se reescribe sin NetTopologySuite.
  - `src/Knock.Api/Hosting/HealthExtensions.cs` (`AddKnockHealth`, etiqueta `ready`): se cambia la comprobación de PostgreSQL por un `ping` a MongoDB, registrado una vez por `AddKnockMongo`.
  - Patrón de la cadena `ConnectionStrings:KnockBlob` de TT-API-05 (variable de entorno o user-secrets) para `ConnectionStrings:KnockMongo`.
  - `tests/Spike.DocumentDb/Support/{SpikeTargets,SpikeSeeds,SpikeQueries}.cs`: molde para la fixture (con el digest de la decisión (a)), la semilla y la medición con `explain` (P3).
  - `tests/IntegrationTest/Support/{KnockApiFactory,ConsoleCapture}.cs`, `tests/UnitTest/Architecture/{ModuleCatalog,ModuleSurfaceTests,ModuleBoundaryTests}.cs`.
  - Datos fuente, sin cambios: `db/seeds/colonias_tula.geojson`, `db/sepomex/tula_colonias.csv`, `db/scripts/validate_colonias_tula.py`.
- **Crear (por confirmar al implementar):**
  - `src/Knock.Api/Hosting/MongoExtensions.cs` (`AddKnockMongo`): `MongoClient` único por proceso, `Mongo:MaxPoolSize` (20) validado, cadena desde `ConnectionStrings:KnockMongo`, base por módulo con `Mongo:Databases:<Módulo>` (Catalog → `market`) y `ping` de readiness. Sin `IMongoCollection` (eso sigue en `Data/`).
  - `src/Knock.Api/Modules/Catalog/Data/`: repositorio de oficios y colonias, creación idempotente de índices, cargador de la semilla (valida `modalidad`).
  - Consulta interna de colonia por punto en `Modules/Catalog/` (`internal`, nombre por confirmar) que devuelve `{ id, nombre, cp, municipio }`; `CatalogApi.FindNeighborhoodAtAsync` delega en ella.
  - `CatalogApi.NeighborhoodExistsAsync` (por `_id` y `cp`, con validación previa de la entrada).
  - `src/Knock.Api/Modules/Catalog/Domain/SearchNameNormalizer.cs`.
  - `db/seeds/catalog/colonias.json` (12 colonias, `_id` fijos, `nombreBusqueda`, centro exportado de PostGIS para las 8 con polígono) y `db/seeds/catalog/oficios.json` (`_id` fijos), con un generador de una sola vez en `db/scripts/` (no en `scripts/`, que es protegido).
  - Pruebas: `tests/DatabaseTest/Catalog/*` (incluidos `NeighborhoodSeedTests` y `NeighborhoodExistsTests`), `tests/DatabaseTest/Support/MongoFixture.cs`, `tests/IntegrationTest/Catalog/*`, `tests/IntegrationTest/Hosting/MongoReadinessTests.cs`, `tests/SecurityTest/Hosting/KnockMongoSecretTests.cs`, `tests/UnitTest/Catalog/*`, `tests/UnitTest/Hosting/{KnockMongoOptionsTests,KnockMongoDatabasesTests}.cs`, `tests/UnitTest/Architecture/MongoAccessTests.cs`.
- **Modificar:** `src/Knock.Api/Knock.Api.csproj` y los `.csproj` de `DatabaseTest`/`IntegrationTest`/`SecurityTest` con sus `packages.lock.json` (agregar `MongoDB.Driver` y `Testcontainers.MongoDb` **tras evaluarlos y registrarlos en `Memoria.md`**; quitar EF Core, Npgsql, NetTopologySuite y `Testcontainers.PostgreSql`); `appsettings.json` (solo `Mongo:Databases:Catalog` y `Mongo:MaxPoolSize` = 20, nunca la cadena); `CatalogApi.cs` (reimplementación).
- **Borrar:** `Modules/Catalog/Migrations/`, `Data/{CatalogDbContext,CatalogDbContextFactory,LocalMigrationConnectionGuard,CatalogSeeder}.cs`, `PostgresReadinessHealthCheck`, `tests/DatabaseTest/{CatalogCoordinatesTests,CatalogNeighborhoodQueriesTests,CatalogTradeSeedTests}.cs` y `Support/CatalogDatabaseFixture.cs`, `tests/IntegrationTest/Data/{PostgisFixture,PostgisInfrastructureTests,CatalogLocationPrivacyTests}.cs`, `tests/IntegrationTest/Support/{CatalogTestDatabase,PostgisReadinessChecks}.cs`, y `db/seeds/catalog/oficios.sql` (**autorización explícita del líder en #53, 2026-10-24; AGENTS.md §4**).
- **No tocar:** `.github/`, `scripts/` (incluido `sdd_lint.py`: lo actualiza el líder, decisión (h)), `.agents/`, configuración de analizadores, `constitution.md`, `deploy/docker-compose.local.yml` (TT-ARQ-01), `db/seeds/colonias_tula.geojson` y `db/sepomex/tula_colonias.csv` (decisión (d): sin corregir polígonos), `docs/openapi.v1.json`, specs de otras tareas (incluido el estado de TT-BD-01 y TT-BD-02), el módulo `Media` e `IMediaFileStore` (decisión (b): tarea «Colección archivos de Media»), el clúster `knock-docdb`.

## 2. Riesgos y efectos secundarios
- **Orden del retiro:** el centro de las 8 colonias con polígono se exporta desde PostGIS **antes** de borrar EF Core, Npgsql y el compose con PostGIS; si se borra primero, hay que reconstruir PostGIS para generar el JSON (decisión (c), caso 21).
- **Comportamiento del contrato:** el desempate interior/borde de PostGIS no existe en `$geoIntersects`; en los 4 traslapes del seed gana el menor `nombreBusqueda` de forma permanente (spec §5, decisión (d)). Se prueba con TC-TT-BD-06-05 y TC-TT-BD-06-19 (una sola consulta para `ICatalogApi` y para TT-API-07).
- **Colonias sin polígono:** si el cargador escribe `geom` o `centro` como `null` en lugar de omitirlos, los índices `2dsphere` pueden fallar o la colonia podría salir por punto; caso 11.
- **`NeighborhoodExistsAsync`:** convertir la entrada a `ObjectId` antes de consultar; un filtro armado con el texto permitiría `{"$ne":null}` (caso 20).
- **Cliente compartido:** un solo `MongoClient` y un solo `maxPoolSize` (20) por proceso para todos los módulos; Catalog e Identidad compiten por el mismo pool mientras sea un solo `Knock.Api` (decisión (i)). Registrar el cliente en un módulo en vez de `Hosting/` obligaría a Identidad a tocar Catalog (TC-TT-BD-06-16 y 18).
- **`modalidad`:** el `$jsonSchema` de colección no está probado en DocumentDB; hasta TT-BD-07 la única barrera es el cargador.
- **Paridad con Azure:** el contenedor local (build de 2025-06-17) no reproduce todo (P7 conexiones, P8 restauración, planificador de P5); por eso el humo contra `knock-docdb` (TT-BD-07) y la recomendación de una corrida manual.
- **Medición de rendimiento:** documentdb-local reporta `executionTimeMillis` en 0 ms y otro formato de `explain` (`explainVersion` 2, P9); la prueba debe leer ambos formatos y no tomar el total de ida y vuelta como criterio.
- **Fuga de secretos:** las excepciones del driver pueden traer el host; no registrar su mensaje crudo (lección del spike). El generador de los JSON no imprime la cadena de PostGIS.
- **Logs:** el driver no escribe por `ILogger`; TC-TT-BD-06-09 prueba con los niveles configurados y sin suscriptores de comandos. La query HTTP con `Microsoft.AspNetCore` en `Information` es de TT-API-07 (#54).
- **Pruebas existentes:** TC-TT-API-03-*, TC-TT-API-02-* y TC-TT-API-01-* usan `KnockApiFactory`; al quitar PostgreSQL, la fábrica debe configurar MongoDB y `/health/ready` sin romper sus casos (TC-TT-API-01-* de health).
- **`sdd_lint --trace`:** al borrar las pruebas de TT-BD-01/02 falla hasta que el líder fusione su cambio a `sdd_lint.py` (decisión (h)); el PR de implementación va después.
- **TT-PRU-01 (#31):** si se fusiona antes, su lectura de oficios y su fixture pasan a MongoDB en esta tarea.
- **CI:** al borrar `oficios.sql`, el job `sqlfluff` puede quedarse sin archivos y fallar o pasar vacío (TT-BD-07).
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
| TC-TT-BD-06-11 | `DatabaseTest/Catalog/NeighborhoodSeedTests` | base de datos |
| TC-TT-BD-06-12 | `DatabaseTest/Catalog/NeighborhoodLookupPerformanceTests` | rendimiento |
| TC-TT-BD-06-13 | `UnitTest/Hosting/KnockMongoOptionsTests` | unitaria |
| TC-TT-BD-06-14 | `SecurityTest/Hosting/KnockMongoSecretTests` | seguridad negativa |
| TC-TT-BD-06-15 | `IntegrationTest/Hosting/MongoReadinessTests` | integración |
| TC-TT-BD-06-16 | `UnitTest/Architecture/MongoAccessTests` | arquitectura |
| TC-TT-BD-06-17 | `UnitTest/Catalog/CatalogContractTests` | arquitectura |
| TC-TT-BD-06-18 | `UnitTest/Hosting/KnockMongoDatabasesTests` | unitaria |
| TC-TT-BD-06-19 | `DatabaseTest/Catalog/NeighborhoodAtPointTests` | base de datos |
| TC-TT-BD-06-20 | `DatabaseTest/Catalog/NeighborhoodExistsTests` | base de datos (con entradas hostiles) |
| TC-TT-BD-06-21 | `dotnet list package`, `--vulnerable`, gitleaks, ausencia de `Migrations/` y `oficios.sql`, salida del generador contra PostGIS, digest de la imagen | comando (evidencia en el PR) |
| TC-TT-BD-06-22 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec (dudas (a)–(j) resueltas; confirmar la imagen de (a)); evaluar `MongoDB.Driver` y `Testcontainers.MongoDb` y registrarlos en `Memoria.md`.
2. **Antes de retirar nada:** correr el generador de `db/scripts/` contra PostGIS local y versionar `colonias.json` (12 colonias, `_id` fijos, centro exportado) y `oficios.json`; guardar su salida para el PR (caso 21).
3. Fixture de Testcontainers con el digest de (a) y pruebas que fallan primero (casos 01 a 08, 10, 11, 12, 16 a 20).
4. Conexión compartida (`AddKnockMongo` en `Hosting/`: `maxPoolSize` 20, secreto, base por módulo) y readiness (casos 13, 14, 15 y 18).
5. Modelo, normalizador, índices y cargador de la semilla (casos 01, 02, 03, 10 y 11).
6. Consulta interna, `CatalogApi` delegando en ella y `NeighborhoodExistsAsync` (casos 04 a 09, 12, 17, 19 y 20).
7. Retiro de EF Core, Npgsql, NetTopologySuite, migraciones, fixtures y pruebas de PostGIS y de `oficios.sql` (casos 16 y 21), después del PR del líder sobre `sdd_lint.py`; si #31 ya está en `sprint-01`, migrar su lectura de oficios.
8. Gates, `Memoria.md` (decisiones y errores a evitar), informe y evidencia en el PR (casos 21 y 22).

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
