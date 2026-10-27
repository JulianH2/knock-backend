# Orden de middlewares de la API

> TT-API-03 (KNOCK-37). Fuente de verdad del orden: `UseKnockApi` en `src/Knock.Api/Hosting/KnockApplicationExtensions.cs`.
> La prueba `PipelineOrderTests.DocumentedOrder_MatchesUseKnockApi_TC_TT_API_03_16` compara esta lista con el código: si cambias uno, cambia el otro.
> El revisor traslada este orden a la constitución §4. Quedan para TT-API-06 (KNOCK-138): SecurityHeaders, HSTS, HttpsRedirection y CORS.

## Orden efectivo

Cada línea empieza con el nombre exacto de la llamada en `UseKnockApi` (para `UseMiddleware<T>` es el tipo `T`).

1. `UseForwardedHeaders` — sustituye la IP de la conexión por la de `X-Forwarded-For` solo si la petición viene de un proxy o red de confianza configurados.
2. `CorrelationIdMiddleware` — asigna o propaga `X-Correlation-Id`, lo agrega a cada log (`LogContext`) y a la respuesta.
3. `UseExceptionHandler` — convierte excepciones en ProblemDetails (excepciones de dominio a 400, 403, 404, 409 y 422; lo demás, 500).
4. `UseSerilogRequestLogging` — un log por petición con la plantilla de la ruta (nunca la ruta con valores ni la query).
5. `UseRateLimiter` — límite por IP (global y estricto para `/auth`); responde 429 antes de autenticar.
6. `RequestBodyLimitMiddleware` — aplica `[RequestSizeLimit]` y responde 413.
7. `UseAuthentication` — JWT propio; 401 uniforme.
8. `UseAuthorization` — políticas y `[Authorize]`.
9. `MapKnockHealth` — `/health` y `/health/ready`.
10. `MapKnockOpenApi` — OpenAPI y Scalar (según entorno).
11. `MapControllers` — endpoints de los módulos.

## Por qué en este orden

- **Reenvío de IP primero (1):** el limitador y los logs deben ver la IP real. Sin proxies de confianza configurados no se confía en nadie y se usa la IP de la conexión.
- **Correlación antes del manejador de excepciones (2 antes de 3):** el id debe existir cuando se escribe el error y en el log del error.
- **Manejador de excepciones antes del registro de peticiones (3 antes de 4):** el manejador captura lo que cruza el registro.
- **Registro antes del limitador (4 antes de 5):** el 429 también queda registrado.
- **Limitador antes de autenticar (5 antes de 7):** un 429 no consulta Firebase ni emite sesión; el costo de una ráfaga se corta antes del trabajo caro.
- **Límite de cuerpo antes de leer el cuerpo (6):** el 413 se responde antes de enlazar el cuerpo.
- **Autenticación antes que autorización (7 antes de 8).**

## Respuestas que cada capa produce

Todas son `application/problem+json`, llevan el encabezado `X-Correlation-Id` y los campos `traceId` y `correlationId` (independientes) en el cuerpo.

| Capa | Códigos |
|---|---|
| Limitador | 429 con `Retry-After` |
| Límite de cuerpo | 413 |
| Autenticación | 401 |
| Filtro de validación (MVC) | 422 con `errors` |
| Manejador de excepciones | 400, 403, 404, 409, 422 (dominio) y 500 |

## Configuración

- `RateLimiting:*`: ver `docs/rate-limit-matrix.md`.
- `ForwardedHeaders:KnownProxies` (lista de IP) y `ForwardedHeaders:KnownNetworks` (lista de CIDR): **vacíos en el repositorio**. Los valores de producción (redes de Azure Container Apps) los define una persona al desplegar; no se infieren aquí.
