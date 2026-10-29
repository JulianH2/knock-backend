# TT-OBS-01 — Observabilidad: Application Insights en la API (OpenTelemetry) y Sentry en la app

> **Jira:** KNOCK-51 · **Área:** OBS · Observabilidad · **Servicio:** Transversal (hoy `src/Knock.Api/Hosting/`; cada servicio de ADR-001 lo reutiliza al separarse con TT-ARQ-01) · **Responsable:** José Julián Hernández Hernández · **Sprint:** S2 · **Puntos:** 2 · **Prioridad:** Técnica · **Capas:** Back · Mobile (otro repo)
> **Repositorio:** knock-backend · **Otra parte en:** knock-frontend (Sentry en la app y `traceId` en los mensajes de error; ver duda P2) · **Rama:** `chore/KNOCK-51-observabilidad` → PR a `sprint-02`
> **Ficha:** `docs/backlog/TT-OBS-01.md` (generada; no se edita).
> **Depende de:** TT-API-03 (KNOCK-37, `X-Correlation-Id`, Serilog y ProblemDetails; fusionado) · **TT-AZ-02 (KNOCK-34, recurso de Application Insights): la ficha dice que depende y el líder dijo que la tarea no tiene dependencias; queda como duda P4.** Esta spec se puede implementar y probar entera sin Azure (§8, «Cómo se prueba sin Azure»); solo los casos 15 y 16 (prueba manual en el tablero) necesitan el recurso.
> **La consumen:** TT-API-07 (KNOCK-141, decisión (f): quitar la query con coordenadas de la telemetría de Application Insights, con su prueba) · TT-ARQ-01/02 (gateway y servicios separados) · ADR-001 regla 5.
> **Fuentes:** ficha TT-OBS-01 · constitución 2.2.1 (principios 3, 8, 9 y 16; §3 «Nube»; §6 secretos; §12 «Observabilidad») · `docs/DECISION_TECNICA.md` §10 · ADR-001 (regla 5) · ADR-006 (punto 5, capa gratuita de Application Insights) · spec TT-API-03 (casos 08 a 12, 15 y 16; decisión 4) · spec TT-API-07 (§3, §6 y decisión (f)) · `docs/MIDDLEWARES.md` · `src/Knock.Api/Hosting/{KnockApplicationExtensions,CorrelationIdMiddleware,LoggingExtensions,ProblemDetailsExtensions}.cs` · `src/Knock.Api/appsettings*.json` · `tests/SecurityTest/Logging/SensitiveLogTests.cs` · `tests/IntegrationTest/Support/{KnockApiFactory,ConsoleCapture}.cs`
> **Skills:** story-workflow, spec-first-writing, foundation-backend, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Ver errores, tiempos y uso de la API (y de la app) para detectar problemas antes que los usuarios, **sin enviar datos personales a la telemetría**: la API emite trazas, métricas y logs con OpenTelemetry y, solo cuando hay cadena de conexión por configuración, los exporta a Application Insights; la app reporta sus errores a Sentry con el entorno, la versión y el `traceId` de la API para que soporte encuentre la petición.

**Terminada cuando:** en las pruebas (sin Azure) un exportador en memoria demuestra que cada petición produce su span, sus métricas y sus logs con la plantilla de la ruta, el `X-Correlation-Id` y el trace-id del cuerpo de error, y que ninguno lleva celulares, tokens, coordenadas, query strings ni `Authorization`; sin cadena de conexión no se exporta nada; y, con el recurso de Azure, un error de prueba aparece en Application Insights y en Sentry y la alerta de 5xx se dispara (V1 y V2 de la ficha, manuales).

