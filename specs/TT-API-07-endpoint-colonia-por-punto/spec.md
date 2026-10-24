# TT-API-07 — Endpoint de colonia por punto: `GET /api/v1/colonias/en-punto` (lat, lon), sobre `ICatalogApi`

> **Jira:** KNOCK-141 · **Área:** API · **Servicio:** Mercado (`Knock.Market.Api`, ADR-001) · **Módulo:** Catalog · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S2 (por confirmar) · **Puntos:** por definir · **Prioridad:** Técnica
> **Repositorio:** knock-backend · **Otra parte en:** knock-frontend (pantalla «¿Dónde buscas expertos?», HU-009) · **Rama:** `docs/KNOCK-141-colonia-by-point-spec` (spec) → PR a `sprint-01`; la implementación va en su propia rama tras la aprobación
> **Ficha:** no existe `docs/backlog/TT-API-07.md` (la tarea aparece en `docs/PLAN_ADAPTACION.md` §E: «Colonia por punto con consulta por polígono de MongoDB»).
> **Depende de:** TT-BD-06 (KNOCK-156, consulta y desempate sobre MongoDB; spec en `docs/KNOCK-156-catalog-mongodb-spec`, Borrador) · TT-API-03 (KNOCK-37, 422 con FluentValidation, ProblemDetails, `X-Correlation-Id`, limitador por IP y logs sin coordenadas; en `sprint-01`) · TT-API-02 (KNOCK-21, sesión JWT) · TT-BD-05 (KNOCK-147, spike, PR #48).
> **La consume:** HU-009 (KNOCK-47, spec en `docs/KNOCK-47-zone-by-location-spec`): TC-HU-009-01 solo usa el resultado de este endpoint.
> **Fuentes:** ADR-001 (reglas 1, 2, 3 y 5) · ADR-002 · ADR-006 · constitución 2.2.0 (principios 3, 4, 9 y 10; §4 pipeline; §5; §6 «ubicación del dispositivo convertida en colonia y descartada»; §12) · spec TT-API-03 (casos 01, 02, 04, 07, 12 y 15) · spec TT-BD-06 (§4, §5, casos 04 a 09, 12 y 17) · spec HU-009 (§4 y decisión (a)) · `src/Knock.Api/Modules/Catalog/` · `docs/rate-limit-matrix.md`
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, api-contract-openapi, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Exponer por HTTP la consulta «colonia de un punto» del catálogo propio: la app manda la ubicación del dispositivo una sola vez y recibe la colonia que le corresponde, o el aviso de que está fuera de cobertura. Las coordenadas **se convierten en colonia y se descartan** (constitución §6): no se guardan, no se registran y no se cachean.

**Terminada cuando:** `GET /api/v1/colonias/en-punto?lat=…&lon=…` responde la colonia (o «fuera de cobertura») con el contrato de §4 desde MongoDB (contenedor de pruebas), rechaza con 422 toda entrada fuera de rango o mal formada, no deja la coordenada en logs, caché ni respuestas, y su p95 local queda dentro de la meta de §8.

**Qué ya existe y qué es nuevo** (leído en `src/Knock.Api/` y con `python scripts/codemap.py --query "neighborhood point colonia coordinates"`):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| Consulta | `ICatalogApi.FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken) : Task<string?>` (solo el **nombre**), implementada hoy con PostGIS en `Services/CatalogApi`; TT-BD-06 la reimplementa en MongoDB **sin cambiar la firma** (TC-TT-BD-06-17). | **Se reutiliza sin cambiar la firma.** El endpoint no reimplementa la consulta ni el desempate. La firma devuelve solo el nombre y el contrato de §4 pide `id`, `cp` y `municipio`: de dónde salen es la **duda (a)** (bloqueante). |
| Validación de coordenadas | `Domain/CatalogCoordinates.ToPoint` valida rangos y lanza `ArgumentOutOfRangeException` sin el valor (TC-TT-BD-06-07). | Se conserva como red de seguridad. La validación de frontera es nueva: un validador de FluentValidation que responde 422 antes de llamar al catálogo. |
| Validación a 422 | `Hosting/ValidationExtensions.ValidationFilter` (TT-API-03): 422 ProblemDetails con `errors`, sin repetir el valor. | **Se reutiliza sin cambios.** |
| Errores, correlación y límite | ProblemDetails, `X-Correlation-Id`, `DomainExceptionHandler`, limitador global por IP (`docs/rate-limit-matrix.md`). | **Se reutiliza sin cambios.** La ruta queda bajo la política global (no está exenta). |
| Logs sin coordenadas | `LoggingExtensions`: el log de petición registra la plantilla de la ruta sin query; `ScopeRequestPathEnricher` quita el `RequestPath` con valores (TC-TT-API-03-15). | **Se reutiliza sin cambios.** Aquí se prueba con el endpoint real. |
| Endpoint | No existe. Catalog no tiene controllers. | **Nuevo:** controller delgado `Modules/Catalog/Controllers/V1/` con `[ApiVersion("1.0")]`. |
| OpenAPI | `/openapi/v1.json` solo en Development (`OpenApiExtensions`, TC-TT-API-01-11). No existen `tests/ContractTest`, `scripts/export_openapi.py` ni `docs/openapi.v1.json`. | La operación se documenta en el OpenAPI generado y se prueba leyendo `/openapi/v1.json` (caso 13). El proyecto de contrato y el exportador quedan fuera de esta tarea (§3). |

## 2. Historia y casos de prueba
**TT-API-07** — Tarea técnica. Los casos 01 a 14 se verifican con pruebas de código; el 15 y el 16 con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_07_KK`. Todas las coordenadas son **sintéticas**: puntos documentados del seed de TT-BD-06 dentro de Tula de Allende y un punto de Pachuca sintético fuera de cobertura; nunca ubicaciones de personas. Las pruebas unitarias usan un doble del catálogo; las de integración, Testcontainers con MongoDB (imagen según la duda (a) de TT-BD-06) y la semilla de TT-BD-06. El token de sesión de las pruebas es ficticio, emitido con el emisor de TT-API-02 o un equivalente sintético.

**Lo que ya está cubierto y no se duplica** (se referencia y debe seguir en verde): el desempate en bordes y traslapes (TC-TT-BD-06-05), el respaldo por centro ≤ 2000 m (TC-TT-BD-06-06), el orden `[lng, lat]` (TC-TT-BD-06-07), la firma de `ICatalogApi` (TC-TT-BD-06-17), el p95 de la consulta en el servidor (TC-TT-BD-06-12), la forma del 422 y del 429 (TC-TT-API-03-01, 02, 04 y 07) y el `X-Correlation-Id` en toda respuesta (TC-TT-API-03-12). **TC-HU-009-01** solo consume el resultado de este endpoint (doble de su respuesta) y no prueba la consulta.

Los casos 03, 04, 05, 10 y 11 asumen las recomendaciones de las dudas (a), (b) y (c) de §8; si el líder elige otra opción, se reescriben antes de aprobar.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-07-01 | Dados `lat` y `lon` válidos en el formato de §4 (incluidos los límites `-90`, `90`, `-180`, `180`, `0`, enteros sin decimales y valores con 1 a 10 decimales), cuando se validan con la cultura del hilo en `es-MX`, `de-DE` y la invariante, entonces el validador los acepta y el valor interpretado es el mismo en las tres culturas (punto decimal, cultura invariante). | `UnitTest/Catalog/PointQueryValidatorTests.ValidCoordinates_AreAcceptedAndParsedWithInvariantCulture_TC_TT_API_07_01` (Theory) |
| TC-TT-API-07-02 | Dados `lat` o `lon` fuera de rango (`90.0000001`, `-91`, `180.5`, `-180.0000001`) o mal formados (vacío, solo espacios, `abc`, `20,05`, `1,000`, `1e1`, `0x10`, `+20.05`, `NaN`, `Infinity`, `-∞`, ` 20.05`, `20.`, `.5`, más de 10 decimales, más de 16 caracteres), cuando se validan, entonces el validador los rechaza con un motivo por campo (`lat` o `lon`) que no contiene el valor recibido; `20,05` nunca se interpreta como `2005` ni como `20.05`. | `UnitTest/Catalog/PointQueryValidatorTests.OutOfRangeOrMalformedCoordinates_AreRejectedWithoutEchoingTheValue_TC_TT_API_07_02` (Theory) |
| TC-TT-API-07-03 | Dado un punto sintético documentado en el interior de cada una de las 8 colonias piloto del seed, cuando se llama `GET /api/v1/colonias/en-punto` con sesión válida, entonces responde `200` `application/json` con `enCobertura: true` y `colonia: { id, nombre, cp, municipio }` iguales a los del documento de esa colonia en `colonias` (`id` = `_id` como 24 caracteres hexadecimales); el cuerpo no trae otros campos de la colonia (`geom`, `centro`, `nombreBusqueda`, `version`) ni las coordenadas enviadas. | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests.PointInsidePilotNeighborhood_Returns200WithItsNeighborhood_TC_TT_API_07_03` (Testcontainers MongoDB + semilla de TT-BD-06) |
| TC-TT-API-07-04 | Dado un punto válido sin colonia (Pachuca sintético, o a más de 2000 m del centro más cercano), cuando se consulta, entonces responde `200` con `enCobertura: false` y `colonia: null` (no `404`), con el mismo formato que el caso 03 (duda (b)). | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests.PointOutOfCoverage_Returns200WithNullNeighborhood_TC_TT_API_07_04` |
| TC-TT-API-07-05 | Dados un punto sobre un borde compartido y uno en un traslape del seed (El Llano 1a Sección–Tultengo), cuando se consulta el endpoint 20 veces y se llama directo a `ICatalogApi.FindNeighborhoodAtAsync` con el mismo punto, entonces el endpoint devuelve siempre la misma colonia y su `nombre` es el que devuelve `ICatalogApi`: el endpoint no reimplementa ni altera el desempate de TT-BD-06. | `IntegrationTest/Catalog/NeighborhoodAtPointEndpointTests.BoundaryAndOverlap_ReturnTheSameNeighborhoodAsCatalogApi_TC_TT_API_07_05` |
| TC-TT-API-07-06 | Dadas peticiones HTTP con `lat` o `lon` ausentes, vacíos, repetidos (`lat=20.05&lat=20.06`), fuera de rango o mal formados (`20,05`, `NaN`, `1e1`), cuando se envían con sesión válida, entonces responden `422` `application/problem+json` con `errors.lat` o `errors.lon` (nunca `400` ni `500`), y el catálogo no se llama (contador del doble en 0). | `IntegrationTest/Catalog/NeighborhoodAtPointValidationTests.InvalidQuery_Returns422AndNeverCallsCatalog_TC_TT_API_07_06` (Theory, doble de `ICatalogApi` con contador) |
| TC-TT-API-07-07 | Dadas coordenadas sintéticas reconocibles (p. ej. `lat=20.0512347`, `lon=-99.3456781`), cuando se responde `200`, `422` (con una de ellas fuera de rango) y `429`, entonces ningún cuerpo ni encabezado de respuesta contiene esos valores, en ninguna representación (texto, redondeado a 4 decimales). | `SecurityTest/Catalog/NeighborhoodAtPointLeakTests.Responses_NeverEchoSubmittedCoordinates_TC_TT_API_07_07` |
| TC-TT-API-07-08 | Dadas las mismas coordenadas reconocibles y los logs capturados en nivel `Debug` (incluidos ASP.NET Core y el driver de MongoDB), cuando se ejecutan una consulta `200`, una `422`, una `429` y una que termina en `500`, entonces ninguna línea de log contiene la latitud ni la longitud, ni la query string de la petición (solo la plantilla de la ruta), y ninguna colección de la base gana documentos. | `SecurityTest/Catalog/NeighborhoodAtPointLogPrivacyTests.Logs_NeverContainCoordinatesOrQueryString_TC_TT_API_07_08` (reutiliza `ConsoleCapture` de TC-TT-API-03-15; Testcontainers MongoDB) |
| TC-TT-API-07-09 | Dada una consulta válida, cuando se responde `200`, entonces la respuesta trae `Cache-Control: no-store` y no trae `ETag` ni `Last-Modified`; y dadas dos peticiones idénticas seguidas, entonces el catálogo se llama dos veces (contador del doble en 2): no hay caché de salida, de memoria ni distribuida por coordenada. | `IntegrationTest/Catalog/NeighborhoodAtPointCacheTests.SameCoordinatesTwice_HitCatalogTwiceAndResponseIsNoStore_TC_TT_API_07_09` (doble de `ICatalogApi` con contador) |
| TC-TT-API-07-10 | Dada una petición sin `Authorization`, con un token vencido, con firma alterada o con otro algoritmo, cuando se consulta con coordenadas válidas, entonces responde `401` `application/problem+json` (el uniforme de TT-API-02) y el catálogo no se llama (duda (c)). | `SecurityTest/Catalog/NeighborhoodAtPointAuthTests.MissingOrInvalidSession_Returns401AndNeverCallsCatalog_TC_TT_API_07_10` (Theory) |
| TC-TT-API-07-11 | Dado el límite global por IP con ventana corta por configuración de pruebas, cuando una IP lo supera en `/api/v1/colonias/en-punto`, entonces recibe `429` `application/problem+json` con `Retry-After`, el catálogo no se llama para esa petición, y otra IP sigue recibiendo `200`: la ruta no está exenta del global. | `IntegrationTest/Catalog/NeighborhoodAtPointRateLimitTests.GlobalLimitAppliesToPointLookup_Returns429WithoutCallingCatalog_TC_TT_API_07_11` |
| TC-TT-API-07-12 | Dado el contenedor de MongoDB detenido (con un tiempo de selección de servidor corto por configuración de pruebas), cuando se consulta con coordenadas válidas, entonces responde `500` `application/problem+json` genérico (el de TT-API-01/03) sin `stackTrace`, sin la cadena de conexión, sin el host y sin las coordenadas, y el log del error tampoco las contiene. | `SecurityTest/Catalog/NeighborhoodAtPointFailureTests.CatalogUnavailable_Returns500WithoutCoordinatesOrConnectionDetails_TC_TT_API_07_12` (Testcontainers MongoDB detenido) |
| TC-TT-API-07-13 | Dada la API en Development, cuando se lee `/openapi/v1.json`, entonces existe `GET /api/v1/colonias/en-punto` con `lat` y `lon` en query, obligatorios, de tipo `number` con `minimum`/`maximum` (±90 y ±180), la respuesta `200` con el esquema de §4 (`enCobertura` booleano obligatorio, `colonia` nulable con `id`, `nombre`, `cp` y `municipio` de tipo `string`) y las respuestas `401`, `422` y `429` como `application/problem+json`. | `IntegrationTest/OpenApi/NeighborhoodAtPointOpenApiTests.OpenApiDocumentsPointLookupContract_TC_TT_API_07_13` |
| TC-TT-API-07-14 | Dada la semilla de TT-BD-06 en el contenedor y la API completa en proceso, cuando se ejecutan 200 peticiones `GET /api/v1/colonias/en-punto` tras 20 de calentamiento (mitad con punto interior y mitad con respaldo por centro o fuera de cobertura), en secuencia y con el limitador configurado para no intervenir, entonces el **p95 del tiempo de respuesta del endpoint** medido en el cliente en proceso es **< 150 ms** (duda (e)); se imprimen p50, p95 y p99. No incluye la red a West US. | `IntegrationTest/Catalog/NeighborhoodAtPointPerformanceTests.EndpointP95UnderBudgetWithTestcontainers_TC_TT_API_07_14` (`[Trait("Category", "Performance")]`) |
| TC-TT-API-07-15 | Dado el trabajo terminado, entonces `docs/endpoints.md` tiene la fila de `GET /api/v1/colonias/en-punto` (método, ruta, autenticación, entrada, respuestas y caso), `docs/rate-limit-matrix.md` indica que la ruta va bajo la política global por IP sin exención, no se agregaron paquetes, y `gitleaks` no encuentra secretos. Si `scripts/export_openapi.py` ya existe al implementar, `docs/openapi.v1.json` se regenera con él. | (comando) existencia y contenido de ambos documentos, `dotnet list Knock.slnx package --vulnerable --include-transitive` y `gitleaks`; salida en el PR |
| TC-TT-API-07-16 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | (comando) `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio, servicio Mercado, módulo Catalog; mientras exista, en `src/Knock.Api/`, TT-ARQ-01):**
  - Controller delgado `Modules/Catalog/Controllers/V1/` con `[ApiVersion("1.0")]` y ruta `api/v{version:apiVersion}/colonias/en-punto`, sin `try/catch` ni acceso a datos; DTO de consulta y de respuesta y validador de FluentValidation, todos `internal`.
  - Validador de coordenadas en la frontera (formato y rangos de §4, cultura invariante) que responde 422 por el `ValidationFilter` de TT-API-03 antes de llamar al catálogo. Para que la entrada mal formada dé 422 y no el 400 automático de `[ApiController]`, `lat` y `lon` llegan al validador como texto y se interpretan con `CultureInfo.InvariantCulture`; el mecanismo exacto (DTO de texto o binder propio) se decide al implementar, sin cambiar el comportamiento de los casos 01, 02 y 06.
  - Llamada al catálogo con `ICatalogApi.FindNeighborhoodAtAsync` **sin cambiar su firma** y, para `id`, `cp` y `municipio`, lo que decida la duda (a).
  - `Cache-Control: no-store` en la respuesta; sin `ETag`, `[OutputCache]`, `IMemoryCache` ni `IDistributedCache` en esta ruta.
  - Autenticación con la sesión JWT de TT-API-02 (`[Authorize]`), según la duda (c).
  - Documentación de la operación en el OpenAPI generado (atributos `ProducesResponseType` y, si hace falta, un transformador para que `lat` y `lon` figuren como `number` con sus rangos).
  - Pruebas de los casos 01 a 14 (incluido el doble de `ICatalogApi` con contador en `tests/IntegrationTest/Support/`), `docs/endpoints.md` (se crea si aún no existe) y la nota de la ruta en `docs/rate-limit-matrix.md`.
