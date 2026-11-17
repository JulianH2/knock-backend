# Tareas 051 — Opiniones y reputación del técnico

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Moderación de la spec 031 y entrega de imágenes de la 032 (hoy no implementadas); Tareas de cierre del ejecutor de la 054 (hoy no implementado) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `GET /api/v1/tecnicos/{id}/opiniones` (OPI-01): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-05 · CA-01 a CA-06 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `POST /api/v1/visitas/{id}/opinion` (OPI-02): DTO, validador, servicio, almacén, controlador y pruebas | R-06 a R-08 · CA-07 a CA-12 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `POST /api/v1/opiniones/{id}/respuesta` (OPI-03): DTO, validador, servicio, almacén, controlador y pruebas | R-09 a R-11 · CA-13 a CA-18 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-12 a R-17 · CA-19 a CA-24 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-18 a R-21 · CA-25 a CA-28 | T-02 a T-05 | T-07 | por asignar | pendiente |
| T-07 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-05 | T-06 | por asignar | pendiente |
| T-08 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
