# Tareas 033 — Panel interno con permisos y doble factor (backend)

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Paralelo:** T-01, T-02 y T-03 arrancan a la vez. T-01 desbloquea al frontend (spec 023) con una API simulada. T-04 bloquea a T-06 y T-09. T-07 puede avanzar en cuanto exista T-02.

| Tarea | Qué se hace | Cubre | Depende de | ¿En paralelo? | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Contrato: DTOs, validadores y controller con respuestas simuladas para publicar los cuatro endpoints, la cookie y los códigos en el OpenAPI de Identity | R-04, R-08, R-12, R-16, R-17, R-19 · CA-02 (400) | — | Sí (con T-02 y T-03) | Alexis | hecha |
| T-02 | Dominio: roles, permisos, mapa de roles, reglas de TOTP (ventana ±1, paso mayor, tiempo constante) y de sesión (15 min, 20 min, 8 h, 3 sesiones) con pruebas unitarias y reloj simulado | R-02, R-09, R-13, R-15 · CA-06, CA-14, CA-16 | — | Sí (con T-01 y T-03) | Guillermo | hecha |
| T-03 | Script de `db/` con las tres colecciones, índices y TTL; almacenes de MongoDB y de Redis (desafío, fallos, caché de la cuenta) | R-03, R-07, R-11, R-15, R-23, R-24 · CA-09, CA-17 | — | Sí (con T-01 y T-02) | Erick | hecha (pruebas de integración y de base de datos escritas; sin Docker no se ejecutaron) |
| T-04 | Herramienta interna: `panel alta`, `roles`, `deshabilitar`, `habilitar`, `reiniciar-totp`, cifrado del secreto, `otpauth://` una sola vez, `--secreto-de-prueba` solo en Development, auditoría e invalidación | R-01, R-25, R-26 · CA-21 | T-02, T-03 | No (bloquea T-06 y T-09) | Alexis | hecha |
| T-05 | Auditoría de solo inserción con falla cerrada y campos prohibidos | R-23, R-24 · CA-18, CA-19 | T-03 | Sí (con T-04) | Erick | hecha |
| T-06 | Servicios y controller reales de celular y TOTP: verificador de Firebase, desafío, límites por cuenta e IP, consumo atómico, emisión del token del panel y la cookie | R-04 a R-13, R-16, R-28 · CA-01 a CA-03, CA-07 a CA-09, CA-24, CA-25 | T-01, T-04, T-05 | No | Guillermo | hecha |
| T-07 | Renovación y cierre: `Origin` y `X-Knock-Panel`, rotación condicionada, reuso que revoca todo, `permissions_changed`, vida de 8 h, cancelación por `sid` | R-17, R-18, R-19 · CA-11 a CA-15 | T-02, T-03 | Sí (con T-06) | Alexis | hecha |
| T-08 | Política `RequierePermiso`, esquema `PanelBearer`, comparación de `pv`, 403 `insufficient_permission` y prueba de arquitectura | R-14, R-20, R-21, R-22 · CA-04, CA-05, CA-20 | T-02, T-05 | Sí (con T-06 y T-07) | Guillermo | hecha (unitarias y arquitectura en verde) |
| T-09 | Gateway: esquema JWT del panel, separación de tokens por ruta, rutas, límite estricto en `/api/v1/panel/sesion/*`, política por usuario y CORS `panel` con credenciales | R-14, R-27 · CA-10, CA-22, CA-23 | T-06, T-08 | No | José Julián | hecha (pruebas del gateway con dobles en verde; sin Docker) |
| T-10 | Seguridad negativa, simultaneidad con dos hosts, regresión de 015 a 018, recorrido de punta a punta con el panel, OpenAPI regenerado, `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `Memoria.md` | CA-01 a CA-25 | T-06, T-07, T-08, T-09 | No (cierre) | Alexis | hecha (sin emulador de Firebase: recorrido de punta a punta con Firebase y navegador pendientes) |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
