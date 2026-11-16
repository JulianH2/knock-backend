# Tareas 020 — Solicitudes y cotizaciones (pedir y enviar)

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Cómo leer la tabla:** «Depende de» son las tareas que deben estar fusionadas antes; «Paralelo» indica con qué tareas se puede trabajar al mismo tiempo. Las tareas sin dependencias pendientes se pueden tomar ya; solo se espera la revisión de par.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Traer o reutilizar el módulo de archivos privados (`IMediaApi`: confirmar dueño, `uso`, `listo` y emitir enlace de lectura de 5 min) en Mercado | R-04 · CA-18 | — (bloquea T-05) | T-02, T-03 | Alexis (dueño de la spec 013) | pendiente |
| T-02 | Crear `MarketApiFactory` con `Testcontainers` (documentdb-local, Redis, Azurite) para las specs 020 a 023 | R-01 a R-22 (infraestructura de prueba) | — (bloquea T-11) | T-01, T-03 | Guillermo | pendiente |
| T-03 | Escribir el script de `db/` con `solicitudes`, `cotizaciones` e índices, y su prueba de base de datos | §6 · R-07, R-17 | — | T-01, T-02 | Alexis | hecha (`db/020-requests-solicitudes-cotizaciones.js`) |
| T-04 | Dominio: estados de la solicitud y de la cotización, `venceEn`, `CotizacionTopeCalculator` con sus pruebas unitarias y de mutación | R-01, R-13, R-14, R-16 · CA-11 | T-03 | T-05, T-08 | Alexis | parcial: unitarias; mutación sin resultado y transiciones no especificadas pendientes de aclaración |
| T-05 | Validadores y servicio de creación de solicitud: catálogo con caché, fotos con `IMediaApi`, moderación, dirección cifrada | R-02 a R-07, R-21 · CA-01, CA-02, CA-04, CA-05, CA-15, CA-18 | T-01, T-03 | T-08 | Guillermo | pendiente |
| T-06 | Lectura de detalle por rol (cliente, técnico del oficio, técnico que cotizó) y listado por cursor | R-08, R-09, R-18, R-22 · CA-12 a CA-14, CA-21 | T-05 | T-09 | Guillermo | pendiente |
| T-07 | `SolicitudesController` con `Location`, `Cache-Control`, 413 y ProblemDetails con `code` | R-01, R-19, R-21 · CA-16, CA-22 | T-05, T-06 | T-10 | Guillermo | pendiente |
| T-08 | Servicio de cotización: técnico por `sub`, oficio, estado cotizable, duplicado, cupo de 5 en transacción, paso a `cotizando` y evento | R-10 a R-12, R-15 a R-17, R-20 · CA-06 a CA-09, CA-19 | T-03, T-04 | T-05, T-06 | Alexis | parcial: filtro R-23, transacción y outbox probados localmente y por HTTP; pendiente entrega del aviso por el despachador, moderador real e índice geográfico y comprobación en el clúster real |
| T-09 | Validador de montos, día, hora y nota con moderación | R-13, R-15 · CA-10 | T-04 | T-06 | Alexis | parcial: validación de campos; moderación de nota pendiente en T-08 |
| T-10 | `CotizacionesController` y límite de 3 por minuto por técnico | R-10, R-19 · CA-16 | T-08, T-09 | T-07 | Alexis | hecha: política por `sub` y réplica; límite compartido pendiente de KNOCK-247 |
| T-11 | Pruebas de integración, seguridad negativa, concurrencia (cupo de cotizaciones y cotización duplicada) y registros sin datos personales | CA-01 a CA-22 (salvo CA-03 y CA-20, retirados) | T-02, T-07, T-10 | T-12 | Guillermo y Alexis | pendiente |
| T-12 | Regenerar OpenAPI de Mercado, actualizar `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §5 | T-07, T-10 | T-11, T-13 | Alexis | parcial: contrato y matrices de cotizaciones actualizados; solicitudes dependen de T-07 |
| T-13 | Semillas de demo (KNOCK-246): solicitudes por modalidad y cotizaciones de técnicos ficticios | §8 (demo) | T-03 | T-04 a T-12 | Alexis | pendiente |
| T-14 | Gates (`gate`, Semgrep, dependencias), `Memoria.md` y revisión de par | §12 | T-11, T-12, T-15 | — | Guillermo | pendiente |
| T-15 | Ajustes que exige la spec 029: `venceEn` sale del plazo configurable `Solicitudes:HorasVencimiento` (1 a 168 horas, 48 por omisión) en lugar de 48 h fijas, y el detalle `GET /api/v1/solicitudes/{id}` agrega `puedeVolverAPedir` | spec 029 R-08 y R-13 · spec 029 CA-14 y CA-10 | T-05, T-06 | T-11 | Alexis | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
