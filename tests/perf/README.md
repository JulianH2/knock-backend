# Pruebas de carga con k6 (spec 010, T-07)

Escenario de lectura repetida del catálogo público `GET /api/v1/oficios` para ajustar
las vigencias de caché (R-04): compara caché fría (primer acceso) contra caché caliente,
cuenta los `ETag` 200/304 como éxitos (una 304 no es un error) y mide la compresión
(`Content-Encoding: br/gzip`) y las latencias p50/p95/p99 con `http_req_duration`.
Solo hace GET públicos: no necesita credenciales ni escribe datos.

## Requisitos (Mac)

- `k6` instalado: `brew install k6` (o el binario de <https://github.com/grafana/k6/releases>).
- Entorno local aislado: MongoDB (documentdb-local) y Redis levantados con
  `docker compose -f deploy/docker-compose.local.yml up` (requiere `POSTGRES_PASSWORD`,
  `KNOCK_MONGO_USER`, `KNOCK_MONGO_PASSWORD` y `KNOCK_REDIS_PASSWORD`),
  el catálogo sembrado con `mongosh "$KNOCK_DB_URI" db/entrega.js` y el servicio
  Market en `http://localhost:5234` (puerto del Gateway para Mercado en
  `src/Knock.Gateway/appsettings.json`).
- **Nunca** correr contra producción ni servicios compartidos.

Cadena de MongoDB local (solo credenciales de mentira del contenedor, sin secretos reales):

```bash
export KNOCK_DB_URI="mongodb://<usuario>:<contraseña>@localhost:10260/<base>?tls=true&tlsAllowInvalidCertificates=true"
export ConnectionStrings__KnockMongo="$KNOCK_DB_URI"
export ConnectionStrings__KnockRedis="localhost:6379,password=<contraseña>"
export Mongo__Databases__Catalog=market
mongosh --quiet "$KNOCK_DB_URI" db/entrega.js
# Solo entorno local: el límite global por IP (100/min) contaría como error en el script y taparía el efecto de la caché.
export RateLimiting__Global__PermitLimit=1000000
dotnet run --project src/Knock.Market.Api -c Release --urls http://localhost:5234
```

## Ejecutar

```bash
KNOCK_BASE_URL=http://localhost:5234 KNOCK_VUS=5 KNOCK_DURATION=1m KNOCK_RUN_LABEL=ttl-86400 \
  k6 run tests/perf/cache-oficios.k6.js
```

Variables: `KNOCK_BASE_URL` (defecto `http://localhost:5234`), `KNOCK_VUS` (5),
`KNOCK_DURATION` (1m; la ronda fría dura 20 s y la caliente arranca a los 30 s),
`KNOCK_RUN_LABEL` (etiqueta para comparar corridas), `KNOCK_P95_MS` (500).

Para medir un estado por separado, añade `KNOCK_MODE=200` o `KNOCK_MODE=304`.
Cada modo corre una sola ronda de `KNOCK_DURATION` con `KNOCK_VUS`: 200 nunca
envía `If-None-Match`; 304 obtiene la ETag en `setup()` y la envía en cada
petición. `setup()` también calienta la caché y no forma parte de las estadísticas
`http_req_duration{scenario:measurement}`. Esa línea muestra conteo y percentiles;
`etag_200`, `etag_304` y `otros_estados`, cuando aparecen, cuentan las respuestas
medidas. El chequeo del estado esperado debe pasar al 100 %. Estos modos aceptan
únicamente `http://localhost` o `http://127.0.0.1` con puerto opcional.

**Limitación de la ronda fría:** el TTL mínimo es 60 s, de modo que no fuerza un
fallo por petición. Tampoco basta borrar la clave en un ciclo paralelo: otra
petición puede volver a poblarla antes de la siguiente lectura. La ronda `cold`
histórica solo mide un primer acceso seguido de lecturas posiblemente calientes;
no demuestra la condición fría por petición con usuarios concurrentes.

## Comparación secuencial fría/caliente (KNOCK-269)

Con Market y el Redis de `deploy/docker-compose.local.yml` aislados y sin otros
clientes, ejecuta `tests/perf/cache-oficios-p95.sh`. Se pueden pasar
`COMPOSE_PROJECT_NAME`, `COMPOSE_ENV_FILES` y `KNOCK_BASE_URL` para seleccionar el
proyecto local. El script borra `catalog:oficios` antes de cada petición fría;
no borra su generación. Verifica ausencia y repoblación de la clave, estado y
ETag. Hace 100 peticiones secuenciales por combinación y solo mide el tiempo
HTTP, no los comandos Redis. En caliente comprueba que la clave permanezca.
La repoblación indica la lectura de MongoDB según la ruta de `CatalogStore`,
no es una captura independiente de sus consultas. Requiere Docker local por
socket Unix, Redis publicado solo en loopback y URL HTTP local. La evidencia de
la corrida está en `docs/evidencia/knock-269-cache-p95.md`; no compara carga de
5 usuarios virtuales ni establece capacidad del sistema.

## Comparar vigencias (R-04)

1. Ajusta `Cache:CatalogLifetimeSeconds` (`.NET` en `src/Knock.Market.Api/appsettings.json`
   o variable `Cache__CatalogLifetimeSeconds`) y reinicia Market.
2. Corre el k6 con un `KNOCK_RUN_LABEL` distinto (p. ej. `ttl-60`, `ttl-86400`).
3. Compara en el resumen de k6 `http_req_duration` (p50/p95/p99) y las métricas
   `cache_cold_duration_ms` / `cache_warm_duration_ms` entre etiquetas.

## Qué garantiza y qué no

Garantiza: el catálogo responde 200/304 sin errores bajo concurrencia constante y
la compresión solo aparece en cuerpos mayores a 1 KB (T-06). No mide escritura,
invalidación al escribir (eso lo cubren T-02 y las pruebas de base de datos) ni
escalado a dos réplicas (eso es TT-QA-02).
