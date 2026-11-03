# TT-API-11 — Redactar los logs de petición con query

> **Jira:** KNOCK-186 · **Área:** API · Backend base · **Épica:** EP-13 (por confirmar en Jira) · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S2 · **Puntos:** 1 (por confirmar en Jira) · **Capas:** Back
> **Repositorio:** knock-backend · **Otra parte en:** — · **Ficha:** TT-API-11 (Jira KNOCK-186; no existe `docs/backlog/TT-API-11.md`) · **Rama:** `docs/KNOCK-186-request-log-query-spec` (solo esta spec) → PR a `sprint-02`; la implementación va en su propia rama, también a `sprint-02`, después de la aprobación
> **Depende de:** TT-API-03 (KNOCK-37, Firmada: Serilog y log de petición con plantilla de ruta) y TT-API-07 (KNOCK-141, fusionada: guarda `AspNetCoreLogLevelGuardTests`, decisión (i)). Con TT-OBS-01 (KNOCK-51, #89) comparte el pipeline de logs de Serilog.
> **Fuentes:** ficha de Jira de TT-API-11 («Seguimiento de TT-API-03 y TT-API-07») · constitución 2.2.x (§6 «Seguridad y privacidad») · `security-core` (check 6, datos sensibles en logs) · spec TT-API-07 (§3 «Out», §6 «Fuga (I)», decisión (i), caso 16) · spec TT-API-03 (caso 15) · lectura de `src/Knock.Api/Hosting/LoggingExtensions.cs`, `src/Knock.Api/appsettings.json` y `tests/SecurityTest/Logging/AspNetCoreLogLevelGuardTests.cs`
> **Skills:** foundation-backend, security-core, testing-strategy-backend, story-workflow
> **Estado:** Borrador

## 1. Objetivo
Que la query de una petición (por ejemplo `lat` y `lon` de `GET /api/v1/colonias/en-punto`) **no pueda salir en ningún log, con cualquier nivel configurado**. Hoy lo único que lo impide es que `Microsoft.AspNetCore` esté en `Warning` en `appsettings.json`: con `Information`, el componente de hospedaje de ASP.NET Core registra «Request starting» y «Request finished» con la URL completa. Se cambia una barrera de configuración (frágil: basta con bajar un nivel para depurar) por una de código que no depende del nivel.

**Terminada cuando** (ficha): ningún nivel de log configurado deja salir la query de una petición, y hay una prueba que lo comprueba.

**Qué ya existe y qué es nuevo** (leído en `sprint-02` el 2026-11-03):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| `appsettings.json` | `Serilog:MinimumLevel:Override:Microsoft.AspNetCore` = `Warning`. | No cambia. Deja de ser la única barrera. |
| `LoggingExtensions.AddKnockLogging` | `ReadFrom.Configuration`, `Enrich.FromLogContext()`, `ScopeRequestPathEnricher` (quita el `RequestPath` con valores que agrega el scope de hospedaje) y sink de consola JSON. Sin ningún filtro por categoría. | **Nuevo:** un filtro en código que descarta **todo** evento de la categoría `Microsoft.AspNetCore.Hosting.Diagnostics`, sin importar el nivel ni la configuración. |
| `ConfigureRequestLogging` (TT-API-03) | Una línea por petición con `RequestMethod`, la plantilla de la ruta, `StatusCode` y `Elapsed`; nunca la ruta con valores ni la query; las rutas sin endpoint salen como `(unmatched)`. | No cambia. Cubre lo que «Request starting/finished» aportaba (una línea por petición, con su duración y su código). |
| `AspNetCoreLogLevelGuardTests` (TC-TT-API-07-16) | Falla si el nivel efectivo de `Hosting.Diagnostics` baja de `Warning`. Lee la configuración; no mira lo que se escribe. | **No se edita** y sigue en verde. Pasa a ser un aviso temprano y no la única defensa. |
| `SensitiveLogTests` (TC-TT-API-03-15) | Comprueba que no salen tokens, celulares ni coordenadas con los niveles configurados. | No cambia. La prueba nueva repite la comprobación **con los niveles bajados**. |

## 2. Historia y casos de prueba
**TT-API-11** — Tarea técnica (no es historia de usuario). Los casos 01 a 05 se verifican con pruebas de código; el 06, con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_11_KK`. Las pruebas capturan la consola con `ConsoleCapture` y `KnockApiFactory`, con coordenadas y valores ficticios y reconocibles (canarios) en la query y en la ruta. Cada prueba incluye un control: un evento que **sí** debe salir, para que la captura vacía no pase en vacío.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-11-01 | Dados Production y Development con el nivel de `Microsoft.AspNetCore` bajado a `Information` y a `Debug`, y el nivel por defecto en `Debug`, cuando se envía una petición con `lat` y `lon` canarios en la query (con y sin ruta con valores, por ejemplo `/api/v1/test/at/{lat}/{lon}`), entonces **ninguna línea de log** contiene la query, `lat=` ni `lon=` ni los valores canarios, y la línea de petición de TT-API-03 sí sale (control). | `SecurityTest/Logging/RequestLogQueryTests.QueryNeverReachesTheLogsWhateverTheConfiguredLevel_TC_TT_API_11_01` (Theory por entorno y nivel; usa `ConsoleCapture`) |
| TC-TT-API-11-02 | Dado un registrador de la categoría `Microsoft.AspNetCore.Hosting.Diagnostics` (la que usa ASP.NET Core para «Request starting» y «Request finished»), cuando escribe eventos con un texto canario en todos los niveles (de `Trace` a `Critical`), entonces ninguno aparece en la consola; y un registrador de otra categoría con el mismo texto y niveles sí aparece (control). La categoría hija (`…Hosting.Diagnostics.Algo`) también queda excluida; una categoría con prefijo parecido (`Microsoft.AspNetCore.Hosting.DiagnosticsOtro`) no. | `SecurityTest/Logging/RequestLogQueryTests.HostingDiagnosticsCategoryIsDroppedAtEveryLevel_TC_TT_API_11_02` |
| TC-TT-API-11-03 | Dada una petición con query y valores canarios que termina en 200, 401, 404, 422, 429 y 500, cuando se capturan los logs con los niveles bajados del caso 01, entonces hay exactamente **una** línea de petición por respuesta, con `RequestMethod`, la plantilla de la ruta (o `(unmatched)`), `StatusCode` y `Elapsed`, y ninguna con la query: lo que cubría «Request finished» sigue cubierto. | `SecurityTest/Logging/RequestLogQueryTests.OneRequestLinePerResponseWithoutQuery_TC_TT_API_11_03` (Theory por código) |
| TC-TT-API-11-04 | Dado el filtro nuevo, cuando se ejecutan los logs de servicio y de error de TT-API-03 (un evento del `TestController`, una excepción no controlada, un 429) y los eventos de arranque (`Hosting starting`, `Now listening on`), entonces siguen saliendo con su nivel y sus propiedades (`CorrelationId`, `TraceId`): el filtro descarta solo la categoría de hospedaje de petición. | `SecurityTest/Logging/RequestLogQueryTests.OtherLogsAreNotAffected_TC_TT_API_11_04` |
| TC-TT-API-11-05 | Dada la guarda de TT-API-07 y las pruebas de TT-API-03 y TT-API-07 que dependen de los logs, cuando corre la suite, entonces `AspNetCoreLogLevelGuardTests` (caso TC-TT-API-07-16), `SensitiveLogTests` (caso TC-TT-API-03-15) y `ConsoleJsonLoggingTests` / `CorrelationLoggingTests` siguen en verde **sin editarse**; y quitar el filtro de `LoggingExtensions` (mutación a mano, que se revierte) hace fallar los casos 01 y 02. | (comando) pruebas de regresión sin editar y mutación manual del filtro (quitarlo y ver fallar los casos 01 y 02): salida y mutación en el PR |
| TC-TT-API-11-06 | Dado el trabajo terminado, cuando se compila y se revisa gobierno, entonces `dotnet build Knock.slnx -c Release --no-restore` da 0 errores y 0 warnings, `dotnet format Knock.slnx --verify-no-changes` no reporta cambios, `python scripts/sdd_lint.py --trace TT-API-11`, `python scripts/codemap.py --check`, `python scripts/ci/check_exceptions.py` y `python scripts/sync_agents.py --check` terminan en OK, no se agregó ningún paquete (`git diff` de `*.csproj` y `packages.lock.json` vacío) y no hay secretos (`gitleaks`). | (comando) salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `Hosting/LoggingExtensions.cs`: un filtro de Serilog en código (`Filter.ByExcluding` con `Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics")`) que descarta todo evento de esa categoría y sus hijas, aplicado **antes** de los sinks (consola y, cuando exista, el sink de OpenTelemetry de TT-OBS-01). Es código, no configuración: ninguna variable de entorno ni `appsettings` lo reactiva.
  - Pruebas de los casos 01 a 05 en `tests/SecurityTest/Logging/`.
  - Una nota en el comentario de `AspNetCoreLogLevelGuardTests` (solo el texto de documentación, sin tocar sus aserciones) o, si prefiere el líder, en `docs/MIDDLEWARES.md`, diciendo que la guarda ya no es la única barrera. Si se prefiere no editar el archivo de la guarda, la nota va solo en `Memoria.md` (duda 3).
  - `Memoria.md`.
- **Out:**
  - Cambiar los niveles de `appsettings*.json` (siguen en `Warning`) o quitar la guarda de TT-API-07: se conserva como aviso temprano.
  - Los logs de otras categorías que podrían llevar una URL con query y que **no** son de petición entrante, como los del cliente HTTP saliente (`System.Net.Http.HttpClient.*`: URL de Azure Blob con `sig`, Firebase) o los del servidor Kestrel con solicitudes mal formadas. Con los niveles actuales (`Default` en `Information`) el cliente HTTP puede registrar la URL saliente; ver la duda 2. No se resuelve aquí sin decisión del líder.
  - La telemetría de Application Insights y su redacción (TT-OBS-01, #89): este filtro corre antes y la complementa.
  - Redactar la query en lugar de excluir los eventos (alternativa descartada, duda 1).
  - Cualquier cambio en el contrato HTTP, el modelo de datos o los eventos.
- **Relación con otras tareas:** TT-API-03 (el log de petición propio sigue siendo la única línea por petición), TT-API-07 (la guarda), TT-OBS-01 (`OpenTelemetryLogSink` reenvía lo que Serilog ya filtró; el filtro nuevo debe quedar antes de ese sink: caso 01 lo vigila al integrarse).

## 4. Contratos API y eventos
Sin endpoints, eventos ni cambios de OpenAPI: no cambia `docs/openapi.v1.json`, `docs/endpoints.md` ni `docs/events.md`. Cambia lo que se escribe en los logs: desaparecen los eventos «Request starting» y «Request finished» de ASP.NET Core (el log de petición de TT-API-03 los sustituye, sin la query).

## 5. Modelo de datos
—

## 6. Seguridad, privacidad y amenazas
STRIDE breve. El riesgo es de fuga de información (I): la query lleva coordenadas (dato de ubicación) y, en el futuro, otros parámetros.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** alguien baja `Microsoft.AspNetCore` a `Information` (para depurar o por error, en `appsettings`, en una variable de entorno o en una configuración de despliegue) y «Request starting/finished» registran la URL con la query. | Filtro en código que descarta la categoría sin importar el nivel; la configuración no puede reactivarlo. | TC-TT-API-11-01 · TC-TT-API-11-02 |
| **Fuga (I):** el nivel por defecto baja a `Debug` o `Trace` y otras categorías de hospedaje muestran la URL. | La prueba del caso 01 corre con `Default` en `Debug` y falla si algún evento lleva los canarios; si aparece otra categoría, la prueba la encuentra (duda 2). | TC-TT-API-11-01 |
| **Alteración (T) / repudio (R):** al quitar los eventos de hospedaje se pierde la traza de cada petición. | El log de petición de TT-API-03 queda como línea única por petición, con código, método, ruta de plantilla y duración, también para 401, 404, 422, 429 y 500. | TC-TT-API-11-03 |
| **Elevación (E) / regresión:** el filtro se pasa de la raya y esconde errores o logs de servicio. | Descarta solo esa categoría (y sus hijas, no prefijos parecidos); los logs de servicio, de error y de arranque siguen saliendo. | TC-TT-API-11-02 · TC-TT-API-11-04 |
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
| Integración / seguridad negativa | Sí | Casos 01 a 04 en `tests/SecurityTest/Logging/` con `KnockApiFactory` y `ConsoleCapture`. Sin Testcontainers: no hay base de datos en estos casos. |
| Regresión | Sí | TC-TT-API-03-15, TC-TT-API-07-16 y las pruebas de logs de TT-API-03 sin editar (caso 05). |
| Contrato, base de datos, UI, rendimiento | No | Sin endpoints ni persistencia. |
| Mutación | No | Sin meta para `Hosting/`, como en TT-API-02, TT-API-03 y TT-API-06; el caso 05 hace una mutación manual con evidencia. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit (con Architecture), Integration, Security y Database en verde con resultado real, `sdd_lint.py --trace TT-API-11`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Límites conocidos:**
- Si en algún momento se necesita el detalle de «Request starting/finished» para depurar un problema de red, no se podrá reactivar con configuración: hay que usar el log de petición propio o el rastro de OpenTelemetry (TT-OBS-01), que usa la plantilla de la ruta.
- El filtro es por categoría. Un paquete nuevo que registre la URL con otra categoría no queda cubierto hasta que la prueba del caso 01 lo encuentre.

**Dudas abiertas para el líder** (con recomendación; no se resuelven inventando):
1. **Excluir o redactar.** *Recomendación:* excluir toda la categoría `Microsoft.AspNetCore.Hosting.Diagnostics`: el log de petición de TT-API-03 ya registra una línea por petición con ruta de plantilla, código y duración, así que los eventos de ASP.NET Core son redundantes. Alternativa: redactar las propiedades `Path` y `QueryString` con un enriquecedor y conservar los eventos (más código y se pierde la garantía de «ninguna query»). Dato por confirmar al implementar: los nombres exactos de las propiedades de esos eventos en .NET 10 (la exclusión no depende de ellos).
2. **Otras categorías con URL con query.** Con `Default` en `Information`, el cliente HTTP saliente (`System.Net.Http.HttpClient.*`) registra la URL completa de las peticiones que hace la API (Azure Blob con `sig`, Firebase), y Kestrel puede registrar solicitudes mal formadas. No son «logs de petición entrante» y la ficha no los nombra. *Recomendación:* que la prueba del caso 01 con `Default` en `Debug` los descubra al implementar y que el líder decida si se excluyen aquí o en otra tarea (hoy `Media` solo firma SAS y no hace llamadas salientes registradas). No se agregan casos sin tu decisión.
3. **Dónde se anota que la guarda ya no es la única barrera.** *Recomendación:* un comentario en `AspNetCoreLogLevelGuardTests` (solo documentación; las aserciones no cambian) y `Memoria.md`. Si prefieres no tocar ese archivo, solo `Memoria.md`.
4. **Épica y puntos en Jira.** La ficha pegada no trae épica ni puntos; la spec asume EP-13 y 1 punto. *Recomendación:* confirmarlos al aprobar.

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
