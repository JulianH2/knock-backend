# TT-API-03 — Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones

> **Jira:** KNOCK-37 · **Área:** API · Backend base · **Épica:** EP-13 · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S2 · **Puntos:** 2 · **Prioridad:** Alta
> **Repositorio:** knock-backend · **Ficha:** TT-API-03 (`docs/backlog/TT-API-03.md`) · **Rama:** `chore/KNOCK-37-middlewares-base` → PR a `sprint-01`
> **Depende de:** TT-API-01. **No depende** del spike de MongoDB (TT-BD-05, ADR-002): esta tarea no tiene persistencia ni toca la base de datos.
> **Fuentes:** DECISION_TECNICA.md §4 · constitución §4 (pipeline) · ADR-001 (reglas 3 y 5)
> **Skills:** foundation-backend, security-core, api-contract-openapi, testing-strategy-backend, story-workflow
> **Estado:** Borrador

## 1. Objetivo
Que todos los endpoints respondan los errores con el mismo formato, que cada petición se pueda rastrear de punta a punta con un `X-Correlation-Id` y que la API tenga una primera defensa contra el abuso (límite de peticiones por IP y validación de entrada con respuesta uniforme).

**Terminada cuando** (ficha): errores con formato estándar y trazables.

**Qué ya existe y qué es nuevo** (verificado en el código con `python scripts/codemap.py --query` y lectura de `src/Knock.Api/Hosting/`):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| ProblemDetails (RFC 9457) | `Hosting/ProblemDetailsExtensions.AddKnockProblemDetails` (`AddProblemDetails` + detalle y `stackTrace` solo en Development) y `ProblemResponses.WriteAsync` (en `RequestBodyLimitMiddleware.cs`) para middleware y eventos de autenticación. `UseExceptionHandler()` sin handlers propios: toda excepción da 500. | **Se reutiliza y se extiende.** Se agrega un `IExceptionHandler` que traduce excepciones de dominio a 400, 403, 404, 409 y 422; el resto sigue en 500 como hoy (TC-TT-API-01-04..06). Los errores llevan además `correlationId`. No se crea otro formato de error ni otro `ProblemResponses`. |
| Serilog | `Hosting/LoggingExtensions.AddKnockLogging` (Serilog.AspNetCore 10.0.0, `RenderedCompactJsonFormatter` a consola, `Enrich.FromLogContext`) y `UseSerilogRequestLogging`. | **Se extiende:** el middleware de correlación empuja `CorrelationId` con `LogContext.PushProperty` (`Enrich.FromLogContext` ya está). No se reconfigura Serilog, no hay otro sink ni paquete enricher. |
| Límite de cuerpo | `RequestBodyLimitMiddleware` + `[RequestSizeLimit(8192)]` en `/auth/firebase` (TC-TT-API-02-11). | **Se reutiliza sin cambios** (413 ya cubierto). |
| Validación de entrada | `DataAnnotations` en `Identity/Dtos/SessionDtos` con el filtro automático de `[ApiController]`: responde 400. | **Nuevo:** filtro propio con FluentValidation que responde 422 en los endpoints nuevos. `/auth/firebase` se mantiene en 400 (decisión 6). |
| Autenticación | `Hosting/AuthenticationExtensions` (JwtBearer propio, 401 uniforme con `ProblemResponses`). | **Se reutiliza sin cambios.** |
| Límite de peticiones | No existe (TT-API-02 lo dejó fuera y lo pidió como condición para no desplegar). | **Nuevo:** política global por IP y política estricta para `/auth` por IP, con el rate limiter del framework. |
| `X-Correlation-Id` | No existe. | **Nuevo:** middleware que asigna o propaga el encabezado, lo agrega a cada log, a la respuesta y al cuerpo de los errores. |
| Pipeline | `UseKnockApi`: `UseExceptionHandler` → `UseSerilogRequestLogging` → `RequestBodyLimitMiddleware` → `UseAuthentication` → `UseAuthorization` → health/OpenAPI → `MapControllers`. | Se extiende con correlación, `ForwardedHeaders` y `UseRateLimiter` (decisiones 2 y 3), documentado en `docs/MIDDLEWARES.md`. |

