# TT-API-03 — Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones

> **Jira:** KNOCK-37 · **Área:** API · Backend base · **Épica:** EP-13 · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S1 · **Puntos:** 2 · **Prioridad:** Alta
> **Repositorio:** knock-backend · **Ficha:** TT-API-03 (`docs/backlog/TT-API-03.md`) · **Rama:** `chore/KNOCK-37-middlewares-base` → PR a `sprint-01`
> **Depende de:** TT-API-01
> **Fuentes:** DECISION_TECNICA.md §4 · constitución §4 (pipeline) · ADR-001 (regla 3 y 5)
> **Skills:** foundation-backend, security-core, api-contract-openapi, testing-strategy-backend, story-workflow
> **Estado:** Borrador

## 1. Objetivo
Que todos los endpoints respondan los errores con el mismo formato, que cada petición se pueda rastrear de punta a punta con un `X-Correlation-Id` y que la API tenga una primera defensa contra el abuso (límite de peticiones y validación de entrada con respuesta uniforme).

**Terminada cuando** (ficha): errores con formato estándar y trazables.

**Qué ya existe y qué es nuevo** (verificado en el código con `python scripts/codemap.py --query` y lectura de `src/Knock.Api/Hosting/`):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| ProblemDetails (RFC 9457) | `Hosting/ProblemDetailsExtensions.AddKnockProblemDetails` (`AddProblemDetails` + detalle y `stackTrace` solo en Development) y `ProblemResponses.WriteAsync` (en `RequestBodyLimitMiddleware.cs`) para middleware y eventos de autenticación. `UseExceptionHandler()` sin handlers propios: toda excepción da 500. | **Se reutiliza y se extiende.** Se agrega un `IExceptionHandler` que traduce excepciones de dominio a 400, 401, 403, 404, 409, 422 y 429; el resto sigue en 500 como hoy (TC-TT-API-01-04..06). No se crea otro formato de error ni otro `ProblemResponses`. |
| Serilog | `Hosting/LoggingExtensions.AddKnockLogging` (Serilog.AspNetCore 10.0.0, `RenderedCompactJsonFormatter` a consola, `Enrich.FromLogContext`) y `UseSerilogRequestLogging`. | **Se extiende:** un middleware de correlación empuja `CorrelationId` al `LogContext` (`Enrich.FromLogContext` ya está). No se reconfigura Serilog ni se agrega otro sink. |
| Límite de cuerpo | `RequestBodyLimitMiddleware` + `[RequestSizeLimit(8192)]` en `/auth/firebase` (TC-TT-API-02-11). | **Se reutiliza sin cambios** (413 ya cubierto). |
| Validación de entrada | `DataAnnotations` en `Identity/Dtos/SessionDtos` (`[Required]`, `[StringLength(4096)]`) con el filtro automático de `[ApiController]`: responde 400. | **Nuevo:** filtro de validación con FluentValidation que responde 422 con los campos inválidos. Qué pasa con `/auth/firebase` (400 o 422) es duda 6 de §8. |
| Autenticación | `Hosting/AuthenticationExtensions` (JwtBearer propio, 401 uniforme con `ProblemResponses`). | **Se reutiliza sin cambios.** |
| Límite de peticiones | No existe (TT-API-02 lo dejó fuera y lo pidió como condición para no desplegar). | **Nuevo:** política global por IP y política estricta para `/auth` con el rate limiter de ASP.NET Core. |
| `X-Correlation-Id` | No existe. | **Nuevo:** middleware que asigna o propaga el encabezado, lo agrega a cada log y a cada respuesta. |
| Pipeline | `UseKnockApi`: `UseExceptionHandler` → `UseSerilogRequestLogging` → `RequestBodyLimitMiddleware` → `UseAuthentication` → `UseAuthorization` → health/OpenAPI → `MapControllers`. | Se extiende con correlación, `ForwardedHeaders` (si el revisor lo acepta, duda 2) y `UseRateLimiter`. El orden definitivo lo decide el revisor (dudas 2, 3 y 8). |

