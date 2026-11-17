# Tareas 041 — Mi casa y mi auto

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: El campo opcional `elementoId` en `POST /solicitudes` (se agrega en una tarea de esta spec; la fundación de la 020 ya está en `sprint-04`); Ruta interna de historial con el secreto nuevo `interno-key-identity` (hoy no implementada ni creada) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `GET /api/v1/me/casa-y-auto` (CAS-01): DTO, validador, servicio, almacén, controlador y pruebas | R-01 · CA-01 a CA-02 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `POST /api/v1/me/casa-y-auto` (CAS-02): DTO, validador, servicio, almacén, controlador y pruebas | R-02 a R-03 · CA-03 a CA-06 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `PUT /api/v1/me/casa-y-auto/{id}` (CAS-03): DTO, validador, servicio, almacén, controlador y pruebas | R-04 a R-06 · CA-07 a CA-12 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `DELETE /api/v1/me/casa-y-auto/{id}` (CAS-04): DTO, validador, servicio, almacén, controlador y pruebas | R-07 a R-09 · CA-13 a CA-17 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | `GET /api/v1/me/casa-y-auto/{id}/historial` (CAS-05): DTO, validador, servicio, almacén, controlador y pruebas | R-10 a R-13 · CA-18 a CA-23 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-14 a R-19 · CA-24 a CA-29 | T-01 | otras rutas | por asignar | pendiente |
| T-08 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-20 a R-23 · CA-30 a CA-33 | T-02 a T-07 | T-09 | por asignar | pendiente |
| T-09 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-07 | T-08 | por asignar | pendiente |
| T-10 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
