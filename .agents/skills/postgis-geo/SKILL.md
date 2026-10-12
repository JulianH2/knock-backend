---
name: postgis-geo
description: Búsqueda geográfica en Knock con PostgreSQL + PostGIS y EF Core (técnicos cercanos, radio de atención, colonia desde la tabla propia SEPOMEX). Úsala en Catalog, Requests (oleadas) y en el mapa.
---

# postgis-geo

## Modelo
- Puntos: `geography(Point,4326)` mapeado con NetTopologySuite (`Point` con SRID 4326; X = longitud, Y = latitud).
- Colonias: `catalog.neighborhoods (id, name, postal_code, municipality, boundary geography(MultiPolygon,4326))` cargada desde el catálogo SEPOMEX.
- Técnico: `base_location` (taller o centro de atención) + `service_radius_km`.
- Índices GiST en toda columna geográfica.

## Consultas
- Cercanos: `ST_DWithin(t.base_location, @point, @radioMetros)` ordenado por `ST_Distance`. En EF: `t.BaseLocation.IsWithinDistance(point, metros)`.
- ¿Llega a mi colonia?: `ST_DWithin(t.base_location, n.boundary, t.service_radius_km * 1000)`.
- Colonia de un punto: `ST_Contains(n.boundary::geometry, @point::geometry)`.

## Privacidad
- **Nunca** se guardan coordenadas exactas del cliente: se resuelve la colonia y se descarta el punto.
- Al cliente se le muestra la ubicación del técnico con precisión reducida si atiende a domicilio (solo zona).

## Verificación
- Pruebas de integración con Testcontainers `postgis/postgis` y datos de 3–4 colonias reales de Pachuca.
- `EXPLAIN` de la consulta de cercanos usa el índice GiST (documentar en `Memoria.md`).