## 2. Historia y casos de prueba
**TT-API-03** — Tarea técnica (no es historia de usuario). Los casos 01 a 16 se verifican con pruebas de código; el 17 y el 18 con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_03_KK`. V1 de la ficha se cubre con el caso 01; V2 con el 03; V3 con el 11 (y el 08, 09 y 12). El resto sale de los pasos de la ficha y de `security-core` (checks 3, 6, 7, 11 y 15). Lo ya cubierto por TC-TT-API-01-04..06 (ProblemDetails de excepciones no controladas) y TC-TT-API-02-11 (límite de cuerpo, 400/413) **se referencia y no se duplica**: esas pruebas deben seguir en verde sin cambios.

Las pruebas que necesitan un endpoint con validador y uno que lance excepciones de dominio usan controllers de prueba en `tests/IntegrationTest/Support/` (como `TestController`), no endpoints de producción.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-03-01 | Dado un endpoint con un validador de FluentValidation y un cuerpo que lo incumple, cuando se invoca, entonces responde 422 `application/problem+json` (`type`, `title`, `status`) con los campos inválidos, y el controller no se ejecuta (V1). | `IntegrationTest/Validation/ValidationFilterTests.InvalidBody_Returns422ProblemDetailsWithFields_TC_TT_API_03_01` |
| TC-TT-API-03-02 | Dado un cuerpo inválido con valores sensibles (por ejemplo un teléfono o un token en el campo erróneo), cuando se responde el 422, entonces la lista de errores nombra el campo y el motivo pero no devuelve el valor enviado. | `SecurityTest/Validation/ValidationErrorLeakTests.Invalid422_DoesNotEchoSubmittedValues_TC_TT_API_03_02` |
| TC-TT-API-03-03 | Dado `POST /api/v1/auth/firebase` repetido por la misma IP más veces que el límite estricto de `/auth` dentro de su ventana, cuando llega una petición más, entonces responde 429 `application/problem+json` con `Retry-After`, y las anteriores no recibieron 429 (V2). Las peticiones usan cuerpo vacío: no llaman a Firebase. | `IntegrationTest/RateLimiting/AuthRateLimitTests.AuthLimitExceeded_Returns429WithRetryAfter_TC_TT_API_03_03` |
| TC-TT-API-03-04 | Dado el límite global por IP, cuando una IP lo supera en un endpoint que no es `/auth`, entonces recibe 429, otra IP distinta en el mismo momento sigue recibiendo 200, y la ventana siguiente vuelve a aceptar peticiones. | `IntegrationTest/RateLimiting/GlobalRateLimitTests.GlobalLimitPerIp_Returns429OnlyForTheOffender_TC_TT_API_03_04` (reloj con `TimeProvider` falso si el rate limiter lo permite; si no, ver duda 9) |
| TC-TT-API-03-05 | Dado un usuario autenticado, cuando supera el límite por usuario (el `sub` del JWT), entonces recibe 429 y otro usuario con otro `sub` desde la misma IP no. | `IntegrationTest/RateLimiting/UserRateLimitTests.LimitPerSub_DoesNotAffectAnotherUser_TC_TT_API_03_05` (**depende de la duda 3**: qué es «por usuario» y en qué orden va el limitador) |
| TC-TT-API-03-06 | Dado un cliente que no es un proxy de confianza y que envía `X-Forwarded-For` (o `Forwarded`) variable en cada petición, cuando supera el límite, entonces el 429 se aplica igual (la IP de partición es la de la conexión, no la del encabezado); y dado el proxy de confianza configurado, entonces sí se usa la IP reenviada. | `SecurityTest/RateLimiting/ForwardedHeadersEvasionTests.SpoofedForwardedFor_DoesNotEvadeLimit_TC_TT_API_03_06` |
| TC-TT-API-03-07 | Dado un 429, cuando se revisa su cuerpo y encabezados, entonces tiene la misma forma que los demás errores y no revela la IP, la clave de partición, el límite configurado ni el contador. | `SecurityTest/RateLimiting/RateLimitResponseLeakTests.Response429_DoesNotRevealPartitionOrQuota_TC_TT_API_03_07` |
| TC-TT-API-03-08 | Dada una petición sin `X-Correlation-Id`, cuando se responde, entonces la respuesta trae `X-Correlation-Id` con un valor generado (formato válido, distinto en cada petición). | `IntegrationTest/Correlation/CorrelationIdTests.NoHeader_GeneratesAndReturnsId_TC_TT_API_03_08` |
| TC-TT-API-03-09 | Dada una petición con un `X-Correlation-Id` válido (dentro de la longitud y los caracteres permitidos), cuando se responde, entonces la respuesta devuelve el mismo valor. | `IntegrationTest/Correlation/CorrelationIdTests.ValidHeader_IsPropagatedToResponse_TC_TT_API_03_09` |
| TC-TT-API-03-10 | Dada una petición con un `X-Correlation-Id` demasiado largo, con caracteres fuera de la lista permitida, con CR/LF o con forma de JSON, cuando se procesa, entonces se descarta y se reemplaza por uno generado: ni la respuesta ni los logs contienen el valor enviado, y no se parte la línea de log ni los encabezados. | `SecurityTest/Correlation/CorrelationIdInjectionTests.HostileHeader_IsReplaced_NoLogOrHeaderInjection_TC_TT_API_03_10` (longitud y alfabeto: duda 4) |
| TC-TT-API-03-11 | Dada una petición con `X-Correlation-Id`, cuando se capturan los logs de la consola (JSON), entonces cada evento escrito durante esa petición (log de petición, log de un servicio y log de error) trae la propiedad `CorrelationId` con ese valor, y los eventos de otra petición no lo traen (V3). | `IntegrationTest/Logging/CorrelationLoggingTests.EveryLogOfARequest_CarriesCorrelationId_TC_TT_API_03_11` (usa `ConsoleCapture`) |
| TC-TT-API-03-12 | Dadas respuestas producidas por distintas capas (manejador de excepciones 500, límite de cuerpo 413, autenticación 401, rate limiter 429, validación 422), cuando se reciben, entonces todas son `application/problem+json` y traen `X-Correlation-Id`. | `IntegrationTest/Errors/ErrorResponseCorrelationTests.ErrorsFromAnyLayer_CarryProblemJsonAndCorrelationId_TC_TT_API_03_12` |
| TC-TT-API-03-13 | Dada una excepción de dominio por cada código del catálogo (400, 401, 403, 404, 409, 422, 429), cuando la lanza un endpoint, entonces el `IExceptionHandler` responde ese código en ProblemDetails (`status` coherente con el HTTP); una excepción desconocida sigue siendo 500; y el controller no tiene `try/catch`. | `IntegrationTest/Errors/DomainExceptionHandlerTests.DomainException_MapsToCatalogStatus_TC_TT_API_03_13` (Theory) · `UnitTest/Errors/DomainExceptionMappingTests.EachCatalogCode_HasAMapping_TC_TT_API_03_13` |
| TC-TT-API-03-14 | Dado un entorno distinto de Development y una excepción de dominio cuyo mensaje o datos internos contienen valores sensibles o trazas, cuando se responde, entonces el ProblemDetails no incluye `stackTrace`, el mensaje interno ni datos de la excepción (solo el texto del catálogo). Complementa TC-TT-API-01-05, que cubre la excepción no controlada. | `SecurityTest/Errors/DomainErrorDetailTests.NonDevelopment_DomainException_HidesInternals_TC_TT_API_03_14` |
| TC-TT-API-03-15 | Dada una petición con `Authorization: Bearer <token>`, un cuerpo con un token y un celular ficticio, y coordenadas ficticias en la ruta, la query o el cuerpo, cuando se capturan los logs (petición, validación 422, excepción y 429), entonces no aparece ningún token, celular ni coordenada. | `SecurityTest/Logging/SensitiveLogTests.Logs_DoNotContainTokensPhonesOrCoordinates_TC_TT_API_03_15` |
| TC-TT-API-03-16 | Dada la API arrancada, cuando se consulta el orden efectivo del pipeline por comportamiento, entonces coincide con el documentado: la correlación va antes del manejador de excepciones; el 429 se responde antes de autenticar (sin consultar Firebase ni emitir sesión); `Authentication` va antes de `Authorization`. | `IntegrationTest/Hosting/PipelineOrderTests.Pipeline_FollowsDocumentedOrder_TC_TT_API_03_16` (el orden exacto depende de las dudas 2, 3 y 8) |
| TC-TT-API-03-17 | Dados los paquetes nuevos que el revisor apruebe (duda 5) y el documento del orden de middlewares (duda 7), entonces cada paquete está evaluado (necesidad, licencia, mantenimiento, vulnerabilidades) y registrado en `Memoria.md`, `dotnet list package --vulnerable --include-transitive` no reporta vulnerables, el documento existe y su orden coincide con `UseKnockApi`; y no hay secretos (`gitleaks`). | Comandos de existencia y contenido, `dotnet list package` y `gitleaks` (comando); salida en el PR |
| TC-TT-API-03-18 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes` (comando); salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `Hosting/`: middleware de `X-Correlation-Id` (asigna o propaga, valida longitud y alfabeto, lo pone en el `LogContext` de Serilog y en la respuesta); `IExceptionHandler` que traduce excepciones de dominio al catálogo (400, 401, 403, 404, 409, 422, 429) reutilizando `AddKnockProblemDetails` y `ProblemResponses`; filtro de validación con FluentValidation que responde 422 con los campos inválidos; registro del rate limiter con la política global por IP y la estricta para `/auth`, respuesta 429 por `ProblemResponses` con `Retry-After`; `ForwardedHeaders` solo en lo necesario para la IP real (duda 2). Extensiones nuevas en el mismo estilo que `LoggingExtensions`/`AuthenticationExtensions` y `UseKnockApi` actualizado.
  - Tipos mínimos de excepción de dominio (en `Common/`) para poder mapear los códigos del catálogo; nombres y jerarquía por definir al implementar (duda 10).
  - Serilog: reutiliza `AddKnockLogging` y `UseSerilogRequestLogging`; solo se agrega la propiedad `CorrelationId` (no hay otra configuración de Serilog).
  - Valores del límite (permisos y ventanas) y proxies de confianza por configuración (`appsettings*.json` sin secretos), no escritos en el código; los valores concretos son duda 1.
  - Pruebas de los casos 01 a 16 y controllers de prueba en `tests/IntegrationTest/Support/`.
  - Documento del orden de middlewares en `docs/` (no protegido) y, si el revisor lo acepta, `docs/rate-limit-matrix.md` con las dos políticas (duda 7). Nota: `docs/rate-limit-matrix.md`, `docs/endpoints.md` y `docs/events.md` que menciona `AGENTS.md §5` no existen todavía en el repositorio.
  - Paquetes: ninguno se agrega en esta fase de spec. Candidatos de la ficha y su estado (duda 5): `FluentValidation.AspNetCore`, `Microsoft.AspNetCore.RateLimiting` y `Serilog.Enrichers.CorrelationId`. **Requieren evaluación** (necesidad, licencia, mantenimiento, vulnerabilidades) y registro en `Memoria.md` antes de agregarlos.
