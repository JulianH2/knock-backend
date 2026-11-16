# BUG-002 — tareas

> Servicio afectado: Gateway. Spec aprobada el 2026-11-10. **Implementada** en el PR #85 (KNOCK-239) y verificada en vivo el 2026-11-13.

## T1 — Reproducir y diagnosticar
- **Estado:** hecha (PR #85). `SecurityTest/Gateway/HstsTests` falla sin el arreglo (3 de 4) y pasa con él.
- **Verificar:** TC-BUG-002-01 · 02

## T2 — Corregir y probar
- **Estado:** hecha (PR #85). Encabezado en `SecurityHeadersMiddleware`, opciones `Security:Hsts` y sin envío en Development.
- **Verificar:** TC-BUG-002-03 · 04

## T3 — Documentar y verificar en vivo
- **Estado:** hecha. `docs/security-headers.md` y modelo de amenazas en el PR #85; `GET` al gateway desplegado el 2026-11-13 con `Strict-Transport-Security: max-age=31536000; includeSubDomains` en `/health`, `/sitemap.xml` y `/api/v1/me/x` (verificación del líder; tabla en el modelo de amenazas).
- **Verificar:** TC-BUG-002-05
- **Dependencia:** el humo de BUG-001 (spec 004 del CI, job `dast`) solo podrá comprobar HSTS cuando BUG-002 esté desplegado; contra `http://localhost` el encabezado no sale a propósito (TC-BUG-002-04).
