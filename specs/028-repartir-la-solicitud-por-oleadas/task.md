# Tareas 028 — Repartir la solicitud por oleadas

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Responsable sugerido de la historia:** Guillermo (KNOCK-68). Revisión de par: Alexis.

| Tarea | Qué se hace | Cubre | Depende de | En paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Script de `db/` con `envios`, índice único de tareas, TTL parcial e índice de elegibilidad de `tecnicos` | R-10, §6 · CA-05 | spec 020 fusionada | T-02, T-03 | Alexis | hecha (índices; el reparto de CA-05 depende de T-06) |
| T-02 | Ejecutor de tareas programadas (reserva atómica, arrendamiento, reintentos, manejadores) con pruebas de dos instancias | R-09, R-10 · CA-05, CA-15 | — | T-01, T-03 | Alexis | hecha; protección de efectos y reparto de CA-05 dependen de T-06 |
| T-03 | `SeleccionDeOleada` pura con unitarias, mutación y rendimiento | R-02, R-03, R-04 · CA-01, CA-04, CA-07, CA-08 | — | T-01, T-02 | Alexis | hecha; CA-04 completo depende del filtro de candidatos de T-05 |
| T-04 | `OleadasOptions` validadas al arrancar e interruptor | R-08 · CA-16 | — | T-01 a T-03 | Alexis | hecha (opciones y validación); comportamiento del interruptor depende de T-06 |
| T-05 | `CandidatosStore` y `EnviosStore` con integración (`$geoNear`, filtros) | R-02, R-05 · CA-04 | T-01 | T-06 | Guillermo | pendiente |
| T-06 | `RepartoService`: manejadores `RepartirOleada` y `RevisarOleada`, transacción, eventos y `SolicitudSinTecnicos`; programación en la creación (020) | R-01, R-05, R-06, R-07, R-16 · CA-01, CA-02, CA-03, CA-09, CA-10 | T-02, T-03, T-05 | T-07 | Guillermo | pendiente |
| T-07 | Cotizar exige envío y tope de 5 con contador condicionado; vista del destinatario en el detalle | R-11, R-12, R-15 · CA-06, CA-11, CA-14 | T-05 | T-06, T-08 | Guillermo | pendiente |
| T-08 | Bandeja y descarte (controllers, validadores, límite de 30 por hora) | R-13, R-14 · CA-12, CA-13, CA-18 | T-05 | T-07 | Alexis | pendiente |
| T-09 | Seguridad negativa, registros y eventos sin datos personales; regresión de la 020 con el interruptor apagado | R-11, R-13, R-14, R-17 · CA-11, CA-12, CA-16, CA-17, CA-18 | T-06, T-07, T-08 | T-10 | Alexis | pendiente |
| T-10 | OpenAPI regenerado, `docs/endpoints.md`, `docs/events.md` y `Memoria.md` | R-16 | T-07, T-08 | T-09 | Guillermo | pendiente |

**Qué bloquea:** T-02 (ejecutor) bloquea la 028 y la 029; T-06 espera T-02, T-03 y T-05. **Qué va en paralelo:** T-01 a T-04 el mismo día con dos personas; T-07 y T-08 en paralelo una vez listo T-05.

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