**Qué ya existe y qué cambia** (leído en el código; `python scripts/codemap.py --query "telemetry tracing correlation logging"` no encuentra nada de OpenTelemetry):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| `Hosting/CorrelationIdMiddleware` + `CorrelationId` | Asigna o propaga `X-Correlation-Id` (máx. 64 de `[A-Za-z0-9._-]`), lo pone en `HttpContext.Items`, en la respuesta y en el `LogContext` de Serilog. | **Se extiende:** además pone el id como atributo `knock.correlation_id` en el span de la petición (`Activity.Current`). Sin cambio de reglas, de encabezado ni de orden. |
| `Hosting/ProblemDetailsExtensions` | Cuerpo de error con `traceId` (lo pone ASP.NET Core: `Activity.Current.Id`, formato W3C `00-<trace-id>-<span-id>-01`) y `correlationId`, separados (decisión 4 de TT-API-03). | **Sin cambios.** Con OpenTelemetry el `Activity` es el mismo, así que el `<trace-id>` del `traceId` es el `operation_Id` de Application Insights (caso 04). |
| `Hosting/LoggingExtensions.AddKnockLogging` | Serilog a consola en JSON, `Enrich.FromLogContext`, `ScopeRequestPathEnricher`, niveles desde `appsettings` (`Microsoft.AspNetCore: Warning`). | **Se extiende lo mínimo:** Serilog reenvía sus eventos ya filtrados al proveedor de logs de OpenTelemetry (`writeToProviders: true`), así los niveles y los enriquecedores siguen siendo los de TT-API-03. La consola no cambia. |
| `Hosting/KnockApplicationExtensions` | `AddKnockApi` / `UseKnockApi` con el orden de `docs/MIDDLEWARES.md`. | **Se agrega** `AddKnockObservability(configuration, environment)` en `AddKnockApi`. **No hay middleware nuevo**: la instrumentación escucha los eventos de diagnóstico, así que el orden de la tubería y `PipelineOrderTests` no cambian. |
| Application Insights / OpenTelemetry / Sentry | No existen (ni paquetes, ni configuración, ni recurso). | **Nuevo:** extensión de observabilidad, procesador de redacción, opciones `Observability:*`, pruebas con exportador en memoria y `docs/OBSERVABILIDAD.md` (alertas y verificación manual). |

