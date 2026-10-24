# HU-009 — tareas

> No en este sprint: se implementa después de TT-BD-06 (ver `plan.md` §0).

## T1 — Contrato y validaciones
- **Verificar:** TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** OpenAPI de `GET /zonas/cp/{cp}` y `POST /zonas/lista-espera` (incluido el `422`); validador de CP sobre un objeto de consulta con su `IValidator` registrado; prueba de contrato (necesita `tests/ContractTest`, `docs/openapi.v1.json` y `docs/endpoints.md`, que llegan con HU-001). La colonia por punto es `GET /colonias/en-punto` de TT-API-07 (no se define aquí).

## T2 — Colonias por CP y propuesta por ubicación
- **Verificar:** TC-HU-009-01 · TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** `GET /zonas/cp/{cp}` sin paginar (excepción al principio 11, decisión (e) del líder) con unitarias (en cobertura y fuera con ciudad piloto) y CP inválido → `422` con `errors.cp`. Propuesta de colonia en el servicio de zona con el resultado de `GET /colonias/en-punto` (unitaria con doble; la consulta la prueba TT-API-07).

## T3 — Lista de espera
- **Verificar:** TC-HU-009-05 · TC-HU-009-06
- **Pendiente:** `POST /zonas/lista-espera` idempotente por usuario y CP con unitarias (`201` / `200` con el registro existente, sin `Idempotency-Key`: excepción al principio 15, decisión (f) del líder); validador de `cp` y `municipio` (máximo 100 caracteres y caracteres permitidos); colección `listas_espera` (`cache: no`) con índice único `(usuarioId, cp)` como script versionado.

## T4 — Integración
- **Verificar:** TC-HU-009-03 · TC-HU-009-04 · TC-HU-009-05
- **Pendiente:** Pruebas con Testcontainers `documentdb-local` contra el catálogo de TT-BD-06; `maxPoolSize` fijo en la conexión.

## Fuera de esta historia (dependencia)
- `PUT /me/zona` en Identidad, con validación síncrona por `ICatalogApi` / `Knock.Contracts`. CA6 («Ahora no» → Centro) y CA7 (perfil solo con `coloniaId` + `cp`): se agregan como casos 07 y 08 cuando exista PUT /me/zona (HU-001/HU-002).