## 2. Historia y casos de prueba
**TT-API-03** — Tarea técnica (no es historia de usuario). Los casos 01 a 04, 06 a 16 se verifican con pruebas de código; el 05 está **diferido** al gateway; el 17 y el 18 se verifican con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_03_KK`. V1 de la ficha se cubre con el caso 01; V2 con el 03; V3 con el 11 (y el 08, 09 y 12). El resto sale de los pasos de la ficha, de las decisiones del revisor y de `security-core` (checks 3, 6, 7, 11 y 15). Lo ya cubierto por TC-TT-API-01-04..06 (ProblemDetails de excepciones no controladas) y TC-TT-API-02-11 (límite de cuerpo, 400/413) **se referencia y no se duplica**: esas pruebas deben seguir en verde sin cambios.

Las pruebas que necesitan un endpoint con validador y uno que lance excepciones de dominio usan controllers de prueba en `tests/IntegrationTest/Support/` (como `TestController`), no endpoints de producción. Las pruebas del limitador usan una ventana corta por configuración de pruebas; el limitador no se desactiva en producción.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-03-01 | Dado un endpoint con un validador de FluentValidation y un cuerpo que lo incumple, cuando se invoca, entonces responde 422 `application/problem+json` (`type`, `title`, `status`) con los campos inválidos, y el controller no se ejecuta (V1). | `IntegrationTest/Validation/ValidationFilterTests.InvalidBody_Returns422ProblemDetailsWithFields_TC_TT_API_03_01` |
| TC-TT-API-03-02 | Dado un cuerpo inválido con valores sensibles (por ejemplo un teléfono o un token en el campo erróneo), cuando se responde el 422, entonces la lista de errores nombra el campo y el motivo pero no devuelve el valor enviado. | `SecurityTest/Validation/ValidationErrorLeakTests.Invalid422_DoesNotEchoSubmittedValues_TC_TT_API_03_02` |
| TC-TT-API-03-03 | Dado `POST /api/v1/auth/firebase` repetido por la misma IP más veces que el límite estricto de `/auth` (10 por minuto, punto de partida), cuando llega una petición más dentro de la ventana, entonces responde 429 `application/problem+json` con `Retry-After`, y las anteriores no recibieron 429 (V2). Las peticiones usan cuerpo vacío (400): no llaman a Firebase. | `IntegrationTest/RateLimiting/AuthRateLimitTests.AuthLimitExceeded_Returns429WithRetryAfter_TC_TT_API_03_03` |
| TC-TT-API-03-04 | Dado el límite global por IP (100 por minuto, punto de partida), cuando una IP lo supera en un endpoint que no es `/auth`, entonces recibe 429, otra IP distinta en el mismo momento sigue recibiendo 200, la ventana siguiente vuelve a aceptar peticiones, y `/health` y `/health/ready` nunca reciben 429 aunque se supere el límite global. | `IntegrationTest/RateLimiting/GlobalRateLimitTests.GlobalLimitPerIp_Returns429OnlyForTheOffenderAndHealthIsExempt_TC_TT_API_03_04` (ventana corta por configuración de pruebas) |
| TC-TT-API-03-05 | **DIFERIDO al gateway.** Antes: límite por usuario (`sub` del JWT). Decisión del revisor: en esta tarea el límite es solo por IP; el límite por usuario lo hará el gateway (TT-ARQ-02 valida el JWT). El número se conserva para no renumerar; no hay prueba en este repositorio ni en esta tarea. | (diferido al gateway; sin prueba en TT-API-03) |
| TC-TT-API-03-06 | Dado un cliente que no es un proxy de confianza y que envía `X-Forwarded-For` (o `Forwarded`) variable en cada petición, cuando supera el límite, entonces el 429 se aplica igual (la IP de partición es la de la conexión, no la del encabezado); y dado el proxy de confianza configurado, entonces sí se usa la IP reenviada. | `SecurityTest/RateLimiting/ForwardedHeadersEvasionTests.SpoofedForwardedFor_DoesNotEvadeLimit_TC_TT_API_03_06` |
| TC-TT-API-03-07 | Dado un 429, cuando se revisa su cuerpo y encabezados, entonces tiene la misma forma que los demás errores y no revela la IP, la clave de partición, el límite configurado ni el contador. | `SecurityTest/RateLimiting/RateLimitResponseLeakTests.Response429_DoesNotRevealPartitionOrQuota_TC_TT_API_03_07` |
| TC-TT-API-03-08 | Dada una petición sin `X-Correlation-Id`, cuando se responde, entonces la respuesta trae `X-Correlation-Id` con un GUID sin guiones generado (distinto en cada petición). | `IntegrationTest/Correlation/CorrelationIdTests.NoHeader_GeneratesAndReturnsId_TC_TT_API_03_08` |
| TC-TT-API-03-09 | Dada una petición con un `X-Correlation-Id` válido (hasta 64 caracteres de `[A-Za-z0-9._-]`), cuando se responde, entonces la respuesta devuelve el mismo valor. | `IntegrationTest/Correlation/CorrelationIdTests.ValidHeader_IsPropagatedToResponse_TC_TT_API_03_09` |
| TC-TT-API-03-10 | Dada una petición con un `X-Correlation-Id` de más de 64 caracteres, con caracteres fuera de `[A-Za-z0-9._-]`, con CR/LF o con forma de JSON, cuando se procesa, entonces se descarta y se reemplaza por un GUID sin guiones: ni la respuesta ni los logs contienen el valor enviado, y no se parte la línea de log ni los encabezados. | `SecurityTest/Correlation/CorrelationIdInjectionTests.HostileHeader_IsReplaced_NoLogOrHeaderInjection_TC_TT_API_03_10` |
| TC-TT-API-03-11 | Dada una petición con `X-Correlation-Id`, cuando se capturan los logs de la consola (JSON), entonces cada evento escrito durante esa petición (log de petición, log de un servicio y log de error) trae la propiedad `CorrelationId` con ese valor, y los eventos de otra petición no lo traen (V3). | `IntegrationTest/Logging/CorrelationLoggingTests.EveryLogOfARequest_CarriesCorrelationId_TC_TT_API_03_11` (usa `ConsoleCapture`) |
| TC-TT-API-03-12 | Dadas respuestas producidas por distintas capas (manejador de excepciones 500, límite de cuerpo 413, autenticación 401, rate limiter 429, validación 422), cuando se reciben, entonces todas son `application/problem+json`, traen el encabezado `X-Correlation-Id` y su cuerpo trae `correlationId` (igual al encabezado) además de `traceId`, y ambos campos son independientes (no se unifican). | `IntegrationTest/Errors/ErrorResponseCorrelationTests.ErrorsFromAnyLayer_CarryProblemJsonAndCorrelationId_TC_TT_API_03_12` |
| TC-TT-API-03-13 | Dada una excepción de dominio por cada código del catálogo de dominio (400, 403, 404, 409, 422), cuando la lanza un endpoint, entonces el `IExceptionHandler` responde ese código en ProblemDetails (`status` coherente con el HTTP); una excepción desconocida sigue siendo 500; el controller no tiene `try/catch`. El 401 y el 429 no son excepciones de dominio (los producen la autenticación y el limitador; casos 12 y 03). | `IntegrationTest/Errors/DomainExceptionHandlerTests.DomainException_MapsToCatalogStatus_TC_TT_API_03_13` (Theory) · `UnitTest/Errors/DomainExceptionMappingTests.EachDomainCode_HasAMapping_TC_TT_API_03_13` |
| TC-TT-API-03-14 | Dado un entorno distinto de Development y una excepción de dominio cuyo mensaje o datos internos contienen valores sensibles o trazas, cuando se responde, entonces el ProblemDetails no incluye `stackTrace`, el mensaje interno ni datos de la excepción (solo el texto del catálogo). Complementa TC-TT-API-01-05, que cubre la excepción no controlada. | `SecurityTest/Errors/DomainErrorDetailTests.NonDevelopment_DomainException_HidesInternals_TC_TT_API_03_14` |
| TC-TT-API-03-15 | Dada una petición con `Authorization: Bearer <token>`, un cuerpo con un token y un celular ficticio, y coordenadas ficticias en la ruta, la query o el cuerpo, cuando se capturan los logs (petición, validación 422, excepción y 429), entonces no aparece ningún token, celular ni coordenada. | `SecurityTest/Logging/SensitiveLogTests.Logs_DoNotContainTokensPhonesOrCoordinates_TC_TT_API_03_15` |
| TC-TT-API-03-16 | Dada la API arrancada, cuando se consulta el orden efectivo del pipeline por comportamiento, entonces coincide con el documentado en `docs/MIDDLEWARES.md`: la correlación va antes del manejador de excepciones; el 429 se responde antes de autenticar (sin consultar Firebase ni emitir sesión); `Authentication` va antes de `Authorization`. | `IntegrationTest/Hosting/PipelineOrderTests.Pipeline_FollowsDocumentedOrder_TC_TT_API_03_16` |
| TC-TT-API-03-17 | Dado el trabajo terminado, entonces existen `docs/MIDDLEWARES.md` (orden de middlewares que coincide con `UseKnockApi`) y `docs/rate-limit-matrix.md` (las dos políticas: global por IP y `/auth` por IP, con sus valores, la exención de `/health` y la nota de las 2 réplicas); cada paquete nuevo (el base `FluentValidation` y su extensión de DI) está evaluado (necesidad, licencia, mantenimiento, vulnerabilidades) y registrado en `Memoria.md`; `dotnet list package --vulnerable --include-transitive` no reporta vulnerables; y no hay secretos (`gitleaks`). | Comandos de existencia y contenido de ambos documentos, `dotnet list package` y `gitleaks` (comando); salida en el PR |
| TC-TT-API-03-18 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes` (comando); salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `Hosting/`: middleware de `X-Correlation-Id` (asigna o propaga, valida máx. 64 caracteres de `[A-Za-z0-9._-]`, si falta o es inválido genera un GUID sin guiones, lo pone en el `LogContext` con `LogContext.PushProperty`, en la respuesta y como `correlationId` en el cuerpo de los errores); `IExceptionHandler` que traduce excepciones de dominio a 400, 403, 404, 409 y 422 reutilizando `AddKnockProblemDetails` y `ProblemResponses`; filtro propio de validación con FluentValidation que responde 422 con los campos inválidos; registro del rate limiter con la política global por IP y la estricta para `/auth` por IP, con 429 por `ProblemResponses` y `Retry-After`; `ForwardedHeaders` para la IP real detrás del proxy. Extensiones nuevas en el mismo estilo que `LoggingExtensions`/`AuthenticationExtensions` y `UseKnockApi` actualizado.
  - Del pipeline de la constitución §4 entran solo **RateLimiter y ForwardedHeaders**. TT-API-03 y TT-API-06 (KNOCK-138) se solapaban: TT-API-03 se queda con esos dos; TT-API-06 se reduce a SecurityHeaders, HSTS, HTTPS y CORS (el revisor ajusta TT-API-06 en Jira).
  - Tipo base mínimo de excepción de dominio (en `Common/`) con 400, 403, 404, 409 y 422; nombres y jerarquía por definir al implementar.
  - Serilog: reutiliza `AddKnockLogging` y `UseSerilogRequestLogging`; solo se agrega la propiedad `CorrelationId`.
  - Valores del límite y proxies de confianza por configuración (`appsettings*.json`, sin secretos). Punto de partida: global 100 por minuto por IP, `/auth` 10 por minuto por IP, `/health` y `/health/ready` exentos del global; se afinan con el k6 de TT-QA-02.
  - Pruebas de los casos 01 a 04 y 06 a 16, controllers de prueba en `tests/IntegrationTest/Support/` y, si hace falta, ajuste de `KnockApiFactory` para una ventana corta y para que las suites existentes no reciban 429.
  - `docs/MIDDLEWARES.md` (orden de middlewares; el revisor lo traslada a la constitución) y `docs/rate-limit-matrix.md` (las dos políticas).
  - Paquetes: ninguno de los de la ficha como estaba escrito. `Microsoft.AspNetCore.RateLimiting` viene en el framework (no se agrega paquete); `Serilog.Enrichers.CorrelationId` no hace falta; no se agrega `FluentValidation.AspNetCore`. Para validación: el paquete base `FluentValidation` y su extensión de DI (nombre exacto por confirmar al evaluar), con un filtro propio. Se evalúan (licencia, mantenimiento, vulnerabilidades) **al implementar** y se registran en `Memoria.md` antes de agregarlos.
