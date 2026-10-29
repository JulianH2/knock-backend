# plan.md — TT-OBS-01 — Observabilidad: Application Insights en la API y Sentry en la app

> Borrador. Servicio afectado: Transversal (`src/Knock.Api/Hosting/`). Dudas P1 a P9 de la spec §8 abiertas para el líder. Nada implementado; los paquetes de la spec §3 no se agregan sin la autorización del líder.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query "telemetry tracing correlation logging"`):**
  - `Hosting/CorrelationIdMiddleware` y `CorrelationId` (reglas del id, `Current(HttpContext)`), `Hosting/ProblemDetailsExtensions` (`traceId` y `correlationId` del cuerpo), `Hosting/LoggingExtensions` (`AddKnockLogging`, `RoutePath` para la plantilla de la ruta, `ScopeRequestPathEnricher`), `Hosting/HealthExtensions` (rutas a excluir).
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `TestController` con `/api/v1/test/at/{…}` y la excepción no controlada, `TestHttp`, `AuthHttp`, `ConsoleCapture`) y `tests/SecurityTest/Logging/SensitiveLogTests` como molde de los datos sensibles.
- **Crear (por confirmar al implementar):**
  - `src/Knock.Api/Hosting/ObservabilityExtensions.cs` (`AddKnockObservability`), `ObservabilityOptions`, `TelemetryRedactor` y los procesadores de spans y logs, todos `internal`.
  - `docs/OBSERVABILIDAD.md` (qué se envía, relación de ids, KQL de soporte, alertas y verificación manual).
  - Pruebas: `tests/UnitTest/Hosting/TelemetryRedactorTests`, `tests/IntegrationTest/Observability/*`, `tests/SecurityTest/Observability/*` y un apoyo de captura en memoria en `tests/IntegrationTest/Support/` (p. ej. `TelemetryCapture`).
- **Modificar:** `Hosting/KnockApplicationExtensions.AddKnockApi` (una línea), `Hosting/CorrelationIdMiddleware` (atributo del span), `Hosting/LoggingExtensions.AddKnockLogging` (`writeToProviders: true`), `appsettings.json` (`Observability:*` sin secretos), `KnockApiFactory` (exportador en memoria), `src/Knock.Api/Knock.Api.csproj` y los `.csproj` de pruebas **solo con los paquetes que agregue o autorice el líder**.
- **No tocar:** `UseKnockApi` y `docs/MIDDLEWARES.md` (no hay middleware nuevo), `PipelineOrderTests`, las pruebas de TT-API-03 y TT-API-07, `.github/`, `scripts/`, `.agents/`, `constitution.md`, `security/exceptions.yaml`, `docs/openapi.v1.json`, configuración de analizadores.

## 2. Riesgos y efectos secundarios
- **Logs dobles o con otros niveles:** el proveedor de logs de OpenTelemetry tiene su propio filtro (`Logging:LogLevel`). Con `writeToProviders: true` Serilog filtra primero; hay que comprobar que no se registra además el proveedor por la ruta de `ILoggingBuilder` (caso 06) y que las propiedades del `LogContext` (`CorrelationId`) llegan como atributos (por verificar con la versión de Serilog.Extensions.Hosting).
- **`url.path` con valores:** la instrumentación pone la ruta real en `url.path`; hay que reemplazarla por la plantilla en el `Enrich`/procesador (caso 08). Application Insights usa `url.path` + `url.query` para la columna `url` de `requests`.
- **Redacción de la query por defecto:** las instrumentaciones 1.20 redactan los valores de la query (`Redacted`), pero se controla por variable de entorno; la prueba no confía en el valor por defecto (casos 08 y 11).
- **Excepciones con datos:** `RecordException` copia el mensaje y la pila; el redactor debe correr sobre los eventos (caso 10).
- **Exportador con disco:** `Azure.Monitor.OpenTelemetry.Exporter` guarda reintentos en disco si no se apaga (`DisableOfflineStorage = true`; caso 02).
- **Versiones de OpenTelemetry:** el exportador 1.10.0 pide `OpenTelemetry.Extensions.Hosting` ≥ 1.18.0; las instrumentaciones y el exportador en memoria deben ir en la misma versión (1.20.1) para no mezclar SDK.
- **Pruebas en paralelo:** `ActivitySource` y `Meter` son globales del proceso; el exportador en memoria de cada fábrica debe filtrar por su propio recurso o las pruebas de telemetría deben ir en una colección sin paralelismo.
- **Costo:** la cuota gratuita es de 5 GB al mes (ADR-006); sin spans de `/health` y con muestreo configurable (duda P7). Las reglas de alerta tienen costo propio (duda P3, TT-AZ-04).
- **Dependencia con Azure:** sin recurso no se cierran los casos 15 y 16 (duda P4); el resto no depende.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-OBS-01-01 | `IntegrationTest/Observability/ExporterRegistrationTests` | integración |
| TC-TT-OBS-01-02 | `IntegrationTest/Observability/ExporterRegistrationTests` | integración (cadena sintética) |
| TC-TT-OBS-01-03 | `IntegrationTest/Observability/TracingTests` | integración (en memoria) |
| TC-TT-OBS-01-04 | `IntegrationTest/Observability/CorrelationTelemetryTests` | integración (en memoria) |
| TC-TT-OBS-01-05 | `SecurityTest/Observability/TraceContextInjectionTests` | seguridad negativa |
| TC-TT-OBS-01-06 | `IntegrationTest/Observability/LogExportTests` | integración (en memoria) |
| TC-TT-OBS-01-07 | `IntegrationTest/Observability/MetricsTests` | integración (en memoria) |
| TC-TT-OBS-01-08 | `SecurityTest/Observability/SpanPrivacyTests` | seguridad negativa |
| TC-TT-OBS-01-09 | `SecurityTest/Observability/LogExportPrivacyTests` | seguridad negativa |
| TC-TT-OBS-01-10 | `SecurityTest/Observability/ExceptionTelemetryPrivacyTests` | seguridad negativa |
| TC-TT-OBS-01-11 | `IntegrationTest/Observability/OutgoingHttpTelemetryTests` | integración (servidor HTTP local) |
| TC-TT-OBS-01-12 | `UnitTest/Hosting/TelemetryRedactorTests` | unitaria |
| TC-TT-OBS-01-13 | `SecurityTest/Observability/ConnectionStringNotInRepoTests` + `gitleaks` | seguridad negativa + comando |
| TC-TT-OBS-01-14 | `IntegrationTest/Observability/ObservabilityRegressionTests` + suites existentes | regresión |
| TC-TT-OBS-01-15 | `docs/OBSERVABILIDAD.md` §«Verificación manual» | manual en Azure (capturas) |
| TC-TT-OBS-01-16 | `docs/OBSERVABILIDAD.md` §«Alertas» | manual en Azure (captura) |
| TC-TT-OBS-01-17 | `dotnet list package --vulnerable`, `check_exceptions.py` | comando |
| TC-TT-OBS-01-18 | `dotnet build` + `dotnet format --verify-no-changes` | comando |
| TC-TT-OBS-01-19 a 22 | knock-frontend | otro repo |

## 4. Secuencia
1. Respuestas del líder a P1 a P9 y aprobación de la spec; el líder agrega o autoriza los paquetes de §3 (P8).
2. Pruebas que fallan primero: redactor (12), registro sin y con cadena (01, 02) y privacidad de spans y logs (08, 09, 10).
3. `TelemetryRedactor` y procesadores; `AddKnockObservability` con la tubería común y el exportador condicional; filtro de `/health`; plantilla de la ruta en `url.path`.
4. Atributo `knock.correlation_id` en `CorrelationIdMiddleware` y `writeToProviders` en Serilog (04, 05, 06); métricas (07); llamadas salientes (11).
5. Regresión de TT-API-03 y TT-API-07 sin editar (14); configuración sin secretos (13).
6. `docs/OBSERVABILIDAD.md` con alertas y pasos manuales; con el recurso de Azure, casos 15 y 16 (una persona pone la cadena por variable).
7. Gates, `Memoria.md` (evaluación de cada paquete), informe y capturas en el PR.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
dotnet list Knock.slnx package --vulnerable --include-transitive
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-OBS-01
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
gitleaks detect --no-banner
```
