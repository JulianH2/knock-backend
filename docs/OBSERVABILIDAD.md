# Observabilidad de la API (TT-OBS-01, KNOCK-51)

La API emite trazas, métricas y logs con OpenTelemetry en todos los entornos. Solo los exporta a Application Insights cuando existe la variable `APPLICATIONINSIGHTS_CONNECTION_STRING`. Código: `src/Knock.Api/Hosting/ObservabilityExtensions.cs` y `TelemetryRedactor.cs`. Spec: `specs/TT-OBS-01-observabilidad-application-insights-y-sentry/spec.md`.

## Configuración

| Clave | Dónde | Valor |
|---|---|---|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Variable de entorno; en Container Apps, referencia a un secreto de Key Vault | Cadena del recurso. **Nunca** en `appsettings*.json`, en el repositorio, en logs ni en el PR. Sin valor (Development, pruebas) no se exporta nada. |
| `Observability:ServiceName` | `appsettings.json` | `knock-api` (`service.name`, el *cloud role* de Application Insights) |
| `Observability:SamplingRatio` | `appsettings.json` | `1.0`: 100 % de las trazas; el gasto lo limita el tope diario del recurso (decisión P7) |
| `Observability:EnableLiveMetrics` | `appsettings.json` | `false` (decisión P7) |

El exportador trae por defecto un muestreo de 5 trazas por segundo. El código lo quita (`TracesPerSecond = null`) para que se aplique `SamplingRatio`. También apaga el almacenamiento local de reintentos (`DisableOfflineStorage`), así que no escribe telemetría en el disco del contenedor.

## Qué se envía y qué no

| Señal | Se envía | No se envía |
|---|---|---|
| Spans de servidor | Método, `http.route` (plantilla), `url.path` con la plantilla de `LoggingExtensions.RoutePath` (`/api/v1/test/at/{latitude}/{longitude}`), código de respuesta, `knock.correlation_id` y el evento `exception` con tipo, mensaje y pila redactados | `url.query`, encabezados (`Authorization`, `Cookie`), cuerpos, spans de `/health` y `/health/ready` |
| Spans de cliente (`HttpClient`) | Destino, método, código y `url.full` con los valores de la query como `Redacted` | Valores de la query (`sig` de una URL SAS, tokens) y encabezados |
| Métricas | `http.server.request.duration` y las de ASP.NET Core, Kestrel y `System.Runtime`, con plantilla, método y código | Ruta con valores, query y `X-Correlation-Id` (cardinalidad y privacidad) |
| Logs | Los mismos eventos que Serilog escribe en consola, con sus niveles (`Microsoft.AspNetCore` en `Warning`), sus propiedades (por ejemplo `CorrelationId`) y la excepción como `exception.*` | Lo que Serilog ya filtra («Request starting/finished») y los valores sensibles, que se redactan |

**Redacción** (segunda barrera, la primera es no capturar cuerpos, encabezados ni la query): las claves `lat`, `lon`, `phone`, `celular`, `curp`, `token`, `idToken`, `id_token`, `accessToken`, `access_token`, `refreshToken`, `refresh_token`, `code`, `sig`, `authorization`, `cookie`, `set-cookie`, `password` y `secret` se redactan siempre (como atributo, en `string`, `string[]`, `object[]` o cualquier lista de textos). En texto libre se redactan los celulares con `+` o `%2B`, los JWT, `Bearer <x>`, la CURP (también en minúsculas), `clave=valor` de esas claves, `Cookie:` y `Set-Cookie:` con todos sus pares, y los **pares** de coordenadas: dos números con 1 a 3 enteros y 4 o más decimales, separados por espacios, `,`, `;` o `/` (`19.4326, -99.1332` y `19.4326,-99.1332` se redactan; `19.4326-99.1332`, sin separador, no). Un número suelto, como el `12.3456 ms` del log de petición, no se toca (decisión P6).

**Riesgo aceptado: celular nacional sin `+`.** Un celular de 10 dígitos sin prefijo (`5512345678`, `55 1234 5678`) **no** se redacta en texto libre: un patrón así también taparía ids, folios, marcas de tiempo y duraciones. Se acepta porque HU-001 solo maneja el formato `+52…` y la API no recibe ni escribe celulares de otra forma; las claves conocidas (`phone`, `celular`) se redactan siempre, con cualquier formato. Si un módulo nuevo acepta celulares sin `+`, se revisa esta decisión.

