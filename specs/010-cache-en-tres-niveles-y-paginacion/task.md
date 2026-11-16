# Tareas 010 — Caché en tres niveles y paginación

> **Estado:** tareas completas; T-05 está cerrada por alcance y T-07 tiene medición secuencial local (KNOCK-269).

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Definir la interfaz de caché y su implementación con Redis | R-01 | hecha (PR #52; verificado en `src`: caché Redis, `ETag`/`If-None-Match` y compresión) |
| T-02 | Envolver los repositorios con caché e invalidación al escribir | R-01, R-02, R-05 · CA-01, CA-02 | hecha (PR #52; verificado en `src`: caché Redis, `ETag`/`If-None-Match` y compresión) |
| T-03 | Implementar `ETag` y 304 | R-03, R-06 · CA-03 | hecha (PR #52; verificado en `src`: caché Redis, `ETag`/`If-None-Match` y compresión) |
| T-04 | Configurar las vigencias por tipo de dato | R-04 | hecha (PR #52; verificado en `src`: caché Redis, `ETag`/`If-None-Match` y compresión) |
| T-05 | Implementar la paginación por cursor y las proyecciones | R-07, R-08 · CA-05 | cerrada por alcance (excepción aprobada en spec §9) |
| T-06 | Configurar la compresión y los serializadores generados | R-09 · CA-04 | hecha (PR #52; verificado en `src`: caché Redis, `ETag`/`If-None-Match` y compresión) |
| T-07 | Escribir la prueba de carga con k6 para ajustar vigencias | R-04 | hecha (KNOCK-269: p95 secuencial 200 fría 22.976 ms, caliente 6.589 ms; 304 fría 16.938 ms, caliente 8.011 ms; [evidencia](../../docs/evidencia/knock-269-cache-p95.md); k6 queda como referencia de carga) |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
