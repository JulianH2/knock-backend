# plan.md — BUG-004 — Mensajería sin JWT, encabezados de seguridad ni límite de peticiones propios

> Spec aprobada el 2026-11-10 (KNOCK-241). **Implementada** (PR #82, fusionado; EXC-021 de 6 archivos ligada a KNOCK-251; Terraform en el PR #86). Mensajería corre desplegada (revisión `0000004`, Healthy, con `Jwt__SigningKey` y la conexión a Redis, ingreso interno).

## 1. Mapeo de archivos
- **Reutilizar:** `Hosting/AuthenticationExtensions`, `SecurityExtensions`, `RateLimitingExtensions` y `RevokedSessions` de Identidad/Mercado/gateway; `SecurityMarketFactory` y `SecurityHostConfiguration` como molde de pruebas. Antes de crear: `python scripts/codemap.py --query "jwt authentication rate limiting security headers"`.
- **Crear:** `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs`, `SecurityExtensions.cs`, `RateLimitingExtensions.cs`, `RevokedSessions.cs`; `tests/SecurityTest/Messaging/*` y su fábrica (extern alias `MessagingApi`); `tests/UnitTest/Messaging/AuthenticationStartupTests.cs`.
- **Modificar:** `src/Knock.Messaging.Api/Program.cs`, `Knock.Messaging.Api.csproj`, `appsettings.json`, `docs/rate-limit-matrix.md`, `docs/security-headers.md`, `docs/modelo-de-amenazas.md` (H-03), `Memoria.md`.
- **No tocar:** `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `docs/openapi.v1.json` a mano, specs ajenas.

## 2. Riesgos y efectos secundarios
Copiar tres extensiones suma duplicados que EXC-007 NO cubre (solo ampara logging, observabilidad y `TelemetryRedactor`): hay que pedir una excepción jscpd propia y temporal ligada a KNOCK-251, o llevar las clases directo a la biblioteca compartida. Una configuración nueva obligatoria hace que Mensajería no arranque si falta la llave en el entorno: hay que avisar al líder antes del despliegue.
