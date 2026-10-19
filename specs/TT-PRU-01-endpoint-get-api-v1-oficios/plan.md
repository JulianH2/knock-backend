# plan.md — TT-PRU-01 Endpoint GET /api/v1/oficios con su prueba

> Plan con la opción propuesta D1-A (controller `internal` en Catalog). Si el revisor elige D1-B, cambian las filas marcadas con (A).

## 1. Mapeo de archivos
- **Reutilizar (buscado con `codemap.py --query "oficios catalog controller"`):**
  - `src/Knock.Api/Modules/Catalog/Domain/Oficio.cs` y `Data/CatalogDbContext.cs` (`DbSet<Oficio> Oficios`) — entidad y contexto de TT-BD-02.
  - `src/Knock.Api/Modules/Catalog/CatalogModuleExtensions.cs` — registro del servicio nuevo.
  - `src/Knock.Api/Hosting/KnockApplicationExtensions.cs` (`AddControllers`, `UseExceptionHandler`, `MapControllers`), `ApiVersioningExtensions.cs`, `ProblemDetailsExtensions.cs`, `OpenApiExtensions.cs` — pipeline de TT-API-01; no se duplica nada.
  - `db/seeds/catalog/oficios.sql` — datos de los casos.
  - `tests/IntegrationTest/Support/KnockApiFactory.cs` (parámetro `connectionString`), `Support/CatalogTestDatabase.cs`, `Data/PostgisFixture.cs` — host y base de pruebas.
  - Patrón de `tests/IntegrationTest/Data/CatalogLocationPrivacyTests.cs` (contenedor por prueba) y `Versioning/ApiVersioningTests.cs` (cliente HTTP).
- **Crear:**
  - (A) `src/Knock.Api/Modules/Catalog/Controllers/V1/OficiosController.cs` — `internal sealed`, `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/oficios")]`, `[AllowAnonymous]`, `[ProducesResponseType<IReadOnlyList<OficioResponse>>(200)]`.
  - `src/Knock.Api/Modules/Catalog/Dtos/OficioResponse.cs` — `internal sealed record OficioResponse(string Clave, string Nombre, string Modalidad)`.
  - `src/Knock.Api/Modules/Catalog/Services/OficioQueries.cs` (nombre tentativo) — `internal sealed`, `ListActiveAsync(CancellationToken)` con `AsNoTracking`, `Where(Activo)`, orden de D4, `Select` al DTO.
  - (A) `src/Knock.Api/Hosting/ModuleControllerFeatureProvider.cs` (nombre tentativo) — acepta controllers `internal` de `Knock.Api.Modules.*`.
  - `tests/IntegrationTest/Catalog/OficiosEndpointTests.cs` — casos 01–05.
  - `tests/IntegrationTest/OpenApi/OficiosOpenApiTests.cs` — caso 06.
  - `docs/endpoints.md` — si el revisor confirma D6.
- **Modificar:**
  - `CatalogModuleExtensions.cs` — `services.AddScoped<OficioQueries>()`.
  - (A) `KnockApplicationExtensions.cs` — `AddControllers().ConfigureApplicationPartManager(...)` con el proveedor nuevo.
  - `Memoria.md` — al cerrar.
- **No tocar:** `ICatalogApi` (en A), `CatalogApi`, migraciones, semillas, `tests/UnitTest/Architecture/**` (incluido `ModuleSurfaceTests`), `.github/**`, `scripts/**`, `.editorconfig` y `Directory.Build.props` (salvo excepción autorizada por el revisor, D1), `docs/openapi.v1.json` (no existe; no se crea a mano), knock-frontend.