- **Out:**
  - Observabilidad en Azure (Application Insights, trazas, alertas): TT-OBS-01.
  - Resto del pipeline de la constitución §4: `SecurityHeaders`, HSTS, `HttpsRedirection` y CORS (propuesta; el revisor decide, duda 2).
  - Edición de `constitution.md` (archivo protegido: lo edita el Tech Lead; duda 7).
  - Rate limiting distribuido o con Redis, y separar servicios o crear el gateway (ADR-001, ADR-006; duda 8). No se separa nada hasta que el spike de la base de datos responda.
  - Rotación de refresh token, bloqueo por intentos de código SMS y límites por número de teléfono (HU-001/HU-003 y límite de SMS de ADR-004).
  - Cambiar el comportamiento ya cubierto: ProblemDetails de excepciones no controladas (TC-TT-API-01-04..06), límites de cuerpo 400/413 (TC-TT-API-02-11) y el 401 uniforme (TC-TT-API-02-02..08), salvo lo que decida la duda 6.
  - Contrato vivo (`docs/openapi.v1.json`): no se agregan endpoints; si se documenta 422/429 en OpenAPI se hace con la publicación del contrato (HU-001), duda 11.
  - Datos de negocio, persistencia, eventos, pantallas y cualquier endpoint nuevo de producción.
  - Operar Azure, Docker o producción, y editar `.github/`, `scripts/`, `.agents/` o la constitución.

