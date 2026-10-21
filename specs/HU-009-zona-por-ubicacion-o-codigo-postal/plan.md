# plan.md — HU-009 — Zona por ubicación o código postal

> Borrador. Preguntas de spec §8 resueltas por el Tech Lead ((c) y (d) con la evidencia del spike, pendiente de que se fusione #48); falta la aprobación de la spec.

## 0. Cuándo se implementa
**No en este sprint.** La implementación empieza **después de la migración del catálogo a Azure DocumentDB (TT-BD-06, Sprint 2 según `docs/PLAN_ADAPTACION.md` §E)**. Lo firme (contrato, validaciones, pruebas unitarias, de seguridad y de contrato) puede adelantarse en cuanto exista el esqueleto del servicio Mercado (TT-ARQ-01) y la spec esté Aprobada.

## 1. Mapeo de archivos
- **Servicio:** Mercado (`Knock.Market.Api`, ADR-001), módulo Catalog.
- **Reutilizar:** la consulta de colonias por CP que entregue TT-BD-06; `GET /colonias/en-punto` de TT-API-07 (KNOCK-141), que la app consume para la ubicación.
- **Crear:** controller de zonas (`GET /zonas/cp/{cp}`, `POST /zonas/lista-espera`), DTOs y validadores internos, servicio de zona por CP y de lista de espera; colección `lista_espera` con índice único `(usuarioId, cp)` como script versionado; fixture de Testcontainers con `documentdb-local`.
- **No tocar:** `/colonias/en-punto` (TT-API-07); `PUT /me/zona` y la base de Identidad (HU-001/HU-002); HU-010 y HU-049.
- **Configuración:** `maxPoolSize` fijo en la conexión del servicio.
- **Antecedente:** el plan anterior reutilizaba `ICatalogApi.FindNeighborhoodAtAsync` con PostGIS/EF Core (TT-BD-02); ADR-002 lo retira.

## 2. Riesgos y efectos secundarios
- Dependencia de TT-API-07 para el flujo con ubicación (TC-HU-009-01) y de HU-001/HU-002 para la zona del perfil (CA6 y CA7 diferidos).
- La evidencia del spike está en la rama `chore/KNOCK-147-documentdb-spike` (#48), aún sin fusionar.
- `documentdb-local` tiene un límite de conexiones menor que Azure (spike P7): las pruebas no deben usarlo como límite real.
- El seed SEPOMEX tiene traslapes de polígonos (spike P2); lo decide TT-BD-06 y afecta a la colonia por punto, no al listado por CP.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite |
|---|---|---|
| TC-HU-009-01 | Flujo con ubicación; backend en TT-API-07. | knock-frontend (y pruebas de TT-API-07) |
| TC-HU-009-02 | Fallback a CP en la app. | knock-frontend |
| TC-HU-009-03 | CP piloto lista sus colonias. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_03` · `ContractTest/Catalog/ZoneContractTests..._TC_HU_009_03` · `IntegrationTest/Catalog/ZoneByPostalCodeTests..._TC_HU_009_03` |
| TC-HU-009-04 | CP fuera de cobertura; CP inválido → `400`. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_04` · `SecurityTest/Catalog/ZoneInputValidationTests..._TC_HU_009_04` · integración |
| TC-HU-009-05 | Lista de espera idempotente por usuario y CP. | `UnitTest/Catalog/WaitlistServiceTests..._TC_HU_009_05` · `IntegrationTest/Catalog/WaitlistTests..._TC_HU_009_05` |

## 4. Secuencia
1. Aprobación de la spec; esperar TT-BD-06 (catálogo en Azure DocumentDB) y la fusión de #48.
2. Contrato OpenAPI → servicio + unitarias → controller + seguridad y contrato → colección `lista_espera` e índice + integración con `documentdb-local` → `export_openapi.py` → gates → `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace HU-009
dotnet test tests/UnitTest tests/SecurityTest tests/ContractTest -c Release
dotnet test tests/IntegrationTest -c Release   # requiere Docker (documentdb-local)
```
