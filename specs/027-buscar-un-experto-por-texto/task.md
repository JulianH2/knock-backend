# Tareas 027 — Buscar un experto por texto

> **Estado:** T-01 a T-05 y T-07 activadas por una persona; T-05 está parcial y las demás siguen pendientes.
> **Responsable sugerido de la historia:** Alexis (KNOCK-66). Revisión de par: Guillermo.

| Tarea | Qué se hace | Cubre | Depende de | En paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Script de `db/` con `sinonimos`, `catalogo_meta`, `busquedas_sin_resultado` (TTL 90 días) e índices de `tecnicos`; semilla de sinónimos; carga en `entrega.js` | R-12, R-13 · CA-03, CA-14 | — | T-02, T-06 | Alexis | hecha |
| T-02 | Ampliar `SearchNameNormalizer` y escribir `InterpretadorDeConsulta` con sus unitarias | R-02, R-04, R-05 · CA-02, CA-05, CA-06 | — | T-01, T-06 | Alexis | hecha |
| T-03 | Escribir `OrdenDeResultados` y `CursorDeBusqueda` (huella, tope de 100) con unitarias | R-09, R-11 · CA-08, CA-10, CA-11 | — | T-01, T-02 | Alexis | hecha |
| T-04 | Escribir `BusquedaStore` (`$geoNear`, filtros de estado, cobertura, prefijos, conteo anónimo) con integración | R-06, R-07, R-08, R-12, R-15 · CA-04, CA-07, CA-09 | T-01 | T-05 | Alexis | hecha |
| T-05 | Escribir `BusquedaService`: coordinación, caché de 30 s, filtro de datos personales (spec 024) antes de contar, 503 | R-03, R-10, R-12, R-13, R-16 · CA-01, CA-03, CA-13, CA-14, CA-15, CA-17 | T-02, T-03 | T-04 | Alexis | parcial |
| T-06 | Política `busqueda` en el gateway (30 por minuto por `sub`) y ruta a Mercado | R-14 · CA-16 | — | T-01 a T-03 | Guillermo | pendiente |
| T-07 | `BusquedaController`, DTOs y `BusquedaQueryValidator` (parámetros repetidos, `limit`, `q`) | R-01, R-02, R-03 · CA-12 | T-05 | — | Alexis | hecha |
| T-08 | Pruebas de integración y seguridad negativa de punta a punta; registros sin datos | R-01 a R-16 · CA-01 a CA-18 | T-04, T-06, T-07 | T-09 | Alexis | parcial |
| T-09 | Prueba de rendimiento con 200 técnicos (sin cobertura) | R-15 · CA-19 | T-04, T-05 | T-08 | Alexis | pendiente |
| T-10 | Regenerar OpenAPI de Mercado, `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `Memoria.md` | R-01, R-14 | T-07 | T-08, T-09 | Alexis | pendiente |

**Qué bloquea:** T-04 espera el script (T-01); T-07 espera el servicio (T-05); T-08 es la última de código. **Qué va en paralelo:** T-01, T-02, T-03 y T-06 arrancan el mismo día y no se pisan (archivos distintos).

**T-05 parcial:** falta integrar el filtro real de la spec 024 (T-03); el #118 no se fusiona sin él. El servicio usa paginación por clave y el sustituto falla cerrado. La segunda búsqueda aún consulta MongoDB para verificar la versión del diccionario y la colonia: CA-15 sigue parcial. Las semillas actuales de colonias no traen `activo` y R-03 lo exige: el servicio las rechaza hasta que una persona concilie el modelo y las semillas. El controller de T-07 conecta el mapeo HTTP; la ruta del gateway y su política `busqueda` por `sub` siguen pendientes de T-06 (Guillermo). Falta verificar la configuración del cursor en el despliegue.

**T-08 parcial:** pruebas HTTP directamente contra Market, sin gateway: T-06 deja pendiente el límite y el flujo completo de CA-16. CA-03 y CA-14 necesitan el filtro real de 024; CA-15 aún consulta MongoDB y CA-19 corresponde a T-09. CA-18 comprueba registros con y sin resultado y textos sintéticos; CA-11 cubre cursor de otra consulta y versión. El cursor no está ligado al usuario: R-11 y R-13 definen consulta y resultado no personal; rechazarlo por otro usuario requiere decisión y enmienda, no se da por probado. Las coincidencias de `--trace` por nombre no demuestran cobertura integral ni ejecución.

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
