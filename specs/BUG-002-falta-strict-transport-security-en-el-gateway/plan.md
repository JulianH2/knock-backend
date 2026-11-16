# plan.md — BUG-002 — Falta el encabezado Strict-Transport-Security en el gateway

> Spec aprobada el 2026-11-10 (KNOCK-239). **Implementada** (PR #85, fusionado) y verificada en el gateway desplegado el 2026-11-13 (TC-BUG-002-05; evidencia en `docs/modelo-de-amenazas.md`, sección «Evidencia de cierre»).

## 1. Mapeo de archivos
- **Reutilizar:** `SecurityHeadersMiddleware` del gateway (ya fija los demás encabezados), `Security:Hsts:MaxAge` de la configuración, `SecurityHostConfiguration` y las fábricas de las pruebas de seguridad. Antes de crear: `python scripts/codemap.py --query "hsts security headers gateway"`.
- **Crear:** `tests/SecurityTest/Gateway/HstsTests.cs` (y la fábrica del gateway si no existe), `tests/UnitTest/Gateway/HstsOptionsTests.cs`.
- **Modificar:** `src/Knock.Gateway/Hosting/SecurityHeadersMiddleware.cs`, `src/Knock.Gateway/Program.cs`, `src/Knock.Gateway/appsettings.json`, `src/Knock.Gateway/Hosting/SecuritySettings.cs` (nuevo, igual que en Identidad/Mercado), `docs/security-headers.md`, `docs/modelo-de-amenazas.md` (H-01), `Memoria.md`.
- **No tocar:** `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `docs/openapi.v1.json` a mano, specs ajenas.

## 2. Riesgos y efectos secundarios
Si la causa fuera la red de confianza mal configurada, la IP de origen también estaría mal para el límite de peticiones por IP (ver `docs/rate-limit-matrix.md`): se reporta aparte.
