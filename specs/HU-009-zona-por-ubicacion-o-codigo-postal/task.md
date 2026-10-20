# HU-009 — tareas

## T1 — Contrato y datos
- **Verificar:** TC-HU-009-05
- **Pendiente:** OpenAPI de `/zonas/*` y `/me/zona`; migración `catalog.lista_espera` (y `activa` si se aprueba).

## T2 — Consultas de zona
- **Verificar:** TC-HU-009-01 · TC-HU-009-03 · TC-HU-009-04
- **Pendiente:** Endpoints `GET /zonas/colonia` y `GET /zonas/cp/{cp}`; consulta por punto con GiST; redacción de coordenadas en logs.

## T3 — Guardar zona
- **Verificar:** TC-HU-009-06 · TC-HU-009-07
- **Pendiente:** `PUT /me/zona` (colonia elegida o Centro por defecto), tras resolver la dependencia de usuario.
