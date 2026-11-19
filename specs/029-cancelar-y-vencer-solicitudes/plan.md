# Plan 029 — Cancelar y vencer solicitudes

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. `TransicionesDeSolicitud` como función pura con sus unitarias (R-01, R-05).
2. `CierreDeSolicitudService` con la cascada transaccional para cancelar y vencer (R-03, R-07, R-09).
3. Ruta de cancelación con validador, idempotencia y límite (R-02, R-04, R-06, R-14, R-15).
4. Manejadores `VencerSolicitud` y `BarrerVencidas` sobre el ejecutor de la spec 028; programación de la tarea en la creación y plazo configurable (R-08, R-10, R-11).
5. Campo `puedeVolverAPedir` y acceso del técnico no elegido en el detalle de la spec 020 (R-12, R-13).
6. Script de `db/` que comprueba índices y programa el barrido; documentación y contrato.

## Archivos que se crean o cambian
- `src/Knock.Market.Api/Modules/Requests/Domain/TransicionesDeSolicitud.cs`.
- `src/Knock.Market.Api/Modules/Requests/Services/CierreDeSolicitudService.cs`, `Services/Tareas/VencerSolicitudHandler.cs`, `Services/Tareas/BarrerVencidasHandler.cs`.
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/CancelacionController.cs`, `Dtos/`, `Validators/CancelacionValidator.cs`, `Options/SolicitudesOptions.cs`.
- Cambios en `SolicitudService` y en la vista del detalle (spec 020).
- `db/0NN_requests_vencimiento.js`; `db/entrega.js` lo carga.
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest`, `tests/DatabaseTest` (`Spec029*`).
- `docs/openapi.v1.json` (regenerado), `docs/endpoints.md`, `docs/events.md`, `docs/rate-limit-matrix.md`, `Memoria.md`.

## Dependencias
- Specs 020 y 021 implementadas; ejecutor de tareas de la spec 028 (T-02 de la 028). Si la 028 no está, la T-02 de la 028 se adelanta aquí.

## Decisiones de la spec que este plan sigue
- Una sola cascada para los dos cierres; tarea por solicitud más barrido; estados finales.

## Riesgos del plan
- La carrera con la elección (CA-05) se prueba con dos transacciones reales; si DocumentDB no admite transacciones, el riesgo es el mismo de la 015.

## Verificación
- Cada criterio con su prueba de la tabla 4.1; `python scripts/sdd_lint.py --trace 029`; `python scripts/export_openapi.py`; comandos de `AGENTS.md`; `Memoria.md` al día.
