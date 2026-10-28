# TT-BD-06 — tareas

> Servicio afectado: Mercado. Spec Aprobada; dudas (a)–(j) resueltas por el líder en #53 (2026-10-24). Implementado en dos ramas: `feature/KNOCK-156-shared-mongo-client` (PR #73, T1) y `feature/KNOCK-156-catalog-migration` (T0 y T2–T6). Estado al 2026-10-27, sin commit de la segunda parte.

## T0 — Exportar las semillas desde PostGIS (antes de cualquier retiro)
- **Estado:** Hecho. `db/scripts/generate_catalog_seeds.py` (de una sola vez, decisión (e)) produjo `db/seeds/catalog/colonias.json` (12 colonias, `_id` fijos, `nombreBusqueda`, centro de `ST_PointOnSurface` para las 8 con polígono; las 4 sin polígono con `piloto: false` y sin `geom` ni `centro`) y `oficios.json` contra un PostGIS desechable (`postgis/postgis:16-3.4`, digest en su salida). Su salida va al PR.
- **Verificar:** TC-TT-BD-06-11 · TC-TT-BD-06-21

## T1 — Paquetes, conexión compartida y readiness
- **Estado:** Hecho (PR #73). `MongoDB.Driver` 3.12.0 evaluado en `Memoria.md`; `Testcontainers.MongoDb` descartado (se usa `Testcontainers` 4.16.0 genérico). `AddKnockMongo` en `Hosting/` con `ConnectionStrings:KnockMongo`, `Mongo:MaxPoolSize` = 20 y `Mongo:Databases:<Módulo>`. Fixture `DocumentDbFixture` con documentdb-local fijada por digest. En la segunda parte, el readiness reporta cualquier error solo con su tipo.
- **Verificar:** TC-TT-BD-06-13 · TC-TT-BD-06-14 · TC-TT-BD-06-15 · TC-TT-BD-06-18

## T2 — Modelo, normalizador, índices y semilla
- **Estado:** Hecho. `Domain/SearchNameNormalizer`, `Data/CatalogStore` (índices de §5) y `Data/CatalogSeeder` (valida antes de escribir, `modalidad` incluida; upsert por clave natural; `version` solo sube si cambia el contenido). Desempate permanente por `nombreBusqueda` y `_id`. `$jsonSchema` queda como riesgo para TT-BD-07; solo se declaran los TTL de caché.
- **Verificar:** TC-TT-BD-06-01 · TC-TT-BD-06-02 · TC-TT-BD-06-03 · TC-TT-BD-06-10 · TC-TT-BD-06-11

## T3 — Consulta interna e `ICatalogApi` sobre MongoDB
- **Estado:** Hecho. Consulta interna `Services/NeighborhoodLocator` (`internal`) que devuelve `Domain/Colonia` `{ id, nombre, cp, municipio }`; `CatalogApi` delega en ella. Firmas existentes sin cambios y `NeighborhoodExistsAsync` nuevo. El CP de `FindNeighborhoodsByPostalCodeAsync` se valida con la misma regla de 5 dígitos.
- **Verificar:** TC-TT-BD-06-04 · TC-TT-BD-06-05 · TC-TT-BD-06-06 · TC-TT-BD-06-07 · TC-TT-BD-06-08 · TC-TT-BD-06-09 · TC-TT-BD-06-17 · TC-TT-BD-06-19 · TC-TT-BD-06-20

## T4 — Rendimiento
- **Estado:** Hecho. 200 consultas tras 10 de calentamiento, `explain` del `find` real; documentdb-local informa 0 ms en el servidor (P9); ida y vuelta p95 3.25 ms (interior) y 5.08 ms (respaldo). La búsqueda por prefijo salió a HU-012 (decisión (g)).
- **Verificar:** TC-TT-BD-06-12

## T5 — Retiro de EF Core, Npgsql, NetTopologySuite, PostGIS y `oficios.sql`
- **Estado:** Hecho, después de T0 y de #58 (`sdd_lint.py` acepta «Reemplazada»). Paquetes quitados por una persona (incluidos `EFCore.NamingConventions` y `Testcontainers.PostgreSql`). `db/seeds/catalog/oficios.sql` borrado con la autorización del líder (#53). TT-BD-01 y TT-BD-02 pasan a «Reemplazada (por TT-BD-06)» según lo que escribió el líder en #53 («cuando fusiones»).
- **Verificar:** TC-TT-BD-06-16 · TC-TT-BD-06-21

## T6 — Gates de compilación y formato
- **Estado:** Hecho. `dotnet build Knock.slnx -c Release` 0/0 y `dotnet format --verify-no-changes` sin cambios; la salida va al PR.
- **Verificar:** TC-TT-BD-06-22

## Fuera de esta tarea (decidido)
- Colección `archivos` y paso de `IMediaFileStore` a camelCase: tarea propia «Colección archivos de Media» (2 puntos, Sprint 3, Alexis); mientras tanto sigue `registros-temporales` (decisión (b)).
- Búsqueda de oficios por prefijo y su caso: HU-012 (decisión (g)).
- Corregir los polígonos traslapados: fuera de esta tarea y de este sprint (decisión (d)).
