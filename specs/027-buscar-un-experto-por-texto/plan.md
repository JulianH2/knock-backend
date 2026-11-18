# Plan 027 — Buscar un experto por texto

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Escribir el script de `db/` con `sinonimos`, `catalogo_meta`, `busquedas_sin_resultado` (TTL) y los índices de lectura de `tecnicos`, y la semilla de sinónimos (R-12, R-13, §6).
2. Ampliar el normalizador existente del módulo Catalog y escribir las funciones puras: interpretador, orden y cursor (R-02, R-04, R-05, R-09, R-11).
3. Escribir `BusquedaStore` con `$geoNear`, proyección mínima y conteo anónimo (R-06 a R-08, R-12, R-15).
4. Escribir `BusquedaService` (coordinación, caché de 30 s, interpretación, cobertura y página) y el controller con su validador (R-01, R-03, R-10, R-13, R-16).
5. Agregar la política `busqueda` al gateway (R-14).
6. Escribir las pruebas desde los criterios y la prueba de rendimiento; regenerar el contrato.

## Archivos que se crean o cambian
- `db/0NN_catalogo_busqueda.js` (número siguiente libre al implementar) y `db/seeds/catalog/sinonimos.json`; `db/entrega.js` lo carga.
- `src/Knock.Market.Api/Modules/Catalog/Domain/SearchNameNormalizer.cs` (se amplía; no se duplica).
- `src/Knock.Market.Api/Modules/Catalog/Domain/` (`InterpretadorDeConsulta`, `OrdenDeResultados`, `CursorDeBusqueda`).
- `src/Knock.Market.Api/Modules/Catalog/Services/BusquedaService.cs`, `Data/BusquedaStore.cs`.
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/BusquedaController.cs`, `Dtos/` y `Validators/BusquedaQueryValidator.cs`.
- `src/Knock.Gateway/` (ruta y política `busqueda`).
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest`, `tests/DatabaseTest`, `tests/PerformanceTest` (clases `Spec027*`).
- `docs/openapi.v1.json` (regenerado, nunca a mano), `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `Memoria.md`.

## Dependencias
- Specs 002, 003, 009, 010, 011, 023 y 024 (el filtro de datos personales debe existir; si la 024 no está implementada, la tarea T-05 se bloquea o usa la interfaz con un doble y no se fusiona sin el real).
- Semillas de `tecnicos` con los campos de §6 (KNOCK-246).
- `MarketApiFactory` con `Testcontainers` (lo crea la spec 020, T-01); si no existe, se crea aquí.

## Decisiones de la spec que este plan sigue
- Búsqueda en MongoDB con normalización y prefijos indexados, sin motor nuevo.
- Tolerancia a errores solo contra el diccionario.
- Resultado no personal, cacheado 30 s.
- Tope de 100 resultados y límite de 30 por minuto.

## Riesgos del plan
- `$geoNear` debe ser la primera etapa de la tubería; si DocumentDB no lo admite con los filtros, se cambia a `$geoWithin` más cálculo de distancia en el servicio sin cambiar el contrato.
- La prueba de rendimiento se corre fuera del paso con cobertura.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con la prueba de la tabla 4.1 antes de dar el cambio por terminado.
- `python scripts/sdd_lint.py --trace 027`, `python scripts/export_openapi.py` y los comandos de `AGENTS.md`; se actualiza `Memoria.md`.
