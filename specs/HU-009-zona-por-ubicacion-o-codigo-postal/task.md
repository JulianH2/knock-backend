# HU-009 — tareas

> No en este sprint: se implementa después de TT-BD-06 (ver `plan.md` §0).

## T1 — Contrato y validaciones
- **Verificar:** TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** OpenAPI de `GET /zonas/cp/{cp}` y `POST /zonas/lista-espera`; validador de CP; prueba de contrato. La colonia por punto es `GET /colonias/en-punto` de TT-API-07 (no se define aquí).

## T2 — Colonias por CP y propuesta por ubicación
- **Verificar:** TC-HU-009-01 · TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** `GET /zonas/cp/{cp}` con unitarias (en cobertura y fuera con ciudad piloto) y CP inválido → `400`. Propuesta de colonia en el servicio de zona con el resultado de `GET /colonias/en-punto` (unitaria con doble; la consulta la prueba TT-API-07).

## T3 — Lista de espera
- **Verificar:** TC-HU-009-05
- **Pendiente:** `POST /zonas/lista-espera` idempotente por usuario y CP con unitarias; colección `lista_espera` con índice único `(usuarioId, cp)` como script versionado.

## T4 — Integración
- **Verificar:** TC-HU-009-03 · TC-HU-009-04 · TC-HU-009-05
- **Pendiente:** Pruebas con Testcontainers `documentdb-local` contra el catálogo de TT-BD-06; `maxPoolSize` fijo en la conexión.

## Fuera de esta historia (dependencia)
- `PUT /me/zona` en Identidad, con validación síncrona por `ICatalogApi` / `Knock.Contracts`. CA6 («Ahora no» → Centro) y CA7 (perfil solo con `coloniaId` + `cp`): se agregan como casos 06 y 07 cuando exista PUT /me/zona (HU-001/HU-002).