- **Out:**
  - Observabilidad en Azure (Application Insights, trazas, alertas): TT-OBS-01.
  - SecurityHeaders, HSTS, HttpsRedirection y CORS del §4: TT-API-06.
  - Límite por usuario (`sub` del JWT): lo hará el gateway (TT-ARQ-02); caso 05 diferido. No se lee el `sub` sin validar ni se mueve `UseRateLimiter` después de la autenticación.
  - Editar `constitution.md` (archivo protegido: lo hace el revisor con `docs/MIDDLEWARES.md`).
  - Redis o límite distribuido (ADR-006), separar servicios o crear el gateway (ADR-001). Esta tarea no depende del spike de MongoDB (TT-BD-05/ADR-002) y no tiene persistencia.
  - Rotación de refresh token, bloqueo por intentos de código SMS y límites por número de teléfono (HU-001/HU-003 y ADR-004).
  - Cambiar lo ya cubierto: ProblemDetails de excepciones no controladas (TC-TT-API-01-04..06), límites de cuerpo (TC-TT-API-02-11), el 401 uniforme (TC-TT-API-02-02..08) y el 400 de `/auth/firebase` (no se enmienda TC-TT-API-02-11).
  - Contrato vivo y OpenAPI de 422, 429 y `X-Correlation-Id`: con HU-001.
  - Datos de negocio, persistencia, eventos, pantallas y cualquier endpoint nuevo de producción.
  - Operar Azure, Docker o producción, y editar `.github/`, `scripts/`, `.agents/` o la constitución.

