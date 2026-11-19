# Plan 028 — Repartir la solicitud por oleadas

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Script de `db/` con `envios`, el índice único de `tareas_programadas`, el TTL parcial y el índice de elegibilidad de `tecnicos` (§6).
2. Ejecutor de tareas programadas genérico en `Common/TareasProgramadas/` con reserva atómica, reintentos y registro de manejadores (R-09, R-10). Se construye primero porque también lo usa la spec 029.
3. `SeleccionDeOleada` como función pura con sus unitarias y su prueba de rendimiento (R-02 a R-04).
4. `RepartoService` con los manejadores `RepartirOleada` y `RevisarOleada` y la transacción de R-05 a R-07; programación de la primera tarea dentro de la creación de la 020 (R-01).
5. Cambios en la cotización de la 020: envío obligatorio y tope de 5 (R-11, R-12); ampliación de la vista del detalle (R-15).
6. Bandeja y descarte con sus controllers, validadores y límite (R-13, R-14).
7. Opciones validadas al arrancar e interruptor (R-08); eventos en `docs/events.md` (R-16); registros sin datos (R-17).

## Archivos que se crean o cambian
- `db/0NN_requests_envios.js`; `db/entrega.js` lo carga.
- `src/Knock.Market.Api/Common/TareasProgramadas/` (`EjecutorDeTareasProgramadas`, `IManejadorDeTarea`, `TareasProgramadasStore`, `TareasProgramadasOptions`).
- `src/Knock.Market.Api/Modules/Requests/Domain/SeleccionDeOleada.cs`, `Services/RepartoService.cs`, `Data/EnviosStore.cs`, `Data/CandidatosStore.cs`, `Options/OleadasOptions.cs`.
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/BandejaController.cs`, `DescarteController.cs`, `Dtos/`, `Validators/`.
- Cambios en `SolicitudService`, `CotizacionService` y `RequestsStore` de la spec 020.
- `src/Knock.Gateway/` (rutas nuevas con la política de sesión).
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest`, `tests/DatabaseTest`, `tests/PerformanceTest` (`Spec028*`).
- `docs/openapi.v1.json` (regenerado), `docs/endpoints.md`, `docs/events.md`, `Memoria.md`.

## Dependencias
- La spec 020 implementada (módulo Requests, `MarketApiFactory`, transacciones y buzón).
- La spec 027 o al menos sus índices de `tecnicos` (`ubicacionBase` 2dsphere).
- Semillas de `tecnicos` con `disponibleHasta`, `trabajosTerminados` y `usuarioId` (KNOCK-246).

## Decisiones de la spec que este plan sigue
- Reparto asíncrono con tareas programadas y ejecutor propio sobre MongoDB.
- Cotizar exige envío con el interruptor `Oleadas:Habilitado`.
- Tope de 5 con contador condicionado.

## Riesgos del plan
- Cambiar la 020 mientras otra persona la implementa: se coordina en la misma rama o se implementa la 028 después de fusionar la 020.
- El ejecutor es infraestructura compartida: su PR debe revisarlo quien implemente la 029.

## Verificación
- Cada criterio de `spec.md` con su prueba de la tabla 4.1; `python scripts/sdd_lint.py --trace 028`; `python scripts/export_openapi.py`; comandos de `AGENTS.md`; `Memoria.md` al día.
