# Tareas 029 — Cancelar y vencer solicitudes

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Responsable sugerido de las historias:** Guillermo (KNOCK-70 y KNOCK-71). Revisión de par: Erick.

| Tarea | Qué se hace | Cubre | Depende de | En paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | `TransicionesDeSolicitud` pura con unitarias y mutación | R-01, R-05 · CA-02, CA-11 | — | T-02, T-05 | Alexis | hecha |
| T-02 | `CierreDeSolicitudService`: cascada transaccional (solicitud, cotizaciones, envíos, evento) | R-03, R-07, R-09 · CA-01, CA-06, CA-15 | T-01; specs 020, 021 y 028 (envíos) | T-05 | Alexis | parcial: cascada y reversión probadas; faltan cobertura completa de criterios e integración con envíos de 028 |
| T-03 | Ruta de cancelación: controller, validador, idempotencia, `no-store`, límite de 10 por hora | R-02, R-04, R-06, R-14, R-15 · CA-03, CA-04, CA-05, CA-12, CA-13 | T-02 | T-04 | Alexis | pendiente |
| T-04 | Manejadores `VencerSolicitud` y `BarrerVencidas`; programación en la creación; opciones del plazo validadas | R-08, R-10, R-11 · CA-06, CA-07, CA-08, CA-09, CA-14 | T-02; ejecutor (T-02 de la 028) | T-03 | Guillermo | pendiente |
| T-05 | `puedeVolverAPedir` en el detalle y 404 para técnicos no elegidos en solicitudes cerradas | R-12, R-13 · CA-10, CA-11 | — | T-01, T-02 | Alexis | pendiente |
| T-06 | Script de `db/` (índices y programación idempotente del barrido) | R-10 · CA-09 | T-04 | T-07 | Guillermo | pendiente |
| T-07 | Pruebas de concurrencia (cancelar contra elegir; dos ejecutores) y seguridad negativa; registros sin datos | R-06, R-11, R-15 · CA-05, CA-08, CA-13, CA-15 | T-03, T-04 | T-06 | Guillermo | pendiente |
| T-08 | OpenAPI regenerado, `docs/endpoints.md`, `docs/events.md`, `docs/rate-limit-matrix.md`, `Memoria.md` | R-02, R-07 | T-03, T-05 | T-07 | Erick | pendiente |

**Qué bloquea:** el ejecutor de la 028 bloquea T-04; T-02 bloquea T-03 y T-04. **Qué va en paralelo:** T-01, T-05 y la parte de la cascada sin envíos arrancan sin esperar a la 028.

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