## 4. Contratos API y eventos
No hay endpoints ni eventos nuevos: la tarea cambia el comportamiento transversal de todos.

**Respuestas de error** (ProblemDetails, RFC 9457, `application/problem+json`):

| Código | Cuándo | Quién lo produce | ¿Excepción de dominio? |
|---|---|---|---|
| 400 | Petición mal formada o cuerpo ilegible | Dominio / framework (existente) | Sí. `/auth/firebase` se mantiene en 400 para cuerpo inválido. |
| 401 | Sin sesión o sesión inválida | `AuthenticationExtensions` (existente) | No |
| 403 | Sin permiso | Dominio | Sí |
| 404 | No existe (o no es tuyo) | Dominio | Sí |
| 409 | Estado o concurrencia | Dominio | Sí (transición inválida, constitución §4) |
| 422 | Validación de campos fallida | Filtro de FluentValidation (nuevo) y dominio | Sí |
| 429 | Límite superado | Rate limiter (nuevo) | No. Con `Retry-After`; sin IP, clave ni cuota. |
| 413, 500 | Cuerpo grande / error no controlado | Existentes | No |

El cuerpo de todo error trae `traceId` (de ASP.NET Core) y `correlationId` (nuevo); se mantienen separados por ahora (el gateway generará el id y los servicios lo propagan, ADR-001 regla 5). Los textos (`title`) del dominio salen de títulos genéricos del estándar HTTP hasta que exista un catálogo propio; no se inventan textos en esta spec.

