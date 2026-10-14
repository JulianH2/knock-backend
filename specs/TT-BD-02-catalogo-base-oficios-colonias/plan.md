# plan.md — TT-BD-02 — Catálogo base de oficios y colonias de Tula

## 1. Mapeo de archivos
- **Reutilizar:** La interfaz pública del módulo Catalog y la infraestructura de integración/Testcontainers existentes o provistos por TT-BD-01; no crear endpoint HTTP para estas consultas.
- **Crear:** Seeds idempotentes en `db/seeds/`; archivo `db/seeds/colonias_tula.geojson` para polígonos aproximados con autor/fecha; importación y README de fuente SEPOMEX en `db/sepomex/`; modelo/migración `catalog.oficios` y `catalog.colonias` al implementar.
- **Modificar:** Interfaz del módulo Catalog y su implementación para consulta por punto y por código postal; pruebas `DatabaseTest` y de privacidad/validación que correspondan.
- **No tocar:** Datos de coordenadas de usuarios ni declarar oficiales las aproximaciones. La actualización de la ciudad piloto general en `DECISION_TECNICA.md` y la ficha del backlog va en PR separado.

## 2. Riesgos y efectos secundarios
- Confusión entre geometría aproximada y límite oficial: documentar condición aproximada, autor y fecha (TC-TT-BD-02-01, 02 y 07).
- Fuga de coordenadas exactas: no persistir ni registrar el punto de consulta (TC-TT-BD-02-06).
- Inversión de ejes: la interfaz `(lat, lng)` se mapea a X=`lng`, Y=`lat` y se validan rangos (TC-TT-BD-02-08).
- Reaplicar seeds no debe duplicar registros (TC-TT-BD-02-09).
- Dependencia de TT-BD-01 (KNOCK-22) para PostgreSQL/PostGIS local y de pruebas.
- Verificar datos SEPOMEX y CP de Tula antes de fijar los datos de fixture; no utilizar el CP de ejemplo `42000`.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite |
|---|---|---|
| TC-TT-BD-02-01 | Punto contenido en polígono devuelve esa colonia. | `DatabaseTest/CatalogNeighborhoodQueriesTests.PointInsidePolygonReturnsContainingNeighborhood_TC_TT_BD_02_01` |
| TC-TT-BD-02-02 | Un punto sintético de otra ciudad fuera de Tula y sin centro a 2 km devuelve fuera de cobertura. | `DatabaseTest/CatalogNeighborhoodQueriesTests.PointOutsideTulaCoverageReturnsOutOfCoverage_TC_TT_BD_02_02` |
| TC-TT-BD-02-03 | Seed contiene los tres oficios activos con sus modalidades. | `DatabaseTest/CatalogTradeSeedTests.SeedContainsRequiredActiveTradesAndModalities_TC_TT_BD_02_03` |
| TC-TT-BD-02-04 | Sin contención, centro más cercano dentro de 2 km gana. | `DatabaseTest/CatalogNeighborhoodQueriesTests.PointWithoutContainingPolygonReturnsNearestCenterWithinTwoKilometers_TC_TT_BD_02_04` |
| TC-TT-BD-02-05 | Consultar CP SEPOMEX devuelve solo las colonias asociadas. | `DatabaseTest/CatalogNeighborhoodQueriesTests.PostalCodeReturnsOnlyMatchingNeighborhoods_TC_TT_BD_02_05` |
| TC-TT-BD-02-06 | Consulta no persiste ni registra coordenadas exactas. | `IntegrationTest/CatalogLocationPrivacyTests.NeighborhoodLookupDoesNotPersistOrLogCoordinates_TC_TT_BD_02_06` |
| TC-TT-BD-02-07 | Borde compartido se resuelve con cobertura y desempate estable. | `DatabaseTest/CatalogNeighborhoodQueriesTests.SharedBoundaryUsesCoversAndStableTieBreak_TC_TT_BD_02_07` |
| TC-TT-BD-02-08 | PostGIS devuelve `ST_X=lng`, `ST_Y=lat` y la colonia fixture esperada; rangos inválidos se rechazan. | `DatabaseTest/CatalogCoordinatesTests.PostgisMapsLatitudeLongitudeToXLongitudeYLatitudeAndRejectsOutOfRange_TC_TT_BD_02_08` |
| TC-TT-BD-02-09 | Reaplicar seed no duplica filas. | `DatabaseTest/CatalogTradeSeedTests.ReapplyingSeedDoesNotDuplicateCatalogRows_TC_TT_BD_02_09` |

## 4. Secuencia
1. Confirmar extracto SEPOMEX, su licencia/fecha y el CP de Tula que se usará en pruebas; documentar la fuente en `db/sepomex/`.
2. Preparar los nombres y CP de SEPOMEX; dibujar aproximaciones de 8–12 polígonos piloto y asignar centros a las otras colonias. Documentar autor y fecha; no tratarlos como oficiales.
3. Definir y migrar el esquema Catalog con `geography(...,4326)` y GiST; sembrar oficios y datos geográficos de forma idempotente.
4. Exponer las consultas a través de la interfaz del módulo Catalog; construir puntos con X=`lng`, Y=`lat` y no persistir ni loggear la coordenada entrante.
5. Verificar todos los TC con Testcontainers/PostGIS, incluyendo cobertura por polígono, un punto fuera de Tula y sin fallback, respaldo de centro, CP, borde, privacidad, `ST_X`/`ST_Y` para el orden lon/lat, modalidad y reejecución de seed.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace TT-BD-02
dotnet test tests/DatabaseTest -c Release
```
La invocación de pruebas de privacidad/integración se ajustará a la suite final al implementarse. No se ejecutan ni se afirman pruebas de producto en este borrador.