## 2. Historia y casos de prueba
**TT-OBS-01** — Tarea técnica (no es historia de usuario). Los casos 01 a 14 se verifican con pruebas de código en este repositorio y sin Azure; el 15 y el 16 son la verificación manual de la ficha (V1 y V2) con evidencia en el PR; el 17 y el 18 con comandos; del 19 al 22 son de la app **(otro repo)**. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_OBS_01_KK`. Datos solo ficticios: celulares `+52 55 5555 01xx` y `+15555550199` (los de TT-API-03), coordenadas de prueba `19.4326/-99.1332`, una CURP de ejemplo con formato válido y tokens sintéticos. La telemetría se captura con el **exportador en memoria** de OpenTelemetry registrado solo por `KnockApiFactory` (nunca en el código de la API).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-OBS-01-01 | Dado un entorno sin `APPLICATIONINSIGHTS_CONNECTION_STRING` (Development, Testing o la variable vacía), cuando la API arranca y atiende peticiones, entonces **no se registra ningún exportador de Azure Monitor** (trazas, métricas ni logs), la API responde igual que hoy y no abre conexiones a Azure. | `IntegrationTest/Observability/ExporterRegistrationTests.NoConnectionString_RegistersNoAzureMonitorExporter_TC_TT_OBS_01_01` |
| TC-TT-OBS-01-02 | Dada una cadena de conexión **sintética** cuyo `IngestionEndpoint` es `http://127.0.0.1:9/` (no responde), cuando la API arranca y atiende 20 peticiones, entonces registra los tres exportadores de Azure Monitor, todas las respuestas son las esperadas (la exportación no bloquea ni lanza), el almacenamiento local de reintentos está apagado (`DisableOfflineStorage`) y el cierre no tarda más de 5 s. | `IntegrationTest/Observability/ExporterRegistrationTests.SyntheticConnectionString_ExportsWithoutBlockingRequests_TC_TT_OBS_01_02` |
| TC-TT-OBS-01-03 | Dada una petición a `GET /api/v1/oficios` y otra a una ruta con parámetro, cuando termina, entonces el exportador en memoria tiene **un span de servidor** con `http.route` = plantilla (`/api/v{version}/…`), método y `http.response.status_code`, y el recurso trae `service.name` = `knock-api`, `service.version` y `deployment.environment`; las peticiones a `/health` y `/health/ready` **no** generan spans. | `IntegrationTest/Observability/TracingTests.ServerSpan_HasRouteTemplateAndResource_HealthIsExcluded_TC_TT_OBS_01_03` |
| TC-TT-OBS-01-04 | Dadas una petición con `X-Correlation-Id` válido y otra sin él que termina en 500, cuando se revisan respuesta y telemetría, entonces el span de servidor tiene `knock.correlation_id` igual al encabezado de la respuesta (propagado o generado), el `<trace-id>` del campo `traceId` del ProblemDetails es el `TraceId` del span, y el cuerpo conserva `traceId` y `correlationId` como campos **separados** (no se unifican ni se cambia el formato de TT-API-03). | `IntegrationTest/Observability/CorrelationTelemetryTests.SpanCarriesCorrelationId_ProblemTraceIdMatchesSpan_TC_TT_OBS_01_04` |
| TC-TT-OBS-01-05 | Dadas peticiones con `traceparent` mal formado, con `baggage` que contiene un celular y un token, y con un `X-Correlation-Id` hostil (CR/LF, 200 caracteres), cuando se procesan, entonces la petición responde normal, el `traceparent` inválido inicia una traza nueva, **ningún valor del `baggage`** aparece en spans ni logs exportados y el id hostil no llega a `knock.correlation_id` (se usa el generado, como en TC-TT-API-03-10). | `SecurityTest/Observability/TraceContextInjectionTests.HostileTraceContextAndBaggage_DoNotReachTelemetry_TC_TT_OBS_01_05` |
| TC-TT-OBS-01-06 | Dada una petición durante la que un servicio escribe un log `Information` y otro `Warning`, cuando se revisan los logs exportados, entonces ambos llegan al exportador con el `TraceId`/`SpanId` de la petición y el atributo `CorrelationId`; los eventos de `Microsoft.AspNetCore` por debajo de `Warning` («Request starting/finished») **no** se exportan (mismos niveles que Serilog); y la consola de Serilog no cambia (TC-TT-API-03-11 en verde sin editarla). | `IntegrationTest/Observability/LogExportTests.ServiceLogs_ExportedWithTraceAndCorrelation_SameLevelsAsSerilog_TC_TT_OBS_01_06` |
| TC-TT-OBS-01-07 | Dadas peticiones que terminan en 200, 404, 422 y 500, cuando se recogen las métricas en memoria, entonces existe `http.server.request.duration` con `http.route` (plantilla), método y código; los 500 se cuentan con `http.response.status_code` = 500 (base de la alerta de 5xx); y **ningún atributo de métrica** contiene la ruta con valores, la query ni el `X-Correlation-Id` (cardinalidad y privacidad). | `IntegrationTest/Observability/MetricsTests.RequestDuration_HasRouteAndStatus_NoHighCardinalityAttributes_TC_TT_OBS_01_07` |
| TC-TT-OBS-01-08 | Dadas peticiones a `GET /api/v1/colonias/en-punto?lat=19.4326&lon=-99.1332` (TT-API-07), a la ruta de pruebas `/api/v1/test/at/19.4326/-99.1332?phone=%2B15555550199&token=…`, a `POST /api/v1/auth/firebase` con un ID token sintético en el cuerpo, todas con `Authorization: Bearer <JWT>` y `Cookie`, cuando se revisa **todo** lo exportado de cada span (nombre, atributos, eventos, descripción del estado, enlaces y recurso), entonces no aparece `19.4326`, `99.1332`, el celular, el JWT, `Bearer `, el ID token ni ninguna query: `url.query` no existe o tiene solo nombres con el valor `Redacted`, `url.path` es la plantilla de la ruta y no hay atributos `http.request.header.*` de `Authorization` ni `Cookie`. | `SecurityTest/Observability/SpanPrivacyTests.Spans_DoNotContainPhonesTokensCoordinatesQueryOrAuthorization_TC_TT_OBS_01_08` |
| TC-TT-OBS-01-09 | Dados los mismos escenarios del caso 08 más un log de servicio que recibe un celular `+52`, una CURP de ejemplo y coordenadas como propiedades estructuradas, cuando se revisan los logs exportados (cuerpo, plantilla, atributos y scopes), entonces ninguno de esos valores sale, tampoco la query ni `Authorization` (lo que pide la constitución §12, ahora también para Application Insights). | `SecurityTest/Observability/LogExportPrivacyTests.ExportedLogs_DoNotContainPhoneCurpTokensOrCoordinates_TC_TT_OBS_01_09` |
| TC-TT-OBS-01-10 | Dada una excepción no controlada cuyo mensaje contiene un celular, un JWT y coordenadas, cuando se exporta, entonces el span tiene estado de error y el evento `exception` con el **tipo** de la excepción, pero `exception.message` y `exception.stacktrace` no contienen esos valores (redactados); el log exportado del error tampoco; la respuesta 500 no cambia (sin detalle fuera de Development). | `SecurityTest/Observability/ExceptionTelemetryPrivacyTests.UnhandledException_ExportedWithoutSensitiveValues_TC_TT_OBS_01_10` |
| TC-TT-OBS-01-11 | Dada una llamada saliente de `HttpClient` a un servidor de prueba local con query `sig=…&se=…&token=…` y encabezado `Authorization`, cuando se exporta el span de cliente, entonces `url.full` no trae valores de la query (como una URL SAS de Blob, TT-API-05, o una llamada a Google de Firebase), no hay encabezados exportados y el `traceparent` se propaga al servidor de prueba (traza distribuida, ADR-001 regla 5). | `IntegrationTest/Observability/OutgoingHttpTelemetryTests.ClientSpan_RedactsQueryAndHeaders_PropagatesTraceparent_TC_TT_OBS_01_11` |
| TC-TT-OBS-01-12 | Dado el redactor de telemetría, cuando recibe textos y pares clave-valor, entonces: reemplaza celulares (`+52` con 10 dígitos, con o sin espacios, y `+1` de prueba), JWT (`eyJ…`.`…`.`…`), `Bearer <x>`, CURP, coordenadas (número con 4 o más decimales en rango de lat/lon, ver duda P6) y los valores de las claves `lat`, `lon`, `phone`, `celular`, `token`, `idToken`, `refreshToken`, `code`, `sig`, `authorization`; no altera textos sin datos sensibles (duraciones, códigos HTTP, GUID); y es estable ante entradas largas (≤ 1 ms por atributo de 4 KB, sin retroceso catastrófico de regex). | `UnitTest/Hosting/TelemetryRedactorTests.Redact_KnownSensitivePatternsAndKeys_LeavesOthersIntact_TC_TT_OBS_01_12` (Theory) |
| TC-TT-OBS-01-13 | Dado el repositorio, cuando se revisan `src/**/appsettings*.json`, `deploy/**` y `tests/**`, entonces no hay ninguna cadena de conexión real de Application Insights (`InstrumentationKey=` con un GUID que no sea el sintético de las pruebas, `IngestionEndpoint=https://…`) ni DSN de Sentry; la configuración solo se lee de `APPLICATIONINSIGHTS_CONNECTION_STRING` (variable o referencia de Key Vault en Container Apps). | `SecurityTest/Observability/ConnectionStringNotInRepoTests.RepositoryHasNoRealConnectionStringOrDsn_TC_TT_OBS_01_13` · (comando) `gitleaks detect`; salida en el PR |
| TC-TT-OBS-01-14 | Dado el cambio terminado, cuando se ejecutan las pruebas de TT-API-03 y TT-API-07, entonces siguen en verde **sin editar sus aserciones**: correlación (08 a 12 de TT-API-03), logs sin datos sensibles (15 de TT-API-03), orden de la tubería (`PipelineOrderTests`, 16 de TT-API-03; `docs/MIDDLEWARES.md` sin cambios) y logs sin coordenadas (08 y 16 de TT-API-07). | Regresión: `IntegrationTest/Observability/ObservabilityRegressionTests.ExistingCorrelationAndPipelineContractsHold_TC_TT_OBS_01_14` (vuelve a comprobar encabezado, cuerpo y orden con la observabilidad activa y exportador en memoria) + las suites existentes sin cambios |
| TC-TT-OBS-01-15 | **(V1, lado API.)** Dado el recurso de Application Insights y la API corriendo con su cadena de conexión puesta por variable de entorno (local o desplegada, sin escribirla en ningún archivo), cuando se provoca un error de prueba (ver duda P5: `GET /api/v1/oficios` con MongoDB detenido) con un `X-Correlation-Id` conocido, entonces en Application Insights aparecen la petición fallida, su excepción y su log, se encuentran buscando ese `correlationId` o el `<trace-id>` del `traceId`, y no tienen la query ni datos personales. | (comando) Prueba manual según `docs/OBSERVABILIDAD.md` §«Verificación manual»; capturas del tablero y la consulta KQL en el PR (sin la cadena de conexión) |
| TC-TT-OBS-01-16 | **(V2.)** Dadas las reglas de alerta de §4 (umbrales de la duda P3) creadas en Azure, cuando se provocan más 5xx que el umbral dentro de la ventana, entonces la alerta de 5xx se dispara y llega al grupo de acciones (correo del proyecto); la de latencia queda creada y su regla se revisa en el portal. | (comando) Prueba manual según `docs/OBSERVABILIDAD.md` §«Alertas»; captura de la alerta disparada en el PR |
| TC-TT-OBS-01-17 | Dado el trabajo terminado, entonces los únicos paquetes nuevos son los de §3 «Paquetes» **que el líder agregó o autorizó**, con versión fija, licencia y evaluación registradas en `Memoria.md`; `dotnet list Knock.slnx package --vulnerable --include-transitive` sin vulnerabilidades; OSV-Scanner y dependency-review en verde; sin `EXC-NNN` ni supresiones nuevas. | (comando) `dotnet list Knock.slnx package --vulnerable --include-transitive` y `python scripts/ci/check_exceptions.py`; salida en el PR |
| TC-TT-OBS-01-18 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | (comando) `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |
| TC-TT-OBS-01-19 | Dada la app compilada con `--dart-define=SENTRY_DSN=…` y `--dart-define=KNOCK_ENV=…`, cuando arranca, entonces Sentry se inicia con `environment` (`dev`, `prueba-cerrada`, `produccion`) y `release` = `knock-app@<versión>+<versionCode>` (constitución §9); sin DSN, Sentry no se inicia y la app funciona igual; el DSN no está en el repositorio. | (otro repo) knock-frontend |
| TC-TT-OBS-01-20 | Dado un error de la API con ProblemDetails, cuando la app muestra el mensaje de error, entonces el mensaje incluye un código de soporte con el `<trace-id>` (32 hexadecimales) del campo `traceId` (o el `correlationId`, según la duda P1), y el evento enviado a Sentry lleva ese valor y el `correlationId` como etiquetas. | (otro repo) knock-frontend |
| TC-TT-OBS-01-21 | Dados errores de la app con celular, token, coordenadas o la URL con query en el contexto, cuando se envían a Sentry, entonces `sendDefaultPii` es `false`, `beforeSend`/`beforeBreadcrumb` quitan esos valores (y la query de las URL de las migas de HTTP), no se adjuntan capturas de pantalla ni la jerarquía de vistas, y Sentry no agrega `sentry-trace`/`baggage` a las peticiones hacia la API (duda P2). | (otro repo) knock-frontend |
| TC-TT-OBS-01-22 | **(V1, lado app.)** Dado el proyecto de Sentry, cuando se provoca un error de prueba en la app con la API apuntando al mismo entorno del caso 15, entonces aparece en Sentry con entorno, versión y el código de soporte, y ese código encuentra la petición en Application Insights. | (otro repo) knock-frontend (manual; capturas en su PR) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `Hosting/ObservabilityExtensions` (nombre por confirmar con `codemap.py`): `AddKnockObservability` registra OpenTelemetry **siempre** (trazas de ASP.NET Core y `HttpClient`, métricas de ASP.NET Core, Kestrel y del runtime, y logs vía el proveedor de OpenTelemetry), con el recurso `service.name`, `service.version` y `deployment.environment`; y agrega los **exportadores de Azure Monitor solo si `APPLICATIONINSIGHTS_CONNECTION_STRING` tiene valor** (caso 01 y 02). Así las pruebas ejercitan la misma tubería que producción y solo cambia el exportador.
  - Filtros: sin spans de `/health` y `/health/ready`; sin captura de encabezados de petición ni respuesta; sin cuerpo; `url.path` = plantilla de la ruta (`http.route`), igual que hace `LoggingExtensions.RoutePath` para el log de petición (se **reutiliza** esa función, no se duplica).
  - Procesador de redacción (`BaseProcessor<Activity>` y de logs) que aplica un único `TelemetryRedactor` a atributos, eventos (`exception.*`), descripción del estado y logs; y el `baggage` nunca se copia a atributos.
  - `CorrelationIdMiddleware`: agrega `knock.correlation_id` al span actual. Nada más cambia de TT-API-03.
  - `AddKnockLogging`: `writeToProviders: true` para que los logs que ya pasaron los niveles y enriquecedores de Serilog lleguen a OpenTelemetry (caso 06).
  - Opciones en `appsettings.json` sin secretos: `Observability:ServiceName`, `Observability:SamplingRatio` (propuesto `1.0` con la cuota de 5 GB; ver duda P7) y `Observability:EnableLiveMetrics` (`false`).
  - `KnockApiFactory`: registra el exportador en memoria de trazas, métricas y logs para las pruebas (único lugar donde existe).
  - `docs/OBSERVABILIDAD.md`: qué se envía y qué no, cómo se relacionan `traceId` y `correlationId`, consultas KQL de soporte, definición de las alertas de §4 y pasos de la verificación manual (casos 15 y 16). `docs/MIDDLEWARES.md` no cambia (no hay middleware nuevo).
  - Pruebas de los casos 01 a 14 y evidencia de los casos 15 a 18.
- **Paquetes nuevos — los agrega el líder o se agregan con su autorización expresa** (no se agregan en esta spec ni sin esa autorización; constitución §3 y AGENTS.md §4). Versiones estables más recientes consultadas en nuget.org y pub.dev el 2026-10-29; se vuelven a confirmar, con sus vulnerabilidades, al implementar:

  | Paquete | Versión | Licencia | Proyecto | Para qué |
  |---|---|---|---|---|
  | `Azure.Monitor.OpenTelemetry.Exporter` | 1.10.0 | MIT | `Knock.Api` | Exportadores de trazas, métricas y logs a Application Insights. Trae `Azure.Core` y `OpenTelemetry.PersistentStorage.FileSystem` (almacenamiento de reintentos, que se apaga). |
  | `OpenTelemetry.Extensions.Hosting` | 1.20.1 | Apache-2.0 | `Knock.Api` | `AddOpenTelemetry()` en el contenedor de servicios. |
  | `OpenTelemetry.Instrumentation.AspNetCore` | 1.20.1 | Apache-2.0 | `Knock.Api` | Spans y métricas de las peticiones entrantes. |
  | `OpenTelemetry.Instrumentation.Http` | 1.20.1 | Apache-2.0 | `Knock.Api` | Spans de las llamadas salientes (`HttpClient`: Firebase, Blob). |
  | `OpenTelemetry.Exporter.InMemory` | 1.20.1 | Apache-2.0 | `tests/IntegrationTest`, `tests/SecurityTest` | Exportador en memoria para probar sin Azure. |
  | `sentry_flutter` (pub) | 10.2.0 | MIT | knock-frontend `app/` | Errores de la app (otro repo). |

  Alternativa (duda P8): la distribución `Azure.Monitor.OpenTelemetry.AspNetCore` 1.6.0 (MIT, un solo paquete que trae los cuatro primeros, pero con OpenTelemetry 1.15.x y exportador 1.8.3). Se propone la lista explícita porque la distribución registra la instrumentación solo cuando se llama `UseAzureMonitor()`, que exige la cadena de conexión: sin ella las pruebas no verían la misma tubería. No se agrega `Serilog.Sinks.OpenTelemetry` (Serilog reenvía a los proveedores de `ILogger`) ni instrumentación de MongoDB (fuera de alcance).
- **Out:**
  - **Crear el recurso de Application Insights, Log Analytics, el grupo de acciones o las reglas de alerta en Azure**, y guardar la cadena de conexión en Key Vault: lo hace una persona o TT-AZ-02 con Terraform (duda P4). El agente no opera Azure.
  - **La parte de la app** (casos 19 a 22): se implementa y prueba en knock-frontend, con su spec o en esta (duda P2); este repositorio solo garantiza que el cuerpo de error trae `traceId` y `correlationId` (ya existe).
  - Tableros (workbooks) a la medida, telemetría de negocio, Real User Monitoring y trazas del panel Angular o de la landing.
  - Instrumentar el driver de MongoDB, Redis, SignalR o MassTransit (entran con sus tareas; reutilizan esta extensión).
  - Unificar `traceId` y `correlationId` o cambiar quién genera el id (el gateway lo hará, ADR-001 regla 5, con TT-ARQ-02).
  - El gateway YARP y la entrada de Container Apps (la query en sus logs es de TT-ARQ-02, decisión (f) de TT-API-07).
  - Editar `.github/`, `scripts/`, `.agents/`, `constitution.md`, `security/exceptions.yaml`, `docs/openapi.v1.json`, configuración de analizadores y specs ajenas.

## 4. Contratos API y eventos
**Sin endpoints ni eventos nuevos y sin cambios en el OpenAPI.** La respuesta de error ya trae `traceId` y `correlationId` (TT-API-03); no cambian ni su nombre ni su formato.

**Relación entre `traceId` (OpenTelemetry) y `X-Correlation-Id` (TT-API-03):**

| Identificador | Quién lo genera | Dónde se ve | Uso |
|---|---|---|---|
| `X-Correlation-Id` / `correlationId` | El cliente (si es válido) o la API (`CorrelationIdMiddleware`); después, el gateway (ADR-001) | Encabezado de respuesta, cuerpo de error, propiedad `CorrelationId` de cada log (consola y Application Insights) y atributo `knock.correlation_id` del span de la petición | Id de negocio para soporte, estable a través de servicios y reintentos del cliente |
| `traceId` (W3C `00-<trace-id>-<span-id>-01`) | ASP.NET Core / OpenTelemetry (`Activity`) | Cuerpo de error (`traceId`), `operation_Id` en Application Insights, `TraceId` de cada log exportado y `traceparent` saliente | Traza técnica de la petición y sus dependencias |

Ninguno sustituye al otro y ninguno sirve para autorizar. No se agrega encabezado `traceparent` ni `traceresponse` a las respuestas.

**Configuración:** `APPLICATIONINSIGHTS_CONNECTION_STRING` (variable de entorno; en Container Apps, referencia a un secreto de Key Vault). Nunca en `appsettings*.json`, en el repositorio, en los logs ni en el PR. En Development y en las pruebas no se define.

**Alertas propuestas** (las define `docs/OBSERVABILIDAD.md`; **umbrales a decidir por el líder, duda P3**; excluyen `/health*`):

| Alerta | Señal | Umbral propuesto | Ventana / frecuencia | Severidad |
|---|---|---|---|---|
| 5xx | Peticiones con `resultCode` ≥ 500 (`requests/failed` o KQL sobre `requests`) | ≥ 5 en 5 min **y** > 2 % de las peticiones | 5 min, evaluada cada 5 min | 1 (error) |
| Latencia alta | p95 de la duración de las peticiones | > 1 000 ms (5 veces la meta de 150 ms de TT-API-07 más margen por las 2 réplicas) | 10 min, evaluada cada 5 min | 2 (aviso) |

Destino: un grupo de acciones con el correo único del proyecto (constitución §9). El costo de cada regla se confirma con la tarifa citada antes de crearlas (principio 16, TT-AZ-04).

**Eventos:** ninguno.

## 5. Modelo de datos
— Sin persistencia: no hay colecciones, índices ni migración. La telemetría vive en Application Insights (retención por defecto del recurso; la fija TT-AZ-02). El almacenamiento local de reintentos del exportador se apaga para no escribir telemetría en el disco del contenedor (principio 2).

## 6. Seguridad, privacidad y amenazas
Toca `security-core` (datos sensibles en logs y telemetría, secretos, cadena de suministro) y la constitución §6 y §12. STRIDE breve:

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** celulares, tokens (JWT, ID token de Firebase, refresh), CURP o coordenadas en spans, logs o excepciones enviados a Application Insights (fuera de nuestra base, en EE. UU.) | Sin cuerpos ni encabezados; `url.path` = plantilla; query redactada; procesador de redacción sobre atributos, eventos y logs; los logs pasan antes por los filtros de Serilog | TC-TT-OBS-01-08, 09, 10, 12 |
| **Fuga (I):** query de `GET /colonias/en-punto` con `lat`/`lon` (decisión (f) de TT-API-07) o `sig` de una URL SAS en la telemetría de peticiones y dependencias | `url.query` sin valores y `url.full` de las dependencias sin query | TC-TT-OBS-01-08, 11 |
| **Fuga (I):** cadena de conexión de Application Insights o DSN de Sentry en el repositorio o en logs | Solo variable de entorno o Key Vault; prueba que recorre la configuración; gitleaks | TC-TT-OBS-01-13 |
| **Suplantación / alteración (S/T):** el cliente envía `traceparent`, `baggage` o `X-Correlation-Id` para inyectar datos o confundir la investigación | `baggage` nunca se copia a la telemetría; `traceparent` inválido se descarta; el id hostil se reemplaza (TT-API-03). **Riesgo aceptado propuesto:** un `traceparent` válido del cliente se acepta (duda P9) | TC-TT-OBS-01-05 |
| **Denegación (D):** Application Insights caído o lento frena las peticiones; ráfagas que agotan la cuota de 5 GB (costo) | Exportación por lotes en segundo plano; sin spans de `/health`; muestreo configurable; tope diario del recurso (TT-AZ-02) | TC-TT-OBS-01-02, 03 |
| **Denegación (D):** atributos de alta cardinalidad (ruta con valores, correlationId) en métricas | Métricas solo con plantilla, método y código | TC-TT-OBS-01-07 |
| **Repudio (R):** un error de un usuario no se puede rastrear | `correlationId` y `traceId` en el cuerpo de error, en el span y en cada log; alertas de 5xx | TC-TT-OBS-01-04, 06, 15, 16 |
| **Elevación (E):** | No aplica: no hay endpoint, permiso ni recurso nuevo | — |
| **Cadena de suministro:** paquetes nuevos con vulnerabilidades o licencias incompatibles | Lista cerrada en §3 con licencia; los agrega o autoriza el líder; versiones fijas; escáneres | TC-TT-OBS-01-17 |

Riesgo residual: un dato personal escrito como texto libre con un formato no previsto podría pasar el redactor; por eso la primera barrera es no capturar cuerpos, encabezados ni query, y la redacción es la segunda. No aplican: autorización por recurso, biométricos, archivos, IA y SSRF.

## 7. Pantallas
— (en este repositorio). En la app: el mensaje de error con el código de soporte (caso 20), en knock-frontend.

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Redactor de telemetría (caso 12): patrones, claves, textos que no se tocan y entradas largas. `tests/UnitTest/Hosting/`. |
| Integración | Sí | Registro de exportadores, spans, correlación, logs, métricas y llamadas salientes con el exportador en memoria (casos 01 a 04, 06, 07, 11 y 14). `tests/IntegrationTest/Observability/`. Sin Azure; con MongoDB solo donde el endpoint lo necesite. |
| Seguridad negativa | Sí | Privacidad de spans, logs y excepciones, contexto de traza hostil y configuración (casos 05, 08, 09, 10 y 13). `tests/SecurityTest/Observability/`. |
| Arquitectura | Sí, existentes | `PipelineOrderTests` y las de módulos en verde sin cambios (no hay middleware nuevo; la extensión vive en `Hosting/`). |
| Regresión | Sí | TT-API-03 (08 a 12, 15 y 16) y TT-API-07 (08 y 16) sin editar (caso 14). |
| Manual (aceptación de la ficha) | Sí | V1 y V2 en Azure (casos 15 y 16), con capturas. |
| Base de datos, contrato, eventos, E2E, UI, rendimiento | No | Sin persistencia, endpoints ni eventos. Rendimiento: la exportación es asíncrona; si el líder quiere medir el costo por petición, se agrega a k6 (TT-QA-02). |
| Mutación | No (informativa) | El redactor es lógica de seguridad: se pide que Stryker mate los mutantes de sus patrones (informativo, `--break-at 0`). |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Cómo se prueba sin Azure:** (1) el exportador en memoria (`OpenTelemetry.Exporter.InMemory`) se registra en `KnockApiFactory` y captura trazas, métricas y logs exactamente como saldrían hacia Azure Monitor, porque la tubería de instrumentación y redacción es la misma en todos los entornos; (2) el caso 02 usa una cadena de conexión sintética que apunta a `127.0.0.1:9` para probar que el exportador real se registra y no bloquea; (3) los casos 15 y 16 son los únicos que necesitan el recurso y se ejecutan cuando exista (TT-AZ-02 o creado a mano por una persona), sin bloquear el resto.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit (con Architecture), Integration y Security en verde (resultado real), `sdd_lint.py --trace TT-OBS-01`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, gitleaks, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día (con la evaluación de cada paquete) e informe de `story-workflow` en el PR con «Uso de IA» y las capturas de los tableros.

**Decisiones previas:**
1. **No se cambia TT-API-03:** `traceId` y `correlationId` siguen separados (decisión 4 de TT-API-03); esta tarea los **enlaza** en la telemetría. Porqué: ADR-001 regla 5 y no romper el contrato de error.
2. **Sin middleware nuevo.** Porqué: la instrumentación de OpenTelemetry escucha `DiagnosticSource`; así el orden de `docs/MIDDLEWARES.md` y su prueba no cambian.
3. **Misma tubería en todos los entornos; solo cambia el exportador.** Porqué: probar sin Azure lo mismo que corre en Azure (principio 9 «real-first», sin fakes de la instrumentación).

**Límites conocidos:**
- La redacción por patrones no garantiza atrapar todo dato personal en texto libre (ver §6, riesgo residual).
- La verificación en el tablero (casos 15, 16 y 22) es manual; no hay prueba automática contra Application Insights.
- El `X-Correlation-Id` todavía lo genera cada API; hasta TT-ARQ-02 no hay gateway que lo fije para todos los servicios.

**Preguntas abiertas para el líder:**
- **P1 — Código de soporte en la app:** ¿la app muestra el `<trace-id>` (32 hexadecimales del `traceId`, el `operation_Id` de Application Insights) como pide la ficha, o el `correlationId`? Propuesta: el `<trace-id>`, y ambos como etiquetas en Sentry.
- **P2 — Parte de la app:** ¿Sentry y el código de soporte (casos 19 a 22) van en una spec aparte en knock-frontend con la misma clave KNOCK-51 (como indica `DECISION_TECNICA.md` §9 para lo que toca ambos repos) o se quedan en esta spec marcados «(otro repo)»? Propuesta: spec espejo en knock-frontend con estos mismos casos y su PR. ¿Se apagan los encabezados `sentry-trace`/`baggage` de la app hacia la API?
- **P3 — Umbrales de alerta:** ¿se aceptan 5xx ≥ 5 en 5 min y > 2 %, y latencia p95 > 1 000 ms en 10 min (§4)? ¿Destino: solo el correo del proyecto?
- **P4 — Dependencia con TT-AZ-02:** la ficha dice que depende de TT-AZ-02 y el líder dijo que no tiene dependencias. ¿Quién crea el recurso de Application Insights, el grupo de acciones y las alertas: TT-AZ-02 con Terraform, o una persona a mano para cerrar V1 y V2 en este sprint? Mientras tanto, los casos 01 a 14 no dependen de Azure (§8).
- **P5 — Error de prueba para V1/V2:** ¿vale provocar el 5xx con la API local (o desplegada) y MongoDB detenido, sin crear un endpoint de prueba en producción? Propuesta: sí; no se agrega ningún endpoint.
- **P6 — Coordenadas en texto libre:** ¿se acepta redactar cualquier número con 4 o más decimales en rango de lat/lon (puede tapar algún valor técnico), o solo los valores de claves conocidas?
- **P7 — Muestreo y cuota:** ¿100 % de las trazas con el tope diario del recurso, o un porcentaje fijo? ¿Live Metrics apagado?
- **P8 — Paquetes:** ¿lista explícita (exportador + instrumentaciones, propuesta) o la distribución `Azure.Monitor.OpenTelemetry.AspNetCore`? ¿Los agrega el líder o autoriza agregarlos en la rama de la implementación?
- **P9 — `traceparent` del cliente:** ¿se acepta un `traceparent` válido que manda la app (riesgo bajo: solo agrupa spans) o la API siempre inicia una traza nueva hasta que exista el gateway?

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
