# HU-009 — tareas

> No en este sprint: se implementa después de TT-BD-06 (ver `plan.md` §0).

## T1 — Contrato y validaciones (firme)
- **Verificar:** TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** OpenAPI de `/zonas/*` y `/me/zona`; validadores de `lat`/`lng` y CP; prueba de contrato.

## T2 — Consultas de zona
- **Verificar:** TC-HU-009-01 · TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** Endpoints `GET /zonas/colonia` y `GET /zonas/cp/{cp}` con unitarias; redacción de coordenadas en logs con prueba `ConsoleCapture`. Consulta por punto e índice: pendiente de TT-BD-05.

## T3 — Lista de espera y zona del perfil
- **Verificar:** TC-HU-009-05 · TC-HU-009-06 · TC-HU-009-07
- **Pendiente:** `POST /zonas/lista-espera` y `PUT /me/zona` (colonia elegida o Centro por defecto) con unitarias; colección de lista de espera: pendiente de TT-BD-05; contrato Identidad ↔ Mercado: pregunta abierta (b).

## T4 — Integración y rendimiento (pendiente de TT-BD-05)
- **Verificar:** TC-HU-009-01 · TC-HU-009-03 · TC-HU-009-04 · TC-HU-009-05 · TC-HU-009-07
- **Pendiente:** Pruebas contra el catálogo real y colonia por punto < 100 ms.
