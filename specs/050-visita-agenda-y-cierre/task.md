# Tareas 050 — Visita: agenda, reprogramación, cancelación, cliente ausente, cierre y permiso de fotos

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Ciclo de la visita de las specs 021 y 022 y tareas de la 054 (hoy no implementados); Moderación e imágenes de las specs 031 y 032 (hoy no implementadas) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `GET /api/v1/agenda` (VIS-02): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-03 · CA-01 a CA-05 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `POST /api/v1/visitas/{id}/reprogramar` (VIS-04): DTO, validador, servicio, almacén, controlador y pruebas | R-04 a R-06 · CA-06 a CA-11 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `POST /api/v1/visitas/{id}/reprogramar/respuesta` (VIS-05): DTO, validador, servicio, almacén, controlador y pruebas | R-07 a R-09 · CA-12 a CA-17 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `POST /api/v1/visitas/{id}/cancelar` (VIS-06): DTO, validador, servicio, almacén, controlador y pruebas | R-10 a R-12 · CA-18 a CA-23 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | `POST /api/v1/visitas/{id}/cliente-ausente` (VIS-07): DTO, validador, servicio, almacén, controlador y pruebas | R-13 a R-15 · CA-24 a CA-28 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | `POST /api/v1/visitas/{id}/terminar` (VIS-09): DTO, validador, servicio, almacén, controlador y pruebas | R-16 a R-18 · CA-29 a CA-34 | T-01 | otras rutas | por asignar | pendiente |
| T-08 | `POST /api/v1/visitas/{id}/no-terminado` (VIS-10): DTO, validador, servicio, almacén, controlador y pruebas | R-19 a R-21 · CA-35 a CA-40 | T-01 | otras rutas | por asignar | pendiente |
| T-09 | `PUT /api/v1/visitas/{id}/permiso-fotos` (VIS-11): DTO, validador, servicio, almacén, controlador y pruebas | R-22 a R-23 · CA-41 a CA-44 | T-01 | otras rutas | por asignar | pendiente |
| T-10 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-24 a R-30 · CA-45 a CA-51 | T-01 | otras rutas | por asignar | pendiente |
| T-11 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-31 a R-34 · CA-52 a CA-55 | T-02 a T-10 | T-12 | por asignar | pendiente |
| T-12 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-10 | T-11 | por asignar | pendiente |
| T-13 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
