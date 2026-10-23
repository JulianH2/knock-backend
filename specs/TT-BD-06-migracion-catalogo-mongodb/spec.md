# TT-BD-06 — Migración del catálogo a MongoDB: oficios, colonias y consulta de colonia por punto

> **Jira:** KNOCK-156 · **Área:** Base de datos · **Servicio:** Mercado (`Knock.Market.Api`, ADR-001) · **Módulo:** Catalog · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S2 · **Puntos:** 8 · **Capas:** BD, Back
> **Repositorio:** knock-backend · **Otra parte en:** — · **Rama:** `docs/KNOCK-156-catalog-mongodb-spec` (spec) → PR a `sprint-01`; la implementación va en su propia rama tras la aprobación
> **Ficha:** no existe `docs/backlog/TT-BD-06.md` (la tarea nace en `docs/PLAN_ADAPTACION.md` §E, «Datos»).
> **Depende de:** TT-BD-05 (KNOCK-147, spike, evidencia en `docs/evidencia/KNOCK-147-spike-documentdb.md`, rama `chore/KNOCK-147-documentdb-spike`, PR #48 sin fusionar) · TT-BD-02 (KNOCK-23) y TT-BD-01 (KNOCK-22), que esta tarea reemplaza.
> **Fuentes:** ADR-001 (reglas 1 y 2) · ADR-002 («Reglas», «Qué se retira») · ADR-006 (región West US) · constitución 2.2.0 (principios 9, 10 y 11; §3; §4 «Transición de datos»; §5) · evidencia KNOCK-147 (P2, P3, P5, P7, P9 y hallazgos) · spec TT-BD-02 · `src/Knock.Api/Modules/Catalog/`
> **Skills:** story-workflow, spec-first-writing, database-scripts, vertical-slice-api, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Que el módulo Catalog del servicio de Mercado guarde sus **oficios** y **colonias** en **MongoDB (Azure DocumentDB, ADR-002)** en lugar de PostgreSQL + PostGIS + EF Core, con la consulta «colonia de un punto» resuelta con índice `2dsphere` y `$geoIntersects`, **sin cambiar el contrato `ICatalogApi` para quienes lo usan**, y que se retire del módulo lo que ya no se use (EF Core, Npgsql, NetTopologySuite, migraciones). Es la primera persistencia de producto en MongoDB: fija la conexión (`maxPoolSize`, secretos), el acceso a datos solo desde `Data/` y el campo normalizado para buscar sin acentos (spike P5).

**Terminada cuando:** `ICatalogApi.FindNeighborhoodAtAsync` y `FindNeighborhoodsByPostalCodeAsync` responden lo mismo que hoy desde MongoDB (contenedor de pruebas), la consulta por punto cumple p95 < 100 ms medida en el servidor como en P3, y el proyecto ya no referencia EF Core, Npgsql ni NetTopologySuite.

**Qué ya existe y qué cambia** (leído en `src/Knock.Api/Modules/Catalog/` y con `python scripts/codemap.py --query "neighborhood colonia postgis health"`):

| Pieza | Hoy (TT-BD-01/02) | En esta tarea |
|---|---|---|
| Contrato | `ICatalogApi` con `ModuleName`, `FindNeighborhoodAtAsync(lat, lng)` (interior → borde → centro ≤ 2 km → `null`) y `FindNeighborhoodsByPostalCodeAsync(cp)` (nombres en orden alfabético). | **Sin cambios** de firma ni de comportamiento observable (excepción documentada en §5, «Desempate»). |
| Implementación | `Services/CatalogApi` con SQL de PostGIS (`ST_Contains`, `ST_Covers`, `ST_DWithin`) sobre `CatalogDbContext`. | Se reemplaza por consultas del driver oficial `MongoDB.Driver` en un repositorio de `Data/`. |
| Datos | Tablas `catalog.oficios` y `catalog.colonias` (`uuid` v7, `geography`, GiST), migraciones EF Core. | Colecciones `oficios` y `colonias` en la base lógica `market` (§5). |
| Semillas | `CatalogSeeder`: `db/seeds/catalog/oficios.sql` + `db/seeds/colonias_tula.geojson`; centro con `ST_PointOnSurface`. | Cargador nuevo e idempotente para MongoDB; formato y origen del centro según la duda (c) de §8. |
| Coordenadas | `Domain/CatalogCoordinates.ToPoint` (NetTopologySuite, valida rangos). | Se conserva la validación (mismos mensajes, sin el valor recibido); el punto pasa a GeoJSON `[lng, lat]` sin NetTopologySuite. |
| Readiness | `Hosting/HealthExtensions.PostgresReadinessHealthCheck` (Npgsql). | Se reemplaza por un `ping` a MongoDB con la etiqueta `ready`. |
| Pruebas | `tests/DatabaseTest` (PostGIS), `IntegrationTest/Data/{PostgisInfrastructureTests,CatalogLocationPrivacyTests,PostgisFixture}`, `Support/{CatalogTestDatabase,PostgisReadinessChecks}`. | Se reemplazan por pruebas con Testcontainers de MongoDB (casos de §2). |

## 2. Historia y casos de prueba
**TT-BD-06** — Tarea técnica. Las pruebas de datos usan un contenedor real con Testcontainers (imagen según la duda (a) de §8) y solo datos ficticios: el seed versionado, puntos sintéticos y documentos inventados; nunca ubicaciones de personas. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_BD_06_KK`. Los casos 01 a 17 se verifican con código; el 18 y el 19 con comandos (la salida va en el PR). Cada caso de TT-BD-02 tiene su equivalente aquí (columna «Reemplaza»), porque sus pruebas se retiran junto con PostGIS.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-06-01 | Dada una base vacía, cuando se aplica la semilla del catálogo, entonces `oficios` tiene exactamente `ele` (Electricidad, domicilio), `mec` (Mecánica, taller) y `plo` (Plomería, domicilio), los tres `activo: true`, con `_id` de tipo `ObjectId`, campos en camelCase y `nombreBusqueda` igual a `ELECTRICIDAD`, `MECANICA` y `PLOMERIA`. Reemplaza TC-TT-BD-02-03. | `DatabaseTest/Catalog/CatalogSeedTests.SeedContainsRequiredActiveTradesWithObjectIdAndNormalizedName_TC_TT_BD_06_01` |
| TC-TT-BD-06-02 | Dada la semilla ya aplicada, cuando se aplica otra vez, entonces los conteos de `oficios` y `colonias` no cambian, cada documento conserva su `_id` y los índices únicos no reportan duplicados. Reemplaza TC-TT-BD-02-09. | `DatabaseTest/Catalog/CatalogSeedTests.ReapplyingSeedKeepsCountsAndIds_TC_TT_BD_06_02` |
| TC-TT-BD-06-03 | Dada la semilla aplicada, cuando se listan los índices, entonces `colonias` tiene `2dsphere` en `geom` y en `centro`, ascendente en `cp` y en `nombreBusqueda` y único en `(municipio, cp, nombre)`, y `oficios` tiene único en `clave` y ascendente en `nombreBusqueda`; además, insertar un polígono que se cruza a sí mismo es rechazado por el servidor y el cargador termina con error sin dejar ese documento. | `DatabaseTest/Catalog/CatalogIndexTests.CollectionsHaveDeclaredIndexesAndRejectInvalidPolygon_TC_TT_BD_06_03` |
| TC-TT-BD-06-04 | Dado un punto sintético documentado en el interior de cada una de las 8 colonias piloto, cuando se llama a `ICatalogApi.FindNeighborhoodAtAsync`, entonces devuelve el nombre de esa colonia (consulta `$geoIntersects` sobre el índice `2dsphere`). Reemplaza TC-TT-BD-02-01. | `DatabaseTest/Catalog/NeighborhoodAtPointTests.PointInsideEachPilotPolygonReturnsItsNeighborhood_TC_TT_BD_06_04` |
| TC-TT-BD-06-05 | Dado un punto en un vértice o borde compartido, y un punto en una zona donde el seed traslapa dos polígonos (p. ej. El Llano 1a Sección–Tultengo, hallazgo del spike P2), cuando se consulta 20 veces, entonces devuelve siempre la misma colonia: la de menor `nombreBusqueda` y, si empata, la de menor `_id`. Reemplaza TC-TT-BD-02-07. | `DatabaseTest/Catalog/NeighborhoodAtPointTests.SharedBoundaryAndOverlapUseStableTieBreak_TC_TT_BD_06_05` |
| TC-TT-BD-06-06 | Dado un punto que no cae en ningún polígono, cuando se consulta, entonces devuelve la colonia de centro más cercano si está a ≤ 2000 m (`$near` con `$maxDistance`, como en P2) y `null` («fuera de cobertura») si el centro más cercano está a más de 2000 m o el punto está en otra ciudad (Pachuca sintético). Reemplaza TC-TT-BD-02-02 y TC-TT-BD-02-04. | `DatabaseTest/Catalog/NeighborhoodAtPointTests.PointOutsidePolygonsUsesNearestCenterWithinTwoKilometersOrOutOfCoverage_TC_TT_BD_06_06` |
| TC-TT-BD-06-07 | Dadas latitud y longitud sintéticas cuyo intercambio cae en otra colonia o fuera de cobertura, cuando se consulta, entonces el resultado corresponde a `[lng, lat]` (no invertido); y dados valores fuera de rango (`lat` 91, `lng` -181, `NaN`, `±∞`), entonces se lanza `ArgumentOutOfRangeException` sin el valor en el mensaje y **no se envía ningún comando** al servidor (contador de eventos del driver en 0). Reemplaza TC-TT-BD-02-08. | `DatabaseTest/Catalog/CatalogCoordinatesTests.PointUsesLongitudeLatitudeOrderAndRejectsOutOfRangeBeforeQuerying_TC_TT_BD_06_07` |
| TC-TT-BD-06-08 | Dado el CP `42803` del extracto SEPOMEX versionado, cuando se llama a `FindNeighborhoodsByPostalCodeAsync`, entonces devuelve solo las colonias de ese CP, en orden alfabético por `nombreBusqueda` (y `nombre` si empata), y ninguna de `42800`; un CP sin colonias devuelve lista vacía y uno en blanco lanza `ArgumentException`. Reemplaza TC-TT-BD-02-05. | `DatabaseTest/Catalog/NeighborhoodsByPostalCodeTests.PostalCodeReturnsOnlyItsNeighborhoodsInAlphabeticalOrder_TC_TT_BD_06_08` |
| TC-TT-BD-06-09 | Dada una consulta de colonia por punto con coordenadas sintéticas reconocibles, cuando se ejecuta a través de la API con los logs capturados en nivel `Debug` (incluido el driver), entonces ninguna colección gana documentos, ninguna línea de log contiene la latitud ni la longitud, y no hay suscriptores que registren el cuerpo de los comandos del driver. Reemplaza TC-TT-BD-02-06. | `IntegrationTest/Catalog/CatalogLocationPrivacyTests.NeighborhoodLookupDoesNotPersistOrLogCoordinates_TC_TT_BD_06_09` |
| TC-TT-BD-06-10 | Dado el normalizador de nombres, cuando recibe `Plomería`, `  electrí  `, `MECÁNICA`, `El Llano 1a Sección` y `Peña`, entonces devuelve `PLOMERIA`, `ELECTRI`, `MECANICA`, `EL LLANO 1A SECCION` y `PENA` (sin marcas diacríticas, mayúsculas invariantes, espacios recortados y colapsados; la «ñ» se vuelve «N», límite conocido de P5). | `UnitTest/Catalog/SearchNameNormalizerTests.RemovesDiacriticsUppercasesAndCollapsesSpaces_TC_TT_BD_06_10` |
| TC-TT-BD-06-11 | Dada la semilla aplicada, cuando se busca por prefijo en el repositorio de oficios con `plomeria`, `PLOM`, `electricidad` y `Electrí`, entonces se obtiene Plomería, Plomería, Electricidad y Electricidad; con `lom` (palabra interior) no hay resultados; con `.*` o `^(a+)+$` no hay resultados (la entrada se escapa) y una entrada de más de 50 caracteres se rechaza antes de consultar; el plan usa el índice de `nombreBusqueda` (con `hint`, porque documentdb-local elige COLLSCAN en colecciones pequeñas, P5). | `DatabaseTest/Catalog/TradePrefixSearchTests.NormalizedPrefixSearchIgnoresAccentsCaseAndEscapesInput_TC_TT_BD_06_11` |
| TC-TT-BD-06-12 | Dada la semilla aplicada en el contenedor, cuando se ejecutan 200 consultas por punto tras 10 de calentamiento (interior con `$geoIntersects` y respaldo con `$near`), entonces el **p95 del tiempo de la consulta en el servidor** (`explain` con `executionStats.executionTimeMillis`, igual que P3) es **< 100 ms** en cada una; se imprimen p50, p95 y p99 de la consulta y del total de ida y vuelta (este último solo informativo). | `DatabaseTest/Catalog/NeighborhoodLookupPerformanceTests.PointLookupServerP95UnderOneHundredMilliseconds_TC_TT_BD_06_12` |
| TC-TT-BD-06-13 | Dada la configuración `Mongo:MaxPoolSize = 7` y una cadena de conexión que trae `maxPoolSize=100`, cuando se construye el cliente, entonces `MongoClientSettings.MaxConnectionPoolSize` es 7 (manda la configuración); con el valor ausente, 0, negativo, mayor de 100 o no numérico, el arranque falla con un mensaje que nombra la clave y no incluye la cadena. | `UnitTest/Catalog/MongoConnectionOptionsTests.MaxPoolSizeComesFromConfigurationAndIsValidated_TC_TT_BD_06_13` |
| TC-TT-BD-06-14 | Dada la API sin la cadena de conexión configurada, cuando arranca, entonces falla con un mensaje que nombra la clave (`ConnectionStrings:Mongo`) sin valores; dada una cadena con usuario y contraseña ficticios hacia un host inalcanzable, cuando falla la conexión, entonces ni el log ni la respuesta (`/health/ready`, ProblemDetails) contienen el usuario, la contraseña ni la cadena; y ningún `appsettings*.json` del repositorio contiene `mongodb://` ni `mongodb+srv://`. | `SecurityTest/Catalog/MongoConnectionSecretTests.ConnectionStringNeverAppearsInConfigLogsOrErrors_TC_TT_BD_06_14` |
| TC-TT-BD-06-15 | Dado el contenedor de MongoDB arriba, cuando se pide `/health/ready`, entonces responde 200; con el contenedor detenido responde 503; `/health` responde 200 en ambos casos. Reemplaza TC-TT-BD-01-03. | `IntegrationTest/Catalog/MongoReadinessTests.HealthReadyReflectsMongoAvailability_TC_TT_BD_06_15` |
| TC-TT-BD-06-16 | Dado el ensamblado `Knock.Api`, cuando corren las pruebas de arquitectura, entonces ningún tipo fuera de `Modules/*/Data` usa `IMongoCollection<T>` (constitución, principio 10), Catalog solo expone `AddCatalogModule` e `ICatalogApi`, y el ensamblado no referencia `Microsoft.EntityFrameworkCore*`, `Npgsql*` ni `NetTopologySuite*`. | `UnitTest/Architecture/MongoAccessTests.MongoCollectionsOnlyInDataAndNoEfCoreOrNpgsql_TC_TT_BD_06_16` |
| TC-TT-BD-06-17 | Dado `ICatalogApi`, cuando se compara por reflexión con la firma esperada, entonces conserva exactamente `ModuleName`, `FindNeighborhoodAtAsync(double, double, CancellationToken) : Task<string?>` y `FindNeighborhoodsByPostalCodeAsync(string, CancellationToken) : Task<IReadOnlyList<string>>`, sin miembros nuevos ni quitados. | `UnitTest/Catalog/CatalogContractTests.CatalogApiSignatureIsUnchanged_TC_TT_BD_06_17` |
| TC-TT-BD-06-18 | Dado el cambio terminado, cuando se revisan paquetes y archivos, entonces `dotnet list Knock.slnx package --include-transitive` no muestra EF Core, Npgsql, NetTopologySuite ni `Testcontainers.PostgreSql`; no existe `Modules/Catalog/Migrations/`; `MongoDB.Driver` y `Testcontainers.MongoDb` están evaluados en `Memoria.md` y `dotnet list package --vulnerable --include-transitive` no reporta vulnerabilidades; gitleaks no encuentra secretos. | (comando) salida en el PR |
| TC-TT-BD-06-19 | Dado el cambio terminado, cuando se compila y se revisa el formato, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings y `dotnet format Knock.slnx --verify-no-changes` no reporta cambios. | (comando) salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio, servicio Mercado, módulo Catalog; mientras exista, en `src/Knock.Api/`, TT-ARQ-01):**
  - Conexión a MongoDB con el driver oficial (`MongoDB.Driver`): un `MongoClient` por proceso, base lógica `market` (ADR-001), `maxPoolSize` desde configuración (`Mongo:MaxPoolSize`, validado al arrancar) y la cadena **solo** por variable de entorno (`ConnectionStrings__Mongo`) en local y CI, o por Key Vault en Azure (TT-SEC-01, ADR-002 regla 5). Nunca en `appsettings*.json`, en el repositorio, en logs ni en mensajes de error.
  - Colecciones `oficios` y `colonias` con sus índices (§5), creados de forma idempotente por el cargador.
  - Semilla del catálogo para MongoDB, repetible sin duplicar, con `nombreBusqueda` calculado por la app; formato y origen del centro según la duda (c).
  - Reimplementar `ICatalogApi` sobre MongoDB sin cambiar su firma (TC-TT-BD-06-17): colonia por punto (`$geoIntersects` y respaldo `$near` ≤ 2000 m) y colonias por CP.
  - Normalizador de nombres (`nombreBusqueda`) y búsqueda de oficios por prefijo anclado en el repositorio interno de `Data/` (resultado de P5); no se expone por `ICatalogApi` (ver duda (g)).
  - Comprobación `ready` con `ping` a MongoDB en `/health/ready`, en lugar de la de PostgreSQL.
  - Prueba de arquitectura: `IMongoCollection<T>` solo en `Data/` (constitución, principio 10) y sin EF Core/Npgsql/NetTopologySuite.
  - **Retiro** (ADR-002, «Qué se retira», dentro de lo que toca a Catalog): `CatalogDbContext`, `CatalogDbContextFactory`, `LocalMigrationConnectionGuard`, `Modules/Catalog/Migrations/`, el `CatalogSeeder` de SQL, `PostgresReadinessHealthCheck`, `Domain/CatalogCoordinates` con NetTopologySuite (se reescribe sin él), los paquetes `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL(.NetTopologySuite)` y `Testcontainers.PostgreSql`, y las pruebas y fixtures de PostGIS de §1. La semilla `db/seeds/catalog/oficios.sql` solo se retira si el líder lo decide (duda (e)).
  - Fixture de Testcontainers de MongoDB para `DatabaseTest` e `IntegrationTest` (reutilizable; la arnés del spike `tests/Spike.DocumentDb/Support/SpikeTargets.cs` sirve de molde).