**Statsbeat apagado** (decisión del líder, 2026-11-03): el exportador de Azure Monitor envía por defecto *Statsbeat*, estadísticas internas del SDK (versión, éxito o falla de los envíos) a puntos de Microsoft fuera del recurso de Application Insights, sin datos de las peticiones. Se apaga con `APPLICATIONINSIGHTS_STATSBEAT_DISABLED=true`: la API la fija al registrar los exportadores (solo cuando hay cadena de conexión), así que en producción no hace falta definirla a mano y no se puede dejar encendido por olvido. Lo comprueba el caso 02 (`ExporterRegistrationTests`).

**Contexto de traza:** la API ignora el `traceparent`, el `tracestate` y el `baggage` que llegan en la petición y siempre inicia una traza nueva (decisión P9, hasta que exista el gateway). Las llamadas salientes sí propagan `traceparent`.

## `traceId` y `correlationId`

| Campo del error | Formato | En Application Insights |
|---|---|---|
| `traceId` | `00-<trace-id>-<span-id>-<banderas>` (W3C, 55 caracteres; contrato en la §4 de la spec) | `<trace-id>` es el `operation_Id` de la petición, sus dependencias, excepciones y logs |
| `correlationId` | El `X-Correlation-Id` de la respuesta | `customDimensions["knock.correlation_id"]` de la petición y `customDimensions.CorrelationId` de cada log |

El código de soporte que muestra la app es el `<trace-id>` (decisión P1, spec espejo en knock-frontend).

## Consultas KQL de soporte

```kusto
// Por código de soporte (<trace-id> del campo traceId)
union requests, dependencies, exceptions, traces
| where operation_Id == "<trace-id>"
| order by timestamp asc

// Por X-Correlation-Id
requests
| where tostring(customDimensions["knock.correlation_id"]) == "<correlationId>"
| project timestamp, name, resultCode, duration, operation_Id
```

## Alertas

Las crea el líder a mano en el portal cuando el código esté listo (decisión P4). TT-AZ-02 las pasa a Terraform después. **El costo de cada regla se confirma con la tarifa vigente antes de crearlas** (decisión P3, principio 16).

| Alerta | Consulta (alerta de búsqueda de logs) | Umbral | Ventana / frecuencia | Severidad |
|---|---|---|---|---|
| 5xx | `requests \| where url !contains "/health" \| summarize total = count(), failed = countif(toint(resultCode) >= 500) \| where failed >= 5 and failed * 100.0 / total > 2` | 5 o más **y** más del 2 % | 5 min / cada 5 min | 1 (error) |
| Latencia alta | `requests \| where url !contains "/health" \| summarize p95 = percentile(duration, 95) \| where p95 > 1000` | p95 por encima de 1 000 ms | 10 min / cada 5 min | 2 (aviso) |

Destino: un grupo de acciones con el correo único del proyecto (constitución §9). La dirección no se escribe en el repositorio.

**Costo y cuota:** con `SamplingRatio = 1` y `TracesPerSecond = null` no hay muestreo ni límite por segundo, así que el **único límite del gasto es el tope diario (daily cap) del recurso**, que fija el líder al crearlo (decisión P7; cuota gratuita de 5 GB al mes, ADR-006). Se recomienda activar al crear el recurso el aviso de Azure al llegar al tope diario (lo decide el líder). Esta tarea no define una regla de alerta de cuota propia (P3 solo pide 5xx y latencia); al llegar al tope, Application Insights deja de ingerir hasta el día siguiente.

## Verificación manual (casos 15 y 16)

Las hace el líder con el recurso creado. La evidencia (capturas y consulta KQL, nunca la cadena de conexión) va en el PR.

**Caso 15 (V1, lado API).**
1. En la terminal, sin escribirla en ningún archivo: `export APPLICATIONINSIGHTS_CONNECTION_STRING='<cadena del recurso>'`.
2. Arrancar la API (`dotnet run --project src/Knock.Api`) con MongoDB detenido, o con `ConnectionStrings__KnockMongo` apuntando a un destino que no responde (decisión P5: sin endpoint de prueba).
3. `curl -i -H 'X-Correlation-Id: obs-v1-<fecha>' http://localhost:<puerto>/api/v1/oficios`. Debe responder 500 y anotar el `traceId` del cuerpo.
4. Esperar de 2 a 5 minutos y buscar en Application Insights con las dos consultas de arriba. Deben aparecer la petición fallida, su excepción y su log, sin query ni datos personales.

**Caso 16 (V2).** Con la API como en el paso 2, repetir la petición del paso 3 más de 5 veces en menos de 5 minutos (y más del 2 % del tráfico de esa ventana). La alerta de 5xx debe dispararse y llegar al correo del grupo de acciones. La de latencia se revisa en el portal (regla creada y su consulta).
