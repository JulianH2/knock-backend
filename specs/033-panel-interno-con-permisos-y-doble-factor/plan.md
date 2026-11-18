# Plan 033 — Panel interno con permisos y doble factor (backend)

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Contrato primero: publicar en el OpenAPI de Identity los cuatro endpoints con sus cuerpos, la cookie y los códigos, para que frontend (spec 023) genere su cliente y avance con una API simulada mientras se implementa.
2. Dominio puro: roles, permisos, mapa de roles, reglas de TOTP (ventana, paso mayor) y de sesión (vidas, máximo de tres), con pruebas unitarias y reloj simulado.
3. Datos: script de `db/` con `panel_cuentas`, `panel_sesiones` y `panel_auditoria`, sus índices y TTL; almacenes de MongoDB y de Redis (desafío, fallos, caché de la cuenta).
4. Herramienta interna (`panel alta`, `roles`, `deshabilitar`, `habilitar`, `reiniciar-totp`), necesaria para crear la cuenta de prueba de todo lo demás.
5. Servicios y controller de sesión: celular, TOTP, renovar y cerrar, con auditoría que falla cerrada.
6. Política `RequierePermiso`, esquema `PanelBearer` y prueba de arquitectura.
7. Gateway: esquema JWT del panel, rutas, límites y CORS `panel`.
8. Seguridad negativa, simultaneidad con dos hosts y recorrido de punta a punta con el panel.

## Archivos que se crean o cambian
- `db/NNN-identity-panel.js` (siguiente número libre).
- `src/Knock.Identity.Api/Modules/Panel/` (`Controllers`, `Dtos`, `Validators`, `Services`, `Domain`, `Data`, `Authorization`, `Cli`).
- `src/Knock.Identity.Api/Program.cs` (modo de herramienta y registro del módulo) y su configuración (`PanelJwt`, `Cors:Panel`).
- `src/Knock.Gateway/` (esquema JWT del panel, rutas `/api/v1/panel/**`, política estricta por IP para `/api/v1/panel/sesion/*`, política CORS `panel`).
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest`, `tests/ArchitectureTest`, `tests/ContractTest` (clases `Spec033*`).
- `docs/openapi.identity.v1.json` (regenerado con el script), `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `Memoria.md`.

## Dependencias
- Specs 009, 015, 016 y 018 implementadas (verificador de Firebase, `sub`, cancelación por `sid`).
- Llaves nuevas en Key Vault creadas por una persona: `PanelJwt:SigningKey` y `panel-totp-key`.
- Origen definitivo del panel en Railway para `Cors:Panel:Origen`.
- Paquete `Otp.NET` evaluado y registrado.

## Riesgos del plan
- La cookie `SameSite=None; Partitioned` se comporta distinto por navegador; el recorrido de punta a punta se prueba en Chrome y Firefox y el resultado se anota en el PR.
- El cambio en el gateway (segundo esquema JWT) puede afectar las rutas de la app: la regresión de 015 a 018 es obligatoria antes de integrar.

## Decisiones de la spec que este plan sigue
- Sesión separada con llave y destinatario propios; permisos fuera del token con `pv`; cuentas ligadas a la de la app; auditoría que falla cerrada; altas solo por herramienta interna.

## Verificación
- `dotnet build Knock.slnx -c Release`, `dotnet test Knock.slnx -c Release`, `dotnet format Knock.slnx --verify-no-changes`.
- `python scripts/sdd_lint.py --trace 033` sin faltantes.
- `python scripts/export_openapi.py --service identity` y contrato sin cambios pendientes; `oasdiff` sin cambios incompatibles.
