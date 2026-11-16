# BUG-004 — tareas

> Servicio afectado: Messaging. Spec aprobada el 2026-11-10. **Implementada** en el PR #82 (KNOCK-241); Mensajería desplegada el 2026-11-13.

## T1 — Autenticación JWT y sesión cancelada
- **Estado:** hecha (PR #82). Esquema JWT, política por defecto «exigir sesión», comprobación de sesión cancelada en Redis (503 si no responde) y arranque con falla rápida.
- **Verificar:** TC-BUG-004-01 · 02 · 05 · 06 · 08

## T2 — Encabezados de seguridad y sin Server
- **Estado:** hecha (PR #82 y PR #78). Middleware de encabezados y `AddServerHeader = false`.
- **Verificar:** TC-BUG-004-03

## T3 — Límite de peticiones
- **Estado:** hecha (PR #82). Limitador global por IP y propio de `/health/ready`, con `Retry-After`.
- **Verificar:** TC-BUG-004-04

## T4 — Documentación
- **Estado:** hecha (PR #82). Matriz de límites, encabezados y modelo de amenazas (H-03 cerrada el 2026-11-13). El «401 sin token» no se puede mostrar con `curl` porque Mensajería tiene ingreso interno; se cita la prueba del CI `SecurityTest/Messaging/AuthenticationTests` (TC-BUG-004-01).
- **Verificar:** TC-BUG-004-07
