# TT-API-11 — Redactar los logs de petición con query

> **Jira:** KNOCK-186 · **Área:** API · Backend base · **Épica:** EP-13 (por confirmar en Jira) · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S2 · **Puntos:** 1 (por confirmar en Jira) · **Capas:** Back
> **Repositorio:** knock-backend · **Otra parte en:** — · **Ficha:** TT-API-11 (Jira KNOCK-186; no existe `docs/backlog/TT-API-11.md`) · **Rama:** `docs/KNOCK-186-request-log-query-spec` (solo esta spec) → PR a `sprint-02`; la implementación va en su propia rama, también a `sprint-02`, después de la aprobación
> **Depende de:** TT-API-03 (KNOCK-37, Firmada: Serilog y log de petición con plantilla de ruta) y TT-API-07 (KNOCK-141, fusionada: guarda `AspNetCoreLogLevelGuardTests`, decisión (i)). Con TT-OBS-01 (KNOCK-51, #89) comparte el pipeline de logs de Serilog.
> **Fuentes:** ficha de Jira de TT-API-11 («Seguimiento de TT-API-03 y TT-API-07») · constitución 2.2.x (§6 «Seguridad y privacidad») · `security-core` (check 6, datos sensibles en logs) · spec TT-API-07 (§3 «Out», §6 «Fuga (I)», decisión (i), caso 16) · spec TT-API-03 (caso 15) · lectura de `src/Knock.Api/Hosting/LoggingExtensions.cs`, `src/Knock.Api/appsettings.json` y `tests/SecurityTest/Logging/AspNetCoreLogLevelGuardTests.cs` · mediciones del 2026-11-03 con el nivel más detallado (ver §1)
> **Skills:** foundation-backend, security-core, testing-strategy-backend, story-workflow
> **Estado:** Borrador

## 1. Objetivo
Que la query de una petición (por ejemplo `lat` y `lon` de `GET /api/v1/colonias/en-punto`) **no pueda salir en ningún log, con cualquier nivel configurado**. Hoy lo único que lo impide es que `Microsoft.AspNetCore` esté en `Warning` en `appsettings.json`: con `Information`, el componente de hospedaje de ASP.NET Core registra «Request starting» y «Request finished» con la URL completa. Se cambia una barrera de configuración (frágil: basta con bajar un nivel para depurar) por una de código que no depende del nivel, y se agregan pruebas que vigilan las otras fuentes posibles de URLs con query: el cliente HTTP saliente y Kestrel (decisión del líder, 2026-11-03).

**Terminada cuando** (ficha): ningún nivel de log configurado deja salir la query de una petición, y hay una prueba que lo comprueba.

**Qué se midió el 2026-11-03** (API en `sprint-02`, `Serilog:MinimumLevel:Default` y `Microsoft.AspNetCore` en `Verbose`, valores canario en la query; con `TestServer` y con Kestrel real en un puerto de loopback):

| Categoría (nombre exacto) | Qué registra | ¿Lleva la query? |
|---|---|---|
| `Microsoft.AspNetCore.Hosting.Diagnostics` | «Request starting» y «Request finished» (nivel `Information`) | **Sí**, completa: `…/api/v1/test/ping?lat=…&lon=…`, tanto con `TestServer` como con Kestrel real. Es la fuga. |
| `System.Net.Http.HttpClient.<nombre>.LogicalHandler` y `.ClientHandler` (`<nombre>` es el del cliente con nombre o el tipo del cliente con tipo) | «Start processing / Sending HTTP request», «Received HTTP response headers», «End processing» y, en `Verbose`, los encabezados | **No**: .NET 10 enmascara la query (`http://host/blob?*`) y los valores de los encabezados (`Server: *`). La fuga no existe hoy; la protege el comportamiento por defecto del framework. |
| `Microsoft.AspNetCore.Server.Kestrel.BadRequests`, `…Kestrel.Connections` y `…Kestrel.Transport.Sockets` | Solicitudes mal formadas, conexiones y transporte (Kestrel real, con solicitudes válidas y tres tipos de solicitud mal formada con query canario) | **No** en las entradas probadas: las líneas de `BadRequests` no traen la línea de la petición. `Microsoft.AspNetCore.Server.Kestrel` (sin sufijo) registra el arranque y los errores de enlace de puertos, que **no** deben perderse. |

**Qué ya existe y qué es nuevo:**

| Pieza | Hoy | En esta tarea |
|---|---|---|
| `appsettings.json` | `Serilog:MinimumLevel:Override:Microsoft.AspNetCore` = `Warning`. | No cambia. Deja de ser la única barrera. |
| `LoggingExtensions.AddKnockLogging` | `ReadFrom.Configuration`, `Enrich.FromLogContext()`, `ScopeRequestPathEnricher` (quita el `RequestPath` con valores que agrega el scope de hospedaje) y sink de consola JSON. Sin ningún filtro por categoría. | **Nuevo:** un filtro en código que descarta **todo** evento de `Microsoft.AspNetCore.Hosting.Diagnostics` (y de sus categorías hijas), sin importar el nivel ni la configuración. |
| `ConfigureRequestLogging` (TT-API-03) | Una línea por petición con `RequestMethod`, la plantilla de la ruta, `StatusCode` y `Elapsed`; nunca la ruta con valores ni la query; las rutas sin endpoint salen como `(unmatched)`. | No cambia. Cubre lo que «Request starting/finished» aportaba (una línea por petición, con su duración y su código). |
| `AspNetCoreLogLevelGuardTests` (TC-TT-API-07-16) | Falla si el nivel efectivo de `Hosting.Diagnostics` baja de `Warning`. Lee la configuración; no mira lo que se escribe. | **No se edita** y sigue en verde. Pasa a ser un aviso temprano y no la única defensa. |
| `SensitiveLogTests` (TC-TT-API-03-15) | Comprueba que no salen tokens, celulares ni coordenadas con los niveles configurados. | No cambia. Las pruebas nuevas repiten la comprobación **con los niveles bajados**. |

## 2. Historia y casos de prueba
**TT-API-11** — Tarea técnica (no es historia de usuario). Los casos 01 a 04, 07 y 08 se verifican con pruebas de código; el 05 y el 06, con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_11_KK`. Las pruebas capturan la consola con `ConsoleCapture` y `KnockApiFactory`, con coordenadas y valores ficticios y reconocibles (canarios) en la query y en la ruta. Cada prueba incluye un control: un evento que **sí** debe salir, para que la captura vacía no pase en vacío. (Los casos 07 y 08 se agregaron al resolver las dudas 1 y 2; el orden de los números no refleja el orden de ejecución.)

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-11-01 | Dados Production y Development con el nivel de `Microsoft.AspNetCore` bajado a `Information` y a `Debug`, y el nivel por defecto en `Debug`, cuando se envía una petición con `lat` y `lon` canarios en la query (con y sin ruta con valores, por ejemplo `/api/v1/test/at/{lat}/{lon}`), entonces **ninguna línea de log** contiene la query, `lat=` ni `lon=` ni los valores canarios, y la línea de petición de TT-API-03 sí sale (control). | `SecurityTest/Logging/RequestLogQueryTests.QueryNeverReachesTheLogsWhateverTheConfiguredLevel_TC_TT_API_11_01` (Theory por entorno y nivel; usa `ConsoleCapture`) |
| TC-TT-API-11-02 | Dado un registrador de la categoría `Microsoft.AspNetCore.Hosting.Diagnostics`, cuando escribe eventos con un texto canario en todos los niveles (de `Verbose` a `Fatal`), entonces ninguno aparece en la consola; y los registradores de otras categorías con el mismo texto y niveles sí aparecen (control): `Microsoft.AspNetCore.Server.Kestrel`, `Microsoft.AspNetCore.Server.Kestrel.BadRequests`, `Microsoft.AspNetCore.Server.Kestrel.Connections`, `System.Net.Http.HttpClient.probe.ClientHandler` y `Microsoft.AspNetCore.Hosting.DiagnosticsOtro` (prefijo parecido). Una categoría hija (`…Hosting.Diagnostics.Algo`) también queda excluida. | `SecurityTest/Logging/RequestLogQueryTests.ExcludedCategoriesAreDroppedAtEveryLevelAndOthersSurvive_TC_TT_API_11_02` |
| TC-TT-API-11-03 | Dada una petición con query y valores canarios que termina en 200, 401, 404, 422, 429 y 500, cuando se capturan los logs con los niveles bajados del caso 01, entonces hay exactamente **una** línea de petición por respuesta, con `RequestMethod`, la plantilla de la ruta (o `(unmatched)`), `StatusCode` y `Elapsed`, y ninguna con la query: lo que cubría «Request finished» sigue cubierto. | `SecurityTest/Logging/RequestLogQueryTests.OneRequestLinePerResponseWithoutQuery_TC_TT_API_11_03` (Theory por código) |
| TC-TT-API-11-04 | Dado el filtro nuevo, cuando se ejecutan los logs de servicio y de error de TT-API-03 (un evento del `TestController`, una excepción no controlada, un 429) y los eventos de arranque (`Hosting starting`, `Now listening on`), entonces siguen saliendo con su nivel y sus propiedades (`CorrelationId`, `TraceId`): el filtro descarta solo las categorías excluidas. | `SecurityTest/Logging/RequestLogQueryTests.OtherLogsAreNotAffected_TC_TT_API_11_04` |
| TC-TT-API-11-05 | Dada la guarda de TT-API-07 y las pruebas de TT-API-03 y TT-API-07 que dependen de los logs, cuando corre la suite, entonces `AspNetCoreLogLevelGuardTests` (caso TC-TT-API-07-16), `SensitiveLogTests` (caso TC-TT-API-03-15) y `ConsoleJsonLoggingTests` / `CorrelationLoggingTests` siguen en verde **sin editarse**; y quitar el filtro de `LoggingExtensions` (mutación a mano, que se revierte) hace fallar los casos 01 y 02. | (comando) pruebas de regresión sin editar y mutación manual del filtro (quitarlo y ver fallar los casos 01 y 02): salida y mutación en el PR |
| TC-TT-API-11-06 | Dado el trabajo terminado, cuando se compila y se revisa gobierno, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings, `dotnet format Knock.slnx --verify-no-changes` no reporta cambios, `python scripts/sdd_lint.py --trace TT-API-11`, `python scripts/codemap.py --check`, `python scripts/ci/check_exceptions.py` y `python scripts/sync_agents.py --check` terminan en OK, no se agregó ningún paquete (`git diff` de `*.csproj` y `packages.lock.json` vacío) y no hay secretos (`gitleaks`). | (comando) salida en el PR |
| TC-TT-API-11-07 | Dado un cliente HTTP **con nombre** y uno **con tipo**, con `Serilog:MinimumLevel:Default` y `System.Net.Http` en `Verbose`, cuando llaman a un servidor local de prueba (loopback) con una URL con `sig` y `token` canarios en la query, entonces ninguna línea de log contiene la query ni los valores canarios (el framework escribe `?*`), y las líneas «Start processing HTTP request» y «Sending HTTP request» sí salen (control). Si una actualización del framework o una configuración (por ejemplo un registro propio del cliente) deja pasar la URL con query, la prueba falla y la categoría `System.Net.Http.HttpClient` se agrega al filtro. | `SecurityTest/Logging/OutgoingAndKestrelLogQueryTests.OutgoingHttpClientLogsNeverCarryTheQuery_TC_TT_API_11_07` (servidor local con `HttpListener`) |
| TC-TT-API-11-08 | Dado un host con **Kestrel real** en un puerto libre de loopback (el `TestServer` no pasa por Kestrel) con `Serilog:MinimumLevel:Default` y `Microsoft.AspNetCore` en `Verbose`, cuando recibe una petición válida y tres solicitudes mal formadas (encabezado inválido, versión HTTP no soportada y método con carácter de control) con `lat` y `lon` canarios en la query, entonces ninguna línea de log de ninguna categoría de Kestrel o de hospedaje contiene la query ni los canarios, las líneas de `Microsoft.AspNetCore.Server.Kestrel.BadRequests` (si salen) tampoco los traen, y las líneas de arranque de Kestrel sí salen (control). | `SecurityTest/Logging/OutgoingAndKestrelLogQueryTests.RealKestrelNeverLogsTheQueryEvenForBadRequests_TC_TT_API_11_08` (sockets en loopback) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `Hosting/LoggingExtensions.cs`: un filtro de Serilog en código (`Filter.ByExcluding` con `Matching.FromSource`) que descarta todo evento de la categoría `Microsoft.AspNetCore.Hosting.Diagnostics` y de sus hijas, aplicado **antes** de los sinks (consola y, cuando exista, el sink de OpenTelemetry de TT-OBS-01). Es código, no configuración: ninguna variable de entorno ni `appsettings` lo reactiva. La lista vive en una sola constante para que agregar una categoría (casos 07 y 08) sea una línea.
  - Pruebas de los casos 01 a 04, 07 y 08 en `tests/SecurityTest/Logging/`.
  - Una nota en el comentario de `AspNetCoreLogLevelGuardTests` (solo el texto de documentación, sin tocar sus aserciones) o, si prefiere el líder, solo en `Memoria.md` (duda 3).
  - `Memoria.md`.
- **Out:**
  - Cambiar los niveles de `appsettings*.json` (siguen en `Warning`) o quitar la guarda de TT-API-07: se conserva como aviso temprano.
  - Excluir `System.Net.Http.HttpClient` o `Microsoft.AspNetCore.Server.Kestrel` enteras: las mediciones del §1 no muestran fuga y ocultarlas quitaría información útil (errores de red del cliente, errores de enlace de puertos del servidor). Se vigilan con los casos 07 y 08, y se agregan al filtro si una prueba falla (duda 5).
  - La telemetría de Application Insights y su redacción (TT-OBS-01, #89): este filtro corre antes y la complementa.
  - Redactar la query en lugar de excluir los eventos (alternativa descartada, duda 1).
  - Cualquier cambio en el contrato HTTP, el modelo de datos o los eventos.
- **Relación con otras tareas:** TT-API-03 (el log de petición propio sigue siendo la única línea por petición), TT-API-07 (la guarda), TT-OBS-01 (`OpenTelemetryLogSink` reenvía lo que Serilog ya filtró; el filtro nuevo debe quedar antes de ese sink: el caso 01 lo vigila al integrarse).

## 4. Contratos API y eventos
Sin endpoints, eventos ni cambios de OpenAPI: no cambia `docs/openapi.v1.json`, `docs/endpoints.md` ni `docs/events.md`. Cambia lo que se escribe en los logs: desaparecen los eventos «Request starting» y «Request finished» de ASP.NET Core y los de solicitudes mal formadas de Kestrel (el log de petición de TT-API-03 los sustituye, sin la query).

## 5. Modelo de datos
—

## 6. Seguridad, privacidad y amenazas
STRIDE breve. El riesgo es de fuga de información (I): la query lleva coordenadas (dato de ubicación) y, en el futuro, otros parámetros.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** alguien baja `Microsoft.AspNetCore` a `Information` (para depurar o por error, en `appsettings`, en una variable de entorno o en una configuración de despliegue) y «Request starting/finished» registran la URL con la query. | Filtro en código que descarta la categoría sin importar el nivel; la configuración no puede reactivarlo. | TC-TT-API-11-01 · TC-TT-API-11-02 |
| **Fuga (I):** el nivel por defecto baja a `Debug` o `Verbose` y otras categorías muestran la URL (cliente HTTP saliente con la firma `sig` de Azure Blob, Kestrel con solicitudes mal formadas). | Las pruebas de los casos 07 y 08 corren con todo en `Verbose` y fallan si algún evento lleva los canarios; `BadRequests` queda vigilada y no excluida (el líder decidió que solo `Hosting.Diagnostics` lleva la query). Hoy el framework enmascara la query del cliente HTTP (`?*`) y Kestrel no registra la línea de la petición. | TC-TT-API-11-07 · TC-TT-API-11-08 |
| **Alteración (T) / repudio (R):** al quitar los eventos de hospedaje se pierde la traza de cada petición. | El log de petición de TT-API-03 queda como línea única por petición, con código, método, ruta de plantilla y duración, también para 401, 404, 422, 429 y 500. | TC-TT-API-11-03 |
| **Elevación (E) / regresión:** el filtro se pasa de la raya y esconde errores, logs de servicio o los errores de arranque de Kestrel. | Descarta solo esas categorías (y sus hijas, no prefijos parecidos); los logs de servicio, de error, de arranque y `Microsoft.AspNetCore.Server.Kestrel` siguen saliendo. | TC-TT-API-11-02 · TC-TT-API-11-04 |
| **Regresión silenciosa:** un cambio futuro quita el filtro y nadie lo nota porque la guarda de nivel sigue en verde. | Las pruebas miran lo que se **escribe**, no la configuración; la mutación manual del caso 05 demuestra que fallan. | TC-TT-API-11-05 |
| **Suplantación (S) / denegación (D):** no aplican: sin endpoints ni identidad nuevos. | — | — |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | No | La lógica es una línea de configuración de Serilog; se prueba mejor de punta a punta. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests` y `ModuleSurfaceTests` siguen en verde: todo vive en `Hosting/`. |
| Integración / seguridad negativa | Sí | Casos 01 a 04, 07 y 08 en `tests/SecurityTest/Logging/` con `KnockApiFactory`, `ConsoleCapture`, un servidor local con `HttpListener` y un host propio con Kestrel real. Sin Testcontainers: no hay base de datos en estos casos. |
| Regresión | Sí | TC-TT-API-03-15, TC-TT-API-07-16 y las pruebas de logs de TT-API-03 sin editar (caso 05). |
| Contrato, base de datos, UI, rendimiento | No | Sin endpoints ni persistencia. |
| Mutación | No | Sin meta para `Hosting/`, como en TT-API-02, TT-API-03 y TT-API-06; el caso 05 hace una mutación manual con evidencia. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit (con Architecture), Integration, Security y Database en verde con resultado real, `sdd_lint.py --trace TT-API-11`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba (solo las mediciones del §1).

**Límites conocidos:**
- Si en algún momento se necesita el detalle de «Request starting/finished» para depurar un problema de red, no se podrá reactivar con configuración: hay que usar el log de petición propio o el rastro de OpenTelemetry (TT-OBS-01), que usa la plantilla de la ruta.
- El filtro es por categoría. Un paquete nuevo que registre la URL con otra categoría no queda cubierto hasta que una prueba lo encuentre; los casos 07 y 08 cubren las dos fuentes conocidas.
- El SDK de Azure Storage (`Azure.Storage.Blobs`) registra por `EventSource`, no por `ILogger`, y esa salida no se activa en la API; si algún día se activa un `AzureEventSourceListener`, hay que revisar la URL con `sig`.

**Dudas del líder:**
1. **Excluir o redactar.** **Resuelto (líder, 2026-11-03): excluir** de Serilog la categoría `Microsoft.AspNetCore.Hosting.Diagnostics`; no se redactan sus propiedades. *Pregunta original:* excluir toda la categoría (recomendado) o redactar `Path` y `QueryString` con un enriquecedor.
2. **Otras categorías con URL con query.** **Resuelto (líder, 2026-11-03): entran el cliente HTTP saliente y Kestrel.** Verificados los nombres exactos (§1): `System.Net.Http.HttpClient.<nombre>.LogicalHandler` y `.ClientHandler`, y `Microsoft.AspNetCore.Server.Kestrel.BadRequests`, `.Connections` y `.Transport.Sockets`. La medición muestra que **hoy no filtran la query**, así que entran como pruebas (casos 07 y 08) y **no** como exclusiones en el filtro; ver la duda 5. *Pregunta original:* si entran el cliente HTTP saliente (con la `sig` de Azure Blob) y Kestrel.
3. **Dónde se anota que la guarda ya no es la única barrera.** *Recomendación:* un comentario en `AspNetCoreLogLevelGuardTests` (solo documentación; las aserciones no cambian) y `Memoria.md`. Si prefieres no tocar ese archivo, solo `Memoria.md`.
4. **Épica y puntos en Jira.** La ficha pegada no trae épica ni puntos; la spec asume EP-13 y 1 punto. Con los dos casos nuevos (07 y 08, uno con Kestrel real) conviene confirmar si siguen siendo 1 punto. *Recomendación:* confirmarlos al aprobar.
5. **¿Excluir también el cliente HTTP y Kestrel enteros?** **Resuelto (líder, 2026-11-03): no; solo `Hosting.Diagnostics` lleva la query y es lo único que se excluye.** Esas categorías quedan vigiladas por los casos 07 y 08 (incluida `Kestrel.BadRequests`, que ya no va en el filtro). *Pregunta original (a raíz de la resolución de la duda 2):* La medición del 2026-11-03 con `Verbose` no encontró la query en esas categorías: el framework escribe `?*` en el cliente HTTP y Kestrel no registra la línea de la petición. Excluirlas enteras ocultaría errores de red del cliente (fallas al llegar a Azure Blob o a Firebase) y los errores de enlace de puertos de Kestrel. *Recomendación (adoptada):* dejar solo `Hosting.Diagnostics` en el filtro y vigilar el resto con los casos 07 y 08, que fallan si una actualización cambia el comportamiento; si alguna vez fallan, la categoría se agrega a la constante del filtro.

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
