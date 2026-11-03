# Encabezados de seguridad, HSTS, HTTPS y CORS

> TT-API-06 (KNOCK-138). Implementación en `src/Knock.Api/Hosting/` (`SecurityHeadersMiddleware`, `CorsMiddleware`, `SecurityExtensions`, `SecuritySettings`). Orden en el pipeline: `docs/MIDDLEWARES.md`. Límites de peticiones: `docs/rate-limit-matrix.md`.

## Encabezados de toda respuesta

Se fijan en `Response.OnStarting` y por asignación (no `Add`): el manejador de excepciones limpia los encabezados antes de escribir el error y el limitador responde antes que el resto del pipeline, así que los llevan **todas** las respuestas (200, 401, 404, 413, 422, 429, 500, `/health` y `/health/ready`) y ninguno sale duplicado.

| Encabezado | Valor | Cuándo | Por qué |
|---|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Siempre | El navegador no adivina el tipo del JSON (OWASP A05). |
| `X-Frame-Options` | `DENY` | Siempre | Clickjacking en navegadores sin CSP. |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | Siempre; en Development no se aplica a `/scalar` ni a `/openapi` | Una API JSON no carga recursos ni se enmarca. Scalar sirve HTML y scripts y la CSP estricta lo rompería. |
| `Referrer-Policy` | `no-referrer` | Siempre | No filtra la URL de la API a otros sitios. |
| `Cache-Control` | `no-store` | Solo si el endpoint no define el suyo | Perfil, enlaces SAS y datos personales no se guardan en cachés. Un endpoint con `ETag` declara el suyo (se respeta y no se duplica). |
| `Strict-Transport-Security` | `max-age=<Security:Hsts:MaxAge>` (+ `includeSubDomains` según `Security:Hsts:IncludeSubDomains`), sin `preload` | Solo HTTPS y fuera de Development; nunca a `localhost` | Evita el downgrade. Sin `preload`: es irreversible en la práctica y el dominio es temporal. |
| `Server`, `X-Powered-By`, `X-AspNet-Version` | ausentes | Siempre | No revelar el servidor ni la versión. Kestrel arranca con `AddServerHeader = false` y el middleware retira los otros dos por defensa. |

No se incluyen `Permissions-Policy`, `X-XSS-Protection` ni `Cross-Origin-*`: no tienen efecto en respuestas JSON (o el auditor XSS heredado es contraproducente) y darían una falsa sensación de control.

## HTTPS detrás del ingress

- En Container Apps el TLS termina en el ingress: la API recibe HTTP interno con `X-Forwarded-Proto: https`. `ForwardedHeaders` procesa `X-Forwarded-For` y `X-Forwarded-Proto` **solo** de los proxies y redes de `ForwardedHeaders:KnownProxies` y `ForwardedHeaders:KnownNetworks`; sin ellos el reenvío queda en `None` (nadie es de confianza) y no se procesa `X-Forwarded-Host` ni el encabezado `Forwarded`.
- **HSTS** (`UseHsts`) solo se emite si la petición es HTTPS (por el esquema reenviado) y fuera de Development.
- **Redirección** (`UseHttpsRedirection`) es opcional: solo existe si `Security:Https:RedirectPort` está configurado y fuera de Development; responde 308 conservando ruta y query. En Container Apps **no se configura**: es el ingress (`allowInsecure=false`, TT-AZ-02) quien exige HTTPS. Con el proxy de confianza y `X-Forwarded-Proto: https` la API ve HTTPS y no redirige (sin bucle).
- **Riesgo aceptado:** redirigir un `POST` por HTTP no protege el cuerpo que ya viajó en claro. El control real es el ingress; la redirección es defensa en profundidad.

## CORS

