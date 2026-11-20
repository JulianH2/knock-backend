# Tareas 030 — Chatear antes de elegir y mis conversaciones

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Responsables sugeridos:** Guillermo (KNOCK-72, chat y envío) y Erick (KNOCK-73, lista, contador y leído). Revisión de par cruzada entre ambos.

| Tarea | Qué se hace | Cubre | Depende de | En paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Ruta de permiso en Mercado (reglas de apertura, técnico resuelto por `sub`, 404 uniforme) sin publicarla en el gateway | R-02, R-03 · CA-06, CA-17 | specs 020, 021, 028, 029 | T-02, T-03 | Guillermo | hecha (KNOCK-72): `ConversationPermissionController`, `ConversationPermissionService`, `ConversationPermissionStore` y `ConversationRules` en Mercado (módulo Requests); el contrato `PermisoConversacion` vive en `Knock.Contracts`. Pruebas: `Spec030PermisoMercadoTests` (CA-17 con 404 uniforme) y `Spec030ConversationRulesTests`. El 404 del gateway (CA-17) lo cubre T-09 al agregar las rutas. Sin `envios` (spec 028 T-05) el técnico participa solo si cotizó |
| T-02 | Script de `db/` con `conversaciones`, `mensajes`, índices y TTL | R-21, §6 · CA-19 | — | T-01, T-03 | Erick | pendiente |
| T-03 | `OcultadorDeContacto` puro con unitarias y mutación | R-12 · CA-04 | — | T-01, T-02 | Guillermo | hecha (KNOCK-72): `OcultadorDeContacto` (función pura en `Modules/Conversaciones/Domain`) con enlaces de WhatsApp y Telegram, correos y teléfonos. Pruebas: `Spec030OcultadorDeContactoTests` (CA-04, la parte de ocultar). La mutación del ocultador (meta 70 %) queda para la corrida de Stryker |
| T-04 | Cliente `MarketPermisoDeConversacion` con caché de 60 s y 503 sin permiso; interfaz en `Knock.Contracts` | R-09 · CA-09 | T-01 | T-05 | Guillermo | hecha (KNOCK-72): interfaz `IPermisoDeConversacion` y sus tipos en `Knock.Contracts`; cliente `MarketPermisoDeConversacion` y caché `PermisoRedisCache` (`conv:permiso:{conversacionId}:{huella del sub}`, 60 s) en `Modules/Conversaciones/Data`. Sin permiso en caché y con Mercado caído responde `NoDisponible` (el 503 `market_unavailable` lo escribe el servicio de T-06). Pruebas: `Spec030PermisoClienteTests` (unitarias) y `Spec030PermisoCacheTests` (Redis real); CA-09 solo en la parte del cliente, el 503 de extremo a extremo llega con T-06. La llave lleva la persona porque el permiso trae el `rol` de quien pregunta |
| T-05 | Crear y leer conversación (idempotente por par, contraparte sin datos, aviso de seguridad) y límite de 20 por hora | R-01, R-04, R-05, R-06, R-07 · CA-03, CA-14, CA-15, CA-20 | T-02, T-04 | T-06 | Erick | pendiente |
| T-06 | Envío de mensajes: validación, límite de 30, ocultamiento, moderación (031), transacción y buzón | R-08, R-10, R-11, R-13, R-14 · CA-01, CA-02, CA-05, CA-07 | T-02, T-03, T-04; interfaz de la 031 | T-07 | Guillermo | pendiente |
| T-07 | Lista, `no-leidas`, mensajes paginados y `leido` con contadores; `no-store` | R-16, R-17, R-18, R-19, R-20 · CA-10, CA-11, CA-12, CA-13, CA-16 | T-02, T-05 | T-06 | Erick | pendiente |
| T-08 | Hub: unión a `usuario:{sub}`, `UnirseAConversacion`/`SalirDeConversacion`, eventos `mensajeConversacion` y `noLeidos`; regresión de la 026 | R-14, R-15 · CA-01, CA-03, CA-12 | T-06, T-07; spec 026 fusionada | — | Guillermo | pendiente |
| T-09 | Rutas del gateway, seguridad negativa completa y registros sin datos | R-22 · CA-03, CA-18, CA-20 | T-05 a T-08 | T-10 | Erick | pendiente |
| T-10 | `docs/endpoints.md`, `docs/events.md`, contrato de Mensajería (con autorización) y `Memoria.md` | R-14, R-15 | T-08 | T-09 | Erick | pendiente |

**Qué bloquea:** T-01 (Mercado) bloquea T-04 y todo el envío; la spec 031 bloquea la fusión de T-06 (no su desarrollo); la 026 fusionada bloquea T-08. **Qué va en paralelo:** T-01, T-02 y T-03 desde el primer día; T-06 (Guillermo) y T-07 (Erick) a la vez.

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