- **Out:**
  - La consulta, el desempate, el respaldo por centro ≤ 2000 m, los índices, la semilla y la conexión a MongoDB: son de **TT-BD-06 (KNOCK-156)**.
  - Cambiar la firma de `ICatalogApi` o agregarle miembros (TC-TT-BD-06-17); moverlo a `Knock.Contracts` (TT-ARQ-03).
  - `GET /zonas/cp/{cp}`, `POST /zonas/lista-espera` y la propuesta de colonia en el registro: **HU-009 (KNOCK-47)**. `PUT /me/zona`: Identidad (HU-001/HU-002).
  - Geocodificación externa (Google, Mapbox, HERE, Nominatim).
  - Límite propio para esta ruta o por usuario: el límite por usuario lo hará el gateway (TT-ARQ-02, KNOCK-154). Cambiar valores del limitador global.
  - Redacción de la query en los logs del gateway, de la entrada de Container Apps y de Application Insights: TT-ARQ-02 y TT-OBS-01 (riesgo anotado en §6 y duda (f)).
  - Crear `tests/ContractTest` y `scripts/export_openapi.py` (TT-QA-01 y archivo protegido); regenerar el cliente Dart (knock-frontend).
  - La pantalla y el permiso de ubicación (knock-frontend).
  - Prueba de carga con k6 contra 2 réplicas (TT-QA-02) y latencia desde West US (TT-AZ-02).
  - Operar Azure, Docker o producción; editar `.github/`, `scripts/`, `.agents/` o la constitución.

