# Plan 026 — Mensajería en tiempo real con SignalR y Redis

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Registrar Redis en el arranque de Mensajería (`AddKnockRedis`, contraseña obligatoria) e incluirlo en `/health/ready`.
2. Crear el módulo `Chat` con `ChatOptions` validadas al arrancar (`Chat:Habilitado`, límites, `Services:Market:*`).
3. Escribir `IVisitaCatalog` en `Knock.Contracts` y `MarketVisitaCatalog` (cliente HTTP tipado, sesión reenviada, 3 s, solo `rol` y `estado`).
4. Escribir `LimiteDeChat` (incremento y vencimiento atómicos en Redis) y `MensajeService`.
5. Escribir `ChatHub` y `UnionService`; configurar el puente `AddStackExchangeRedis` con prefijo `knock:chat`, transportes y validación del token en el servicio.
6. Agregar al gateway la ruta `mensajeria-chat-hub` con lectura del token desde la consulta solo para esa ruta, conversión a `Authorization`, eliminación de `access_token` y reenvío del salto de protocolo.
7. Documentar el hub en `docs/endpoints.md` y la decisión en `Memoria.md`.

## Archivos que se crean o cambian
- `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs` (copia con su excepción de duplicación) y `Program.cs`
- `src/Knock.Messaging.Api/Modules/Chat/` (`Hubs/ChatHub.cs`, `Services/`, `Data/MarketVisitaCatalog.cs`, `Options/ChatOptions.cs`)
- `src/Knock.Contracts/Visitas/IVisitaCatalog.cs`
- `src/Knock.Gateway/` (ruta, transformaciones y autenticación por consulta acotada a `/hubs/chat`)
- `tests/` (clases `Spec026*`, con dos hosts de Mensajería y Redis real)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `Memoria.md`

## Dependencias
- Spec 022 (vista de la visita con `rol`) fusionada o, para avanzar antes, un servidor HTTP de prueba que la simule.
- Specs 003, 006, 009, 011 y 016; Redis ya desplegado (KNOCK-184).

## Orden y paralelismo
- T-01 (Redis y salud), T-02 (opciones) y T-06 (gateway) se pueden hacer a la vez desde el primer día.
- T-03 y T-04 en paralelo; T-05 (hub) integra; las pruebas de dos copias al final.
- No bloquea la cadena de cotizaciones (P3): si no cabe en el Sprint 4, pasa al 5.

## Verificación
- `python scripts/sdd_lint.py --trace 026` sin faltantes; comandos de `AGENTS.md`; contrato OpenAPI sin cambios; `Memoria.md` al día.
