# Plan 030 — Chatear antes de elegir y mis conversaciones

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Ruta de permiso en Mercado (módulo Requests), no publicada en el gateway (R-02, R-03).
2. Script de `db/` para `conversaciones` y `mensajes` en `messaging`, con el TTL de 180 días (§6, R-21).
3. Módulo `Conversaciones` en Mensajería: `OcultadorDeContacto` (puro), cliente de permiso con caché, stores, servicios y controllers (R-01, R-04 a R-14, R-16 a R-20, R-22).
4. Integración con el hub de la spec 026: grupos `usuario:{sub}` y `conversacion:{id}`, métodos nuevos y publicación tras confirmar (R-14, R-15, R-19).
5. Moderación con la interfaz de la spec 031 y límites en Redis (R-11, R-13).
6. Pruebas desde los criterios, contrato de Mensajería y documentación.

## Archivos que se crean o cambian
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/PermisoConversacionController.cs`, `Services/PermisoConversacionService.cs`, `Dtos/`.
- `src/Knock.Messaging.Api/Modules/Conversaciones/` (`Controllers`, `Dtos`, `Validators`, `Services`, `Domain/OcultadorDeContacto.cs`, `Data/`).
- `src/Knock.Messaging.Api/Modules/Chat/Hubs/ChatHub.cs` (métodos nuevos y unión a `usuario:{sub}`).
- `src/Knock.Contracts/` (`IPermisoDeConversacion` y sus tipos, sin lógica).
- `src/Knock.Gateway/` (rutas `/api/v1/conversaciones*` hacia Mensajería; la ruta de permiso **no** se agrega).
- `db/0NN_messaging_conversaciones.js`; `db/entrega.js` lo carga.
- `tests/` (`Spec030*` en UnitTest, IntegrationTest, SecurityTest, DatabaseTest).
- `docs/endpoints.md`, `docs/events.md`, `Memoria.md`; contrato de Mensajería (requiere autorización para tocar `scripts/export_openapi.py`).

## Dependencias
- Spec 026 implementada (hub, Redis de Mensajería, `Services:Market:BaseUrl`).
- Spec 031 implementada o, mientras tanto, la interfaz con un moderador que siempre responde «no disponible» (falla cerrada); no se fusiona con un moderador que apruebe todo.
- Specs 020, 021, 028 y 029 para el cálculo del permiso.

## Decisiones de la spec que este plan sigue
- Enviar por REST y entregar por el hub; conversación por par; permiso en Mercado con caché de 60 s; contacto oculto antes de elegir; contadores guardados.

## Riesgos del plan
- Tocar el hub de la 026 mientras otra persona lo implementa: se hace después de fusionar la 026 o en la misma rama.
- El contrato de Mensajería necesita cambiar un script protegido.

## Verificación
- Cada criterio con su prueba de la tabla 4.1; `python scripts/sdd_lint.py --trace 030`; comandos de `AGENTS.md`; `Memoria.md` al día.