## 4. Contratos API y eventos
No hay endpoints ni eventos nuevos: la tarea cambia el comportamiento transversal de todos.

**Catálogo de errores** (ProblemDetails, RFC 9457, `application/problem+json`):

| Código | Cuándo | Quién lo produce | Notas |
|---|---|---|---|
| 400 | Petición mal formada o cuerpo ilegible | Excepción de dominio / framework (existente) | `/auth/firebase` hoy responde 400 con DataAnnotations (duda 6). |
| 401 | Sin sesión o sesión inválida | `AuthenticationExtensions` (existente) | Uniforme, sin motivo. |
| 403 | Sin permiso | Excepción de dominio | Sin revelar si el recurso existe más allá de lo que defina cada HU. |
| 404 | No existe (o no es tuyo) | Excepción de dominio | |
| 409 | Estado o concurrencia | Excepción de dominio | Transición inválida de la máquina de estados (constitución §4). |
| 422 | Validación de campos fallida | Filtro de FluentValidation (nuevo) | Cuerpo con los campos inválidos, sin el valor enviado. |
| 429 | Límite superado | Rate limiter (nuevo) | Con `Retry-After`; sin IP, clave ni cuota. |
| 413, 500 | Cuerpo grande / error no controlado | Existentes | Sin cambios. |

