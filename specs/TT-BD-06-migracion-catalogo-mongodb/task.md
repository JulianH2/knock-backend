# TT-BD-06 — tareas

> Servicio afectado: Mercado. Ninguna tarea empieza hasta que la spec esté Aprobada. Dudas (a)–(j) resueltas por el líder en #53 (2026-10-24).

## T0 — Exportar las semillas desde PostGIS (antes de cualquier retiro)
- **Estado:** pendiente de aprobación de la spec (decisión (c)). Generador de una sola vez en `db/scripts/` que produce `db/seeds/catalog/colonias.json` (12 colonias, `_id` fijos, `nombreBusqueda`, centro de `ST_PointOnSurface` para las 8 con polígono; las 4 sin polígono con `piloto: false` y sin `geom` ni `centro`) y `oficios.json`; su salida va al PR.
- **Verificar:** TC-TT-BD-06-11 · TC-TT-BD-06-21

## T1 — Paquetes, conexión compartida y readiness
- **Estado:** pendiente (evaluar `MongoDB.Driver` y `Testcontainers.MongoDb`; registrar en `Memoria.md`). `AddKnockMongo` en `Hosting/` con `ConnectionStrings:KnockMongo`, `Mongo:MaxPoolSize` = 20 (decisión (i)) y `Mongo:Databases:<Módulo>`. Fixture con `documentdb-local` fijada por digest (decisión (a)).
- **Verificar:** TC-TT-BD-06-13 · TC-TT-BD-06-14 · TC-TT-BD-06-15 · TC-TT-BD-06-18

## T2 — Modelo, normalizador, índices y semilla
- **Estado:** pendiente (desempate permanente por `nombreBusqueda`, decisión (d); `modalidad` validada en el cargador y `$jsonSchema` como riesgo para TT-BD-07; solo se declaran los TTL de caché, decisión (f))
- **Verificar:** TC-TT-BD-06-01 · TC-TT-BD-06-02 · TC-TT-BD-06-03 · TC-TT-BD-06-10 · TC-TT-BD-06-11

## T3 — Consulta interna e `ICatalogApi` sobre MongoDB
- **Estado:** pendiente (consulta interna con `{ id, nombre, cp, municipio }` y `ICatalogApi` delegando en ella; firmas existentes sin cambios; nuevo `NeighborhoodExistsAsync`, decisión (j))
- **Verificar:** TC-TT-BD-06-04 · TC-TT-BD-06-05 · TC-TT-BD-06-06 · TC-TT-BD-06-07 · TC-TT-BD-06-08 · TC-TT-BD-06-09 · TC-TT-BD-06-17 · TC-TT-BD-06-19 · TC-TT-BD-06-20

## T4 — Rendimiento
- **Estado:** pendiente (la búsqueda por prefijo salió a HU-012, decisión (g))
- **Verificar:** TC-TT-BD-06-12

## T5 — Retiro de EF Core, Npgsql, NetTopologySuite, PostGIS y `oficios.sql`
- **Estado:** pendiente. Después de T0 y del PR del líder que actualiza `sdd_lint.py` (decisión (h); el estado de TT-BD-01/02 no se cambia aquí). Borrar `db/seeds/catalog/oficios.sql` con la autorización explícita del líder (#53, 2026-10-24; AGENTS.md §4).
- **Verificar:** TC-TT-BD-06-16 · TC-TT-BD-06-21

## T6 — Gates de compilación y formato
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-06-22

## Fuera de esta tarea (decidido)
- Colección `archivos` y paso de `IMediaFileStore` a camelCase: tarea propia «Colección archivos de Media» (2 puntos, Sprint 3, Alexis); mientras tanto sigue `registros-temporales` (decisión (b)).
- Búsqueda de oficios por prefijo y su caso: HU-012 (decisión (g)).
- Corregir los polígonos traslapados: fuera de esta tarea y de este sprint (decisión (d)).
