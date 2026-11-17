# Tareas 021 — Comparar y elegir cotización

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Script de `db/` con `visitas` (índice único `{ solicitudId }`) y campos nuevos de `solicitudes`, `cotizaciones` y `tecnicos`; prueba de base de datos | §6 · R-10, R-17 | spec 020 T-03 | T-02, T-03 | Alexis | pendiente |
| T-02 | `ComparadorDeCotizaciones`: orden, desempates, etiquetas, `cubre`, `tiposMixtos`, `puedeElegir` y `motivo`, con unitarias y mutación | R-02 a R-08 · CA-01, CA-02, CA-11, CA-14 | — | T-01, T-03 | Guillermo | pendiente |
| T-03 | `VisitaFactory`: código criptográfico, cifrado con llave versionada y `codigoVenceEn` | R-17 · CA-10 | — | T-01, T-02 | Guillermo | pendiente |
| T-04 | `EleccionService`: transacción condicionada, rechazo de las demás, visita, eventos y reintento ante conflicto transitorio | R-09 a R-13, R-18, R-20 · CA-04 a CA-08, CA-17 | T-01, T-03, spec 020 fusionada | T-05 | Guillermo | pendiente |
| T-05 | Listado: `TecnicosStore` (proyección pública) y `CotizacionesController` con validación de `orden` | R-01, R-02, R-04, R-08 · CA-01, CA-03, CA-15, CA-18 | T-02, spec 020 fusionada | T-04 | Alexis (revisa Guillermo) | pendiente |
| T-06 | `EleccionController` con respuesta de cita, contacto público y lugar | R-14 a R-16, R-21 · CA-09, CA-12 | T-04 | T-07 | Guillermo | pendiente |
| T-07 | Pruebas de integración, concurrencia (dos elecciones reales), seguridad negativa y registros | CA-01 a CA-18 (salvo CA-16, retirado) · R-19 | T-05, T-06 | T-08 | Guillermo | pendiente |
| T-08 | OpenAPI, `docs/endpoints.md`, `docs/rate-limit-matrix.md`; semillas con `contactoPublico`, `tallerDireccion` y `nivelVerificacion` | §5 | T-05, T-06 | T-07 | Alexis | hecha: OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` en el PR #138; semillas de demo: el cuarto técnico (`demo-tecnico-04`) trae `contactoPublico`, `nivelVerificacion` y `tallerDireccion`, y el cargador de técnicos los valida |
| T-09 | Gates, `Memoria.md` y revisión de par | §12 | T-07, T-08 | — | Guillermo | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
