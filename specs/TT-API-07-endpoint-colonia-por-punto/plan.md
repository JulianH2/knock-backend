# plan.md — TT-API-07 — Endpoint de colonia por punto

> Borrador. Servicio afectado: Mercado (módulo Catalog). Incluye la revisión de Guillermo en #54. La duda (a) queda cubierta por TT-BD-06 (#53), la (g) resuelta y la (e) y la (i) resueltas por el líder en #54 (2026-10-24); siguen abiertas (b), (c), (d), (f), (h) y la nueva (j) (OpenAPI como `string` frente al enlace a `string[]`); falta la aprobación de la spec. La implementación espera a que TT-BD-06 (KNOCK-156) esté fusionada con su consulta interna.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query "neighborhood point colonia coordinates"`):**
  - La consulta interna de colonia por punto de TT-BD-06 (`{ id, nombre, cp, municipio }`, TC-TT-BD-06-19) y su desempate; `ICatalogApi` sin cambios; `Domain/CatalogCoordinates` como segunda barrera de rangos.
  - `Hosting/ValidationExtensions` (`ValidationFilter`, 422 sin el valor, claves en camelCase del nombre de la propiedad), `ProblemDetailsExtensions`, `DomainExceptionHandler`, `CorrelationIdMiddleware`, `RateLimitingExtensions` (política global), `LoggingExtensions` (`ScopeRequestPathEnricher`), `AuthenticationExtensions` y `OpenApiExtensions`: sin cambios.
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `ConsoleCapture`, `TestHttp`, `AuthHttp`), la fixture de MongoDB y la semilla de TT-BD-06, y `tests/SecurityTest/Logging/SensitiveLogTests` y `tests/IntegrationTest/OpenApi/ScalarTests` como molde.
- **Crear (por confirmar al implementar):**
  - `src/Knock.Api/Modules/Catalog/Controllers/V1/` (controller de colonias), DTO de consulta con propiedades **`Lat` y `Lon` de tipo `string[]`** (o `StringValues`), DTO de respuesta y validador de coordenadas (exactamente un valor, formato, rangos, cultura invariante), todos `internal`.
  - Sin transformador de OpenAPI (decisión del líder en #54): `lat` y `lon` se documentan como `string` con el patrón fijo mediante atributos de esquema; cómo se concilia con `string[]` es la duda (j).
  - Doble de la consulta interna con contador en `tests/IntegrationTest/Support/`.
  - Pruebas: `tests/UnitTest/Catalog/` (validador y enlace), `tests/IntegrationTest/Catalog/`, `tests/IntegrationTest/OpenApi/`, `tests/SecurityTest/Catalog/` y `tests/SecurityTest/Logging/AspNetCoreLogLevelGuardTests`.
  - `docs/endpoints.md` si todavía no existe.
- **Modificar:** `docs/rate-limit-matrix.md` (la ruta va bajo la política global, sin exención); `docs/endpoints.md` (fila de la ruta); `KnockApiFactory` solo si hace falta para la ventana corta del límite o el tiempo de selección de servidor de MongoDB en las pruebas.
- **No tocar:** `ICatalogApi` y la consulta interna (TT-BD-06), `Modules/Catalog/Data/` (TT-BD-06), `Hosting/` y la configuración de Serilog (TT-API-03; la redacción de `Microsoft.AspNetCore.Hosting.Diagnostics` es la tarea de seguimiento de la decisión (i)), `appsettings*.json` (el caso 16 los lee, no los cambia), `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `docs/openapi.v1.json` a mano, specs ajenas.

## 2. Riesgos y efectos secundarios
- **Dependencia de TT-BD-06:** sin la colección `colonias`, la fixture y la consulta interna con `id`, `cp` y `municipio` (TC-TT-BD-06-19), no se pueden cerrar los casos 03, 04, 05, 08, 12 y 14.
- **Parámetro repetido:** con `string` MVC toma en silencio el primer valor (`?lat=20.05&lat=99.9` → `20.05`, comprobado por Guillermo en #54). Se enlaza como `string[]` y el validador exige exactamente un valor (casos 02 y 06).
- **400 automático que repite el valor:** con `double` o `decimal`, `[ApiController]` responde 400 antes del `ValidationFilter` con «The value '20,05x' is not valid», y el enlace numérico acepta `NaN`, `Infinity` y separadores de miles. Sin parámetros tipados (caso 15).
- **Claves de `errors`:** salen del nombre de la propiedad; `Latitude` con `[FromQuery(Name = "lat")]` daría `errors.latitude`. Propiedades `Lat` y `Lon` (casos 06 y 15).
- **Logs con la query:** la protección depende de `Microsoft.AspNetCore: Warning`; si alguien lo baja, «Request starting/finished» registran la query (casos 08 y 16). La redacción sin importar el nivel es la tarea aparte de seguimiento de TT-API-03 (decisión (i)).
- **OpenAPI:** el líder pidió `string` con el patrón fijo y sin transformador (caso 13); con los parámetros enlazados como `string[]`, el generador los documentaría como `array` de `string` (esperado, sin verificar). Se resuelve con la duda (j) antes de implementar.
- **Fuga fuera de la API:** la query con coordenadas en el gateway, la entrada de Container Apps y Application Insights (duda (f)).
- **Pruebas de rendimiento inestables en el CI:** el caso 14 informa contra la meta de 150 ms y falla solo con más de 500 ms (decisión (e)); si falla por el entorno, se informa y no se cambia el umbral sin decisión del líder.
- **Límite global en las suites:** el caso 14 hace 220 peticiones; necesita una configuración de pruebas que no le dé 429 (como la decisión 9 de TT-API-03).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-API-07-01 | `UnitTest/Catalog/PointQueryValidatorTests` | unitaria |
| TC-TT-API-07-02 | `UnitTest/Catalog/PointQueryValidatorTests` | unitaria |
| TC-TT-API-07-03 | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests` | integración (MongoDB) |
| TC-TT-API-07-04 | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests` | integración (MongoDB) |
| TC-TT-API-07-05 | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests` | integración (MongoDB) |
| TC-TT-API-07-06 | `IntegrationTest/Catalog/NeighborhoodAtPointValidationTests` | integración (doble) |
| TC-TT-API-07-07 | `SecurityTest/Catalog/NeighborhoodAtPointLeakTests` | seguridad negativa |
| TC-TT-API-07-08 | `SecurityTest/Catalog/NeighborhoodAtPointLogPrivacyTests` | seguridad negativa (MongoDB, niveles configurados) |
| TC-TT-API-07-09 | `IntegrationTest/Catalog/NeighborhoodAtPointCacheTests` | integración (doble) |
| TC-TT-API-07-10 | `SecurityTest/Catalog/NeighborhoodAtPointAuthTests` | seguridad negativa |
| TC-TT-API-07-11 | `IntegrationTest/Catalog/NeighborhoodAtPointRateLimitTests` | integración (doble) |
| TC-TT-API-07-12 | `SecurityTest/Catalog/NeighborhoodAtPointFailureTests` | seguridad negativa (MongoDB detenido) |
| TC-TT-API-07-13 | `IntegrationTest/OpenApi/NeighborhoodAtPointOpenApiTests` | contrato (OpenAPI generado) |
| TC-TT-API-07-14 | `IntegrationTest/Catalog/NeighborhoodAtPointPerformanceTests` | rendimiento local (MongoDB) |
| TC-TT-API-07-15 | `UnitTest/Catalog/NeighborhoodAtPointBindingTests` y `SecurityTest/Catalog/NeighborhoodAtPointLeakTests` | unitaria (reflexión) y seguridad negativa |
| TC-TT-API-07-16 | `SecurityTest/Logging/AspNetCoreLogLevelGuardTests` | seguridad negativa (configuración) |
| TC-TT-API-07-17 | `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `dotnet list package --vulnerable`, `gitleaks` | comando (evidencia en el PR) |
| TC-TT-API-07-18 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec con las dudas (b), (c), (d), (f), (h) y (j) resueltas, y TT-BD-06 (#53) fusionada con su consulta interna (caso 19 de TT-BD-06); `NeighborhoodExistsAsync` (caso 20 de TT-BD-06) no lo usa este endpoint.
2. Contrato (spec §4) y pruebas que fallan primero: validador y enlace (casos 01, 02 y 15), endpoint con doble (casos 06, 07, 09, 10, 11 y 13) y guarda del nivel de log (caso 16).
3. DTO con `Lat`/`Lon` como `string[]`, validador, controller con `[Authorize]`, `Cache-Control: no-store` y la documentación OpenAPI.
4. Con TT-BD-06 fusionada: casos 03, 04, 05, 08, 12 y 14 con Testcontainers de MongoDB.
5. `docs/endpoints.md` y `docs/rate-limit-matrix.md` (caso 17); `export_openapi.py` si ya existe.
6. Gates, `Memoria.md`, informe y evidencia en el PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
KNOCK_PERF_STRICT=1 dotnet test tests/IntegrationTest -c Release --no-build --filter "Category=Performance"   # meta de 150 ms, bajo demanda
dotnet list Knock.slnx package --vulnerable --include-transitive
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-07
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
