# Matriz de límites de peticiones

> TT-API-03 (KNOCK-37). Los valores salen de `src/Knock.Api/appsettings.json` (sección `RateLimiting`), sin secretos, y se afinan con el k6 de TT-QA-02.
> Orden en el pipeline: `docs/MIDDLEWARES.md`. Implementación: `src/Knock.Api/Hosting/RateLimitingExtensions.cs`.

## Políticas (solo por IP)

| Política | Alcance | Límite (punto de partida) | Ventana | Clave de partición |
|---|---|---|---|---|
| Global | Cualquier ruta salvo `/health` y `/health/ready` | 100 peticiones | 1 minuto (fija) | IP del cliente |
| Estricta `/auth` | `/api/v{n}/auth/*` | 10 peticiones | 1 minuto (fija) | IP del cliente |

- Las peticiones a `/auth/*` cuentan en las dos políticas (la estricta y la global).
- `/health` y `/health/ready` están **exentos del global** porque no hacen trabajo costoso hoy. Si `/health/ready` empieza a consultar la base de datos de forma pesada, la exención se revisa.
- Respuesta al superar el límite: `429` `application/problem+json` con `Retry-After` (segundos); el cuerpo es el mismo de los demás errores (`type`, `title`, `status`, `traceId`, `correlationId`) y **no** revela IP, clave de partición, límite ni contador.
- Los valores se configuran con `RateLimiting:Global:PermitLimit`, `RateLimiting:Global:Window`, `RateLimiting:Auth:PermitLimit` y `RateLimiting:Auth:Window` (`hh:mm:ss`). No hay valores por defecto en código: sin configuración válida la API no arranca.

## IP del cliente

- Sin proxies de confianza configurados se usa la IP de la conexión; `X-Forwarded-For` falso no mueve al cliente de partición. El middleware de ASP.NET Core solo procesa `X-Forwarded-For` (y proto/host si se habilitan); el encabezado `Forwarded` (RFC 7239) se **ignora**.
- Con `ForwardedHeaders:KnownProxies` o `ForwardedHeaders:KnownNetworks` configurados, solo las peticiones que llegan desde esas direcciones sustituyen la IP por la de `X-Forwarded-For`. El repositorio no trae valores de producción: una persona los define al desplegar. Mal configurado deja evadir el límite o hace que todos compartan la IP del proxy.
- IPv4 mapeada en IPv6 se normaliza a IPv4 y las direcciones IPv6 se agrupan por prefijo /64.

## Límites conocidos (aceptados)

- **Solo por IP, no por usuario.** Personas detrás de la misma IP (NAT) comparten el contador. El límite por usuario lo hará el gateway (TT-ARQ-02, KNOCK-154), que valida el JWT. El caso TC-TT-API-03-05 comprueba que hoy el límite no distingue por usuario.
- **Por proceso, sin Redis (ADR-006).** El limitador vive en la memoria de cada réplica. **Con 2 réplicas el límite efectivo se duplica** (hasta 200 por minuto por IP en el global y 20 en `/auth`, según cómo reparta la carga el balanceador). Es defensa en profundidad, no estado de negocio; el gateway podrá aplicar el límite global (ADR-001, regla 3).
- Memoria: cada IP distinta crea una partición que expira con su ventana; la clave es la IP validada, no datos libres del cliente.
- Fuera de alcance de esta matriz: bloqueo por intentos de código SMS y límites por número de teléfono (HU-001/HU-003, ADR-004) y chat 60/min por usuario (Messaging).

## Advertencias para el despliegue (TT-AZ)
- **Detrás de Container Apps la IP de conexión es la del proxy de entrada.** Sin `ForwardedHeaders:KnownProxies` o `ForwardedHeaders:KnownNetworks` configurados, todos los clientes compartirían la misma partición (100 por minuto global y 10 por minuto en `/auth`): una denegación de servicio autoinfligida. El despliegue (Bicep) debe fijar la red del ingress, por ejemplo `ForwardedHeaders__KnownNetworks__0=<CIDR del ingress>`.
- **No usar `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`:** haría que el host confiara en cualquier remitente y permitiría evadir el límite con `X-Forwarded-For`. El Dockerfile no la define y no debe definirse.
- Un valor de red demasiado amplio también permite evadir el límite: usar el CIDR mínimo del ingress.