Los títulos (`title`) salen de «los mensajes del catálogo» que cita `security-core`; ese catálogo no existe como documento en el repositorio (duda 10): no se inventan textos en esta spec.

**Encabezado `X-Correlation-Id`:**
- Petición (opcional): si trae un valor que cumple la regla de longitud y alfabeto, se propaga; si no, se descarta y se genera uno (propuesta: hasta 64 caracteres de `[A-Za-z0-9._-]`; duda 4).
- Respuesta: siempre `X-Correlation-Id`, también en errores de cualquier capa (caso 12).
- Logs: propiedad `CorrelationId` en cada evento de la petición (caso 11).
- Cuerpo de errores: ProblemDetails ya incluye `traceId` por defecto; si además debe llevar `correlationId` es duda 4.

**Rate limiting:** política global por IP (todo endpoint salvo excepciones que decida el revisor, por ejemplo `/health`, duda 1) y política estricta para `/auth/*`; «por usuario» se define en la duda 3. Respuesta 429 en ProblemDetails con `Retry-After`. Permisos y ventanas: configuración, valores en la duda 1.

**Eventos:** ninguno.

## 5. Modelo de datos
— Sin persistencia: no hay tablas, `DbContext` ni migración. El rate limiter y el contador de la correlación viven en memoria del proceso (ver riesgo de réplicas en §6 y duda 8).

