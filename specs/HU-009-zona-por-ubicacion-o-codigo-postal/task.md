# HU-009 — tareas

> No en este sprint: se implementa después de TT-BD-06 (ver `plan.md` §0).

## T1 — Contrato y validaciones
- **Verificar:** TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** OpenAPI de `GET /zonas/cp/{cp}` y `POST /zonas/lista-espera`; validador de CP; prueba de contrato. La colonia por punto es `GET /colonias/en-punto` de TT-API-07 (no se define aquí).

## T2 — Colonias por CP
- **Verificar:** TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** `GET /zonas/cp/{cp}` con unitarias (en cobertura y fuera con ciudad piloto) y CP inválido → `400`.

## T3 — Lista de espera
- **Verificar:** TC-HU-009-05
- **Pendiente:** `POST /zonas/lista-espera` idempotente por usuario y CP con unitarias; colección `lista_espera` con índice único `(usuarioId, cp)` como script versionado.

## T4 — Integración
- **Verificar:** TC-HU-009-03 · TC-HU-009-04 · TC-HU-009-05
- **Pendiente:** Pruebas con Testcontainers `documentdb-local` contra el catálogo de TT-BD-06; `maxPoolSize` fijo en la conexión.

## Fuera de esta historia (dependencia)
- `PUT /me/zona` en Identidad (CA6 «Ahora no» → Centro, CA7 perfil solo con `coloniaId` + `cp`), con validación síncrona por `ICatalogApi` / `Knock.Contracts`: espera a HU-001/HU-002.
