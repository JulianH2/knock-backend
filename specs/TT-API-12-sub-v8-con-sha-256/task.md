# TT-API-12 — tareas

> Borrador. Va primero: fusiona antes de HU-001 y de cualquier dato guardado.

## T1 — Derivación UUID v8 con SHA-256
- **Estado:** pendiente de aprobación de la spec
- **Verificar:** TC-TT-API-12-01 · TC-TT-API-12-02 · TC-TT-API-12-03 · TC-TT-API-12-04 · TC-TT-API-12-07
- **Pendiente:** recalcular los vectores de forma independiente; reemplazar `UserIdDerivation.Derive` (SHA-256 del framework, versión 8, variante `10`); borrar el SHA-1 propio; actualizar `UserIdDerivationTests` conservando `_TC_TT_API_02_10`.

## T2 — Integración y privacidad
- **Estado:** pendiente
- **Verificar:** TC-TT-API-12-05 · TC-TT-API-12-06
- **Pendiente:** el `sub` del JWT y de `/me` es el v8 de `Derive(uid)`; `uid` y `sub` fuera de logs y respuestas (con el emulador de Firebase).

## T3 — Regresión, gates y evidencia
- **Estado:** pendiente
- **Verificar:** TC-TT-API-12-08 · TC-TT-API-12-09 · TC-TT-API-12-10 (y regresión de TC-TT-API-02-01..14)
- **Pendiente:** `grep` sin SHA-1 en `src/`, paquetes sin cambios, gitleaks, build y formato; salida en el PR; anotar en el PR que TC-TT-API-02-10 queda reemplazado (`Memoria.md` lo escribe el líder).