- Una sola política, por configuración. Lista vacía = **CORS apagado** (la app móvil y los servicios internos no usan CORS; solo el panel de Angular lo necesita si se sirve desde otro origen).
- `Security:Cors:AllowedOrigins` (lista) con comparación **exacta** de esquema, host y puerto: sin comodín, sin `Origin: null`, sin subdominios, prefijos ni sufijos. Se rechazan al arrancar el comodín, las entradas vacías, rutas, query, fragmento, credenciales en la URL, esquemas distintos de `https` (salvo `http` en hosts loopback), mayúsculas distintas de la forma canónica y la barra final.
- Sin `Access-Control-Allow-Credentials` (el JWT va en `Authorization`, no en cookies).
- Métodos `GET, POST, PUT, PATCH, DELETE`; encabezados permitidos `Authorization`, `Content-Type`, `X-Correlation-Id`, `Idempotency-Key`, `If-Match`; expuestos `X-Correlation-Id`, `Retry-After`, `ETag`; preflight cacheado según `Security:Cors:PreflightMaxAge` (10 minutos).
- El preflight (`OPTIONS` con `Origin` permitido y `Access-Control-Request-Method`) se responde 204 en el propio middleware, antes del limitador y sin enrutar: no pide sesión y una ruta que solo acepta `POST` no da 405. Un método o encabezado fuera de las listas no se concede.
- Los encabezados CORS de la respuesta se fijan en `OnStarting`, así que el panel también puede leer el `ProblemDetails` de un 401, 422, 429 o 500.
- **Riesgo aceptado:** el preflight no cuenta para el límite (el orden de la constitución §4 pone CORS antes del limitador) y es una respuesta barata. El gateway podrá limitarlo.
- **Encabezados nuevos del panel.** El preflight concede todo o nada: si el panel agrega un encabezado que no está en la lista (por ejemplo de trazas, como `traceparent`, `tracestate`, `sentry-trace` o `baggage`), el preflight responde 204 sin `Access-Control-Allow-Origin` y **fallan todas** las llamadas, no solo esa. Un encabezado nuevo del panel exige actualizar `AllowedHeaders` en `CorsMiddleware` en el mismo PR que lo agrega; hoy no se amplía la lista porque el panel no los envía (revisión de pares de Erick, #94).
- **Origen del panel en Development.** `appsettings.Development.json` trae `http://localhost:4200` (el puerto por defecto de `ng serve`) para que el panel local llame a la API sin configurar nada; en Production la lista queda vacía y el origen real se inyecta por variable de entorno.
- Está escrito a mano y no con `UseCors` por esas dos razones (preflight sin enrutar y encabezados que sobreviven al manejador de excepciones).

## Configuración

Sin valores por defecto en código; una configuración inválida impide el arranque y el mensaje nombra la clave (nunca el valor).

| Clave | Valor en el repositorio | Notas |
|---|---|---|
| `Security:Hsts:MaxAge` | `365.00:00:00` | Debe ser > 0. |
| `Security:Hsts:IncludeSubDomains` | `true` | |
| `Security:Https:RedirectPort` | (ausente) | 1 a 65535. Ausente = no se redirige. |
| `Security:Cors:AllowedOrigins` | `[]` | **Vacía en el repositorio**; nunca un dominio real. |
| `Security:Cors:PreflightMaxAge` | `00:10:00` | Debe ser > 0 si hay orígenes. |
| `RateLimiting:Ready:PermitLimit` y `RateLimiting:Ready:Window` | `60` por `00:01:00` | Política propia de `/health/ready`. |

Los valores reales los inyecta quien despliega por variables de entorno, por ejemplo `Security__Cors__AllowedOrigins__0=<origen del panel>`, `Security__Hsts__MaxAge` y `Security__Https__RedirectPort`. El origen del panel, las redes del ingress (`ForwardedHeaders__KnownNetworks__0`) y `allowInsecure=false` se fijan en TT-AZ-02; no se infieren aquí. `appsettings.Development.json` solo trae el origen local del panel (`http://localhost:4200`); `appsettings.json` trae la lista vacía.

## Reparto entre servicio y gateway (no implementado aquí)

- **Cada servicio** (incluido `Knock.Api`): encabezados de seguridad y `Server` apagado, `ForwardedHeaders` confiando en la red del gateway, CORS **apagado** y HTTPS/HSTS sin efecto (el tráfico interno es HTTP).
- **Gateway (TT-ARQ-02):** única entrada pública. Termina HTTPS, aplica HSTS y CORS con el origen del panel, **sobrescribe** (no suma) los encabezados de seguridad que traiga la respuesta del servicio, retira `Server` y `X-Powered-By` y limita por IP y por usuario.

## Riesgos y pendientes

- `AllowedHosts: "*"` (filtrado del encabezado `Host`) queda fuera de esta tarea; se fija por variable de entorno al desplegar o en una tarea aparte.
- Con varias réplicas el limitador vive en la memoria de cada una (ver `docs/rate-limit-matrix.md`).
- El `curl -I` contra la API en ejecución (el `TestServer` no emite `Server`) va como evidencia en el PR.