**Encabezado `X-Correlation-Id`:**
- Petición (opcional): si cumple máx. 64 caracteres de `[A-Za-z0-9._-]` se propaga; si falta o no cumple, se genera un GUID sin guiones.
- Respuesta: siempre `X-Correlation-Id`, también en errores de cualquier capa (caso 12).
- Logs: propiedad `CorrelationId` en cada evento de la petición (caso 11).

**Rate limiting (solo por IP):**

| Política | Alcance | Punto de partida |
|---|---|---|
| Global | Todo endpoint salvo `/health` y `/health/ready` | 100 por minuto por IP |
| Estricta `/auth` | `/api/v1/auth/*` | 10 por minuto por IP |

Valores en configuración (se afinan con el k6 de TT-QA-02). Con 2 réplicas el límite efectivo se duplica (aceptado, defensa en profundidad). Respuesta 429 en ProblemDetails con `Retry-After`.

**Eventos:** ninguno.

## 5. Modelo de datos
— Sin persistencia: no hay tablas, `DbContext` ni migración (y, por eso, ninguna relación con el spike de MongoDB). El rate limiter vive en memoria del proceso: es una defensa, no estado de negocio.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. Toca errores, logs y límites (`security-core`, checks 3, 6, 7, 11 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Suplantación/alteración: el cliente fija `X-Correlation-Id` para confundir la investigación o para inyectar líneas o campos en el log (CR/LF, JSON) | Se valida longitud (64) y alfabeto; el valor inválido se descarta y se genera otro; el log es JSON estructurado (la propiedad es un valor, no texto concatenado); el id es solo para rastrear, nunca para autorizar | TC-TT-API-03-10, TC-TT-API-03-09 |
| Alteración: inyección de encabezados de respuesta por el valor reenviado | Mismo filtro; solo se devuelve un valor validado o generado | TC-TT-API-03-10 |
| Suplantación: el cliente evade el límite por IP con `X-Forwarded-For`/`Forwarded` falsos | `ForwardedHeaders` solo desde proxies/redes de confianza configurados; sin proxy de confianza, se usa la IP de la conexión | TC-TT-API-03-06 |
| Denegación: ráfagas contra endpoints públicos (canje de `/auth/firebase`, fuerza bruta, costo de SMS) | Política estricta para `/auth` y global por IP; 429 antes de autenticar o llamar a Firebase; cuerpo ya acotado (TC-TT-API-02-11) | TC-TT-API-03-03, TC-TT-API-03-04, TC-TT-API-03-16 |
| Denegación: varias personas detrás de la misma IP (NAT) comparten el límite | Riesgo aceptado: solo hay límite por IP en esta tarea; el límite por usuario lo hará el gateway | Diferido (TC-TT-API-03-05) |
| Denegación: agotar `/health` o usarlo para saturar el limitador | `/health` y `/health/ready` exentos del global porque no hacen trabajo costoso (sin comprobaciones hoy); si luego consultan la base, se revisa la exención | TC-TT-API-03-04 (exención); riesgo anotado |
| Denegación/memoria: demasiadas particiones del limitador (muchas IP) | Ventanas cortas que expiran las particiones; la clave es la IP validada, no datos libres del cliente | Sin caso automático: riesgo anotado |
| Denegación: el límite efectivo se duplica con las réplicas | Aceptado: el limitador de ASP.NET Core es por proceso; no es estado de negocio; sin Redis (ADR-006); el gateway podrá aplicar el límite global (ADR-001 regla 3) | Sin caso (documentado en `docs/rate-limit-matrix.md`, caso 17) |
| Fuga: el 429 revela IP, clave de partición, cuota o contador | Cuerpo uniforme de ProblemDetails; solo `Retry-After` | TC-TT-API-03-07 |
| Fuga: stack trace, mensaje interno o datos de la excepción de dominio fuera de Development | El handler escribe solo el texto genérico; el detalle de excepción sigue únicamente en Development (`CustomizeProblemDetails` existente) | TC-TT-API-03-14, TC-TT-API-01-05 |
| Fuga: el 422 repite el valor enviado (teléfono, token) | La lista de errores lleva campo y motivo, no el valor | TC-TT-API-03-02 |
| Fuga: tokens, celulares o coordenadas en los logs (petición, validación, excepción, 429, query string) | Logs estructurados sin cuerpos ni encabezados; el log de petición registra ruta y no query; las excepciones se registran con tipo y traza solo en el log del servidor, sin datos del usuario en el mensaje | TC-TT-API-03-15 |
| Repudio: sin rastro de qué petición produjo un error | `CorrelationId` en cada log, encabezado y cuerpo de error; el 429 y el 422 también se registran | TC-TT-API-03-11, TC-TT-API-03-12 |
| Elevación: un error de orden del pipeline deja pasar peticiones sin límite o sin autenticar | Orden documentado en `docs/MIDDLEWARES.md` y probado por comportamiento | TC-TT-API-03-16, TC-TT-API-03-17 |
| Cadena de suministro: paquete nuevo de validación (`FluentValidation` y su extensión de DI) con licencia o vulnerabilidades | Evaluación y registro en `Memoria.md` al implementar; `NuGetAudit`; `dotnet list package --vulnerable` | TC-TT-API-03-17 |

No aplican aquí: autorización por recurso (sin recursos), cifrado de campo, biométricos, archivos, IA y SSRF.

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Mapa excepción de dominio → código (caso 13, parte unitaria) y validación del `X-Correlation-Id`, `UnitTest/Errors/`. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests` y `ModuleSurfaceTests` de TT-API-01 siguen en verde sin cambios (lo nuevo vive en `Hosting/` y `Common/`). |
| Integración | Sí | `WebApplicationFactory` (`KnockApiFactory`) con controllers de prueba en `Support/`: casos 01, 03, 04, 08, 09, 11, 12, 13 y 16. Sin Testcontainers: no hay base de datos. Los casos con `/auth/firebase` mandan cuerpo vacío para no depender del emulador de Firebase. |
| Seguridad negativa | Sí | Casos 02, 06, 07, 10, 14 y 15 en `tests/SecurityTest`. |
| Contrato | No | No hay endpoints nuevos; OpenAPI de 422, 429 y `X-Correlation-Id` con HU-001. |
| Base de datos | No | Sin persistencia. |
| Mutación | No | Sin meta para `Hosting/`, como en TT-API-02. |
| Rendimiento (k6), fuzzing, caos, E2E, UI | No | Los valores del límite se afinan con el k6 de TT-QA-02; aquí se prueba funcionalmente. |
| Regresión | Sí | TC-TT-API-01-04..06 y TC-TT-API-02-02..13 siguen en verde sin editar. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit, Integration y Security en verde, `sdd_lint.py --trace TT-API-03`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Evidencia en el PR: salida de `dotnet test` y la de los casos 17 y 18. El caso 05 diferido no tiene prueba; `--trace` lo marcará FALTA hasta que el revisor lo anote como diferido o lo mueva (ver nota abajo). `docs/endpoints.md` y `docs/events.md` del gate no existen aún y no hay endpoints nuevos.

**Decisiones previas (de `Memoria.md`):**
1. El pipeline de seguridad de la constitución §4 se dejó fuera de TT-API-01 y TT-API-02 «para otra tarea con su spec».
2. TT-API-02 dejó el canje `/auth/firebase` sin rate limit solo para correr en local y pidió no desplegar sin él (TC-TT-API-02-16): esta tarea lo cierra.
3. Paquetes nuevos: evaluación y registro previos (docs/FLUJO_EQUIPO.md §2). Serilog se reutiliza.

**Dudas resueltas por el revisor (PR #49):**
1. **Valores del límite — Resuelto:** en `appsettings`, sin secretos. Punto de partida: global 100 por minuto por IP; `/auth` 10 por minuto por IP; `/health` y `/health/ready` exentos del global. Se afinan con el k6 de TT-QA-02. Con 2 réplicas el límite efectivo se duplica (aceptado, defensa en profundidad); se documenta.
2. **Alcance del pipeline §4 — Resuelto:** entran RateLimiter y ForwardedHeaders; quedan fuera SecurityHeaders, HSTS, HttpsRedirection y CORS. TT-API-03 se queda con RateLimiter y ForwardedHeaders; TT-API-06 (KNOCK-138) se reduce a SecurityHeaders, HSTS, HTTPS y CORS (el revisor ajusta TT-API-06 en Jira).
3. **«Por usuario» — Resuelto:** opción (c), solo por IP en esta tarea. El límite por usuario lo hará el gateway (TT-ARQ-02 valida el JWT). No se mueve `UseRateLimiter` ni se lee el `sub` sin validar. TC-TT-API-03-05 queda DIFERIDO (conserva su número).
4. **`X-Correlation-Id` — Resuelto:** máx. 64 caracteres de `[A-Za-z0-9._-]`; si falta o es inválido, GUID sin guiones. `correlationId` además de `traceId` en el cuerpo de los errores; separados por ahora (el gateway generará el id y los servicios lo propagan, ADR-001).
5. **Paquetes — Resuelto:** `Microsoft.AspNetCore.RateLimiting` viene en el framework (no es paquete que agregar); `Serilog.Enrichers.CorrelationId` no hace falta (`LogContext.PushProperty` en el middleware); validación con el paquete base `FluentValidation`, su extensión de DI y un filtro propio que responde 422, no `FluentValidation.AspNetCore`. Licencia, mantenimiento y vulnerabilidades se evalúan al implementar y se registran en `Memoria.md`.
6. **`/auth/firebase` — Resuelto:** se mantiene en 400; el 422 aplica a los endpoints nuevos. No se enmienda TC-TT-API-02-11. Se revisa al publicar el contrato con HU-001.
7. **Orden de middlewares — Resuelto:** `docs/MIDDLEWARES.md` (no protegido); el revisor lo traslada a la constitución. Se crea `docs/rate-limit-matrix.md` con las dos políticas.
8. **Servicio o gateway — Resuelto:** el límite vive en `Knock.Api` como defensa en profundidad, sin Redis (ADR-006). Cuando exista el gateway se decide si se duplica o se retira. No se separan servicios hasta que el spike de la base de datos responda (ADR-001).
9. **Pruebas del limitador — Resuelto:** ventana corta por configuración de pruebas, sin desactivar el limitador en producción; se permite tocar `KnockApiFactory` si hace falta.
10. **Excepciones de dominio — Resuelto:** tipo base mínimo con 400, 403, 404, 409 y 422. El 401 y el 429 no son excepciones de dominio (los producen la autenticación y el limitador) pero siguen siendo respuestas de la API.
11. **OpenAPI — Resuelto:** 422, 429 y `X-Correlation-Id` con HU-001.
12. **Mutación — Resuelto:** sin meta para `Hosting/`, como en TT-API-02.

**Límites y dudas abiertas:**
- Ninguna de las 12 dudas anteriores sigue abierta. Pendientes de proceso, no de diseño:
  - El revisor ajusta TT-API-06 (KNOCK-138) en Jira para quitarle RateLimiter y ForwardedHeaders.
  - Qué redes o proxies son de confianza para `ForwardedHeaders` en Container Apps (los valores reales) no se conoce todavía; se pone la configuración sin valores de producción y una persona los define al desplegar (no se infieren aquí).
  - `sdd_lint.py --trace TT-API-03` marcará FALTA el caso 05 (diferido, sin prueba); el revisor decide cómo anotarlo.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
