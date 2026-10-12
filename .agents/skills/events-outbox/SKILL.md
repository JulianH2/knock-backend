---
name: events-outbox
description: Eventos, saga y plazos programados en Knock con MassTransit y outbox transaccional de EF Core (oleadas a 15 min, vencimiento a 48 h, gracia de 7 días, borrados programados). Úsala cuando una HU tenga pasos asíncronos o plazos.
---

# events-outbox

## Reglas
- Publicar siempre dentro de la transacción del cambio de estado: `AddEntityFrameworkOutbox<TDbContext>()` + `UseBusOutbox()`.
- Plazos con mensajes programados (`ScheduleSend` / `SchedulePublish`), **nunca** `Task.Delay`, timers ni `BackgroundService` con `while`.
- Consumidores **idempotentes**: guardar el `MessageId` procesado o verificar el estado antes de actuar (si la solicitud ya venció, ignorar).
- Reintentos: `UseMessageRetry(r => r.Exponential(5, 1s, 60s, 5s))`; luego cola de errores (DLQ).
- Transporte: InMemory en Dev/Test (con `UseDelayedMessageScheduler`), Azure Service Bus en nube.
- Nombres en pasado para eventos (`QuoteAccepted`) e imperativo para comandos/plazos (`ReviewWave`).
- Todo evento nuevo se registra en `docs/events.md`.

## Flujos de Knock
- **Oleadas (decisión #6):** `RequestCreated` → seleccionar hasta 5 técnicos con «Disponible hoy» (3 mejor ordenados, 1 nuevo, 1 cercano) → programar `ReviewWave` a +15 min → si hay < 2 cotizaciones, ampliar → programar `ExpireRequest` a +48 h.
- **Eliminar cuenta (decisión #11):** `AccountDeletionScheduled` → `ExecuteAccountDeletion` a +7 días, cancelable.
- **Verificación (HU de Trust):** `VerificationSubmitted` → `AlertVerifier` a +24 h y `PurgeVerificationMedia` a +7 días (borra las fotos y destruye la llave; estado vuelve a «Sin verificar» con aviso). Al decidir antes, se purga en el mismo momento.
- **Disponible hoy:** se apaga a medianoche (America/Mexico_City) con un mensaje programado.
- La librería (MassTransit 8 o Wolverine) se fija en TT-API-04; las reglas de arriba aplican a ambas.
- **Ventanas:** reseña hasta 30 días, reporte «no se terminó» hasta 7 días: se validan con fecha en el servicio, no con mensajes.

## Verificación
- Pruebas con `ITestHarness` de MassTransit: el evento se publica una vez, el consumidor es idempotente, el plazo se programa con el retraso correcto (usar `TimeProvider` falso).
