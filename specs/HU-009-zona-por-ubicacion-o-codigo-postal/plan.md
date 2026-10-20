# plan.md — HU-009 — Zona por ubicación o código postal

> Borrador. Se completa cuando la spec esté Aprobada y se resuelvan las preguntas abiertas de spec §8.

## 1. Mapeo de archivos
- **Reutilizar:** `ICatalogApi.FindNeighborhoodAtAsync` y `FindNeighborhoodsByPostalCodeAsync`, `CatalogCoordinates`, fixture de Testcontainers de IntegrationTest.
- **Crear:** controller de zonas en Catalog, DTOs y validadores internos, tabla `catalog.lista_espera` y su migración.
- **Modificar:** `CatalogApi` (consulta por punto que use GiST), `ICatalogApi` (validar colonia activa), `catalog.colonias` (columna `activa` si se aprueba).
- **No tocar:** tablas de Identity desde Catalog; HU-010 y HU-049.

## 2. Riesgos y efectos secundarios
- Fuga de coordenadas en logs de URL (TC-HU-009-01).
- Dependencia de la entidad de usuario de HU-001/HU-002 (TC-HU-009-05 a 07).
- Reescribir la consulta por punto puede cambiar resultados de TT-BD-02: correr sus pruebas.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite |
|---|---|---|
| TC-HU-009-01 | Punto en ciudad piloto propone colonia sin persistir coordenadas. | `IntegrationTest/ZoneEndpointsTests..._TC_HU_009_01` |
| TC-HU-009-02 | Fallback a CP en la app. | knock-frontend |
| TC-HU-009-03 | CP piloto lista sus colonias. | `IntegrationTest/ZoneEndpointsTests..._TC_HU_009_03` |
| TC-HU-009-04 | Punto o CP fuera de cobertura. | `IntegrationTest/ZoneEndpointsTests..._TC_HU_009_04` |
| TC-HU-009-05 | Lista de espera guarda CP. | `IntegrationTest/ZoneEndpointsTests..._TC_HU_009_05` |
| TC-HU-009-06 | «Ahora no» asigna Centro. | `IntegrationTest/ZoneEndpointsTests..._TC_HU_009_06` |
| TC-HU-009-07 | Perfil guarda solo colonia y CP. | `IntegrationTest/ZoneEndpointsTests..._TC_HU_009_07` |

## 4. Secuencia
1. Resolver preguntas abiertas (a)–(e).
2. Contrato OpenAPI → migración → servicio + unitarias → controller + integración, seguridad y contrato → `export_openapi.py` → gates → `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace HU-009
dotnet test tests/UnitTest tests/IntegrationTest tests/SecurityTest tests/DatabaseTest tests/ContractTest -c Release
```