- **Out:**
  - Endpoints HTTP: `GET /api/v1/colonias/en-punto` es de **TT-API-07 (KNOCK-141)**; `GET /zonas/cp/{cp}` y `POST /zonas/lista-espera` son de **HU-009 (KNOCK-47)**; la búsqueda por texto para personas es de HU-012.
  - Colección `lista_espera` (HU-009) y semillas de clientes, técnicos y solicitudes (**TT-BD-03, KNOCK-40**).
  - Colección `archivos` de TT-API-05 (KNOCK-42), salvo que el líder decida lo contrario (duda (b)).
  - CI: quitar el job `sqlfluff`, ajustar workflows y Dependabot, y la prueba de humo contra el DocumentDB real del principio 9 (**TT-BD-07**; `.github/` es protegido).
  - Caché `cache-aside` con Redis (**TT-BD-08**, ver duda (f)); aquí solo se declara el TTL de cada colección (§5).
  - Índices definitivos del resto de colecciones (TT-BD-08) y script `mongosh` de entrega en `db/` (TT-BD-09).
  - Rehacer `deploy/docker-compose.local.yml` (hoy con `postgis/postgis:16-3.4`): se rehace en TT-ARQ-01 (como en TT-API-05).
  - Crear, borrar o reconfigurar el clúster `knock-docdb` (TT-AZ-02 / TT-AZ-05); medir la latencia desde West US (TT-AZ-02, evidencia P3).
  - Separar el servicio de Mercado (TT-ARQ-01) y mover `ICatalogApi` a `Knock.Contracts` (TT-ARQ-03).
  - Corregir los polígonos aproximados del seed (ver duda (d)) y agregar colonias de otros municipios.
