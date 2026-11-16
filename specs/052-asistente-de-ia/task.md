# Tareas 052 — Asistente de IA: resumen de chat, borrador de cotización, rango de precio y bitácora

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Recurso de Azure OpenAI y cliente de la spec 024 (el recurso no está en Terraform; autorizado con límites); Filtro de datos personales y `uso_ia` de la spec 024 (hoy no implementados); Cálculo de la referencia de mercado de la spec 048 (hoy no implementado) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `POST /api/v1/asistente/conversaciones` (ASI-01): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-02 · CA-01 a CA-04 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `POST /api/v1/asistente/resumen-chat` (ASI-02): DTO, validador, servicio, almacén, controlador y pruebas | R-03 a R-04 · CA-05 a CA-08 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `POST /api/v1/asistente/borrador-cotizacion` (ASI-03): DTO, validador, servicio, almacén, controlador y pruebas | R-05 a R-06 · CA-09 a CA-12 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `GET /api/v1/asistente/rango-precio` (ASI-04): DTO, validador, servicio, almacén, controlador y pruebas | R-07 a R-09 · CA-13 a CA-17 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | `PUT /api/v1/asistente/bitacora/{solicitudId}` (ASI-05): DTO, validador, servicio, almacén, controlador y pruebas | R-10 a R-12 · CA-18 a CA-23 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | `GET /api/v1/asistente/bitacora/{solicitudId}` (ASI-06): DTO, validador, servicio, almacén, controlador y pruebas | R-13 a R-15 · CA-24 a CA-28 | T-01 | otras rutas | por asignar | pendiente |
| T-08 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-16 a R-22 · CA-29 a CA-35 | T-01 | otras rutas | por asignar | pendiente |
| T-09 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-23 a R-26 · CA-36 a CA-39 | T-02 a T-08 | T-10 | por asignar | pendiente |
| T-10 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-08 | T-09 | por asignar | pendiente |
| T-11 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