## 4. Contratos API y eventos
Diseño propuesto; se fija en OpenAPI al implementar. Errores en ProblemDetails (RFC 9457) con `traceId` y `correlationId` (TT-API-03). Desde fuera se llegará por el gateway (ADR-001 regla 3).

| Método y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /api/v1/colonias/en-punto?lat={lat}&lon={lon}` | `lat`, `lon` en query, obligatorios (formato abajo) · `Authorization: Bearer <JWT>` (duda (c)) | `200` `{ enCobertura: true, colonia: { id, nombre, cp, municipio } }` · `200` `{ enCobertura: false, colonia: null }` (duda (b)) · `401` sin sesión o sesión inválida · `422` `lat` o `lon` ausente, repetido, fuera de rango o mal formado · `429` límite global por IP superado (`Retry-After`) · `500` error no controlado (genérico) |

**Formato de `lat` y `lon`:**
- Expresión: `^-?\d{1,3}(\.\d{1,10})?$`, máximo 16 caracteres; punto como separador decimal y cultura invariante, sin importar la cultura del servidor ni `Accept-Language`.
- Rangos: `lat` en `[-90, 90]` y `lon` en `[-180, 180]`, límites incluidos; mismos rangos que `CatalogCoordinates` (TT-BD-06).
- Se rechazan (422): coma decimal, separador de miles, exponente, hexadecimal, signo `+`, espacios, `NaN`, `Infinity`, valores vacíos, parámetro repetido y más de 10 decimales (duda (d)). Los parámetros desconocidos se ignoran.
- El nombre del parámetro es `lon` (como en TC-TT-API-03-15); HU-009 y TT-BD-06 dicen `lng` en su texto (duda (g)).

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
- **Contrato entre módulos:** `ICatalogApi` sin cambios (TC-TT-BD-06-17).
- **Eventos:** ninguno.
- Actualizar `docs/endpoints.md` y `docs/rate-limit-matrix.md`; `docs/openapi.v1.json` con `scripts/export_openapi.py` cuando exista (caso 15).

## 5. Modelo de datos
Sin colecciones, índices ni semillas nuevos. El endpoint lee, a través del módulo Catalog, la colección `colonias` de la base lógica `market` que crea TT-BD-06 (campos `_id`, `nombre`, `cp`, `municipio`, índice `2dsphere` en `geom` y `centro`).

- **Caché:** colonia por punto `cache: no` (ya declarado en TT-BD-06 §5, principio 10): la clave de caché tendría coordenadas, un dato de ubicación que no se guarda.
- **Invariantes:** el punto solo viaja como argumento de la consulta; ninguna colección lo guarda (caso 08); el `id` devuelto es estable entre cargas de la semilla si se aprueba la duda (c) de TT-BD-06 (`_id` fijos).
- El acceso a `IMongoCollection` sigue solo en `Modules/Catalog/Data/` (TC-TT-BD-06-16); el controller no lo usa.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. La entrada es la ubicación exacta del dispositivo: el riesgo principal es que se quede guardada en algún lado (`security-core`, checks 1, 3, 6, 7, 11 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Fuga (I):** las coordenadas quedan en logs de la API (log de petición, scope `RequestPath`, validación, excepción, 429, driver de MongoDB). | Log de petición con plantilla de ruta y sin query; `ScopeRequestPathEnricher`; el 422 no repite el valor; sin suscriptor de comandos del driver. | TC-TT-API-07-08 · TC-TT-API-03-15 · TC-TT-BD-06-09 |
| **Fuga (I):** las coordenadas se guardan en una caché (de salida, de memoria, Redis) o en la caché HTTP de la app o de un intermediario. | Sin caché en la ruta; `Cache-Control: no-store`; sin `ETag`. | TC-TT-API-07-09 |
| **Fuga (I):** la respuesta (200, 422, 429, 500) repite las coordenadas o revela polígono, centro o distancia. | DTO de respuesta con solo `id`, `nombre`, `cp`, `municipio`; 422 sin valores; errores genéricos. | TC-TT-API-07-03 · TC-TT-API-07-07 · TC-TT-API-07-12 |
| **Fuga (I), riesgo residual:** la query con coordenadas queda en los logs del gateway, de la entrada de Container Apps o en la telemetría de peticiones de Application Insights (que registra la URL). | Fuera del código de esta tarea: TT-ARQ-02 y TT-OBS-01 deben quitar la query de esta ruta. Riesgo anotado; ver duda (f). | Sin caso aquí |
| **Alteración (T):** entrada ambigua o especial (`20,05` leído como `2005`, `NaN`, `Infinity`, exponente) clasifica otro sitio o rompe la consulta. | Formato estricto con cultura invariante y rangos en la frontera; `CatalogCoordinates` como segunda barrera. | TC-TT-API-07-01 · TC-TT-API-07-02 · TC-TT-API-07-06 |
| **Alteración (T):** ejes invertidos (`lat` por `lon`). | El endpoint pasa `lat` y `lon` a `FindNeighborhoodAtAsync(latitude, longitude)` por nombre; el orden `[lng, lat]` lo prueba TT-BD-06. | TC-TT-API-07-03 · TC-TT-BD-06-07 |
| **Suplantación (S):** consultas sin sesión o con un token falso. | `[Authorize]` con la validación JWT de TT-API-02 (duda (c)). | TC-TT-API-07-10 |
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
| Unitarias | Sí | Validador de coordenadas: formato, rangos y cultura invariante (casos 01 y 02). `tests/UnitTest/Catalog/`. |
| Integración | Sí | `KnockApiFactory` con Testcontainers de MongoDB y la semilla de TT-BD-06 (casos 03, 04, 05 y 14); con doble de `ICatalogApi` con contador para validación, caché y límite (casos 06, 09 y 11). |
| Seguridad negativa | Sí | Coordenadas fuera de respuestas y logs, sesión inválida y caída de la base (casos 07, 08, 10 y 12). `tests/SecurityTest/Catalog/`. |
| Contrato | Sí, sobre la base que hay | No existe `tests/ContractTest` ni `docs/openapi.v1.json`: el caso 13 lee `/openapi/v1.json` en Development, como TC-TT-API-01-11. La comparación contra el contrato publicado llega con TT-QA-01. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests`, `ModuleSurfaceTests` y las de TT-BD-06 (`MongoAccessTests`, firma de `ICatalogApi`) siguen en verde sin cambios. |
| Rendimiento | Sí, local | p95 del endpoint en proceso con Testcontainers (caso 14), separado de la red. La latencia a West US es de TT-AZ-02 y la carga con 2 réplicas, del k6 de TT-QA-02. |
| Base de datos | No | Sin colecciones ni índices nuevos; los cubre TT-BD-06. |
| Eventos, UI, E2E, fuzzing, caos | No | Sin eventos ni pantallas en este repositorio; la entrada es pequeña y la cubre la teoría del caso 02. |
| Mutación | Según la meta vigente del módulo Catalog | Nightly con Stryker; el validador es el objetivo principal. |
| Regresión | Sí | TC-TT-API-03-01..16 y TC-TT-BD-06-04..09 y 17 siguen en verde sin editar. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: build Release 0/0, `dotnet format`, Unit (con Architecture), Integration y Security en verde con resultado real, `sdd_lint.py --trace TT-API-07`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba.

