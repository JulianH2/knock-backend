# Tareas 039 — Cuenta y datos personales: cambio de número, recuperación de acceso, exportación, consentimientos y eliminación

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-00 | Prerrequisitos que hoy no existen y no son parte de esta spec: Rutas internas con el token de la spec 037: sus claves `avisos-key-*` y el secreto nuevo `interno-key-identity` (Identidad firma, Mercado verifica) en Key Vault y Terraform (hoy no implementados ni creados); Contenedor privado `exportaciones` en la cuenta de almacenamiento privada de la spec 025 (hoy no existe en Terraform); Ejecutor de tareas y bandeja de salida de la spec 054 (hoy no implementados); Cola del panel de la spec 053 y panel de la spec 033 (hoy no implementados) | §8 | — | T-01 | por asignar | pendiente |
| T-01 | Script de `db/` con colecciones e índices de §6 y prueba de base de datos | §6 | — | T-02 en adelante (con datos de prueba) | por asignar | pendiente |
| T-02 | `POST /api/v1/me/cambio-numero` (USR-03): DTO, validador, servicio, almacén, controlador y pruebas | R-01 a R-03 · CA-01 a CA-05 | T-01 | otras rutas | por asignar | pendiente |
| T-03 | `POST /api/v1/me/cambio-numero/confirmar` (USR-04): DTO, validador, servicio, almacén, controlador y pruebas | R-04 a R-06 · CA-06 a CA-10 | T-01 | otras rutas | por asignar | pendiente |
| T-04 | `POST /api/v1/recuperacion-acceso` (USR-05): DTO, validador, servicio, almacén, controlador y pruebas | R-07 a R-08 · CA-11 a CA-13 | T-01 | otras rutas | por asignar | pendiente |
| T-05 | `POST /api/v1/me/exportaciones` (USR-06): DTO, validador, servicio, almacén, controlador y pruebas | R-09 a R-10 · CA-14 a CA-16 | T-01 | otras rutas | por asignar | pendiente |
| T-06 | `GET /api/v1/me/consentimientos` (USR-07): DTO, validador, servicio, almacén, controlador y pruebas | R-11 · CA-17 a CA-18 | T-01 | otras rutas | por asignar | pendiente |
| T-07 | `PUT /api/v1/me/consentimientos` (USR-08): DTO, validador, servicio, almacén, controlador y pruebas | R-12 a R-13 · CA-19 a CA-22 | T-01 | otras rutas | por asignar | pendiente |
| T-08 | `GET /api/v1/me/pendientes` (USR-09): DTO, validador, servicio, almacén, controlador y pruebas | R-14 · CA-23 a CA-24 | T-01 | otras rutas | por asignar | pendiente |
| T-09 | `POST /api/v1/me/eliminacion` (USR-10): DTO, validador, servicio, almacén, controlador y pruebas | R-15 a R-17 · CA-25 a CA-29 | T-01 | otras rutas | por asignar | pendiente |
| T-10 | `POST /api/v1/me/eliminacion/cancelar` (USR-11): DTO, validador, servicio, almacén, controlador y pruebas | R-18 a R-19 · CA-30 a CA-32 | T-01 | otras rutas | por asignar | pendiente |
| T-11 | Reglas de negocio específicas de la spec (estados, plazos y límites) | R-20 a R-26 · CA-33 a CA-39 | T-01 | otras rutas | por asignar | pendiente |
| T-12 | Transversales: `sub` del token, registros sin datos personales, 429 y 413, sin `Idempotency-Key` | R-27 a R-30 · CA-40 a CA-43 | T-02 a T-11 | T-13 | por asignar | pendiente |
| T-13 | OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-02 a T-11 | T-12 | por asignar | pendiente |
| T-14 | Gates, `Memoria.md` y revisión de par | §12 | todas | — | por asignar | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
