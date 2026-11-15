# Matriz de límites de peticiones

> TT-API-03 (KNOCK-37). Los valores salen de `src/Knock.Api/appsettings.json` (sección `RateLimiting`), sin secretos, y se afinan con el k6 de TT-QA-02.
> Orden en el pipeline: `docs/MIDDLEWARES.md`. Implementación: `src/Knock.Api/Hosting/RateLimitingExtensions.cs`.

## Políticas por IP

| Política | Alcance | Límite (punto de partida) | Ventana | Clave de partición |
|---|---|---|---|---|
| Global | Cualquier ruta salvo `/health` y `/health/ready` | 100 peticiones | 1 minuto (fija) | IP del cliente |
| Propia `/health/ready` | Solo `/health/ready` (TT-API-06) | 60 peticiones | 1 minuto (fija) | IP del cliente |
| Estricta `/auth` | `/api/v{n}/auth/*` | 10 peticiones | 1 minuto (fija) | IP del cliente |

**Mensajería** (`Knock.Messaging.Api`, BUG-004 / KNOCK-241) aplica el mismo mecanismo por IP con dos políticas, sin la estricta de `/auth` (no tiene rutas de sesión): **Global** 100 peticiones por minuto (cualquier ruta salvo `/health` y `/health/ready`) y **propia de `/health/ready`** 60 por minuto, configuradas en `RateLimiting:Global` y `RateLimiting:Ready` de su `appsettings.json`. Responde `429` con `Retry-After`. El chat de 30 mensajes por minuto por persona lo define la spec 026. **Límite conocido (aceptado):** el limitador vive en la memoria de cada réplica, así que con 2 réplicas el límite efectivo se duplica, igual que en Identidad y Mercado (se mueve a Redis en el Sprint 5). Además, la IP del cliente solo es la real cuando `ForwardedHeaders` confía en la red del ingreso (`ForwardedHeaders:KnownNetworks`, del #61, KNOCK-201, que Mensajería ya usa): si el despliegue no la fija, Mensajería ve la IP del proxy y el límite se comparte entre todos los clientes. La prueba de «otra IP no se ve afectada» (TC-BUG-004-04) fija la IP con un encabezado de prueba, no de punta a punta.

- Las peticiones a `/auth/*` cuentan en las dos políticas (la estricta y la global).
- `/health` (sin comprobaciones) es la **única exención** del límite. `/health/ready` hace un ping a MongoDB por petición anónima, así que desde TT-API-06 (KNOCK-138) tiene su propia política por IP (60 por minuto) y no cuenta en la global.
- La prueba `PublicEndpointsRateLimitTests` (TC-TT-API-06-16) enumera los endpoints públicos de la API y exige 429 en todos salvo `/health`; un endpoint público nuevo hace fallar la prueba hasta que se documente. Hoy son `/health`, `/health/ready`, `POST /api/v1/auth/firebase`, `GET /api/v1/oficios` y, solo en Development, `/openapi/v1.json` y `/scalar`.
- Respuesta al superar el límite: `429` `application/problem+json` con `Retry-After` (segundos); el cuerpo es el mismo de los demás errores (`type`, `title`, `status`, `traceId`, `correlationId`) y **no** revela IP, clave de partición, límite ni contador.
- Los valores se configuran con `RateLimiting:Global:PermitLimit`, `RateLimiting:Global:Window`, `RateLimiting:Auth:PermitLimit`, `RateLimiting:Auth:Window`, `RateLimiting:Ready:PermitLimit` y `RateLimiting:Ready:Window` (`hh:mm:ss`). No hay valores por defecto en código: sin configuración válida la API no arranca.

## IP del cliente

- Sin proxies de confianza configurados se usa la IP de la conexión; `X-Forwarded-For` falso no mueve al cliente de partición. El middleware de ASP.NET Core procesa `X-Forwarded-For` y `X-Forwarded-Proto` (este último desde TT-API-06, solo de proxies de confianza; `X-Forwarded-Host` no); el encabezado `Forwarded` (RFC 7239) se **ignora**.
- Con `ForwardedHeaders:KnownProxies` o `ForwardedHeaders:KnownNetworks` configurados, solo las peticiones que llegan desde esas direcciones sustituyen la IP por la de `X-Forwarded-For`. El repositorio no trae valores de producción: una persona los define al desplegar. Mal configurado deja evadir el límite o hace que todos compartan la IP del proxy.
- IPv4 mapeada en IPv6 se normaliza a IPv4 y las direcciones IPv6 se agrupan por prefijo /64.

## Límites conocidos (aceptados)

- **Solo por IP, no por usuario.** Personas detrás de la misma IP (NAT) comparten el contador. El límite por usuario lo hará el gateway (TT-ARQ-02, KNOCK-154), que valida el JWT. El caso TC-TT-API-03-05 comprueba que hoy el límite no distingue por usuario.
- **Por proceso, sin Redis (ADR-006).** El limitador vive en la memoria de cada réplica. **Con 2 réplicas el límite efectivo se duplica** (hasta 200 por minuto por IP en el global y 20 en `/auth`, según cómo reparta la carga el balanceador). Es defensa en profundidad, no estado de negocio; el gateway podrá aplicar el límite global (ADR-001, regla 3).
- Memoria: cada IP distinta crea una partición que expira con su ventana; la clave es la IP validada, no datos libres del cliente.
- Fuera de alcance de esta matriz: bloqueo por intentos de código SMS y límites por número de teléfono (HU-001/HU-003, ADR-004) y chat 60/min por usuario (Messaging).

## Advertencias para el despliegue (TT-AZ)
- **Detrás de Container Apps la IP de conexión es la del proxy de entrada.** Sin `ForwardedHeaders:KnownProxies` o `ForwardedHeaders:KnownNetworks` configurados, todos los clientes compartirían la misma partición (100 por minuto global y 10 por minuto en `/auth`): una denegación de servicio autoinfligida. El despliegue (Terraform, ADR-003) debe fijar la red del ingress, por ejemplo `ForwardedHeaders__KnownNetworks__0=<CIDR del ingress>`.
- **No usar `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`:** haría que el host confiara en cualquier remitente y permitiría evadir el límite con `X-Forwarded-For`. El Dockerfile no la define y no debe definirse.
- Un valor de red demasiado amplio también permite evadir el límite: usar el CIDR mínimo del ingress.

- `/api/v1/colonias/en-punto` va bajo la política global por IP sin exención (TT-API-07, KNOCK-141).

## Lista de espera en Mercado (spec 023, KNOCK-47)

`GET /api/v1/zonas/cp/{cp}` requiere sesión y usa los límites globales del gateway por IP y usuario y el límite por IP de Mercado; no tiene política específica adicional.

`POST /api/v1/zonas/lista-espera` tiene además una política por `sub` del JWT de **5 peticiones por hora**, ventana fija, por proceso; la respuesta 429 incluye `Retry-After`. Con una réplica el límite es de 5; con dos puede alcanzar 10 por hora. El límite compartido en Redis queda para KNOCK-247. Se mantienen los límites globales del gateway y el límite por IP de Mercado.

## Cotizaciones en Mercado (spec 020, KNOCK-74)

`POST /api/v1/solicitudes/{id}/cotizaciones` añade una ventana fija de **3 peticiones por minuto por `sub`** y por proceso, además de los límites generales. La cuarta responde 429 con `Retry-After`. Con dos réplicas pueden pasar hasta 6 por minuto; el límite compartido queda para KNOCK-247.

## Gateway (spec 009, KNOCK-204)

El gateway limita con contadores de ventana fija en Redis (`ratelimit:ip:*`, `ratelimit:auth:*`, `ratelimit:user:*`), compartidos entre réplicas, a diferencia del limitador en memoria de los servicios.

| Política | Alcance | Límite (punto de partida) | Ventana | Clave |
|---|---|---|---|---|
| IP | Toda ruta salvo `/health`, antes de autenticar | 100 peticiones | 1 minuto | IP del cliente (IPv6 por /64) |
| Estricta `/auth` | `/api/v{n}/auth/*`, además de la de IP | 10 peticiones | 1 minuto | IP del cliente |
| Usuario | Peticiones con sesión, después de autenticar | 120 peticiones | 1 minuto | `sub` del JWT |
| `busqueda` (**pendiente T-06**, Guillermo) | `GET /api/v1/busqueda`, además de IP y usuario | 30 peticiones | 1 minuto | `sub` del JWT |

- Configuración: `RateLimiting:Ip`, `RateLimiting:Auth` y `RateLimiting:User` (`PermitLimit` y `Window`), sin valores por defecto en código.
- 429 en `application/problem+json` con `Retry-After` en segundos (mínimo 1); el cuerpo no revela IP, clave ni contador.
- Si Redis no responde, el gateway responde 503 con `Retry-After: 5`: sin contadores no se puede limitar.
