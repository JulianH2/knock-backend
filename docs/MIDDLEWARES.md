# Orden de middlewares de la API

> TT-API-03 (KNOCK-37). Fuente de verdad del orden: `UseKnockApi` en `src/Knock.Api/Hosting/KnockApplicationExtensions.cs`.
> La prueba `PipelineOrderTests.DocumentedOrder_MatchesUseKnockApi_TC_TT_API_03_16` compara esta lista con el código: si cambias uno, cambia el otro.
> El revisor traslada este orden a la constitución §4. TT-API-06 (KNOCK-138) completó las capas de seguridad: SecurityHeaders, HSTS, HttpsRedirection y CORS.

## Orden efectivo

Cada línea empieza con el nombre exacto de la llamada en `UseKnockApi` (para `UseMiddleware<T>` es el tipo `T`).

1. `UseForwardedHeaders` — sustituye la IP de la conexión por la de `X-Forwarded-For`, y el esquema por el de `X-Forwarded-Proto`, solo si la petición viene de un proxy o red de confianza configurados. No procesa `X-Forwarded-Host`.
2. `CorrelationIdMiddleware` — asigna o propaga `X-Correlation-Id`, lo agrega a cada log (`LogContext`) y a la respuesta.
3. `SecurityHeadersMiddleware` — fija en `OnStarting` los encabezados de seguridad de toda respuesta (también las de error y el 429), `Cache-Control: no-store` si el endpoint no define el suyo, y quita `Server`, `X-Powered-By` y `X-AspNet-Version`.
4. `AuthNoStoreMiddleware` — `Cache-Control: no-store` en toda respuesta de `/api/v{n}/auth/*` (tokens de sesión), también en errores (HU-001).
5. `UseExceptionHandler` — convierte excepciones en ProblemDetails (excepciones de dominio a 400, 403, 404, 409 y 422, con `code` estable si la excepción lo declara; lo demás, 500).
6. `UseSerilogRequestLogging` — un log por petición con la plantilla de la ruta (nunca la ruta con valores ni la query).
7. `UseHsts` — `Strict-Transport-Security` solo fuera de Development y solo si la petición es HTTPS (nunca a `localhost`).
8. `UseHttpsRedirection` — 308 a HTTPS solo fuera de Development y solo si hay `Security:Https:RedirectPort`.
9. `CorsMiddleware` — CORS por configuración (`Security:Cors`); responde el preflight 204 antes del limitador y aplica los encabezados en `OnStarting`.
10. `UseRateLimiter` — límite por IP (global, estricto para `/auth` y propio para `/health/ready`); responde 429 antes de autenticar.
11. `RequestBodyLimitMiddleware` — aplica `[RequestSizeLimit]` y responde 413.
12. `UseAuthentication` — JWT propio; 401 uniforme.
13. `UseAuthorization` — políticas y `[Authorize]`.
14. `MapKnockHealth` — `/health` y `/health/ready`.
15. `MapKnockOpenApi` — OpenAPI y Scalar (según entorno).
16. `MapControllers` — endpoints de los módulos.

## Por qué en este orden

- **Reenvío de IP y esquema primero (1):** el limitador, los logs, HSTS y la redirección deben ver la IP y el esquema reales. Sin proxies de confianza configurados no se confía en nadie y se usa la conexión.
- **Correlación antes del manejador de excepciones (2 antes de 5):** el id debe existir cuando se escribe el error y en el log del error.
- **Encabezados de seguridad antes del manejador y del limitador (3 antes de 5 y de 10):** se fijan en `OnStarting`, así que cubren el 500 (el manejador limpia los encabezados) y el 429 (el limitador responde antes que el resto).
- **`Cache-Control: no-store` en `/auth/*` (4):** va justo después de los encabezados de seguridad y antes del manejador, y se fija en `OnStarting`, así que también cubre los 4xx, el 413 y el 429 (HU-001).
- **Manejador de excepciones antes del registro de peticiones (5 antes de 6):** el manejador captura lo que cruza el registro.
- **Registro antes del limitador (6 antes de 10):** el 429 también queda registrado.
- **HSTS y redirección antes de CORS y del limitador (7 y 8 antes de 9 y 10):** la redirección a HTTPS no cuenta para el límite y HSTS ya va en la respuesta; ambos dependen del esquema que dejó el reenvío (1).
- **CORS antes del limitador (9 antes de 10):** el 429 también lleva los encabezados CORS, para que el panel pueda leerlo, y el preflight (204) se responde sin pasar por el límite ni por la autenticación. Riesgo aceptado: el preflight es una respuesta barata que el limitador no cuenta.
- **Limitador antes de autenticar (10 antes de 12):** un 429 no consulta Firebase ni emite sesión; el costo de una ráfaga se corta antes del trabajo caro.
- **Límite de cuerpo antes de leer el cuerpo (11):** el 413 se responde antes de enlazar el cuerpo.
- **Autenticación antes que autorización (12 antes de 13).**

## Respuestas que cada capa produce

Todas son `application/problem+json` (salvo el 308 y el 204 de abajo), llevan el encabezado `X-Correlation-Id`, los encabezados de seguridad de `docs/security-headers.md` y los campos `traceId` y `correlationId` (independientes) en el cuerpo. Formatos (contrato en la §4 de la spec TT-OBS-01): `X-Correlation-Id` y `correlationId` son el mismo valor, el recibido si es válido (1 a 64 caracteres de `[A-Za-z0-9._-]`) o uno generado de 32 hex; `traceId` es W3C `00-<trace-id de 32 hex>-<span-id de 16 hex>-<banderas>`. El encabezado `X-Correlation-Id` también va en las respuestas de éxito; la API no devuelve `traceparent` ni `traceresponse`.

| Capa | Códigos |
|---|---|
| Redirección a HTTPS | 308 con `Location` (solo si hay `Security:Https:RedirectPort`; sin cuerpo) |
| CORS | 204 al preflight de un origen permitido (sin cuerpo) |
| Limitador | 429 con `Retry-After` |
| Límite de cuerpo | 413 |
| Autenticación | 401 |
| Filtro de validación (MVC) | 422 con `errors` |
| Manejador de excepciones | 400, 403, 404, 409, 422 (dominio, con `code` estable si lo declara) y 500 |

## Configuración

- `RateLimiting:*` (incluida la política propia de `/health/ready`): ver `docs/rate-limit-matrix.md`.
- `Security:Hsts`, `Security:Https:RedirectPort` y `Security:Cors` (orígenes permitidos vacíos en el repositorio): ver `docs/security-headers.md`.
- `ForwardedHeaders:KnownProxies` (lista de IP) y `ForwardedHeaders:KnownNetworks` (lista de CIDR): **vacíos en el repositorio**. Los valores de producción (redes de Azure Container Apps) los define una persona al desplegar; no se infieren aquí.
