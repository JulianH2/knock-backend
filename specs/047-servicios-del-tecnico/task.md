# Tareas 047 — Servicios y precios de cada técnico

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Perfil del técnico y catálogo de oficios activos (spec 046, hoy no implementada) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `GET /api/v1/tecnicos/{id}/servicios` (SRV-01): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-04 · CA-01 a CA-05 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `POST /api/v1/tecnicos/me/servicios` (SRV-02): DTO, validador, servicio, almacén, controlador y pruebas | R-05 a R-06 · CA-06 a CA-10 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `PUT /api/v1/tecnicos/me/servicios/{id}` (SRV-03): DTO, validador, servicio, almacén, controlador y pruebas | R-07 a R-09 · CA-11 a CA-16 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `DELETE /api/v1/tecnicos/me/servicios/{id}` (SRV-04): DTO, validador, servicio, almacén, controlador y pruebas | R-10 a R-12 · CA-17 a CA-21 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-13 a R-17 · CA-22 a CA-26 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-18 a R-21 · CA-27 a CA-30 | T-02 a T-06 | T-08 | por asignar | pendiente |
| T-08 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-06 | T-07 | por asignar | pendiente |
| T-09 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
