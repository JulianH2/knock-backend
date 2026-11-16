# plan.md — BUG-003 — El gateway y Mensajería exponen el encabezado Server: Kestrel

> Spec aprobada el 2026-11-10 (KNOCK-240). **Implementada** (PR #78, fusionado) y verificada en el gateway desplegado el 2026-11-13: ninguna de las tres rutas probadas devuelve `Server` ni `X-Powered-By` (TC-BUG-003-04; tabla en `docs/modelo-de-amenazas.md`).

## 1. Mapeo de archivos
- **Reutilizar:** `Hosting/SecurityExtensions.cs` de Identidad y Mercado (patrón), `SecurityHeadersMiddleware` del gateway, `SecurityMarketFactory` como molde. Antes de crear: `python scripts/codemap.py --query "AddServerHeader kestrel security"`.
- **Crear:** `tests/SecurityTest/Gateway/ServerHeaderTests.cs`, `tests/SecurityTest/Messaging/ServerHeaderTests.cs`, fábricas de los dos hosts si no existen.
- **Modificar:** `src/Knock.Gateway/Program.cs`, `src/Knock.Messaging.Api/Program.cs`, `docs/security-headers.md`, `docs/modelo-de-amenazas.md` (H-02), `Memoria.md`.
- **No tocar:** `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `docs/openapi.v1.json` a mano, specs ajenas.

## 2. Riesgos y efectos secundarios
Ninguno relevante; el cambio es mínimo.
