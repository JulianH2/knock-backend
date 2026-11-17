# Tareas 042 — Técnicos de confianza

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Almacén de técnicos y su visibilidad de las specs 027 y 028 (hoy no implementados) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `GET /api/v1/me/tecnicos-confianza` (TCO-01): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-02 · CA-01 a CA-03 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}` (TCO-02): DTO, validador, servicio, almacén, controlador y pruebas | R-03 a R-05 · CA-04 a CA-08 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}` (TCO-03): DTO, validador, servicio, almacén, controlador y pruebas | R-06 a R-08 · CA-09 a CA-12 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-09 a R-11 · CA-13 a CA-15 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-12 a R-15 · CA-16 a CA-19 | T-02 a T-05 | T-07 | por asignar | pendiente |
| T-07 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-05 | T-06 | por asignar | pendiente |
| T-08 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
