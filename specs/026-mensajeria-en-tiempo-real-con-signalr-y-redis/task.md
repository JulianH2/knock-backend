# Tareas 026 — Mensajería en tiempo real con SignalR y Redis

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Registrar Redis en Mensajería y agregarlo a `/health/ready`; saludo 503 sin Redis | R-15, R-16 · CA-14 | — | T-02, T-06 | Alexis | hecha (KNOCK-62): Redis en `/health/ready` (`AddKnockRedis`) y el saludo del hub responde 503 sin Redis. Prueba: `Spec026SaludTests` |
| T-02 | `ChatOptions` validadas al arrancar (`Chat:Habilitado` explícito, límites, dirección de Mercado) | R-08, R-11, R-17 · CA-17 | — | T-01, T-06 | Guillermo | hecha (KNOCK-62): `ChatOptions` y `MarketServiceOptions` validadas al arrancar. Pruebas: `Spec026ApagadoTests` y las de arranque de `Spec026*` |
| T-03 | `IVisitaCatalog` y `MarketVisitaCatalog` (sesión reenviada, 3 s, solo `rol` y `estado`, falla cerrada) | R-06, R-07, R-09 · CA-05, CA-06, CA-15 | T-02 | T-04 | Guillermo | hecha (KNOCK-62): `IVisitaCatalog` y `MarketVisitaCatalog`. Pruebas: `Spec026AutorizacionTests` (CA-05, CA-06, CA-15) |
| T-04 | `LimiteDeChat` y `MensajeService` (validación del texto, límite compartido, `mensajeId` UUID v7) | R-10 a R-14 · CA-08 a CA-11 | T-01 | T-03 | Alexis | hecha (KNOCK-62): `ChatRedisStore` (límite atómico) y `MensajeService`. Pruebas: `Spec026MensajesTests` y `Spec026LimiteTests` |
| T-05 | `ChatHub` y `UnionService`, puente de Redis, transportes y validación del token en el servicio con cierre al vencer | R-01, R-03 a R-05, R-12, R-20 · CA-01 a CA-04, CA-07, CA-16 | T-03, T-04 | T-06 | Guillermo | hecha (KNOCK-62): `ChatHub`, `UnionService`, puente de Redis y cierre al vencer el token (WebSockets y eventos del servidor, sin long polling). Pruebas: `Spec026ConexionTests`, `Spec026AutenticacionTests`, `Spec026PuenteTests` |
| T-06 | Gateway: ruta del hub, token por consulta solo ahí, conversión a encabezado, sin `access_token` en la URL ni en registros | R-02, R-18, R-19 · CA-18, CA-13 | — | T-01 a T-05 | José Julián (dueño del gateway) | hecha (KNOCK-62, por Guillermo): ruta `mensajeria-hub` y `HubQueryTokenMiddleware` en el gateway. Prueba: `GatewayRoutingTests` (CA-18) |
| T-07 | Pruebas de integración con dos hosts y Redis real, seguridad negativa y registros | CA-01 a CA-18 | T-05, T-06 | T-08 | Guillermo | hecha (KNOCK-62): `tests/IntegrationTest/Chat/Spec026*Tests.cs` con Redis real y dos copias del servicio; CA-18 en `GatewayRoutingTests` |
| T-08 | `docs/endpoints.md` (contrato del hub), `docs/rate-limit-matrix.md`, `Memoria.md` (Mensajería usa Redis, dependencias, excepción de duplicación) | §5, §12 | T-05 | T-07 | Alexis | hecha (KNOCK-62): `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `Memoria.md` |
| T-09 | Gates y revisión de par | §12 | T-07, T-08 | — | Guillermo (revisa José Julián) | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
