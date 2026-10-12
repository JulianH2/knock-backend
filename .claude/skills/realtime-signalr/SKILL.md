---
name: realtime-signalr
description: Chat y avisos en tiempo real de Knock con SignalR (+ Redis backplane) y FCM cuando el destinatario no está conectado. Úsala en el módulo Messaging y para avisos en vivo.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/realtime-signalr/SKILL.md. No editar a mano. -->
# realtime-signalr

## Diseño
- Hub `/hubs/messaging` con `[Authorize]`; el JWT llega por `access_token` en el query solo para la ruta del hub.
- Grupos por conversación (`conv:{id}`); al conectar, el servidor valida que el usuario participe.
- Mensajes se **persisten primero** (esquema `messaging`) y luego se emiten al grupo.
- Si el destinatario no tiene conexión activa (presencia en Redis con TTL), se publica `MessageSent` → Notifications envía FCM.
- Varias instancias: `AddSignalR().AddStackExchangeRedis(...)`.
- Antes de elegir cotización: número oculto y advertencia de pagos por adelantado. Después de elegir: se habilita el botón de WhatsApp (decisión #5).

## Cliente (lo implementa knock-frontend)
- Paquete `signalr_netcore`; reconexión automática con backoff; el repositorio expone un `Stream<Message>`.
- Si el socket falla, la conversación se recarga por REST (`GET /conversations/{id}/messages?cursor=`).

## Seguridad
- Rate limit `Chat` 60/min por usuario.
- Texto máximo 2000 caracteres; filtro que detecta teléfonos y avisos de pago por adelantado → advertencia, no bloqueo.

## Verificación
- Pruebas de integración con `HubConnection` contra `WebApplicationFactory`: un no participante no recibe mensajes; mensaje persistido antes de emitirse.
