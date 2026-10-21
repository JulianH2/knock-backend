# plan.md — HU-009 — Zona por ubicación o código postal

> Borrador. Se completa cuando la spec esté Aprobada y se resuelvan las preguntas abiertas de spec §8.

## 0. Cuándo se implementa
**No en este sprint.** La implementación empieza **después de la migración del catálogo a MongoDB (TT-BD-06, Sprint 2 según `docs/PLAN_ADAPTACION.md` §E)**, que a su vez espera el spike TT-BD-05. Hasta entonces solo se refina la spec. Lo firme (contrato, validaciones, privacidad, pruebas unitarias, de seguridad y de contrato) puede adelantarse en cuanto exista el esqueleto del servicio Mercado (TT-ARQ-01) y la spec esté Aprobada; lo pendiente de TT-BD-05 no.

## 1. Mapeo de archivos
- **Servicio:** Mercado (`Knock.Market.Api`, ADR-001), módulo Catalog.
- **Reutilizar:** la consulta de colonia por punto y por CP que entregue TT-BD-06; `ConsoleCapture` y el patrón de `SecurityTest/Identity/SensitiveDataLeakTests` para la prueba de logs.
- **Crear:** controller de zonas, DTOs y validadores internos, servicio de zona y de lista de espera; contrato de validación de colonia en `Knock.Contracts` (pregunta b).
- **Pendiente de TT-BD-05:** colección de lista de espera, su índice único y cómo se marca la cobertura (pregunta d).
- **No tocar:** la base de Identidad desde Mercado; HU-010 y HU-049.
- **Antecedente:** el plan anterior reutilizaba `ICatalogApi.FindNeighborhoodAtAsync` con PostGIS/EF Core (TT-BD-02); ADR-002 lo retira.

## 2. Riesgos y efectos secundarios
- Fuga de coordenadas en logs de URL, en el servicio o en el gateway (TC-HU-009-01).
- Dependencia del usuario de HU-001/HU-002 y del contrato Identidad ↔ Mercado (TC-HU-009-05 a 07).
- Si el spike falla y se pasa al plan B (Atlas), cambian la consulta y el índice, no el contrato.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite |
|---|---|---|
| TC-HU-009-01 | Punto en ciudad piloto propone colonia; coordenadas fuera de logs. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_01` · `SecurityTest/Catalog/ZoneLocationPrivacyTests..._TC_HU_009_01` · integración pendiente de TT-BD-05 |
| TC-HU-009-02 | Fallback a CP en la app. | knock-frontend |
| TC-HU-009-03 | CP piloto lista sus colonias. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_03` · `ContractTest/Catalog/ZoneContractTests..._TC_HU_009_03` |
| TC-HU-009-04 | Punto o CP fuera de cobertura; entradas inválidas → `400`. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_04` · `SecurityTest/Catalog/ZoneInputValidationTests..._TC_HU_009_04` |
| TC-HU-009-05 | Lista de espera idempotente por usuario y CP. | `UnitTest/Catalog/WaitlistServiceTests..._TC_HU_009_05` · integración pendiente de TT-BD-05 |
| TC-HU-009-06 | «Ahora no» asigna Centro. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_06` |
| TC-HU-009-07 | Perfil guarda solo colonia y CP. | `UnitTest/Catalog/ZoneServiceTests..._TC_HU_009_07` · integración pendiente de TT-BD-05 |

## 4. Secuencia
1. Esperar TT-BD-05 (spike) y TT-BD-06 (catálogo en MongoDB); resolver preguntas abiertas (a)–(d).
2. Contrato OpenAPI → servicio + unitarias → controller + seguridad y contrato → colecciones e índices + integración y rendimiento → `export_openapi.py` → gates → `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace HU-009
dotnet test tests/UnitTest tests/SecurityTest tests/ContractTest -c Release
# Integración y rendimiento: comandos pendientes de TT-BD-05
```