- **Relación con otras tareas:**
  - **TT-BD-03 (KNOCK-40):** depende de esta tarea. Reutiliza el cargador idempotente y, si se aprueba la duda (c), los `_id` fijos de las colonias para referenciar `coloniaId` en técnicos y clientes ficticios. Sus semillas pasan a JSON para MongoDB (`docs/PLAN_ADAPTACION.md` §D).
  - **TT-API-07 (KNOCK-141):** consume `ICatalogApi.FindNeighborhoodAtAsync` para `GET /colonias/en-punto` y fija ahí la redacción de `lat`/`lng` en los logs de la petición. Esta tarea garantiza que el módulo no persiste ni registra el punto (TC-TT-BD-06-09).
  - **HU-009 (KNOCK-47):** usa `FindNeighborhoodsByPostalCodeAsync` (o una consulta propia por `cp`) sobre la colección `colonias` que crea esta tarea, con el índice por `cp` de §5; su respuesta incluye `id` de colonia, que será el `ObjectId` de §5.
  - **TT-PRU-01 (KNOCK-29, PR #31 sin fusionar):** su `GET /api/v1/oficios` lee `catalog.oficios` con EF Core. Si #31 se fusiona antes, esta tarea migra esa lectura al repositorio de MongoDB y sus pruebas TC-TT-PRU-01-01..06 siguen en verde con la fixture nueva; si no, #31 se ajusta después (`docs/PLAN_ADAPTACION.md` §D).
  - **TT-API-05 (KNOCK-42):** ver duda (b).
  - **TT-BD-07:** toma lo que esta tarea deja fuera del CI y la imagen de Testcontainers para el resto del producto.

## 4. Contratos API y eventos
- **Sin endpoints nuevos ni eventos.** No cambia `docs/openapi.v1.json`, `docs/endpoints.md` ni `docs/events.md`.
- **Contrato entre módulos:** `ICatalogApi` no cambia (TC-TT-BD-06-17). Comportamiento conservado: punto interior → colonia; borde → colonia con desempate estable; sin polígono → centro más cercano a ≤ 2000 m; si no → `null`; coordenadas fuera de rango → `ArgumentOutOfRangeException` sin el valor; CP en blanco → `ArgumentException`; el punto no se guarda ni se registra. El único matiz es el desempate de §5.
- **Configuración (no es contrato HTTP):** `ConnectionStrings:Mongo` (secreto; solo variable de entorno o Key Vault), `Mongo:Database` (`market`), `Mongo:MaxPoolSize` (entero 1..100; propuesta 20, ver §5). Nombres por confirmar al implementar.

## 5. Modelo de datos
Base lógica **`market`** del servicio de Mercado (ADR-001). Colecciones en `snake_case` plural y campos en camelCase, `_id` de tipo `ObjectId` y fechas en UTC (constitución 2.2.0 §5). El acceso es solo desde `Modules/Catalog/Data/`.

**`oficios`** — `cache: 86400` (catálogo, principio 10)
```
{ _id: ObjectId, clave: string(≤16), nombre: string(≤100), nombreBusqueda: string,
  modalidad: "domicilio" | "taller", activo: bool, version: int }
```
- Índices: `{ clave: 1 }` único · `{ nombreBusqueda: 1 }`.
- `modalidad` se valida en el cargador (dominio); el `CHECK` de PostgreSQL no tiene equivalente probado en el spike.

**`colonias`** — por CP `cache: 86400` (catálogo) · por punto `cache: no` (la clave de caché tendría coordenadas: dato de ubicación que no se guarda)
```
{ _id: ObjectId, nombre: string(≤150), nombreBusqueda: string, cp: string(5), municipio: string(≤100),
  piloto: bool, aproximado: bool, geom?: GeoJSON MultiPolygon, centro?: GeoJSON Point, version: int }
```
- Índices: `{ geom: "2dsphere" }` · `{ centro: "2dsphere" }` · `{ cp: 1 }` · `{ nombreBusqueda: 1 }` · `{ municipio: 1, cp: 1, nombre: 1 }` único (clave natural del upsert, como en TT-BD-02).
- Sin polígono, el campo `geom` **no existe** (no `null`); igual con `centro` si no hay centro. Las coordenadas van en orden GeoJSON `[longitud, latitud]`, WGS 84.
- `version` empieza en 1 y el cargador la incrementa solo si cambia el contenido (base para el `ETag` de TT-API-07 y HU-009, constitución §5).

**Consultas:**
- **Colonia de un punto:** (1) `$geoIntersects` con el punto sobre `geom`, proyección `{ nombre, nombreBusqueda }`; si hay varias (borde compartido o traslape), se ordena por `nombreBusqueda` y luego `_id` y se toma la primera; (2) si no hay ninguna, `$near` sobre `centro` con `$maxDistance: 2000` y `limit 1`; (3) si no, `null`. Spike P2: mismo resultado en mongo:8.0, documentdb-local y Azure; P3: p95 de la consulta 0.6 ms en Azure.
- **Desempate (cambio de matiz frente a PostGIS):** PostGIS distinguía interior (`ST_Contains`) de borde (`ST_Covers`); `$geoIntersects` no lo distingue. Con polígonos sin traslape el resultado es el mismo; donde el seed traslapa (Centro–La Huerta, Centro–San Lorenzo, El Cielito–San Lorenzo, El Llano 1a Sección–Tultengo, hallazgo de P2) gana el menor `nombreBusqueda`, no «el interior». Ver duda (d).
- **Colonias por CP:** filtro por `cp` con orden `{ nombreBusqueda: 1, nombre: 1 }`, proyección `{ nombre }`.
- **Oficios por prefijo:** `nombreBusqueda` con expresión regular anclada `^` + entrada normalizada y escapada (`Regex.Escape`), longitud 1..50, `hint` al índice. Sin stemming ni palabra interior; «ñ» = «n» (límites de P5).

**Normalización (`nombreBusqueda`):** la calcula la app en cada escritura (cargador hoy; cualquier escritura futura debe usar el mismo normalizador, riesgo de P5): descomposición NFD, quitar marcas diacríticas, mayúsculas invariantes, recortar y colapsar espacios.

**Conexión:** `maxPoolSize` fijo por servicio desde configuración, porque el límite de conexiones del nivel gratuito no es público (P7: ≥ 256 conexiones medidas, sin cifra oficial). Propuesta: 20 por réplica (3 servicios con base × 2 réplicas × 20 = 120 conexiones, debajo de lo medido); la cifra final la confirma el líder.

**Semillas:** el GeoJSON `db/seeds/colonias_tula.geojson` (8 polígonos aproximados) y `db/sepomex/tula_colonias.csv` (12 colonias) se conservan como fuente; cómo se cargan, de dónde sale el centro y si entran las 4 colonias sin polígono es la duda (c).

## 6. Seguridad, privacidad y amenazas
STRIDE breve. Sin endpoints nuevos; el riesgo está en la conexión, los secretos y la ubicación.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** la cadena de conexión (usuario y contraseña del clúster) se filtra en el repositorio, que es público, o en logs, errores, PR o prompts. | Solo variable de entorno o Key Vault; ningún `appsettings*.json` la contiene; los mensajes de arranque nombran la clave, no el valor; los errores del driver no se registran con su mensaje crudo si traen el host o credenciales; gitleaks como red. | TC-TT-BD-06-14 · TC-TT-BD-06-18 |
| **Fuga (I):** coordenadas de la persona guardadas o registradas (incluido el log de comandos del driver o una clave de caché). | El punto solo viaja en la consulta; ninguna colección lo guarda; sin suscriptor que registre el cuerpo de los comandos; colonia por punto con `cache: no`. Riesgo residual: el registro de consultas lentas del servidor en Azure (fuera de nuestro control; se anota). | TC-TT-BD-06-09 |
| **Alteración (T):** inyección de operadores o expresión regular en la búsqueda por prefijo (`$where`, `.*`, ReDoS). | Filtros tipados del driver (nunca JSON armado con la entrada); prefijo escapado y anclado; longitud máxima 50. | TC-TT-BD-06-11 |
| **Alteración (T):** ejes invertidos o coordenadas fuera de rango clasifican otro sitio. | Validación de rangos antes de consultar; GeoJSON `[lng, lat]`. | TC-TT-BD-06-07 |
| **Alteración (T):** polígonos inválidos o semilla repetida dejan datos corruptos o duplicados. | Índice `2dsphere` rechaza polígonos inválidos; upsert por clave única; idempotencia probada. | TC-TT-BD-06-02 · TC-TT-BD-06-03 |
| **Denegación (D):** agotar las conexiones del nivel gratuito con varios servicios y réplicas. | `maxPoolSize` fijo desde configuración, validado al arrancar; un solo `MongoClient` por proceso. | TC-TT-BD-06-13 |
| **Denegación (D):** consulta por punto lenta (sin índice) bajo carga. | Índices `2dsphere`; p95 en servidor < 100 ms medido. | TC-TT-BD-06-12 |
| **Elevación (E):** otro módulo o servicio lee las colecciones del catálogo directamente. | ADR-001 regla 1; `IMongoCollection` solo en `Data/`; Catalog expone solo `AddCatalogModule` e `ICatalogApi`. | TC-TT-BD-06-16 |
| **Suplantación (S) / repudio (R):** no aplican: no hay identidad de usuario ni escrituras de usuario en esta tarea; las escrituras son de la semilla. | — | — |
| **Cadena de suministro:** paquetes nuevos con vulnerabilidades o licencia incompatible. | Evaluar `MongoDB.Driver` (Apache-2.0) y `Testcontainers.MongoDb` (MIT) y registrar en `Memoria.md`; el spike ya los auditó en NuGet (3.12.0 y 4.16.0, sin vulnerabilidades, 2026-10-23); imagen de pruebas fijada por etiqueta o digest. | TC-TT-BD-06-18 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Base de datos | Sí | Testcontainers con MongoDB real (imagen de la duda (a)): semilla, índices, consultas, búsqueda por prefijo y rendimiento. TC-TT-BD-06-01 a 08, 11 y 12. `tests/DatabaseTest`. |
| Integración | Sí | API completa con el contenedor: privacidad del punto y readiness. TC-TT-BD-06-09 y 15. `tests/IntegrationTest`. |
| Unitarias | Sí | Normalizador y opciones de conexión (`maxPoolSize`). TC-TT-BD-06-10 y 13. |
| Seguridad negativa | Sí | Cadena de conexión fuera de configuración, logs y errores. TC-TT-BD-06-14. `tests/SecurityTest`. |
| Arquitectura | Sí | `IMongoCollection` solo en `Data/`, superficie del módulo, sin EF Core/Npgsql/NetTopologySuite; firma de `ICatalogApi`. TC-TT-BD-06-16 y 17. |
| Rendimiento | Sí | p95 de la consulta en servidor < 100 ms con `explain`, como P3 (TC-TT-BD-06-12). El contenedor local no representa la red a West US: esa medición es de TT-AZ-02 (evidencia P3). |
| Contrato HTTP, eventos, UI | No | Sin endpoints ni eventos. |
| Humo contra DocumentDB real | No en esta tarea | Principio 9; lo agrega TT-BD-07. Recomendación: correr una vez TC-TT-BD-06-04 a 08 contra `knock-docdb` con la variable de entorno, como en el spike, y anotar el resultado en el PR. |
| Mutación | Según la meta vigente del módulo Catalog | Nightly con Stryker. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: build Release 0/0, `dotnet format`, Unit (con Architecture), Integration, Security y Database en verde con resultado real, `sdd_lint.py --trace TT-BD-06`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Límites conocidos (del spike):**
- documentdb-local agota conexiones entre 64 y 96 (P7): ninguna prueba de esta tarea mide conexiones contra el contenedor.
- documentdb-local prefiere COLLSCAN en colecciones pequeñas (P5): las pruebas que comprueban el uso de índice usan `hint`.
- La búsqueda por prefijo no tiene stemming ni palabra interior, y «ñ» = «n» (P5).
- El nivel gratuito no tiene respaldo y se pausa tras 60 días sin conexiones: lo cubre TT-BD-04, no esta tarea.

**Dudas abiertas para el líder:**
- **(a) Imagen de las pruebas.** Opciones: (1) `ghcr.io/microsoft/documentdb/documentdb-local` fijada por digest (la del spike es de 2025-06; las nuevas se publican en `ghcr.io/documentdb/documentdb/documentdb-local`); reproduce a Azure en P5 y es la que recomienda el spike para TT-BD-07 y la que nombra HU-009. (2) `mongo:8.0.34`, más rápida y con `explain` estándar, pero en P5 se comporta distinto que Azure. (3) Ambas con una teoría parametrizada por destino (como P9), a cambio de tiempo de CI. **Recomendación:** (1) con digest fijo, revisando antes la imagen nueva; ¿la decide esta tarea o espera a TT-BD-07?
- **(b) Colección `archivos` de TT-API-05.** Su spec dice «la implementación es de TT-BD-06», pero mezcla `propietarioId` (§5 de su spec) con índices en `propietario_id` (y `IMediaFileStore` documenta todo en snake_case) y fija `_id` como UUID v7, mientras la constitución 2.2.0 pide campos en camelCase y `_id` `ObjectId`. El `id` del archivo ya es parte del contrato HTTP (`/archivos/{id}`, GUID) y da nombre a los blobs. Opciones: (1) entra aquí: `_id` `ObjectId` + campo `archivoId` (UUID v7) con índice único, `propietarioId` y `estado` en camelCase, sin cambiar el contrato HTTP ni los blobs; (2) entra aquí con `_id` `ObjectId` también en la API (cambio de contrato y de nombres de blob); (3) sale a una tarea propia de Media (después de TT-API-05) y esta queda solo con el catálogo. **Recomendación:** (3), para no mezclar módulos en una tarea de 8 puntos, con la regla de nombres de (1) escrita ya en la spec de TT-API-05.
- **(c) Carga de `colonias_tula.geojson`.** Hoy el centro de cada colonia lo calcula PostGIS (`ST_PointOnSurface`) y solo se cargan las 8 con polígono; las 4 del CSV sin polígono (Mastrantos, Bugambilias, Del Llano, Tierra Blanca) no tienen centro en el repositorio (spike, §5). MongoDB no calcula un punto interior. Opciones: (1) un JSON versionado nuevo (`db/seeds/catalog/colonias.json`) generado una vez desde el GeoJSON y el CSV, con `_id` fijos, `nombreBusqueda` y el centro exportado una vez desde PostGIS antes de retirarlo; el GeoJSON queda como fuente de los dibujos; (2) el cargador lee el GeoJSON y el CSV en cada carga y calcula el centro en C# (sin NetTopologySuite habría que escribir el algoritmo de punto interior); (3) `mongoimport` de las features, sin centro. Y, en cualquier caso: ¿entran las 4 colonias sin polígono (`piloto: false`, sin `geom` ni `centro`, visibles por CP y nunca por punto) o siguen fuera como hoy? **Recomendación:** (1) con las 4 colonias incluidas, para que HU-009 liste el CP completo y los `_id` sean estables entre entornos (TT-BD-03 y la zona del perfil guardan `coloniaId`).
- **(d) Traslapes del seed.** El spike encontró 4 pares de polígonos traslapados. Opciones: (1) aceptar el desempate por `nombreBusqueda` de §5 (sin tocar el seed); (2) corregir los polígonos en una versión nueva del GeoJSON (AGENTS.md §4 prohíbe editar semillas ya fusionadas) y agregar una prueba de «sin traslapes». **Recomendación:** (1) en esta tarea y (2) como tarea aparte del responsable de los dibujos.
- **(e) Semilla `db/seeds/catalog/oficios.sql`.** Es SQL de PostgreSQL ya fusionado; sin la tabla no sirve, y el job `sqlfluff` del CI la revisa. ¿Se borra aquí (con la autorización explícita que pide AGENTS.md §4) o se deja hasta TT-BD-07?
- **(f) Caché.** El principio 10 exige que toda lectura pase por un repositorio con caché; Redis y la capa `cache-aside` son de TT-BD-08. ¿Esta tarea solo declara los TTL de §5 y deja la caché a TT-BD-08 (recomendado), o implementa ya `cache-aside` para `oficios` y colonias por CP?
- **(g) Búsqueda por prefijo sin consumidor.** Sin un método en `ICatalogApi` ni endpoint, la búsqueda de oficios por prefijo solo la usan las pruebas (la constitución prohíbe código muerto). Opciones: (1) dejar solo el campo, el índice y el normalizador (que sí usa el desempate) y mover la consulta a HU-012; (2) mantenerla en el repositorio interno con su prueba, como base de HU-012. **Recomendación:** (1), y mover TC-TT-BD-06-11 a HU-012 si se elige.
- **(h) Estado «Reemplazada» de TT-BD-01 y TT-BD-02.** La constitución 2.2.0 §11 ya lo define, pero `scripts/sdd_lint.py` solo acepta Borrador, Aprobada y Firmada, y `--trace TT-BD-01/02` fallará cuando se borren sus pruebas. `scripts/` es protegido: ¿el líder actualiza `sdd_lint.py` antes de esta tarea, o las specs viejas se quedan como están y se excluyen del `--trace`?
- **(i) Valor de `maxPoolSize`.** Propuesta: 20 por réplica (§5). ¿Se confirma?

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
