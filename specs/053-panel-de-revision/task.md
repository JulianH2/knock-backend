# Tareas 053 — Panel de revisión: verificaciones de identidad y reportes

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Panel con doble factor, permisos y auditoría de la spec 033 (hoy no implementados); Cuenta de almacenamiento privada y cifrado de fotos de la spec 025 (hoy no existen en Terraform); Moderación de la spec 031 (hoy no implementada) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `GET /api/v1/admin/verificaciones` (VER-03): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-03 · CA-01 a CA-05 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `GET /api/v1/admin/verificaciones/{id}` (VER-04): DTO, validador, servicio, almacén, controlador y pruebas | R-04 a R-06 · CA-06 a CA-11 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `POST /api/v1/admin/verificaciones/{id}/decision` (VER-05): DTO, validador, servicio, almacén, controlador y pruebas | R-07 a R-09 · CA-12 a CA-18 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `GET /api/v1/admin/reportes` (REP-04): DTO, validador, servicio, almacén, controlador y pruebas | R-10 a R-12 · CA-19 a CA-23 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | `POST /api/v1/admin/reportes/{id}/decision` (REP-05): DTO, validador, servicio, almacén, controlador y pruebas | R-13 a R-15 · CA-24 a CA-29 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-16 a R-22 · CA-30 a CA-36 | T-01 | otras rutas | por asignar | pendiente |
| T-08 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-23 a R-26 · CA-37 a CA-40 | T-02 a T-07 | T-09 | por asignar | pendiente |
| T-09 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-07 | T-08 | por asignar | pendiente |
| T-10 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