**Orden:** la implementación empieza cuando TT-BD-06 esté fusionada (los casos 03, 04, 05, 08, 12 y 14 necesitan la colección y la fixture de MongoDB). Los casos 01, 02, 06, 07, 09, 10, 11 y 13 usan el doble de `ICatalogApi` y no dependen de la base.

**Límites conocidos:**
- documentdb-local agota conexiones entre 64 y 96 (spike P7): el caso 14 corre en secuencia y no mide conexiones.
- El p95 local no representa producción: no incluye la red hasta West US, el gateway ni la entrada de Container Apps.
- La redacción de la query fuera de la API (gateway, entrada, Application Insights) no se prueba aquí (duda (f)).

**Dudas abiertas para el líder:**
- **(a) `id`, `cp` y `municipio` con la firma actual de `ICatalogApi` (bloqueante).** `FindNeighborhoodAtAsync` devuelve solo el nombre (`Task<string?>`), y TC-TT-BD-06-17 prohíbe cambiar la firma o agregar miembros. El nombre no basta: HU-009 necesita el `id` para `PUT /me/zona`, y el nombre no es único entre CP. Opciones: (1) la respuesta lleva solo `nombre` (no cumple el contrato pedido ni a HU-009); (2) cambiar la firma de `ICatalogApi` para devolver un registro (rompe TC-TT-BD-06-17 y la instrucción de esta tarea); (3) TT-BD-06 agrega dentro del módulo Catalog una consulta **interna** que devuelve `{ id, nombre, cp, municipio }` con el mismo desempate; `ICatalogApi.FindNeighborhoodAtAsync` queda igual y delega en ella (proyecta el nombre), y el controller, que vive en el mismo módulo, usa la consulta interna; (4) el endpoint llama a `FindNeighborhoodAtAsync` y luego busca la colonia por nombre (dos consultas y ambigüedad si dos CP comparten nombre). **Recomendación:** (3). Una sola consulta y un solo desempate, sin tocar el contrato entre módulos. Requiere agregar esa consulta interna al alcance de TT-BD-06 (su spec también es borrador) y cambiar el caso 05 para comparar el `id` además del `nombre`.
- **(b) Resultado «sin colonia».** Opciones: (1) `200` con `{ enCobertura: false, colonia: null }`; (2) `404` ProblemDetails. **Recomendación:** (1). «Fuera de cobertura» es una respuesta válida del negocio y no un recurso inexistente; la app no la trata como error y se distingue de un 404 de ruta o de versión. Es coherente con `GET /zonas/cp/{cp}` de HU-009 (`200` con `enCobertura: false`). Sub-duda: ¿se agrega `ciudadPiloto` como en HU-009 CA4, para que la app muestre «Por ahora solo atendemos [ciudad piloto]» sin otra llamada? Recomendación: sí, si HU-009 no lo obtiene ya de `/zonas/cp/{cp}`.
- **(c) Autenticación.** Hoy no hay política de autorización por defecto (`FallbackPolicy`): sin `[Authorize]`, la ruta es pública. Opciones: (1) requiere sesión JWT; (2) pública, solo con el límite por IP. **Recomendación:** (1). El paso de zona va después del nombre en el registro (`FUNCIONES_Y_FLUJOS.md §3.3`), cuando ya hay sesión; las rutas de HU-009 son autenticadas; reduce el barrido anónimo y permite el límite por usuario en el gateway. Si se quiere mostrar cobertura antes de iniciar sesión (landing o primer arranque), se elige (2) y el caso 10 pasa a probar que la ruta anónima sigue limitada por IP.
- **(d) Precisión.** Opciones: (1) hasta 10 decimales y rechazo con 422 si hay más; (2) aceptar cualquier cantidad y redondear en el servidor; (3) exigir exactamente 6 decimales. **Recomendación:** (1), y que la app envíe 6 decimales (~0.1 m), más que suficientes para una colonia; rechazar es más claro que redondear en silencio.
- **(e) Meta del p95 local del endpoint.** Propuesta: < 150 ms en proceso con Testcontainers (la consulta en servidor ya mide p95 0.6 ms en Azure y < 100 ms es la meta de TT-BD-06). ¿Se confirma la cifra y que corra en el CI o solo bajo demanda (`Category=Performance`)? Recomendación: en el CI, porque su costo es de segundos.
- **(f) Coordenadas en la query fuera de la API.** La ruta es `GET` con `lat`/`lon` en query (decisión (a) de HU-009), y la query puede quedar en los logs del gateway (YARP), de la entrada de Container Apps y en la telemetría de peticiones de Application Insights. Opciones: (1) mantener `GET` y pedir en TT-ARQ-02 y TT-OBS-01 que quiten la query de esta ruta, con su propia prueba; (2) cambiar a `POST` con cuerpo (no lo registran por defecto, pero es una lectura y se pierde la semántica de `GET`). **Recomendación:** (1). Hay que anotarlo como requisito en esas dos tareas antes de desplegar.
- **(g) `lon` o `lng`.** El pedido de esta tarea y TC-TT-API-03-15 usan `lon`; HU-009 y TT-BD-06 dicen `lng` en su texto. **Recomendación:** `lon`, y corregir el texto de HU-009 y TT-BD-06 en sus PR (ambas son borradores).
- **(h) Puntos y sprint.** La tarea no tiene ficha en `docs/backlog/`: ¿puntos y sprint?

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
