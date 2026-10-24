# plan.md — TT-API-07 — Endpoint de colonia por punto

> Borrador. Servicio afectado: Mercado (módulo Catalog). Dudas (a) a (h) de spec §8 sin resolver; falta la aprobación de la spec. La implementación espera a que TT-BD-06 (KNOCK-156) esté fusionada.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query "neighborhood point colonia coordinates"`):**
  - `ICatalogApi.FindNeighborhoodAtAsync` (firma sin cambios) y su implementación de TT-BD-06; `Domain/CatalogCoordinates` como segunda barrera de rangos.
  - `Hosting/ValidationExtensions` (`ValidationFilter`, 422 sin el valor), `ProblemDetailsExtensions`, `DomainExceptionHandler`, `CorrelationIdMiddleware`, `RateLimitingExtensions` (política global), `LoggingExtensions` (`ScopeRequestPathEnricher`), `AuthenticationExtensions` y `OpenApiExtensions`: sin cambios.
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `ConsoleCapture`, `TestHttp`, `AuthHttp`), la fixture de MongoDB y la semilla de TT-BD-06, y `tests/SecurityTest/Logging/SensitiveLogTests` y `tests/IntegrationTest/OpenApi/ScalarTests` como molde.
- **Crear (por confirmar al implementar):**
  - `src/Knock.Api/Modules/Catalog/Controllers/V1/` (controller de colonias), DTO de consulta, DTO de respuesta y validador de coordenadas, todos `internal`.
  - Si la duda (a) se resuelve con la opción (3): la consulta interna con `{ id, nombre, cp, municipio }` es de TT-BD-06; aquí solo se consume.
  - Doble de `ICatalogApi` con contador en `tests/IntegrationTest/Support/`.
  - Pruebas: `tests/UnitTest/Catalog/`, `tests/IntegrationTest/Catalog/`, `tests/IntegrationTest/OpenApi/` y `tests/SecurityTest/Catalog/`.
  - `docs/endpoints.md` si todavía no existe.
- **Modificar:** `docs/rate-limit-matrix.md` (la ruta va bajo la política global, sin exención); `docs/endpoints.md` (fila de la ruta); `KnockApiFactory` solo si hace falta para la ventana corta del límite o el tiempo de selección de servidor de MongoDB en las pruebas.
- **No tocar:** firma de `ICatalogApi`, `Modules/Catalog/Data/` (TT-BD-06), `Hosting/` (TT-API-03), `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md`, `docs/openapi.v1.json` a mano, specs ajenas.

## 2. Riesgos y efectos secundarios
- **Dependencia de TT-BD-06:** sin la colección `colonias`, la fixture y (según la duda (a)) la consulta interna con `id`, `cp` y `municipio`, no se pueden cerrar los casos 03, 04, 05, 08, 12 y 14.
- **400 en lugar de 422:** si `lat`/`lon` se enlazan como `double`, `[ApiController]` responde 400 antes del `ValidationFilter`, y el enlace numérico acepta `NaN`, `Infinity` y separadores de miles (`20,05` → `2005`). Se enlazan como texto y se interpretan con cultura invariante (casos 01, 02 y 06).
- **OpenAPI:** con los parámetros enlazados como texto, el documento generado diría `string`; hace falta un transformador o atributos para declarar `number` con rangos (caso 13).
- **Fuga fuera de la API:** la query con coordenadas en el gateway, la entrada de Container Apps y Application Insights (duda (f)).
- **Pruebas de rendimiento inestables en el CI:** el caso 14 corre en secuencia, con calentamiento y con un margen amplio; si falla por el entorno, se informa y no se baja la meta sin decisión del líder.
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
| TC-TT-API-07-08 | `SecurityTest/Catalog/NeighborhoodAtPointLogPrivacyTests` | seguridad negativa (MongoDB) |
| TC-TT-API-07-09 | `IntegrationTest/Catalog/NeighborhoodAtPointCacheTests` | integración (doble) |
| TC-TT-API-07-10 | `SecurityTest/Catalog/NeighborhoodAtPointAuthTests` | seguridad negativa |
| TC-TT-API-07-11 | `IntegrationTest/Catalog/NeighborhoodAtPointRateLimitTests` | integración (doble) |
| TC-TT-API-07-12 | `SecurityTest/Catalog/NeighborhoodAtPointFailureTests` | seguridad negativa (MongoDB detenido) |
| TC-TT-API-07-13 | `IntegrationTest/OpenApi/NeighborhoodAtPointOpenApiTests` | contrato (OpenAPI generado) |
| TC-TT-API-07-14 | `IntegrationTest/Catalog/NeighborhoodAtPointPerformanceTests` | rendimiento local (MongoDB) |
| TC-TT-API-07-15 | `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `dotnet list package --vulnerable`, `gitleaks` | comando (evidencia en el PR) |
| TC-TT-API-07-16 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. Aprobación de la spec con las dudas (a) a (h) resueltas; si (a) es la opción (3), ajustar la spec de TT-BD-06 antes de implementarla.
2. Contrato (spec §4) y pruebas que fallan primero: validador (casos 01 y 02) y endpoint con doble (casos 06, 07, 09, 10, 11 y 13).
3. Validador, DTOs y controller con `[Authorize]`, `Cache-Control: no-store` y la documentación OpenAPI.
4. Con TT-BD-06 fusionada: casos 03, 04, 05, 08, 12 y 14 con Testcontainers de MongoDB.
5. `docs/endpoints.md` y `docs/rate-limit-matrix.md` (caso 15); `export_openapi.py` si ya existe.
6. Gates, `Memoria.md`, informe y evidencia en el PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
dotnet list Knock.slnx package --vulnerable --include-transitive
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-07
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
