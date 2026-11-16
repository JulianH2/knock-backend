# Tareas 054 — Bandeja de salida y tareas programadas (INT-01)

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: entrega interna de avisos y claves por emisor (spec 037), ejecutor de tareas de la spec 028 y biblioteca compartida (KNOCK-251) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con índices, índice único y TTL de las dos colecciones y prueba de base de datos | §6 · R-01, R-06, R-11 · CA-09, CA-18 | — | T-02, T-04 | por asignar | pendiente |
| T-02 | Escritura del evento en la misma transacción, `_id` determinista y carga sin datos personales | R-02 · CA-02 a CA-04 | T-01 | T-04 | por asignar | pendiente |
| T-03 | `ProcesadorDeOutbox` con reserva atómica, entrega a `/interno/avisos`, reintentos y `fallido` | R-03 a R-05 · CA-05 a CA-08 | T-01, T-02 | T-04 | por asignar | pendiente |
| T-04 | Ejecutor de tareas de Identidad con `IManejadorDeTarea`, reintentos y arrendamiento | R-07, R-09 · CA-10, CA-11, CA-14, CA-15 | T-01 | T-02, T-03 | por asignar | pendiente |
| T-05 | Catálogo cerrado de 12 tipos y plazos; tipo desconocido pasa a `fallida` | R-08 · CA-12, CA-13 | T-04 | T-03 | por asignar | pendiente |
| T-06 | Concurrencia: dos réplicas, reserva vencida y tiempo simulado | R-10, R-12 · CA-16, CA-17, CA-19 | T-03, T-04 | — | por asignar | pendiente |
| T-07 | Métricas, alerta, registros sin datos personales y salud independiente de la cola | R-13, R-14 · CA-20, CA-21 | T-03, T-04 | T-06 | por asignar | pendiente |
| T-08 | Sin rutas públicas: prueba de arquitectura y de rutas del gateway | R-01 · CA-01 | T-03, T-04 | T-07 | por asignar | pendiente |
| T-09 | Esquema de llamadas internas con `permisos` por ruta y rutas nuevas de las specs 039 y 041 | R-15 · CA-22 | T-03 | T-07 | por asignar | pendiente |
| T-10 | Gates, `docs/observabilidad.md`, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
