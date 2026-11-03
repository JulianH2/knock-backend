# plan.md — TT-API-11 — Redactar los logs de petición con query

> Borrador. Servicio afectado: Transversal (`Knock.Api`, capa de arranque `Hosting/`). Falta la aprobación de la spec y la respuesta del líder a las dudas de spec §8. Sin endpoints, base de datos ni eventos.

## 1. Mapeo de archivos
- **Reutilizar:**
  - `src/Knock.Api/Hosting/LoggingExtensions.cs` (`AddKnockLogging`, `ConfigureRequestLogging` y `ScopeRequestPathEnricher` de TT-API-03): el filtro nuevo va en el mismo `AddSerilog(...)`, antes del sink de consola.
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `TestHttp`, `ConsoleCapture`, `TestController` con el endpoint `at/{latitude}/{longitude}` y `log`), enlazado en `SecurityTest`.
  - `tests/SecurityTest/Logging/AspNetCoreLogLevelGuardTests.cs` (TT-API-07): se conserva sin tocar aserciones.
  - `python scripts/codemap.py --query "serilog logging filter console capture"` antes de crear nada.
- **Crear:** `tests/SecurityTest/Logging/RequestLogQueryTests.cs` (casos 01 a 04).
- **Modificar:** `src/Knock.Api/Hosting/LoggingExtensions.cs` (una llamada a `Filter.ByExcluding`); `Memoria.md`; opcionalmente un comentario en `AspNetCoreLogLevelGuardTests` (spec §8, duda 3).
- **No tocar:** `.github/`, `scripts/`, configuración de analizadores, constitución, `appsettings*.json` (los niveles siguen en `Warning`), `docs/openapi.v1.json`, `*.csproj` y `packages.lock.json` (sin paquetes nuevos: `Serilog.Filters`/`Matching` ya vienen con Serilog), pruebas de TT-API-03 y TT-API-07.

## 2. Riesgos y efectos secundarios
- **Orden con TT-OBS-01 (#89):** esa tarea agrega `WriteTo.Sink(new OpenTelemetryLogSink(...))` en `AddKnockLogging`. El filtro debe quedar **antes** de todos los sinks para que la telemetría tampoco reciba los eventos excluidos; el que fusione el segundo PR resuelve el conflicto de ese archivo y vuelve a correr el caso 01.
- **Qué se pierde:** los eventos «Request starting» y «Request finished» de ASP.NET Core. No se reactivan con configuración. El log de petición de TT-API-03 los sustituye (caso 03).
- **Lo que la prueba puede encontrar:** con `Default` en `Debug`, otra categoría podría traer la URL (duda 2). Si pasa, no se agrega a escondidas: se documenta y el líder decide.
- **Conexiones y duración:** pruebas sin Testcontainers (no hay base de datos); `KnockApiFactory` con la cadena sintética que no responde, como en TT-API-03.
- **Fragilidad de la captura de consola:** `ConsoleCapture` cambia `Console.Out`; las pruebas que la usan no deben correr en paralelo con otras que también lo hagan (mismo cuidado que `SensitiveLogTests`, en la misma colección si hace falta).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-API-11-01 | `SecurityTest/Logging/RequestLogQueryTests.QueryNeverReachesTheLogsWhateverTheConfiguredLevel_TC_TT_API_11_01` | seguridad |
| TC-TT-API-11-02 | `SecurityTest/Logging/RequestLogQueryTests.HostingDiagnosticsCategoryIsDroppedAtEveryLevel_TC_TT_API_11_02` | seguridad |
| TC-TT-API-11-03 | `SecurityTest/Logging/RequestLogQueryTests.OneRequestLinePerResponseWithoutQuery_TC_TT_API_11_03` | seguridad |
| TC-TT-API-11-04 | `SecurityTest/Logging/RequestLogQueryTests.OtherLogsAreNotAffected_TC_TT_API_11_04` | seguridad |
| TC-TT-API-11-05 | `AspNetCoreLogLevelGuardTests`, `SensitiveLogTests` y las de logs de TT-API-03 sin editar + mutación manual | regresión (evidencia en el PR) |
| TC-TT-API-11-06 | build, formato y scripts de gobierno | comando (evidencia en el PR) |

## 4. Secuencia
1. Aprobación de la spec y respuesta a las dudas 1 y 2 de spec §8.
2. Escribir las pruebas de los casos 01 a 04 y verlas **fallar** contra el estado actual (con `Microsoft.AspNetCore` bajado a `Information` salen los eventos de hospedaje con la query).
3. Agregar el filtro en `LoggingExtensions.AddKnockLogging` y verlas pasar.
4. Mutación manual (quitar el filtro) para el caso 05; revertirla.
5. Gates, `Memoria.md` y PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/SecurityTest -c Release --no-build --filter "FullyQualifiedName~Logging"
dotnet test tests/UnitTest tests/IntegrationTest tests/SecurityTest tests/DatabaseTest -c Release --no-build   # requiere Docker
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-11
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
