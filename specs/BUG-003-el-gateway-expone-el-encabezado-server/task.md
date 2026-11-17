# BUG-003 — tareas

> Servicio afectado: Gateway y Mensajería. Spec aprobada el 2026-11-10. **Implementada**: código y pruebas en el PR #78 (fusionado) y verificación en vivo el 2026-11-13.

## T1 — Apagar el encabezado
- **Estado:** hecha (PR #78, KNOCK-240, fusionado). En el gateway y en Mensajería.
- **Verificar:** TC-BUG-003-01 · 02 · 03

## T2 — Documentación
- **Estado:** hecha. Documento y modelo de amenazas en el PR #78; `GET` al gateway desplegado el 2026-11-13 sin `Server` ni `X-Powered-By` en `/health`, `/sitemap.xml` y `/api/v1/me/x` (verificación del líder). H-02 cerrada.
- **Verificar:** TC-BUG-003-04
