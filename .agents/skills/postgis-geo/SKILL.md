---
name: postgis-geo
description: Búsqueda geográfica en Knock con MongoDB (Azure DocumentDB) e índices 2dsphere (técnicos cercanos, radio de atención, colonia de un punto). El nombre de la skill es histórico (antes PostgreSQL + PostGIS). Úsala en Catalog, Requests (oleadas) y en el mapa.
---

# postgis-geo

> El nombre se conserva por compatibilidad con los adaptadores; ya no se usa PostgreSQL ni PostGIS (ADR-002).

## Modelo
- Puntos y polígonos en GeoJSON (`[longitud, latitud]`, WGS84) con el modelo del driver oficial (`MongoDB.Driver.GeoJsonObjectModel`); sin NetTopologySuite.
- Colonias: colección `colonias` (`_id`, `nombre`, `nombreBusqueda`, `cp`, `municipio`, `geom` polígono, `centro` punto, `aproximado`), cargada desde SEPOMEX por la semilla versionada `db/seeds/catalog/colonias.json`. La referencia es `CatalogStore`.
- Técnico: ubicación base (taller o centro de atención) y radio de atención en km, tal como lo defina la spec de la HU.
- Índice `2dsphere` en todo campo geográfico, creado con `EnsureIndexesAsync` del módulo (repetirlo con la misma definición no cambia nada).

## Consultas
- Colonia de un punto: `$geoIntersects` sobre `geom`, con desempate fijo (menor `nombreBusqueda`, luego menor `_id`); si no hay polígono, `$near` sobre `centro` con un máximo en metros.
- Cercanos: `$near` con `$maxDistance` en metros sobre un campo con índice `2dsphere`.
- Filtros siempre tipados del driver (`Builders<BsonDocument>.Filter`); ninguna coordenada ni operador se arma como JSON con datos del usuario. Cada consulta lleva `maxTimeMS`.
- «¿Llega a mi colonia?» (distancia de un técnico a un polígono) se define en la spec de la HU y se prueba antes con `documentdb-local` y contra el clúster real.

## Privacidad
- **Nunca** se guardan coordenadas exactas del cliente: se resuelve la colonia y se descarta el punto.
- La colonia por punto no se cachea: la clave llevaría coordenadas.
- Al cliente se le muestra la ubicación del técnico con precisión reducida si atiende a domicilio (solo zona).

## Verificación
- Pruebas de integración con Testcontainers y `documentdb-local` (`DocumentDbFixture`) y datos de 3–4 colonias reales de Tula.
- Una prueba de humo contra DocumentDB real, porque `documentdb-local` no reproduce todo (ADR-002, P9).
- El plan de ejecución (`explain`) de la consulta de cercanos usa el índice `2dsphere`; documéntalo en `Memoria.md` y, si el motor no lo permite, anótalo.
