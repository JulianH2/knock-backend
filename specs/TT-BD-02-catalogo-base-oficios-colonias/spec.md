# TT-BD-02 — Catálogo base: 3 oficios como datos y colonias de Tula de Allende (SEPOMEX + polígonos); consulta "colonia de un punto"

> **Jira:** KNOCK-23 · **Área:** Base de datos · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S0 · **Puntos:** 3
> **Repositorio:** knock-backend · **Ficha:** TT-BD-02_CatalogoBase3Oficios_Knock.docx
> **Skills:** story-workflow, testing-strategy-backend, security-core, database-scripts, postgis-geo
> **Estado:** Aprobada

## 1. Objetivo
Sembrar los oficios Plomería, Electricidad y Mecánica como datos de catálogo, no como enum; cargar los códigos postales y nombres de colonia de Tula de Allende, Hidalgo, desde SEPOMEX; y consultar la colonia de un punto con PostGIS. El equipo decidió el 2026-10-13 que Tula de Allende es la ciudad piloto de este catálogo; la actualización de la ficha y documentación general corresponde a un PR separado.

## 2. Historia y casos de prueba
**TT-BD-02** — Tarea técnica. Las pruebas de base de datos usarán PostgreSQL/PostGIS real con Testcontainers y datos de prueba versionados, sin coordenadas de usuarios.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-02-01 | Dado un punto cubierto por un polígono de colonia de Tula, cuando se consulta mediante el módulo Catalog, entonces devuelve esa colonia por contención espacial (`ST_Contains`). | `DatabaseTest/CatalogNeighborhoodQueriesTests.PointInsidePolygonReturnsContainingNeighborhood_TC_TT_BD_02_01`; PostGIS con fixture de una colonia piloto y coordenada sintética documentada. |
| TC-TT-BD-02-02 | Dado un punto sintético de otra ciudad, fuera de Tula y sin centro de colonia a 2 km o menos, cuando se consulta, entonces devuelve «fuera de cobertura». | `DatabaseTest/CatalogNeighborhoodQueriesTests.PointOutsideTulaCoverageReturnsOutOfCoverage_TC_TT_BD_02_02`; usar un punto sintético fuera del área de Tula y comprobar también que no aplica el fallback. |
| TC-TT-BD-02-03 | Dado el seed de oficios ejecutado, cuando se consulta el catálogo, entonces existen Plomería y Electricidad con modalidad domicilio, Mecánica con modalidad taller, y los tres están activos. | `DatabaseTest/CatalogTradeSeedTests.SeedContainsRequiredActiveTradesAndModalities_TC_TT_BD_02_03`; inspeccionar `clave`, `nombre`, `modalidad` y `activo` persistidos. |
| TC-TT-BD-02-04 | Dado un punto que no está dentro de ningún polígono, pero tiene un centro de colonia a no más de 2 km, cuando se consulta, entonces devuelve la colonia cuyo centro es el más cercano. | `DatabaseTest/CatalogNeighborhoodQueriesTests.PointWithoutContainingPolygonReturnsNearestCenterWithinTwoKilometers_TC_TT_BD_02_04`; comprobar `ST_DWithin` y orden por distancia con PostGIS. |
| TC-TT-BD-02-05 | Dado un código postal de Tula existente en el catálogo SEPOMEX cargado, cuando se consultan colonias por código postal, entonces devuelve las colonias asociadas a ese CP y ninguna de otro CP. | `DatabaseTest/CatalogNeighborhoodQueriesTests.PostalCodeReturnsOnlyMatchingNeighborhoods_TC_TT_BD_02_05`; CP y resultado tomados del extracto SEPOMEX versionado. CP concreto por verificar antes de importar; no usar el CP de ejemplo `42000`. |
| TC-TT-BD-02-06 | Dada una consulta con coordenadas, cuando el módulo Catalog resuelve la colonia, entonces devuelve el resultado y descarta las coordenadas sin persistirlas ni registrarlas en logs. | `IntegrationTest/CatalogLocationPrivacyTests.NeighborhoodLookupDoesNotPersistOrLogCoordinates_TC_TT_BD_02_06`; consultar almacenamiento y logs de prueba, comprobando que solo se conserva el resultado de colonia. |
| TC-TT-BD-02-07 | Dado un punto exactamente en el borde compartido de polígonos, cuando no hay coincidencia estricta por `ST_Contains` y se consulta con `ST_Covers`, entonces el borde se considera cubierto y se elige siempre la colonia con menor nombre normalizado (desempate por `id`) antes del fallback por centro. | `DatabaseTest/CatalogNeighborhoodQueriesTests.SharedBoundaryUsesCoversAndStableTieBreak_TC_TT_BD_02_07`; fixture PostGIS con borde compartido. |
| TC-TT-BD-02-08 | Dadas latitud y longitud sintéticas distintas, cuando Catalog construye el punto y consulta PostGIS, entonces `ST_X` devuelve la longitud, `ST_Y` devuelve la latitud y la consulta identifica la colonia fixture esperada; coordenadas fuera de rango se rechazan antes de consultar PostGIS. | `DatabaseTest/CatalogCoordinatesTests.PostgisMapsLatitudeLongitudeToXLongitudeYLatitudeAndRejectsOutOfRange_TC_TT_BD_02_08`; con Testcontainers, comprobar `ST_X`/`ST_Y` y el resultado geográfico para detectar ejes invertidos. |
| TC-TT-BD-02-09 | Dado el seed de catálogo ejecutado dos veces, cuando se consultan los oficios y las colonias de referencia, entonces no hay duplicados. | `DatabaseTest/CatalogTradeSeedTests.ReapplyingSeedDoesNotDuplicateCatalogRows_TC_TT_BD_02_09`; aplicar el seed dos veces y comparar claves únicas y conteos. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Sembrar `catalog.oficios` como datos: columnas `id`, `clave`, `nombre`, `modalidad`, `activo`; Plomería y Electricidad en modalidad domicilio y Mecánica en modalidad taller; los tres activos.
  - Importar nombres y códigos postales de Tula desde SEPOMEX en `db/sepomex/`, conservando fuente, versión/fecha y licencia.
  - Versionar en `db/seeds/colonias_tula.geojson` polígonos aproximados dibujados para 8–12 colonias piloto y puntos centrales para las demás. Son aproximaciones, no límites oficiales; documentar autor y fecha de elaboración. Si los límites oficiales no están disponibles, no representarlos como oficiales ni bloquear por ello la elaboración aproximada indicada.
  - Modelar `catalog.colonias` con columnas `id`, `nombre`, `cp`, `municipio`, `piloto`, `geom geography(MultiPolygon,4326) NULL` y `centro geography(Point,4326)`, e índices GiST en las columnas geográficas. `geography` sigue `AGENTS.md §6`; para las operaciones topológicas de contención se convierte a `geometry`, y las distancias se consultan sobre `geography` en metros.
  - Exponer por la interfaz pública del módulo Catalog (sin endpoint HTTP en esta tarea) dos consultas: colonia de un punto aplicando, en orden, `ST_Contains`, resolución inclusiva de borde con `ST_Covers`, respaldo por centro más cercano dentro de 2 km y resultado fuera de cobertura; y colonias por código postal.
  - Hacer los seeds repetibles sin duplicar filas.
