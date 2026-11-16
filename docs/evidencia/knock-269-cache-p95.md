# KNOCK-269 — latencias secuenciales del catálogo

Medición del 14 de noviembre de 2026, `GET /api/v1/oficios` directo a Market local:

| Modo | Caché | p50 (ms) | p95 (ms) | p99 (ms) | n | Estados |
|---|---|---:|---:|---:|---:|---|
| 200 | fría | 10.303 | 22.976 | 37.115 | 100 | 200: 100 |
| 200 | caliente | 3.522 | 6.589 | 11.785 | 100 | 200: 100 |
| 304 | fría | 9.884 | 16.938 | 71.035 | 100 | 304: 100 |
| 304 | caliente | 3.915 | 8.011 | 16.633 | 100 | 304: 100 |

## Entorno y comandos

- Mac Intel Core i5-8257U a 1.40 GHz, 8 GiB, macOS 15.8.2; Docker 29.5.3.
- `deploy/docker-compose.local.yml`, proyecto aislado `knock269`: DocumentDB local y Redis, catálogo de ejemplo de 3 oficios; Market Release en el host, `Development`, directo en `127.0.0.1:5234`. Sin otro cliente durante la medición.
- Redis con TTL normal de catálogo de 86400 segundos; límite de peticiones de Market aumentado solo en local a 1000000. La conexión local a DocumentDB usó TLS con `tlsInsecure=true`, exclusivamente para su certificado autofirmado de prueba; no se usó un proxy ni se cambió código de producción.
- Preparación: `docker compose --env-file <archivo-local> -p knock269 -f deploy/docker-compose.local.yml up -d documentdb redis`, siembra con `mongosh --nodb --quiet --file db/entrega.js` y arranque de Market con `dotnet run --project src/Knock.Market.Api -c Release --no-build --no-restore --urls http://127.0.0.1:5234`; configuración y credenciales sintéticas solo en variables locales.
- Medición: `COMPOSE_PROJECT_NAME=knock269 COMPOSE_ENV_FILES=<archivo-local> KNOCK_BASE_URL=http://127.0.0.1:5234 tests/perf/cache-oficios-p95.sh`.

## Método y límites

Un único cliente secuencial hace 100 peticiones por combinación. Antes de cada fría borra únicamente `catalog:oficios` y comprueba su ausencia; tras cada petición comprueba que la misma clave volvió a existir en el Redis del compose. En `CatalogStore.FindActiveOficiosAsync`, una ausencia lleva a consultar MongoDB antes de repoblar la clave; esta es una verificación indirecta basada en esa ruta de código, no un perfilador de MongoDB. La generación `catalog:gen:oficios` no se borra: es un contador persistente para impedir resurrección de datos antiguos. El entorno aislado y un solo cliente evitan carreras de repoblación.

La pasada caliente empieza con una petición de calentamiento y comprueba que la clave siga presente. La 200 no envía `If-None-Match`; la 304 usa la ETag vigente obtenida antes de la pasada. Se valida el estado y la ETag de cada respuesta. Solo se cronometra `curl -w '%{time_total}'`; los comandos de Redis y la preparación quedan fuera. Los percentiles usan interpolación lineal entre posiciones `(n-1) × p`.

La primera ejecución completa se interrumpió por el límite de tiempo externo durante la cuarta pasada; la tabla procede de una segunda ejecución completa. Son mediciones locales con una muestra pequeña, no capacidad ni latencia de producción. El k6 existente de 5 usuarios virtuales queda como referencia de estabilidad bajo carga, no como comparación de caché fría contra caliente.
