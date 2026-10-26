# TT-API-07 — Endpoint de colonia por punto: `GET /api/v1/colonias/en-punto` (lat, lon), sobre `ICatalogApi`

> **Jira:** KNOCK-141 · **Área:** API · **Servicio:** Mercado (`Knock.Market.Api`, ADR-001) · **Módulo:** Catalog · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S2 · **Puntos:** 2 · **Prioridad:** Técnica
> **Repositorio:** knock-backend · **Otra parte en:** knock-frontend (pantalla «¿Dónde buscas expertos?», HU-009) · **Rama:** `docs/KNOCK-141-colonia-by-point-spec` (spec) → PR a `sprint-01`; la implementación va en su propia rama tras la aprobación
> **Ficha:** no existe `docs/backlog/TT-API-07.md` (la tarea aparece en `docs/PLAN_ADAPTACION.md` §E: «Colonia por punto con consulta por polígono de MongoDB»).
> **Depende de:** TT-BD-06 (KNOCK-156, consulta interna `{ id, nombre, cp, municipio }`, desempate y conexión sobre MongoDB; spec en #53, Borrador) · TT-API-03 (KNOCK-37, 422 con FluentValidation, ProblemDetails, `X-Correlation-Id`, limitador por IP y logs sin coordenadas; en `sprint-01`) · TT-API-02 (KNOCK-21, sesión JWT) · TT-BD-05 (KNOCK-147, spike, #48 fusionado).
> **La consume:** HU-009 (KNOCK-47, #40): TC-HU-009-01 solo usa el resultado de este endpoint.
> **Fuentes:** ADR-001 (reglas 1, 2, 3 y 5) · ADR-002 · ADR-006 · constitución 2.2.0 (principios 3, 4, 9 y 10; §4 pipeline; §5; §6 «ubicación del dispositivo convertida en colonia y descartada»; §12) · spec TT-API-03 (casos 01, 02, 04, 07, 12 y 15) · spec TT-BD-06 (§4, §5, casos 04 a 09, 12, 17, 19 y 20) · respuestas del líder en #54 (2026-10-24) · spec HU-009 (§4 y decisión (a)) · revisión de Guillermo en #54 (probada con la API y con un proyecto mínimo de MVC) · `src/Knock.Api/Modules/Catalog/` · `docs/rate-limit-matrix.md`
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, api-contract-openapi, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Exponer por HTTP la consulta «colonia de un punto» del catálogo propio: la app manda la ubicación del dispositivo una sola vez y recibe la colonia que le corresponde, o el aviso de que está fuera de cobertura. Las coordenadas **se convierten en colonia y se descartan** (constitución §6): no se guardan, no se registran y no se cachean.

**Terminada cuando:** `GET /api/v1/colonias/en-punto?lat=…&lon=…` responde la colonia (o «fuera de cobertura») con el contrato de §4 desde MongoDB (contenedor de pruebas), rechaza con 422 toda entrada fuera de rango, mal formada o repetida, no deja la coordenada en logs, caché ni respuestas, una prueba falla si la configuración de logs deja de proteger la query, y su p95 local queda dentro de la meta de §8.

**Qué ya existe y qué es nuevo** (leído en `src/Knock.Api/` y con `python scripts/codemap.py --query "neighborhood point colonia coordinates"`):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| Consulta | `ICatalogApi.FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken) : Task<string?>` (solo el **nombre**), implementada hoy con PostGIS en `Services/CatalogApi`. TT-BD-06 (#53) la reimplementa en MongoDB, conserva sus tres firmas y agrega el miembro `NeighborhoodExistsAsync(coloniaId, cp, ct)` para Identidad (TC-TT-BD-06-17 y 20), y agrega una **consulta interna** del módulo Catalog que devuelve `{ id, nombre, cp, municipio }` con el mismo desempate; `ICatalogApi` delega en ella (TC-TT-BD-06-19). | **Se reutiliza la consulta interna de TT-BD-06** (el controller vive en el mismo módulo Catalog). El endpoint no reimplementa la consulta ni el desempate, y no cambia `ICatalogApi`. |
| Validación de coordenadas | `Domain/CatalogCoordinates.ToPoint` valida rangos y lanza `ArgumentOutOfRangeException` sin el valor (TC-TT-BD-06-07). | Se conserva como red de seguridad. La validación de frontera es nueva: un validador de FluentValidation que responde 422 antes de llamar al catálogo. |
| Validación a 422 | `Hosting/ValidationExtensions.ValidationFilter` (TT-API-03): 422 ProblemDetails con `errors`, sin repetir el valor; las claves de `errors` son el nombre de la propiedad del DTO en camelCase. | **Se reutiliza sin cambios.** El DTO usa propiedades `Lat` y `Lon` para que los errores salgan como `errors.lat` y `errors.lon`. |
| Errores, correlación y límite | ProblemDetails, `X-Correlation-Id`, `DomainExceptionHandler`, limitador global por IP (`docs/rate-limit-matrix.md`). | **Se reutiliza sin cambios.** La ruta queda bajo la política global (no está exenta). |
| Logs sin coordenadas | `LoggingExtensions`: el log de petición registra la plantilla de la ruta sin query; `ScopeRequestPathEnricher` quita el `RequestPath` con valores (TC-TT-API-03-15). Guillermo comprobó en #54 que esa garantía depende del override `Microsoft.AspNetCore: Warning` de `appsettings.json`: con `Information`, «Request starting» y «Request finished» llevan la query completa. | **Se reutiliza** y se agrega una prueba que falla si alguien baja ese override (caso 16). La defensa en profundidad (excluir o redactar `Microsoft.AspNetCore.Hosting.Diagnostics` sin importar el nivel) es una tarea aparte de seguimiento de TT-API-03 (decisión (i)). |
| Endpoint | No existe. Catalog no tiene controllers. | **Nuevo:** controller delgado `Modules/Catalog/Controllers/V1/` con `[ApiVersion("1.0")]`. |
| OpenAPI | `/openapi/v1.json` solo en Development (`OpenApiExtensions`, TC-TT-API-01-11). No existen `tests/ContractTest`, `scripts/export_openapi.py` ni `docs/openapi.v1.json`. | La operación se documenta en el OpenAPI generado, sin transformador, con `lat` y `lon` como `string` con el patrón de formato fijo, y se prueba leyendo `/openapi/v1.json` (caso 13). El proyecto de contrato y el exportador quedan fuera de esta tarea (§3). |

## 2. Historia y casos de prueba
**TT-API-07** — Tarea técnica. Los casos 01 a 16 se verifican con pruebas de código; el 17 y el 18 con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_07_KK`. Todas las coordenadas son **sintéticas**: puntos documentados del seed de TT-BD-06 dentro de Tula de Allende y un punto de Pachuca sintético fuera de cobertura; nunca ubicaciones de personas. Las pruebas unitarias usan un doble del catálogo; las de integración, Testcontainers con MongoDB (imagen según la duda (a) de TT-BD-06) y la semilla de TT-BD-06. El token de sesión de las pruebas es ficticio, emitido con el emisor de TT-API-02 o un equivalente sintético. Los logs se capturan **con los niveles configurados** en `appsettings*.json`, no con niveles bajados para la prueba.

**Lo que ya está cubierto y no se duplica** (se referencia y debe seguir en verde): el desempate en bordes y traslapes (TC-TT-BD-06-05), el respaldo por centro ≤ 2000 m (TC-TT-BD-06-06), el orden `[lon, lat]` (TC-TT-BD-06-07), las firmas de `ICatalogApi` (TC-TT-BD-06-17), la consulta interna y la delegación de `ICatalogApi` (TC-TT-BD-06-19), el p95 de la consulta en el servidor (TC-TT-BD-06-12), la forma del 422 y del 429 (TC-TT-API-03-01, 02, 04 y 07) y el `X-Correlation-Id` en toda respuesta (TC-TT-API-03-12). **TC-HU-009-01** solo consume el resultado de este endpoint (doble de su respuesta) y no prueba la consulta.

Los casos 04, 10, 13 y 15 aplican las decisiones (b), (c) y (j) de §8, resueltas por el líder en #54 (2026-10-26).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-07-01 | Dados `lat` y `lon` válidos en el formato de §4, cada uno con **exactamente un valor** (incluidos los límites `-90`, `90`, `-180`, `180`, `0`, enteros sin decimales y valores con 1 a 10 decimales), cuando se validan con la cultura del hilo en `es-MX`, `de-DE` y la invariante, entonces el validador los acepta y el valor interpretado es el mismo en las tres culturas (punto decimal, cultura invariante). | `UnitTest/Catalog/PointQueryValidatorTests.ValidCoordinates_AreAcceptedAndParsedWithInvariantCulture_TC_TT_API_07_01` (Theory) |
| TC-TT-API-07-02 | Dados `lat` o `lon` sin valor (`null`), fuera de rango (`90.0000001`, `-91`, `180.5`, `-180.0000001`) o mal formados (vacío, solo espacios, `abc`, `20,05`, `1,000`, `1e1`, `0x10`, `+20.05`, `NaN`, `Infinity`, `-∞`, ` 20.05`, `20.`, `.5`, más de 10 decimales, más de 16 caracteres), cuando se validan, entonces el validador los rechaza con un motivo en `lat` o `lon` que no contiene el valor recibido; `20,05` nunca se interpreta como `2005` ni como `20.05`. El validador recibe un solo valor o `null`, porque eso es lo que entrega el binder; el parámetro repetido lo rechaza el binder (caso 15). | `UnitTest/Catalog/PointQueryValidatorTests.MissingOutOfRangeOrMalformedCoordinates_AreRejectedWithoutEchoingTheValue_TC_TT_API_07_02` (Theory) |
| TC-TT-API-07-03 | Dado un punto sintético documentado en el interior de cada una de las 8 colonias piloto del seed, cuando se llama `GET /api/v1/colonias/en-punto` con sesión válida, entonces responde `200` `application/json` con `enCobertura: true` y `colonia: { id, nombre, cp, municipio }` iguales a los del documento de esa colonia en `colonias` (`id` = `_id` como 24 caracteres hexadecimales); el cuerpo no trae otros campos de la colonia (`geom`, `centro`, `nombreBusqueda`, `version`) ni las coordenadas enviadas. | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests.PointInsidePilotNeighborhood_Returns200WithItsNeighborhood_TC_TT_API_07_03` (Testcontainers MongoDB + semilla de TT-BD-06) |
| TC-TT-API-07-04 | Dado un punto válido sin colonia (Pachuca sintético, o a más de 2000 m del centro más cercano), cuando se consulta, entonces responde `200` con `enCobertura: false` y `colonia: null` (no `404`), con el mismo formato que el caso 03. | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests.PointOutOfCoverage_Returns200WithNullNeighborhood_TC_TT_API_07_04` |
| TC-TT-API-07-05 | Dados un punto sobre un borde compartido y uno en cada traslape del seed (los 4 de TT-BD-06 §5), cuando se consulta el endpoint 20 veces y se llama directo a la consulta interna de TT-BD-06 con el mismo punto, entonces el endpoint devuelve siempre la misma colonia y su `id`, `nombre`, `cp` y `municipio` son los de la consulta interna: el endpoint no reimplementa ni altera el desempate. | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests.BoundaryAndOverlap_ReturnTheSameNeighborhoodAsTheInternalLookup_TC_TT_API_07_05` |
| TC-TT-API-07-06 | Dadas peticiones HTTP con `lat` o `lon` ausentes, vacíos, **repetidos** (`?lat=20.05&lat=99.9&lon=-99.34`, y lo mismo con `lon`), fuera de rango o mal formados (`20,05`, `NaN`, `1e1`), cuando se envían con sesión válida, entonces responden `422` `application/problem+json` con `errors.lat` o `errors.lon` (nunca `400` ni `500`, y nunca un `200` con el primer valor), y el catálogo no se llama (contador del doble en 0). | `IntegrationTest/Catalog/NeighborhoodAtPointValidationTests.InvalidOrRepeatedQuery_Returns422AndNeverCallsCatalog_TC_TT_API_07_06` (Theory, doble de la consulta con contador) |
| TC-TT-API-07-07 | Dadas coordenadas sintéticas reconocibles (p. ej. `lat=20.0512347`, `lon=-99.3456781`), cuando se responde `200`, `422` (con una de ellas fuera de rango) y `429`, entonces ningún cuerpo ni encabezado de respuesta contiene esos valores, en ninguna representación (texto, redondeado a 4 decimales). | `SecurityTest/Catalog/NeighborhoodAtPointLeakTests.Responses_NeverEchoSubmittedCoordinates_TC_TT_API_07_07` |
| TC-TT-API-07-08 | Dadas las mismas coordenadas reconocibles y los logs capturados **con los niveles configurados** (`appsettings.json` en Production y `appsettings.Development.json` en Development), cuando se ejecutan una consulta `200`, una `422`, una `429` y una que termina en `500`, entonces ninguna línea de log contiene la latitud ni la longitud, ni la query string de la petición (solo la plantilla de la ruta), y ninguna colección de la base gana documentos. | `SecurityTest/Catalog/NeighborhoodAtPointLogPrivacyTests.Logs_NeverContainCoordinatesOrQueryString_TC_TT_API_07_08` (Theory por entorno; reutiliza `ConsoleCapture` de TC-TT-API-03-15; Testcontainers MongoDB) |
| TC-TT-API-07-09 | Dada una consulta válida, cuando se responde `200`, entonces la respuesta trae `Cache-Control: no-store` y no trae `ETag` ni `Last-Modified`; y dadas dos peticiones idénticas seguidas, entonces el catálogo se llama dos veces (contador del doble en 2): no hay caché de salida, de memoria ni distribuida por coordenada. | `IntegrationTest/Catalog/NeighborhoodAtPointCacheTests.SameCoordinatesTwice_HitCatalogTwiceAndResponseIsNoStore_TC_TT_API_07_09` (doble de la consulta con contador) |
| TC-TT-API-07-10 | Dada una petición sin `Authorization`, con un token vencido, con firma alterada o con otro algoritmo, cuando se consulta con coordenadas válidas, entonces responde `401` `application/problem+json` (el uniforme de TT-API-02) y el catálogo no se llama. | `SecurityTest/Catalog/NeighborhoodAtPointAuthTests.MissingOrInvalidSession_Returns401AndNeverCallsCatalog_TC_TT_API_07_10` (Theory) |
| TC-TT-API-07-11 | Dado el límite global por IP con ventana corta por configuración de pruebas, cuando una IP lo supera en `/api/v1/colonias/en-punto`, entonces recibe `429` `application/problem+json` con `Retry-After`, el catálogo no se llama para esa petición, y otra IP sigue recibiendo `200`: la ruta no está exenta del global. | `IntegrationTest/Catalog/NeighborhoodAtPointRateLimitTests.GlobalLimitAppliesToPointLookup_Returns429WithoutCallingCatalog_TC_TT_API_07_11` |
| TC-TT-API-07-12 | Dado el contenedor de MongoDB detenido (con un tiempo de selección de servidor corto por configuración de pruebas), cuando se consulta con coordenadas válidas, entonces responde `500` `application/problem+json` genérico (el de TT-API-01/03) sin `stackTrace`, sin la cadena de conexión, sin el host y sin las coordenadas, y el log del error tampoco las contiene. | `SecurityTest/Catalog/NeighborhoodAtPointFailureTests.CatalogUnavailable_Returns500WithoutCoordinatesOrConnectionDetails_TC_TT_API_07_12` (Testcontainers MongoDB detenido) |
| TC-TT-API-07-13 | Dada la API en Development, cuando se lee `/openapi/v1.json`, entonces existe `GET /api/v1/colonias/en-punto` con los parámetros `lat` y `lon` (en minúsculas) en query, con `required: true`, de tipo **`string`** (no `array`) con el patrón de formato fijo de §4 (`pattern: ^-?\d{1,3}(\.\d{1,10})?$`, `maxLength: 16`), generados **sin transformador de OpenAPI** (decisión del líder en #54) a partir de los atributos de las propiedades `string` del DTO (§3, decisión de enlace); los rangos ±90 y ±180 los acota el validador y se describen en `description`; la respuesta `200` con el esquema de §4 (`enCobertura` booleano obligatorio, `colonia` nulable con `id`, `nombre`, `cp` y `municipio` de tipo `string`) y las respuestas `401`, `422` y `429` como `application/problem+json`. | `IntegrationTest/OpenApi/NeighborhoodAtPointOpenApiTests.OpenApiDocumentsPointLookupContract_TC_TT_API_07_13` |
| TC-TT-API-07-14 | Dada la semilla de TT-BD-06 en el contenedor y la API completa en proceso, cuando se ejecutan 200 peticiones `GET /api/v1/colonias/en-punto` tras 20 de calentamiento (mitad con punto interior y mitad con respaldo por centro o fuera de cobertura), en secuencia y con el limitador configurado para no intervenir, entonces se imprimen p50, p95 y p99 del tiempo de respuesta medido en el cliente en proceso junto con la meta (**p95 < 150 ms**), y la prueba **falla solo con una desviación clara**: p95 > 500 ms en el CI, o p95 > 150 ms cuando se corre bajo demanda con `KNOCK_PERF_STRICT=1` (decisión (e) del líder). No incluye la red a West US. | `IntegrationTest/Catalog/NeighborhoodAtPointPerformanceTests.EndpointP95ReportedAgainstBudgetAndFailsOnClearDeviation_TC_TT_API_07_14` (`[Trait("Category", "Performance")]`) |
| TC-TT-API-07-15 | Dada la acción del controller, cuando se inspecciona por reflexión, entonces su parámetro de consulta es un DTO cuyas propiedades se llaman `Lat` y `Lon`, son de tipo `string` y llevan el binder de un solo valor (fuente query, nombres `lat` y `lon`) y `[ValidateNever]`, **sin parámetros tipados** (`double`, `decimal`, `float`, `int` ni sus nulables) ni `string[]` en la acción ni en el DTO. Dado el binder con 0, 1 y 2 o más valores, entonces devuelve siempre un resultado: `null`, el valor, o un valor que el validador rechaza; **nunca el primero ni ninguno de los recibidos**. Y dados por HTTP `lat=20,05x`, `lat=abc` y `?lat=20.05&lat=99.9`, entonces la respuesta es `422` con `errors.lat` y su cuerpo no contiene el valor enviado (nunca el `400` automático de `[ApiController]`, ni el que repite el valor —«The value '20,05x' is not valid»— ni el de DataAnnotations). | `UnitTest/Catalog/NeighborhoodAtPointBindingTests.QueryDtoUsesSingleValueStringsNamedLatAndLon_TC_TT_API_07_15` · `UnitTest/Catalog/SingleValueQueryBinderTests.RepeatedParameter_NeverYieldsAReceivedValue_TC_TT_API_07_15` (Theory) · `SecurityTest/Catalog/NeighborhoodAtPointLeakTests.MalformedOrRepeated_Returns422WithoutEchoNotAutomatic400_TC_TT_API_07_15` |
| TC-TT-API-07-16 | Dada la configuración efectiva del host en Production y en Development (`appsettings.json`, `appsettings.Development.json` y variables de entorno), cuando se lee el nivel mínimo de Serilog que aplica a `Microsoft.AspNetCore.Hosting.Diagnostics` (override de `Microsoft.AspNetCore` o uno más específico), entonces es `Warning` o mayor; y si una prueba lo baja a `Information`, entonces la comprobación falla con un mensaje que nombra la clave. Mientras no se fusione la tarea de seguimiento de TT-API-03 (decisión (i)), este caso es la única barrera contra los logs «Request starting/finished» con la query. | `SecurityTest/Logging/AspNetCoreLogLevelGuardTests.HostingDiagnosticsStaysAtWarningOrAbove_TC_TT_API_07_16` (Theory por entorno, con un dato negativo que baja el override) |
| TC-TT-API-07-17 | Dado el trabajo terminado, entonces `docs/endpoints.md` tiene la fila de `GET /api/v1/colonias/en-punto` (método, ruta, autenticación, entrada, respuestas y caso), `docs/rate-limit-matrix.md` indica que la ruta va bajo la política global por IP sin exención, no se agregaron paquetes, y `gitleaks` no encuentra secretos. Si `scripts/export_openapi.py` ya existe al implementar, `docs/openapi.v1.json` se regenera con él. | (comando) existencia y contenido de ambos documentos, `dotnet list Knock.slnx package --vulnerable --include-transitive` y `gitleaks`; salida en el PR |
| TC-TT-API-07-18 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | (comando) `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio, servicio Mercado, módulo Catalog; mientras exista, en `src/Knock.Api/`, TT-ARQ-01):**
  - Controller delgado `Modules/Catalog/Controllers/V1/` con `[ApiVersion("1.0")]` y ruta `api/v{version:apiVersion}/colonias/en-punto`, sin `try/catch` ni acceso a datos; DTO de consulta y de respuesta y validador de FluentValidation, todos `internal`.
  - **Decisión de enlace (opción 2 de la duda (j), elegida por el líder en #54, 2026-10-26; comprobada con un proyecto mínimo, ver `Memoria.md` 2026-10-26):**
    - `lat` y `lon` se enlazan desde la query a un DTO con propiedades **`Lat` y `Lon` de tipo `string`**, cada una con un **binder de modelo propio de un solo valor** (fuente query, nombres `lat` y `lon`). Porqué: con `string` y el enlace por defecto, MVC se queda en silencio con el **primer** valor de un parámetro repetido (`?lat=20.05&lat=99.9` → `20.05`); con `string[]` el OpenAPI lo documenta como `array` y el cliente generado recibiría una lista.
    - El binder **siempre** devuelve un resultado: `null` si falta el parámetro, el valor si llega uno, y si llega repetido un valor que no cumple el formato. Así el validador lo rechaza con 422. **Nunca toma el primero** ni ningún valor recibido, y no marca `ModelState` como inválido (eso daría el 400 automático).
    - Las propiedades llevan `[BindRequired]`, `[RegularExpression]` con el patrón de §4 y `[MaxLength(16)]` **solo para el esquema de OpenAPI**, más `[ValidateNever]`. Porqué: el generador lee esos atributos (`required`, `pattern`, `maxLength`), pero sin `[ValidateNever]` MVC los valida y responde con el 400 automático de `[ApiController]` antes del `ValidationFilter`. `[BindRequired]` no genera error porque el binder siempre devuelve un resultado.
    - **Sin parámetros tipados** (`double`, `decimal`, `float`, `int` ni nulables) en la acción ni en el DTO. Porqué: con un tipo numérico, el 400 automático de `[ApiController]` llega antes del `ValidationFilter` y **repite el valor** («The value '20,05x' is not valid»); además el enlace numérico acepta `NaN`, `Infinity` y separadores de miles (`20,05` → `2005`). Lo protege el caso 15.
    - Las propiedades se llaman `Lat` y `Lon` (no `Latitude` con `[FromQuery(Name = "lat")]`), porque el `ValidationFilter` pone en camelCase el nombre de la **propiedad**: así los errores salen como `errors.lat` y `errors.lon`.
    - El validador rechaza el valor ausente y el que deja el binder para el parámetro repetido, aplica el formato y los rangos de §4 y los interpreta con `CultureInfo.InvariantCulture`; responde 422 por el `ValidationFilter` de TT-API-03 antes de llamar al catálogo.
  - Llamada a la **consulta interna de TT-BD-06** (`{ id, nombre, cp, municipio }`, TC-TT-BD-06-19), que vive en el mismo módulo Catalog; sin cambiar `ICatalogApi`.
  - `Cache-Control: no-store` en la respuesta; sin `ETag`, `[OutputCache]`, `IMemoryCache` ni `IDistributedCache` en esta ruta.
  - Autenticación con la sesión JWT de TT-API-02 (`[Authorize]`), decisión (c).
  - Documentación de la operación en el OpenAPI generado **sin transformador** (decisión del líder en #54: el contrato ya lo acota el validador y se evita una pieza nueva): atributos `ProducesResponseType` y los atributos de esquema que el generador ya lee, para que `lat` y `lon` figuren como `string` con el patrón de formato fijo y `maxLength: 16`. Los rangos y la regla de un solo valor los aplican el binder y el validador (decisión de enlace, opción 2 de (j)).
  - Prueba de guarda del nivel de log de `Microsoft.AspNetCore` (caso 16), sin cambiar la configuración de Serilog de TT-API-03.
  - Pruebas de los casos 01 a 16 (incluido el doble de la consulta con contador en `tests/IntegrationTest/Support/`), `docs/endpoints.md` (se crea si aún no existe) y la nota de la ruta en `docs/rate-limit-matrix.md`.
- **Out:**
  - La consulta interna, el desempate, el respaldo por centro ≤ 2000 m, los índices, la semilla y la conexión a MongoDB: son de **TT-BD-06 (KNOCK-156)**.
  - Cambiar las firmas de `ICatalogApi` o agregarle miembros: es de TT-BD-06 (TC-TT-BD-06-17). La validación de `coloniaId` + `cp` que necesitará `PUT /me/zona` de Identidad quedó en TT-BD-06 como `ICatalogApi.NeighborhoodExistsAsync(coloniaId, cp, ct)` (TC-TT-BD-06-20, decisión (j) de TT-BD-06); este endpoint no la usa. Moverlo a `Knock.Contracts` es de TT-ARQ-03.
  - Excluir o redactar los logs de `Microsoft.AspNetCore.Hosting.Diagnostics` en Serilog sin importar el nivel: tarea aparte de seguimiento de TT-API-03, que abre el líder en Jira (decisión (i)); aquí solo la prueba de guarda del caso 16.
  - `GET /zonas/cp/{cp}`, `POST /zonas/lista-espera` y la propuesta de colonia en el registro: **HU-009 (KNOCK-47)**. `PUT /me/zona`: Identidad (HU-001/HU-002).
  - Geocodificación externa (Google, Mapbox, HERE, Nominatim).
  - Límite propio para esta ruta o por usuario: el límite por usuario lo hará el gateway (TT-ARQ-02, KNOCK-154). Cambiar valores del limitador global.
  - Redacción de la query en los logs del gateway, de la entrada de Container Apps y de Application Insights: TT-ARQ-02 y TT-OBS-01, como requisito con su prueba que anota el líder (decisión (f); riesgo en §6).
  - Un transformador de OpenAPI para `lat` y `lon` (decisión del líder en #54).
  - Crear `tests/ContractTest` y `scripts/export_openapi.py` (TT-QA-01 y archivo protegido); regenerar el cliente Dart (knock-frontend).
  - La pantalla y el permiso de ubicación (knock-frontend).
  - Prueba de carga con k6 contra 2 réplicas (TT-QA-02) y latencia desde West US (TT-AZ-02).
  - Operar Azure, Docker o producción; editar `.github/`, `scripts/`, `.agents/` o la constitución.

## 4. Contratos API y eventos
Diseño propuesto; se fija en OpenAPI al implementar. Errores en ProblemDetails (RFC 9457) con `traceId` y `correlationId` (TT-API-03). Desde fuera se llegará por el gateway (ADR-001 regla 3).

| Método y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /api/v1/colonias/en-punto?lat={lat}&lon={lon}` | `lat`, `lon` en query, obligatorios y de un solo valor (formato abajo) · `Authorization: Bearer <JWT>` (decisión (c)) | `200` `{ enCobertura: true, colonia: { id, nombre, cp, municipio } }` · `200` `{ enCobertura: false, colonia: null }` (decisión (b)) · `401` sin sesión o sesión inválida · `422` `lat` o `lon` ausente, repetido, fuera de rango o mal formado (`errors.lat`, `errors.lon`) · `429` límite global por IP superado (`Retry-After`) · `500` error no controlado (genérico) |

**Formato de `lat` y `lon`:**
- Exactamente un valor por parámetro; un parámetro repetido (`?lat=20.05&lat=99.9`) es 422, nunca se toma el primero.
- Expresión: `^-?\d{1,3}(\.\d{1,10})?$`, máximo 16 caracteres; punto como separador decimal y cultura invariante, sin importar la cultura del servidor ni `Accept-Language`.
- Rangos: `lat` en `[-90, 90]` y `lon` en `[-180, 180]`, límites incluidos; mismos rangos que `CatalogCoordinates` (TT-BD-06).
- Se rechazan (422): coma decimal, separador de miles, exponente, hexadecimal, signo `+`, espacios, `NaN`, `Infinity`, valores vacíos, parámetro repetido y más de 10 decimales (decisión (d)). Los parámetros desconocidos se ignoran.
- El nombre del parámetro es `lon` (como en TC-TT-API-03-15, HU-009 y TT-BD-06).

**Respuesta `200`:**

| Campo | Tipo | Nota |
|---|---|---|
| `enCobertura` | `boolean` | Siempre presente. |
| `colonia` | objeto o `null` | `null` si `enCobertura` es `false`. |
| `colonia.id` | `string` | `_id` de la colonia (`ObjectId`, 24 caracteres hexadecimales, TT-BD-06 §5); es el `coloniaId` que guardará Identidad. |
| `colonia.nombre` | `string` | Nombre SEPOMEX tal como está en el catálogo (con acentos). |
| `colonia.cp` | `string` | 5 dígitos. |
| `colonia.municipio` | `string` | Por ejemplo `Tula de Allende`. |

- La respuesta nunca incluye las coordenadas recibidas, el polígono, el centro ni la distancia.
- **Encabezados:** `Cache-Control: no-store` y sin `ETag`. El principio 10 y TT-BD-06 §5 ya declaran `cache: no` para la colonia por punto: la clave tendría coordenadas. `no-store` y no el `private, no-cache` de §12, porque la URL lleva la ubicación y ningún intermediario ni la app deben guardarla. `X-Correlation-Id` como en toda respuesta.
- **Límite de peticiones:** política global por IP (100 por minuto, `docs/rate-limit-matrix.md`); sin política propia.
- **Contrato entre módulos:** esta tarea no cambia `ICatalogApi`; el endpoint usa la consulta interna de Catalog (TC-TT-BD-06-19). El miembro nuevo `NeighborhoodExistsAsync` (TC-TT-BD-06-20) lo agrega TT-BD-06 para Identidad, no este endpoint.
- **Eventos:** ninguno.
- Actualizar `docs/endpoints.md` y `docs/rate-limit-matrix.md`; `docs/openapi.v1.json` con `scripts/export_openapi.py` cuando exista (caso 17).

## 5. Modelo de datos
Sin colecciones, índices ni semillas nuevos. El endpoint lee, a través de la consulta interna del módulo Catalog, la colección `colonias` de la base lógica `market` que crea TT-BD-06 (campos `_id`, `nombre`, `cp`, `municipio`, índice `2dsphere` en `geom` y `centro`).

- **Caché:** colonia por punto `cache: no` (ya declarado en TT-BD-06 §5, principio 10): la clave de caché tendría coordenadas, un dato de ubicación que no se guarda.
- **Invariantes:** el punto solo viaja como argumento de la consulta; ninguna colección lo guarda (caso 08); el `id` devuelto es estable entre cargas de la semilla si se aprueba la duda (c) de TT-BD-06 (`_id` fijos).
- El acceso a `IMongoCollection` sigue solo en `Modules/Catalog/Data/` (TC-TT-BD-06-16); el controller no lo usa.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. La entrada es la ubicación exacta del dispositivo: el riesgo principal es que se quede guardada en algún lado (`security-core`, checks 1, 3, 6, 7, 11 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** las coordenadas quedan en logs de la API (log de petición, scope `RequestPath`, validación, excepción, 429). | Log de petición con plantilla de ruta y sin query; `ScopeRequestPathEnricher`; el 422 no repite el valor; sin suscriptor de comandos del driver; pruebas con los niveles configurados. | TC-TT-API-07-08 · TC-TT-API-03-15 · TC-TT-BD-06-09 |
| **Fuga (I):** alguien baja `Microsoft.AspNetCore` a `Information` (para depurar o por error) y «Request starting/finished» registran la query completa (comprobado por Guillermo en #54). | Prueba de guarda que falla si el nivel efectivo de `Microsoft.AspNetCore.Hosting.Diagnostics` baja de `Warning`; la defensa en profundidad es una tarea aparte de seguimiento de TT-API-03 (decisión (i)). | TC-TT-API-07-16 |
| **Fuga (I):** las coordenadas se guardan en una caché (de salida, de memoria, Redis) o en la caché HTTP de la app o de un intermediario. | Sin caché en la ruta; `Cache-Control: no-store`; sin `ETag`. | TC-TT-API-07-09 |
| **Fuga (I):** la respuesta (200, 400 automático, 422, 429, 500) repite las coordenadas o revela polígono, centro o distancia. | DTO de respuesta con solo `id`, `nombre`, `cp`, `municipio`; propiedad `string` con binder de un solo valor que rechaza el repetido con 422; `[ValidateNever]` para que no actúe el 400 automático; sin tipos numéricos; 422 sin valores; errores genéricos. | TC-TT-API-07-03 · TC-TT-API-07-07 · TC-TT-API-07-12 · TC-TT-API-07-15 |
| **Fuga (I), riesgo residual:** la query con coordenadas queda en los logs del gateway, de la entrada de Container Apps o en la telemetría de peticiones de Application Insights (que registra la URL). | Fuera del código de esta tarea: TT-ARQ-02 y TT-OBS-01 deben quitar la query de esta ruta, como requisito con su prueba que anota el líder (decisión (f)). | Sin caso aquí |
| **Alteración (T):** entrada ambigua o especial (`20,05` leído como `2005`, `NaN`, `Infinity`, exponente) clasifica otro sitio o rompe la consulta. | Formato estricto con cultura invariante y rangos en la frontera; `CatalogCoordinates` como segunda barrera. | TC-TT-API-07-01 · TC-TT-API-07-02 · TC-TT-API-07-06 |
| **Alteración (T):** parámetro repetido (`?lat=20.05&lat=99.9`) del que el servidor toma en silencio el primero, mientras un proxy, el gateway o un log toman otro. | Propiedad `string` con binder de un solo valor que rechaza el repetido con 422; `[ValidateNever]` para que no actúe el 400 automático. Nunca se elige uno. | TC-TT-API-07-06 · TC-TT-API-07-15 |
| **Alteración (T):** ejes invertidos (`lat` por `lon`). | El endpoint pasa `Lat` y `Lon` a la consulta interna por nombre; el orden `[lon, lat]` lo prueba TT-BD-06. | TC-TT-API-07-03 · TC-TT-BD-06-07 |
| **Suplantación (S):** consultas sin sesión o con un token falso. | `[Authorize]` con la validación JWT de TT-API-02 (decisión (c)). | TC-TT-API-07-10 |
| **Denegación (D):** ráfagas de consultas geográficas o barrido de puntos para dibujar el mapa de cobertura. | Límite global por IP antes de autenticar; 429 sin llamar al catálogo; límite por usuario en el gateway (TT-ARQ-02). El catálogo no es secreto: el barrido no expone datos personales. | TC-TT-API-07-11 |
| **Denegación (D):** consulta lenta bajo carga. | Índices `2dsphere` de TT-BD-06 (p95 en servidor < 100 ms, TC-TT-BD-06-12) y p95 del endpoint medido. | TC-TT-API-07-14 |
| **Fuga (I):** la caída de MongoDB devuelve o registra la cadena de conexión, el host o el punto. | Manejador de excepciones genérico (500 sin detalle fuera de Development); TT-BD-06 no registra el mensaje crudo del driver. | TC-TT-API-07-12 · TC-TT-BD-06-14 |
| **Elevación (E):** el controller lee la colección de otro módulo o del propio sin pasar por el módulo. | Controller sin acceso a datos; `IMongoCollection` solo en `Data/`. | TC-TT-BD-06-16 (existente) |
| **Repudio (R):** no aplica: no hay escrituras ni acciones de usuario que auditar; cada petición lleva `X-Correlation-Id` (TC-TT-API-03-12). | — | — |

No aplican: autorización por recurso (el catálogo es igual para todos), cifrado de campo, biométricos, archivos, IA, SSRF e idempotencia (es una lectura).

## 7. Pantallas
— (La pantalla «¿Dónde buscas expertos?» y el permiso de ubicación son de HU-009 en knock-frontend.)

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Validador de coordenadas: un solo valor, formato, rangos y cultura invariante (casos 01 y 02); DTO sin tipos numéricos y con `Lat`/`Lon` (caso 15, parte de reflexión). `tests/UnitTest/Catalog/`. |
| Integración | Sí | `KnockApiFactory` con Testcontainers de MongoDB y la semilla de TT-BD-06 (casos 03, 04, 05 y 14); con doble de la consulta con contador para validación, caché y límite (casos 06, 09 y 11). |
| Seguridad negativa | Sí | Coordenadas fuera de respuestas y logs, 400 automático evitado, guarda del nivel de log, sesión inválida y caída de la base (casos 07, 08, 10, 12, 15 y 16). `tests/SecurityTest/`. |
| Contrato | Sí, sobre la base que hay | No existe `tests/ContractTest` ni `docs/openapi.v1.json`: el caso 13 lee `/openapi/v1.json` en Development, como TC-TT-API-01-11. La comparación contra el contrato publicado llega con TT-QA-01. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests`, `ModuleSurfaceTests` y las de TT-BD-06 (`MongoAccessTests`, firmas de `ICatalogApi`) siguen en verde sin cambios. |
| Rendimiento | Sí, local | p95 del endpoint en proceso con Testcontainers (caso 14), separado de la red: informa contra la meta de 150 ms y en el CI falla solo con más de 500 ms; los 150 ms se exigen con `KNOCK_PERF_STRICT=1` (decisión (e)). La latencia a West US es de TT-AZ-02 y la carga con 2 réplicas, del k6 de TT-QA-02. |
| Base de datos | No | Sin colecciones ni índices nuevos; los cubre TT-BD-06. |
| Eventos, UI, E2E, fuzzing, caos | No | Sin eventos ni pantallas en este repositorio; la entrada es pequeña y la cubre la teoría del caso 02. |
| Mutación | Según la meta vigente del módulo Catalog | Nightly con Stryker; el validador es el objetivo principal. |
| Regresión | Sí | TC-TT-API-03-01..16 y TC-TT-BD-06-04..09, 17 y 19 siguen en verde sin editar. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: build Release 0/0, `dotnet format`, Unit (con Architecture), Integration y Security en verde con resultado real, `sdd_lint.py --trace TT-API-07`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Orden:** la implementación empieza cuando TT-BD-06 esté fusionada con su consulta interna (los casos 03, 04, 05, 08, 12 y 14 necesitan la colección, la consulta y la fixture de MongoDB). Los casos 01, 02, 06, 07, 09, 10, 11, 13, 15 y 16 usan el doble de la consulta o solo la configuración y no dependen de la base.

**Límites conocidos:**
- documentdb-local agota conexiones entre 64 y 96 (spike P7): el caso 14 corre en secuencia y no mide conexiones.
- El p95 local no representa producción: no incluye la red hasta West US, el gateway ni la entrada de Container Apps; en runners compartidos del CI puede variar (por eso el umbral con margen del caso 14).
- La redacción de la query fuera de la API (gateway, entrada, Application Insights) no se prueba aquí: es requisito de TT-ARQ-02 y TT-OBS-01 (decisión (f)).
- La privacidad de la query en los logs de la API depende hoy de un nivel de configuración; el caso 16 lo vigila, pero no la redacta (la redacción es la tarea de seguimiento de TT-API-03, decisión (i)).

**Decisiones y dudas para el líder:**
- **(a) `id`, `cp` y `municipio` — cubierta por TT-BD-06 (#53, Borrador).** `ICatalogApi.FindNeighborhoodAtAsync` devuelve solo el nombre. TT-BD-06 ya no congela la interfaz por completo (TC-TT-BD-06-17: las tres firmas de siempre más `NeighborhoodExistsAsync`, TC-TT-BD-06-20) y agrega la consulta interna `{ id, nombre, cp, municipio }` con el mismo desempate, en la que `ICatalogApi` delega (TC-TT-BD-06-19). Esta spec usa esa consulta. Queda solo como dependencia: si la spec de TT-BD-06 cambia esa consulta antes de aprobarse, se ajustan aquí los casos 03 y 05.
- **(b) Resultado «sin colonia» — Resuelto (líder, #54, 2026-10-26).** `200` con `{ enCobertura: false, colonia: null }`, no `404`: «fuera de cobertura» es una respuesta válida del negocio y no un recurso inexistente. **Sin `ciudadPiloto`**: eso es de HU-009, que lo obtiene con `GET /zonas/cp/{cp}`. Caso 04.
- **(c) Autenticación — Resuelto (líder, #54, 2026-10-26).** Requiere sesión JWT (`[Authorize]`, TT-API-02). Caso 10.
- **(d) Precisión — Resuelto (líder, #54, 2026-10-26).** Hasta 10 decimales y 422 si hay más; la app envía 6 decimales (~0.1 m). Casos 01 y 02.
- **(e) Meta del p95 local y su uso en el CI — Resuelto (líder, #54, 2026-10-24).** Caso 14 aceptado tal cual: en el CI la prueba informa p50/p95/p99 frente a la meta de **150 ms** y solo falla con un p95 de **más de 500 ms**; los 150 ms se exigen bajo demanda con `KNOCK_PERF_STRICT=1`. La consulta en servidor mantiene su propio umbral de 100 ms (TC-TT-BD-06-12).
- **(f) Coordenadas en la query fuera de la API — Resuelto (líder, #54, 2026-10-26).** Se mantiene `GET` con `lat`/`lon` en query (decisión (a) de HU-009). Quitar la query de esta ruta en los logs del gateway (YARP), de la entrada de Container Apps y de la telemetría de Application Insights queda como requisito, con su propia prueba, en TT-ARQ-02 y TT-OBS-01; lo anota el líder en esas tareas.
- **(g) `lon` o `lng` — resuelta.** Las tres specs usan `lon` (TT-API-07, HU-009 en #40 y TT-BD-06 en #53).
- **(h) Puntos y sprint — Resuelto (líder, #54, 2026-10-26).** 2 puntos, Sprint 2, después de TT-BD-06.
- **(i) Defensa en profundidad para la query en los logs de ASP.NET Core — Resuelto (líder, #54, 2026-10-24).** Será una **tarea aparte de seguimiento de TT-API-03**, que el líder abre en Jira: excluir o redactar los logs de `Microsoft.AspNetCore.Hosting.Diagnostics` sin importar el nivel (hoy, con `Information`, «Request starting/finished» registran la query completa; comprobado en #54 y anotado en `Memoria.md`, #55). Esta spec solo conserva el caso 16, que falla si ese nivel baja de `Warning`; cuando esa tarea se fusione, se revisa si el caso 16 sigue haciendo falta.
- **(j) OpenAPI como `string` frente al enlace a `string[]` — Resuelto (líder, #54, 2026-10-26): opción 2.** Propiedades `string` en el DTO, documentadas como `string` con patrón y `maxLength`, y un binder de un solo valor que rechaza el parámetro repetido con 422 (nunca toma el primero). Comprobado con un proyecto mínimo con el mismo generador (`Microsoft.AspNetCore.OpenApi` 10.0.13; detalle en `Memoria.md`, 2026-10-26):
  - `string` sale como `string`; `string[]`, como `array`. El binder no cambia la documentación.
  - Los atributos de esquema necesitan `[ValidateNever]` para no disparar el 400 automático.
  - `required: true` sale con `[BindRequired]` porque el binder siempre devuelve un resultado.

  Aplicado en §3 (decisión de enlace) y en los casos 13 y 15.

**Dudas abiertas para el líder:** ninguna.
- **Pregunta cruzada de Identidad — resuelta en TT-BD-06.** La validación de `coloniaId` + `cp` para `PUT /me/zona` es `ICatalogApi.NeighborhoodExistsAsync(coloniaId, cp, ct)` (TC-TT-BD-06-20, decisión (j) de TT-BD-06, #53). Esta spec no la usa ni la prueba.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