## 6. Seguridad, privacidad y amenazas
STRIDE breve. Toca errores, logs y límites (`security-core`, checks 3, 6, 7, 11 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Suplantación/alteración: el cliente fija `X-Correlation-Id` para confundir la investigación (usar el id de otra petición) o para inyectar líneas o campos en el log (CR/LF, JSON) | Se valida longitud y alfabeto; el valor inválido se descarta y se genera otro; el log es JSON estructurado (la propiedad es un valor, no texto concatenado); el id es solo para rastrear, nunca para autorizar | TC-TT-API-03-10, TC-TT-API-03-09 |
| Alteración: inyección de encabezados de respuesta por el valor reenviado | Mismo filtro; solo se devuelve un valor validado o generado | TC-TT-API-03-10 |
| Suplantación: el cliente evade el límite por IP con `X-Forwarded-For`/`Forwarded` falsos | `ForwardedHeaders` solo desde proxies/redes de confianza configurados (`KnownProxies`/`KnownNetworks`); sin proxy de confianza, se usa la IP de la conexión; sin `ForwardLimit` abierto | TC-TT-API-03-06 |
| Denegación: ráfagas contra endpoints públicos (canje de `/auth/firebase`, fuerza bruta, costo de SMS) | Política estricta para `/auth` y global por IP; 429 antes de autenticar o llamar a Firebase; el cuerpo ya está acotado (TC-TT-API-02-11) | TC-TT-API-03-03, TC-TT-API-03-04, TC-TT-API-03-16 |
| Denegación: un atacante agota el límite de otra persona detrás de la misma IP (NAT) o la apaga a ella | Límite por usuario (`sub`) en endpoints autenticados además del de IP; riesgo residual de falsos positivos por NAT | TC-TT-API-03-05 (duda 3) |
| Denegación/consumo de memoria: demasiadas claves de partición en el limitador (muchas IP o `sub`) | Ventanas cortas que expiran las particiones; sin claves tomadas de datos libres del cliente; verificar al implementar si hace falta cota | Sin caso automático: se anota como riesgo; duda 9 |
| Denegación: el límite efectivo se multiplica con las réplicas | Riesgo conocido: el rate limiter de ASP.NET Core es por proceso, con 2 réplicas el límite efectivo se duplica y el reparto depende del balanceador. No es estado de negocio (el límite es una defensa, no un dato). Sin Redis (ADR-006: no se despliega sin decisión). Mitigación posible: que lo aplique el gateway (ADR-001 regla 3) | Sin caso (no probable con una instancia); duda 8 |
| Fuga: el 429 revela IP, clave de partición, cuota o contador | Cuerpo uniforme de ProblemDetails; solo `Retry-After` | TC-TT-API-03-07 |
| Fuga: stack trace, mensaje interno o datos de la excepción de dominio en la respuesta fuera de Development | El handler escribe solo el texto del catálogo; el detalle de excepción sigue únicamente en Development (`CustomizeProblemDetails` existente) | TC-TT-API-03-14, TC-TT-API-01-05 |
| Fuga: el 422 repite el valor enviado (teléfono, token) o datos de otros campos | La lista de errores lleva campo y motivo, no el valor | TC-TT-API-03-02 |
| Fuga: tokens, celulares o coordenadas en los logs (petición, validación, excepción, 429, query string) | Logs estructurados sin cuerpos ni encabezados; el log de petición registra ruta y no query; las excepciones se registran con su tipo y traza solo en el log del servidor, nunca con datos del usuario en el mensaje | TC-TT-API-03-15 |
| Repudio: sin rastro de qué petición produjo un error | `CorrelationId` en cada log y respuesta; el 429 y el 422 también se registran | TC-TT-API-03-11, TC-TT-API-03-12 |
| Elevación: un error de orden del pipeline deja pasar peticiones sin límite o sin autenticar (p. ej. `RateLimiter` después de la autenticación pesada) | Orden documentado y probado por comportamiento | TC-TT-API-03-16 |
| Cadena de suministro: paquetes nuevos (FluentValidation, rate limiting, enricher) con licencia o vulnerabilidades | Evaluación y registro en `Memoria.md` antes de agregarlos; `NuGetAudit`; `dotnet list package --vulnerable` | TC-TT-API-03-17 |

No aplican aquí: autorización por recurso (sin recursos), cifrado de campo, biométricos, archivos, IA y SSRF.

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Mapa excepción de dominio → código del catálogo y validación del `X-Correlation-Id` (caso 13, parte unitaria), `UnitTest/Errors/`. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests` y `ModuleSurfaceTests` de TT-API-01 siguen en verde sin cambios (lo nuevo vive en `Hosting/` y `Common/`). |
| Integración | Sí | `WebApplicationFactory` (`KnockApiFactory`) con controllers de prueba en `Support/`: casos 01, 03, 04, 05, 08, 09, 11, 12, 13 y 16. Sin Testcontainers: no hay base de datos. Los casos que usan `/auth/firebase` mandan cuerpo vacío para no depender del emulador de Firebase. |
| Seguridad negativa | Sí | Casos 02, 06, 07, 10, 14 y 15 en `tests/SecurityTest`. |
| Contrato | No | No hay endpoints nuevos; OpenAPI de 422/429 se publica con HU-001 (duda 11). |
| Base de datos | No | Sin persistencia. |
| Mutación | No en esta tarea | Sin lógica de dominio; la meta del módulo aplica a los módulos de negocio. Si el revisor lo pide, `Hosting/` con meta propia (duda 12). |
| Rendimiento (k6), fuzzing, caos, E2E, UI | No | Fuera de alcance; el límite se prueba funcionalmente. Una prueba de carga del limitador sería de TT-QA-02/k6 si existe. |
| Regresión | Sí | TC-TT-API-01-04..06 y TC-TT-API-02-02..13 siguen en verde sin editar (los límites nuevos no deben dar 429 a esas suites; ver duda 9). |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit, Integration y Security en verde, `sdd_lint.py --trace TT-API-03`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Evidencia en el PR: salida de `dotnet test` y la de los casos 17 y 18. `docs/endpoints.md`, `docs/events.md` y `docs/rate-limit-matrix.md` del gate no existen aún (duda 7).

**Decisiones previas (de `Memoria.md`):**
1. El pipeline de seguridad de la constitución §4 se dejó fuera de TT-API-01 y TT-API-02 «para otra tarea con su spec» (Memoria, 2026-10-15 y TT-API-02 §3).
2. TT-API-02 dejó el canje `/auth/firebase` sin rate limit solo para correr en local y pidió no desplegar sin él (nota del spike, TC-TT-API-02-16): esta tarea lo cierra.
3. Paquetes nuevos: evaluación y registro previos (docs/FLUJO_EQUIPO.md §2). Serilog se reutiliza.

**Límites y dudas abiertas (para el revisor; no se resuelven aquí):**
1. **Valores del límite.** La ficha no da permisos por ventana ni dónde se excluye `/health`. Propuesta: valores en `appsettings` (sin código); número concreto para global y para `/auth` lo decide el revisor (referencia: `docs/rate-limit-matrix.md`, que no existe; ADR-004 pide límite de intentos por número y alerta de cuota SMS). ¿`/health` y `/health/ready` quedan exentos del límite global?
2. **Qué parte de la constitución §4 entra aquí.** Pipeline de §4: SecurityHeaders → ExceptionHandling → ForwardedHeaders → HSTS → HttpsRedirection → CORS → RateLimiter → Authentication → Authorization → MapControllers. Esta tarea pide rate limiting, así que propongo que entren **RateLimiter** y **ForwardedHeaders** (necesario para la IP real detrás de Container Apps o un proxy) y que queden **fuera** SecurityHeaders, HSTS, HttpsRedirection y CORS (otra tarea con su spec; TT-API-02 dijo lo mismo). ¿Está de acuerdo o prefiere el §4 completo aquí o ForwardedHeaders aparte?
3. **«Por usuario».** Propuesta: el `sub` del JWT, en endpoints autenticados, además del límite por IP. Conflicto de orden: en §4 `RateLimiter` va antes de `Authentication`, de modo que `HttpContext.User` aún no existe cuando se calcula la partición. Opciones: (a) mover `UseRateLimiter` después de `UseAuthentication` para los endpoints protegidos (el 429 llegaría tras validar el JWT, que es barato y local, pero se aparta del orden fijo de §4); (b) leer el `sub` sin validar firma solo para particionar (no recomendable: permite al atacante fabricar claves); (c) solo por IP en esta tarea y por usuario cuando haya sesión (HU-001). ¿Cuál? Los casos 05 y 16 dependen de esto.
4. **Reglas de `X-Correlation-Id`.** Propuesta: máx. 64 caracteres, `[A-Za-z0-9._-]`; generado como GUID sin guiones; propiedad de log `CorrelationId`. ¿Se acepta? ¿Además de `traceId` (que ASP.NET Core ya pone en el ProblemDetails) debe ir `correlationId` en el cuerpo de los errores? ¿`traceId` y `CorrelationId` se unifican (usar el id de `Activity`) o quedan separados? Nota para ADR-001 regla 5: el gateway generará el id y los servicios lo propagarán; aquí solo se propaga el entrante.
5. **Paquetes (no agregar hasta evaluar).** (a) `FluentValidation.AspNetCore`: no consultado en la red; no puedo afirmar si está obsoleto o sin mantenimiento (conocimiento no verificado: su integración automática de MVC se desaconseja desde hace tiempo). Alternativas a evaluar: el paquete base `FluentValidation` (más `FluentValidation.DependencyInjectionExtensions`) con un filtro propio (`IAsyncActionFilter`) que responde 422, o seguir con `DataAnnotations` como TT-API-02 (`SessionDtos`) y mapear `ModelState` a 422 (sin dependencia nueva, pero la ficha pide FluentValidation y `security-core` check 3 también). Decisión del revisor tras evaluar necesidad, licencia, mantenimiento y vulnerabilidades. (b) `Microsoft.AspNetCore.RateLimiting`: verificado que `Microsoft.AspNetCore.RateLimiting.dll` y `System.Threading.RateLimiting.dll` vienen en el runtime compartido `Microsoft.AspNetCore.App` 10.0.10 instalado, por lo que **probablemente no hace falta paquete** (NuGet: no consultado). (c) `Serilog.Enrichers.CorrelationId`: no consultado; como el middleware propio ya empuja la propiedad al `LogContext` con `Enrich.FromLogContext` (existente), se propone **no agregarlo**; el revisor decide. Mientras tanto, ninguno se agrega.
6. **422 frente a 400 en `/auth/firebase`.** Hoy un cuerpo inválido (vacío, sin campo, vacío o > 4096) da 400 (TC-TT-API-02-11, spec aprobada). La ficha dice 422 para validación fallida. ¿Se mantiene 400 en `/auth/firebase` (el 422 aplica a los endpoints nuevos) o pasa a 422 y se enmienda TC-TT-API-02-11? Si cambia, es una modificación de contrato (aviso al frontend) y de una spec aprobada: no la edito.
7. **Documento del orden de middlewares.** La ficha pide documentarlo en la constitución, que es archivo protegido (solo el Tech Lead). Propuesta: documentarlo en un doc no protegido (p. ej. `docs/MIDDLEWARES.md`; nombre por decidir) y el Tech Lead lo traslada a `constitution.md §4` si quiere. También: ¿se crea `docs/rate-limit-matrix.md` (lo citan `AGENTS.md §5` y `security-core` pero no existe) con las dos políticas?
8. **Rate limiting: servicio, gateway o ambos.** ADR-001 regla 3: el gateway termina HTTPS, valida el JWT, aplica el límite de tasa y enruta; los servicios no se exponen. Hoy hay un único `Knock.Api` y el gateway (TT-ARQ-02) no existe; ADR-001 (Consecuencias) manda no separar nada hasta que el spike de la base de datos (ADR-002) responda. Propuesta: el límite de esta tarea vive en `Knock.Api` como defensa en profundidad (también cierra la condición de TT-API-02) y no se mueve ni se cambia nada del gateway; cuando exista el gateway, se decide si el límite se duplica allí o se retira del servicio. Riesgo: el limitador es por proceso (2 réplicas duplican el límite efectivo; ADR-006 exige ≥ 2 instancias) y no se propone Redis; si el revisor considera que eso no basta, el límite real iría al gateway. ¿Confirma?
9. **Pruebas del limitador.** Si el rate limiter de ASP.NET Core no usa `TimeProvider` falso, los casos 04 y 05 necesitan una ventana corta configurable en pruebas o esperar tiempo real. También: las suites existentes (muchas peticiones a `/auth/firebase` y a `/me`) no deben recibir 429 por el límite nuevo; los valores en `KnockApiFactory` o por configuración de pruebas deben evitarlo sin desactivar el limitador en producción. Se resuelve al implementar; avisar si hace falta tocar la fábrica.
10. **Excepciones de dominio y catálogo.** No hay hoy un tipo base ni un documento de «catálogo de errores» (lo cita `security-core` check 11). Propuesta mínima: un tipo base con un código del catálogo, el mapa a 400/401/403/404/409/422/429 y títulos genéricos del estándar HTTP; el revisor define si quiere textos propios. ¿Los códigos 401/429 lanzados como excepción de dominio tienen sentido (hoy los producen autenticación y rate limiter)? Se incluyen porque la ficha los lista.
11. **OpenAPI.** ¿Se documentan 422/429/`X-Correlation-Id` en OpenAPI (Scalar) en esta tarea o con HU-001? Propuesta: HU-001 (igual que TT-API-02), salvo que el revisor pida lo contrario.
12. **Mutación.** ¿Se fija una meta para `Hosting/` (no es módulo de negocio) o queda sin meta como en TT-API-02?

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