## 2. Riesgos y efectos secundarios
- **Arquitectura:** el proveedor de controllers cambia cómo MVC descubre controllers para toda la API. Debe seguir aceptando los públicos (el `TestController` de pruebas) y limitar los `internal` a `Knock.Api.Modules.*`. Lo cubren 01 (se sirve), 06 (aparece en OpenAPI) y la suite de TT-API-01 (`/api/v1/test/*`).
- **Analizadores:** CA1812 podría marcar el controller `internal` como no instanciado; CA1515 no aplica en `Modules/**`. Con `TreatWarningsAsErrors` cualquier aviso rompe el build: resolverlo con el revisor (D1), nunca con `#pragma`.
- **Orden:** la intercalación de la base puede variar entre contenedor y Azure (D4); la semilla tiene el mismo orden por `clave` que por nombre, por eso el caso 03 agrega un oficio sintético que los distingue.
- **Contrato:** primer endpoint de negocio; la publicación a knock-frontend no existe todavía (D5). Declararlo en el PR.
- **Datos:** las pruebas insertan oficios sintéticos solo en su contenedor; nada se siembra en la semilla versionada.
- **Tiempo de CI:** cada prueba levanta un contenedor PostGIS (patrón de la suite); 6 pruebas nuevas suman minutos al job de integración.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-PRU-01-01 | `OficiosEndpointTests.GetOficios_SeededCatalog_Returns200WithThreeTradesOrderedByName_TC_TT_PRU_01_01` — 200, `application/json`, 3 elementos en orden ele/mec/plo con sus valores exactos | IntegrationTest |
| TC-TT-PRU-01-02 | `OficiosEndpointTests.GetOficios_InactiveTrade_IsExcluded_TC_TT_PRU_01_02` — inserta «Albañilería» inactiva; no aparece; siguen 3 | IntegrationTest |
| TC-TT-PRU-01-03 | `OficiosEndpointTests.GetOficios_OrderIsByNameNotByKeyOrInsertion_TC_TT_PRU_01_03` — inserta `zap`/«Carpintería» activa; orden Carpintería, Electricidad, Mecánica, Plomería | IntegrationTest |
| TC-TT-PRU-01-04 | `OficiosEndpointTests.GetOficios_Response_ExposesOnlyClaveNombreModalidad_TC_TT_PRU_01_04` — `JsonDocument`: cada objeto con exactamente esas 3 propiedades, no vacías | IntegrationTest |
| TC-TT-PRU-01-05 | `OficiosEndpointTests.GetOficios_WithoutCredentials_Returns200_TC_TT_PRU_01_05` — sin `Authorization` → 200 | IntegrationTest |
| TC-TT-PRU-01-06 | `OficiosOpenApiTests.GetOpenApi_Development_DocumentsOficiosOperation_TC_TT_PRU_01_06` — `/openapi/v1.json` contiene la ruta de oficios (`/api/v1/oficios` o `/api/v{version}/oficios`, D5) con GET 200 y esquema de arreglo con las 3 propiedades | IntegrationTest |
| TC-TT-PRU-01-07 | Checks de GitHub (`gate`) + build Release y `dotnet format` (comando) | CI / PR |

## 4. Secuencia
1. Revisor resuelve D1–D9; el autor pasa las resueltas a «Resuelto» y el revisor aprueba la spec.
2. Contrato: `docs/endpoints.md` (si D6) con la fila de §4.
3. DTO + servicio de consulta + registro en `CatalogModuleExtensions`.
4. Proveedor de controllers (A) + controller delgado.
5. Pruebas de integración 01–06 (escribirlas antes de dar el código por bueno; comprobar que 02 y 03 fallan si se quita el filtro o el orden).
6. Regresión: Unit + Architecture (TC-TT-API-01-07/09), Integration y Security completas.
7. Gates + `Memoria.md` + informe de `story-workflow` en el PR (con «Uso de IA» y la nota de la ficha: tiempo, dificultades).

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build     # requiere Docker
dotnet test tests/SecurityTest tests/DatabaseTest -c Release --no-build
PYTHONIOENCODING=utf-8 python scripts/sdd_lint.py && PYTHONIOENCODING=utf-8 python scripts/sdd_lint.py --trace TT-PRU-01
PYTHONIOENCODING=utf-8 python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
En la máquina del autor no hay .NET 10 SDK ni Docker en marcha: lo que no se ejecute localmente se declara así en el informe y se toma la evidencia del CI.
