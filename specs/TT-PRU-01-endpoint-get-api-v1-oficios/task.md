# TT-PRU-01 — tareas

> Solo después de que el revisor apruebe la spec. Tareas escritas para la opción D1-A; con D1-B cambia T2.

## T1 — Contrato y consulta [Back]
- **Crear:** `Modules/Catalog/Dtos/OficioResponse.cs` (`internal sealed record`); `Modules/Catalog/Services/OficioQueries.cs` (`AsNoTracking`, solo activos, orden de §8 D4, proyección al DTO); `docs/endpoints.md` si se confirma D6.
- **Modificar:** `Modules/Catalog/CatalogModuleExtensions.cs` (registro scoped).
- **Verificar:** TC-TT-PRU-01-02 · TC-TT-PRU-01-03 · TC-TT-PRU-01-04
- **No hacer:** cambiar `ICatalogApi`, `Oficio`, migraciones o semillas; serializar la entidad; SQL interpolado.
- **Agente:** api-builder

## T2 — Controller y descubrimiento [Back]
- **Crear:** `Modules/Catalog/Controllers/V1/OficiosController.cs` (`internal sealed`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/oficios")]`, `[AllowAnonymous]`, `[ProducesResponseType]`, sin `try/catch` ni `DbContext`). El proveedor de controllers `internal` ya existe (`AddInternalControllers()`, TT-API-02): no se crea otro.
- **Verificar:** TC-TT-PRU-01-01 · TC-TT-PRU-01-05 · TC-TT-PRU-01-06 · regresión TC-TT-API-01-07, TC-TT-API-01-09, TC-TT-API-01-10
- **No hacer:** hacer público el controller o el DTO; tocar `ModuleSurfaceTests`; `#pragma`/`NoWarn` para avisos de analizadores (si aparece CA1812, consultarlo con el revisor).
- **Agente:** api-builder

## T3 — Pruebas de integración [Pruebas]
- **Crear:** `tests/IntegrationTest/Catalog/OficiosEndpointTests.cs` (01–05) y `tests/IntegrationTest/OpenApi/OficiosOpenApiTests.cs` (06), con `PostgisFixture`, `KnockApiFactory(connectionString: …)` y `CatalogTestDatabase.MigrateAndSeedAsync`; datos extra sintéticos insertados en la prueba.
- **Verificar:** TC-TT-PRU-01-01 a TC-TT-PRU-01-06; comprobar a mano que 02 y 03 fallan al quitar el filtro o el orden (y revertir).
- **No hacer:** EF InMemory, `Skip`, asserts que solo comprueban «200»; datos reales.
- **Agente:** api-builder

## T4 — Gates, CI y cierre [Pruebas]
- **Verificar:** TC-TT-PRU-01-07 (checks de GitHub, build Release 0/0, `dotnet format`); `sdd_lint.py --trace TT-PRU-01`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`.
- **Modificar:** `Memoria.md`; descripción del PR con «cambia el contrato», «Uso de IA», tiempo y dificultades (ficha §5).
- **No hacer:** bajar umbrales ni tocar workflows; afirmar resultados no ejecutados.
- **Agente:** api-builder · security-reviewer · traceability-clerk · docs-keeper