- **Out:**
  - Endpoint REST/GraphQL, geocodificación externa o persistencia de coordenadas exactas de usuarios.
  - Polígonos declarados oficiales: las geometrías piloto de este alcance son aproximadas.
  - Colonias de otros municipios y oficios adicionales a los tres definidos.
- **Dependencia:** TT-BD-01 (KNOCK-22).

## 4. Contratos API y eventos
No se agrega endpoint ni evento. La interfaz pública del módulo Catalog ofrecerá métodos para (1) consultar la colonia de un punto con la regla de contención y respaldo por centro y (2) consultar colonias por CP. Los nombres y tipos concretos de los métodos se fijarán al implementar; las pruebas de aceptación se harán con Testcontainers/PostGIS.

## 5. Modelo de datos
- `catalog.oficios (id, clave, nombre, modalidad, activo)` como catálogo persistido y sembrado; no usar enum para estos oficios.
- `catalog.colonias (id, nombre, cp, municipio, piloto, geom geography(MultiPolygon,4326) NULL, centro geography(Point,4326))`.
- Índices GiST sobre `geom` y `centro`; claves y valores de `id` según las convenciones de base de datos del proyecto.
- Para contención: primero `ST_Contains(geom::geometry, punto::geometry)`. Si el punto no cae en el interior, resolver los bordes con `ST_Covers(geom::geometry, punto::geometry)`; ante más de una coincidencia, desempatar por nombre normalizado y luego `id`. Solo si no hay coincidencia interior ni de borde, usar `ST_DWithin(centro, punto, 2000)` ordenado por `ST_Distance`; sin centro dentro del radio, devolver fuera de cobertura.
- La coordenada entrante se expresa como `(lat, lng)` en la interfaz, y se construye en NetTopologySuite como X=`lng`, Y=`lat`, SRID 4326. El valor se usa en la consulta y no se persiste ni registra.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Polígonos aproximados podrían confundirse con límites oficiales o producir falsa precisión. | Etiquetar y documentar los polígonos como aproximados, indicando quién los dibujó y cuándo; registrar aparte la procedencia/versión/licencia de los datos SEPOMEX de nombre y CP. | TC-TT-BD-02-01 · TC-TT-BD-02-02 · TC-TT-BD-02-07 |
| Coordenadas exactas del usuario podrían revelar ubicación si se almacenan o registran. | La consulta del módulo Catalog consume el punto, devuelve solo la colonia y descarta el punto; no persistirlo ni incluirlo en logs. | TC-TT-BD-02-06 |
| Latitud/longitud invertidas o fuera de rango podrían clasificar otro sitio. | Convertir latitud/longitud a X=`lng`, Y=`lat`; rechazar valores fuera de rango y parametrizar la consulta PostGIS. | TC-TT-BD-02-01 · TC-TT-BD-02-02 · TC-TT-BD-02-08 |
| Reaplicar datos de catálogo podría duplicar filas. | Semillas idempotentes y claves únicas para identificar registros de catálogo. | TC-TT-BD-02-05 · TC-TT-BD-02-09 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Base de datos / integración geoespacial | Sí | PostgreSQL/PostGIS real con Testcontainers en `tests/DatabaseTest`; verificar contención, respaldo de centro a 2 km, fuera de cobertura, consulta por CP y comportamiento de borde (TC-TT-BD-02-01 a TC-TT-BD-02-04 y TC-TT-BD-02-07). |
| Semillas / idempotencia | Sí | Aplicar seeds de catálogo dos veces y validar oficios, modalidad, activo y ausencia de duplicados (TC-TT-BD-02-03 y TC-TT-BD-02-09). |
| Privacidad / coordenadas | Sí | Verificar con prueba de integración que el punto no se persiste ni se registra, y con prueba PostGIS el orden X=longitud/Y=latitud y el rango de coordenadas (TC-TT-BD-02-06 y TC-TT-BD-02-08). |
| Contrato HTTP / eventos | No | No se agrega endpoint ni evento. |
| Mutación | Según la meta vigente del módulo Catalog | No se establece una meta específica nueva para esta tarea. |
| Rendimiento, UI y accesibilidad | No | No forman parte del alcance de esta tarea de datos y consultas. |

**Gates:** aplicar `AGENTS.md §5` a la implementación; no se afirma aquí que se hayan ejecutado pruebas de producto.

**Límites y dudas abiertas:**
- Confirmar contra el catálogo SEPOMEX la versión/fecha/licencia del extracto, qué CP de Tula usar en el caso de prueba y la cobertura de los nombres/CP de sus colonias. No usar el CP de ejemplo `42000`.
- Definir y registrar quién dibuja y la fecha de los polígonos aproximados de las 8–12 colonias piloto; las demás tendrán punto central.
- Confirmar coordenadas sintéticas de los fixtures PostGIS y cuáles colonias se usarán; no usar ubicaciones de usuarios.
- La decisión del equipo de usar Tula de Allende (2026-10-13) aplica a este catálogo. La actualización de `DECISION_TECNICA.md` §2 y de la ficha del backlog corresponde a un PR separado del equipo.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-13
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
