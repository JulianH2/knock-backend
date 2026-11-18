# Tareas 035 — Redacción de registros con parámetros de consulta y encabezados reenviados

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Paralelo:** T-01 y T-06 arrancan a la vez (frentes distintos). T-03 y T-04 no dependen de nada. Como los dos frentes editan los mismos `Hosting/`, se integra primero el frente A y luego el B.

| Tarea | Qué se hace | Cubre | Depende de | ¿En paralelo? | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Prueba de registros por proyecto: host con todo en `Verbose`, receptor en memoria, petición con consulta, llamada saliente y reenvío del gateway (falla hoy) | R-08 · CA-01 a CA-04 | — | Sí (con T-03, T-04 y T-06) | Guillermo | pendiente |
| T-02 | Filtro de fuentes en `LoggingExtensions` de los cuatro proyectos y limpieza de anulaciones en `appsettings*.json` | R-01, R-02, R-03, R-05 · CA-01 a CA-05 | T-01 | No | Guillermo | pendiente |
| T-03 | Regla de redacción de consultas en `TelemetryRedactor` (cuatro copias) con pruebas unitarias y de tiempo | R-04 · CA-06 | — | Sí | Erick | hecha (PR #121) |
| T-04 | `instance` de ProblemDetails sin consulta, regla de Semgrep contra `UseHttpLogging` (la agrega una persona) y prueba de trazas sin consulta | R-06, R-07 · CA-07, CA-08, CA-16 | — | Sí | Alexis | pendiente |
| T-05 | Integrar el frente A y actualizar `docs/OBSERVABILIDAD.md` | R-01 a R-08 | T-02, T-03, T-04 | No (cierre del frente A) | Guillermo | pendiente |
| T-06 | Validación al arrancar de `KnownNetworks` y `KnownProxies` (vacío fuera de `Development`, redes privadas, prefijo mínimo) en los cuatro proyectos | R-09, R-10, R-12 · CA-09, CA-10, CA-12 | — | Sí (con T-01) | José Julián | pendiente |
| T-07 | Transformación de YARP en el gateway y encabezados reenviados en Mensajería | R-13 · CA-13 | T-06 | Sí (con T-08) | José Julián | pendiente |
| T-08 | Terraform: validación de `container_apps_subnet_cidr` y variable para los cuatro componentes; `terraform test` | R-11 · CA-14 | — | Sí | José Julián | pendiente |
| T-09 | Prueba de dos clientes con Redis real, humo en el entorno de evaluación, `docs/rate-limit-matrix.md` y `Memoria.md` | R-14, R-15 · CA-11, CA-15 | T-05, T-07, T-08 | No (cierre) | José Julián | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas o comandos en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
