# Tareas 027 — Buscar un experto por texto

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Responsable sugerido de la historia:** Alexis (KNOCK-66). Revisión de par: Guillermo.

| Tarea | Qué se hace | Cubre | Depende de | En paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Script de `db/` con `sinonimos`, `catalogo_meta`, `busquedas_sin_resultado` (TTL 90 días) e índices de `tecnicos`; semilla de sinónimos; carga en `entrega.js` | R-12, R-13 · CA-03, CA-14 | — | T-02, T-06 | Alexis | pendiente |
| T-02 | Ampliar `SearchNameNormalizer` y escribir `InterpretadorDeConsulta` con sus unitarias | R-02, R-04, R-05 · CA-02, CA-05, CA-06 | — | T-01, T-06 | Alexis | pendiente |
| T-03 | Escribir `OrdenDeResultados` y `CursorDeBusqueda` (huella, tope de 100) con unitarias | R-09, R-11 · CA-08, CA-10, CA-11 | — | T-01, T-02 | Alexis | pendiente |
| T-04 | Escribir `BusquedaStore` (`$geoNear`, filtros de estado, cobertura, prefijos, conteo anónimo) con integración | R-06, R-07, R-08, R-12, R-15 · CA-04, CA-07, CA-09 | T-01 | T-05 | Alexis | pendiente |
| T-05 | Escribir `BusquedaService`: coordinación, caché de 30 s, filtro de datos personales (spec 024) antes de contar, 503 | R-03, R-10, R-12, R-13, R-16 · CA-01, CA-03, CA-13, CA-14, CA-15, CA-17 | T-02, T-03 | T-04 | Alexis | pendiente |
| T-06 | Política `busqueda` en el gateway (30 por minuto por `sub`) y ruta a Mercado | R-14 · CA-16 | — | T-01 a T-03 | Guillermo | hecha (KNOCK-66): política `busqueda` de 30 por minuto por `sub` con contador en Redis y ruta `mercado-busqueda` en el gateway. Prueba: `GatewaySecurityTests.SearchRoute_HasItsOwnLimitPerPerson_AndNeedsASession_CA_027_16` y `GatewayRoutingTests` |
| T-07 | `BusquedaController`, DTOs y `BusquedaQueryValidator` (parámetros repetidos, `limit`, `q`) | R-01, R-02, R-03 · CA-12 | T-05 | — | Alexis | pendiente |
| T-08 | Pruebas de integración y seguridad negativa de punta a punta; registros sin datos | R-01 a R-16 · CA-01 a CA-18 | T-04, T-06, T-07 | T-09 | Alexis | pendiente |
| T-09 | Prueba de rendimiento con 200 técnicos (sin cobertura) | R-15 · CA-19 | T-04, T-05 | T-08 | Alexis | pendiente |
| T-10 | Regenerar OpenAPI de Mercado, `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `Memoria.md` | R-01, R-14 | T-07 | T-08, T-09 | Alexis | pendiente |

**Qué bloquea:** T-04 espera el script (T-01); T-07 espera el servicio (T-05); T-08 es la última de código. **Qué va en paralelo:** T-01, T-02, T-03 y T-06 arrancan el mismo día y no se pisan (archivos distintos).

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
