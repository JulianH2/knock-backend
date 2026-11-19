# KNOCK-66 — búsqueda sin caché, CA-19

## Ejecución del 2026-11-14

- Mac Intel Core i5-8257U a 1,40 GHz; Docker Desktop Linux x86_64, 8 CPU y 3,82 GiB asignados; .NET 11 Release.
- DocumentDB local con Testcontainers, no Compose: la fixture usa la misma imagen fijada por digest que `deploy/docker-compose.local.yml`. Una base lógica aislada se elimina al terminar.
- 200 técnicos activos ficticios, con radio de 1 km y centros a más de 30 km de la colonia consultada; catálogo y diccionario de semillas. Ninguno atiende la colonia.
- Diez búsquedas de calentamiento y **200 medidas secuenciales**, alternando `boiler` y nombres ficticios. Se llama a `BusquedaService` con `DisabledCatalogCache`; se comprueba un comando Mongo de técnicos en cada búsqueda. No hay aciertos Redis, aunque el sistema y Mongo pueden tener cachés calientes.
- Tiempo completo de la llamada al servicio (incluye consultas y transporte local), percentil por rango más cercano: **p50 143,969 ms; p95 177,391 ms; p99 201,536 ms**. El p95 es inferior a 300 ms en esta corrida, no una garantía para otros entornos o concurrencias.

## Planes observados

Se ejecutó `explain` con `executionStats` sobre los comandos de agregación capturados del store, tanto para coincidencias en cobertura como fuera de ella:

- `$geoNear`: `GEO_NEAR_2DSPHERE` con `IXSCAN` sobre `ubicacionBase_2dsphere`; sin `COLLSCAN` en el plan ganador. En el caso sin límite de distancia examinó 200 documentos para devolver una coincidencia.
- Prefijo sin centro: `FETCH` → `IXSCAN` sobre `oficios_1_estado_1_oculto_1`; sin `COLLSCAN` en el plan ganador. **El prefijo se evaluó como filtro en ejecución**, no se eligió `palabrasBusqueda_1`; en el grupo fuera de cobertura se examinaron los 200 documentos y se entregaron 20 después del orden y límite. La existencia de `IXSCAN` no demuestra que el prefijo sea selectivo ni garantiza escalabilidad con más técnicos.

Se inspeccionaron esas cuatro variantes, no todos los planes posibles ni Azure DocumentDB. La salida completa de cada `explain` la imprime la prueba.

## Reproducción

```bash
dotnet test tests/DatabaseTest -c Release --filter 'FullyQualifiedName~Doscientas_Busquedas_Sin_Cache_CA_027_19' --logger 'console;verbosity=detailed'
```

La prueba exige p95 menor de 300 ms y verifica los índices y la ausencia de `COLLSCAN` en los planes ganadores observados. Lleva `Category=Performance`, pero el paso actual de DatabaseTest en `ci-pr.yml` no excluye esa categoría; no se modificó el workflow en este cambio.
