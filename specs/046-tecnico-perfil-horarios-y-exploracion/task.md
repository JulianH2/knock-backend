# Tareas 046 — Técnico: perfil, horarios, disponibilidad, mapa y explorar

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Almacén de búsqueda de la spec 027 y elegibilidad de la 028 (hoy no implementados); `disponibleHasta` (HU-019): hoy solo existe en las semillas | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `POST /api/v1/tecnicos` (TEC-01): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-03 · CA-01 a CA-05 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `GET /api/v1/tecnicos` (TEC-02): DTO, validador, servicio, almacén, controlador y pruebas | R-04 a R-07 · CA-06 a CA-09 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `GET /api/v1/tecnicos/autocompletar` (TEC-03): DTO, validador, servicio, almacén, controlador y pruebas | R-08 a R-10 · CA-10 a CA-12 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `GET /api/v1/mapa/tecnicos` (TEC-04): DTO, validador, servicio, almacén, controlador y pruebas | R-11 a R-13 · CA-13 a CA-15 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | `GET /api/v1/tecnicos/{id}` (TEC-05): DTO, validador, servicio, almacén, controlador y pruebas | R-14 a R-17 · CA-16 a CA-19 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia` (TEC-06): DTO, validador, servicio, almacén, controlador y pruebas | R-18 a R-21 · CA-20 a CA-24 | T-01 | otras rutas | por asignar | pendiente |
| T-08 | `GET /api/v1/tecnicos/{id}/fotos-trabajos` (TEC-07): DTO, validador, servicio, almacén, controlador y pruebas | R-22 a R-26 · CA-25 a CA-30 | T-01 | otras rutas | por asignar | pendiente |
| T-09 | `PUT /api/v1/tecnicos/me` (TEC-08): DTO, validador, servicio, almacén, controlador y pruebas | R-27 a R-28 · CA-31 a CA-35 | T-01 | otras rutas | por asignar | pendiente |
| T-10 | `PUT /api/v1/tecnicos/me/disponibilidad` (TEC-09): DTO, validador, servicio, almacén, controlador y pruebas | R-29 a R-30 · CA-36 a CA-39 | T-01 | otras rutas | por asignar | pendiente |
| T-11 | `GET /api/v1/tecnicos/me/horarios` (TEC-10): DTO, validador, servicio, almacén, controlador y pruebas | R-31 · CA-40 a CA-42 | T-01 | otras rutas | por asignar | pendiente |
| T-12 | `PUT /api/v1/tecnicos/me/horarios` (TEC-11): DTO, validador, servicio, almacén, controlador y pruebas | R-32 a R-33 · CA-43 a CA-47 | T-01 | otras rutas | por asignar | pendiente |
| T-13 | `GET /api/v1/tecnicos/me/reputacion` (TEC-13): DTO, validador, servicio, almacén, controlador y pruebas | R-34 a R-35 · CA-48 a CA-51 | T-01 | otras rutas | por asignar | pendiente |
| T-14 | `GET /api/v1/explorar` (TEC-14): DTO, validador, servicio, almacén, controlador y pruebas | R-36 a R-38 · CA-52 a CA-54 | T-01 | otras rutas | por asignar | pendiente |
| T-15 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-39 a R-49 · CA-55 a CA-65 | T-01 | otras rutas | por asignar | pendiente |
| T-16 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-50 a R-53 · CA-66 a CA-69 | T-02 a T-15 | T-17 | por asignar | pendiente |
| T-17 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-15 | T-16 | por asignar | pendiente |
| T-18 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
