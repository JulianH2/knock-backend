# Evidencia del spike — KNOCK-147 · TT-BD-05 Azure DocumentDB (nivel gratuito) frente a MongoDB Atlas

> Spec: `specs/TT-BD-05-spike-documentdb-vs-atlas/spec.md` (Aprobada) · Fecha: 2026-10-21 · Arnés: `tests/Spike.DocumentDb/` (fuera de `Knock.slnx` y del CI hasta TT-BD-07)
> Es un spike: entrega mediciones y una recomendación, no código de producto. Solo datos ficticios (seed versionado, puntos y centros sintéticos, documentos inventados).
> No se registran cadenas de conexión, usuarios, contraseñas ni nombres de host del clúster.

## Recomendación
**Seguir con Azure DocumentDB en su nivel gratuito (ADR-002); no activar el plan B (Atlas).** P2, P4 y P7 pasan en Azure; P1 queda por confirmar con la captura del portal. Si esa captura no muestra región West US, nivel Free y servidor 8.0, la recomendación se revisa.

Condiciones para adoptarla:
- Búsqueda de oficios con el **campo normalizado sin acentos** calculado en la app (P5 falla con `$text` y con colación en Azure).
- **`maxPoolSize` fijo por servicio** (P7: el límite de conexiones del nivel gratuito no es público ni lo expone `serverStatus`).
- **Pausa tras 60 días sin conexiones y sin respaldo automático:** hace falta un `mongodump` programado con restauración serial (P8 pasa contra Azure así).

## Condiciones (leer primero)
- **Pendiente:** solo P1 (captura del portal por el líder: región, nivel Free, versión 8.0). El resto está medido en los tres destinos.
- **Destinos:** `mongo:8.0.34` como réplica de un nodo (referencia del plan B, Atlas); `ghcr.io/microsoft/documentdb/documentdb-local@sha256:0fcf6345…` (build de 2025-06-17; las versiones nuevas se publican en `ghcr.io/documentdb/documentdb/documentdb-local`); Azure DocumentDB `knock-docdb`, West US, nivel gratuito (`KNOCK_SPIKE_MONGO`).
- **Fase 1** (2026-10-21) corrió el arnés contra Azure: **44/46** pruebas en verde; las 2 que fallan son P5 en Azure y en documentdb-local. Las mediciones de tiempo de Azure de esa corrida (p. ej. el borrado TTL) no se registraron.
- **Fase 2** agregó P3, P7, P8, P9, la alternativa de P5 y el chequeo único de conexión a Azure. Local: 57 pruebas, 42 pasan, 1 falla (P5 en documentdb-local) y 14 de Azure se omiten sin `KNOCK_SPIKE_MONGO`. Azure: P3 y P7 medidos desde Tula, Hgo., red doméstica.

## Cómo se ejecuta
```bash
dotnet test tests/Spike.DocumentDb -c Release --logger "console;verbosity=detailed"            # local
KNOCK_SPIKE_MONGO=<desde el gestor de contraseñas> SPIKE_ORIGEN="<ciudad — red>" \
  dotnet test tests/Spike.DocumentDb -c Release --logger "console;verbosity=detailed"          # + Azure; nunca pegar el valor
# P3 limpio (sin otras pruebas en paralelo):
#   ... dotnet test tests/Spike.DocumentDb -c Release --filter "FullyQualifiedName~DocumentDbLatencyTests" --logger "console;verbosity=detailed"
SPIKE_BACKUP_URI=<URI> tests/Spike.DocumentDb/scripts/backup-restore.sh                       # P8 (si no existe, usa KNOCK_SPIKE_MONGO)
```
- P3 y P7 imprimen sus mediciones con el prefijo `SPIKE-RESULT` (`grep SPIKE-RESULT` sobre la salida detallada).
- **Conexión a Azure:** un solo chequeo al inicio (`ping` con `serverSelectionTimeout` de 10 s). Si no responde, falla solo `AzureConnectivityTests.AzureCluster_RespondsToPingWithinTenSeconds` con «Sin conexión de red al clúster: no respondió en 10 s…» y el resto de las pruebas de Azure se omiten con ese motivo. Verificado con un host IP inalcanzable y con un host `mongodb+srv` que no resuelve: un fallo a los ~10 s, 0 apariciones de las credenciales falsas en la salida.
- **Secretos:** el arnés nunca imprime la cadena; los errores del driver se reportan solo por tipo de excepción (sin mensaje, que puede incluir el host), y las líneas `SPIKE-RESULT` de P3 y P7 no llevan datos de conexión. El script de P8 escribe la URI en un `--config` temporal con permisos 600 (no va en la línea de comandos), filtra la salida con un enmascarado de `usuario:contraseña` y borra el dump y las bases al terminar.

## Resultados por pregunta

| P | Caso | mongo:8.0 (local) | documentdb-local | Azure DocumentDB | Resultado |
|---|---|---|---|---|---|
| P1 | TC-TT-BD-05-01 | — | — | pendiente: captura del portal (líder) | pendiente |
| P2 | TC-TT-BD-05-02 | pasa | pasa | pasa (fase 1) | **pasa** |
| P3 | TC-TT-BD-05-03 | pasa (referencia) | pasa (referencia) | pasa (consulta p95 0.6 ms) | **pasa** |
| P4 | TC-TT-BD-05-04 | pasa | pasa | pasa (fase 1) | **pasa** |
| P5 | TC-TT-BD-05-05 | pasa | **falla** | **falla** | **falla** → contorno |
| P5 alt. | (hallazgo) | pasa | pasa | pasa | contorno válido |
| P6 | TC-TT-BD-05-06 | pasa | pasa | pasa (fase 1) | **pasa** |
| P7 | TC-TT-BD-05-07 | pasa (referencia) | pasa (referencia) | pasa (64 clientes sin rechazo) | **pasa** |
| P8 | TC-TT-BD-05-08 | pasa | pasa | pasa con restauración serial (en paralelo, `WriteConflict`) | **pasa** |
| P9 | TC-TT-BD-05-09 | tabla | tabla | tabla | **registrada** |

### P1 — Clúster `knock-docdb` (comando)
- **Pendiente:** el líder entrega una captura del portal con la región (West US), el nivel Free y la versión del servidor (8.0); de preferencia también el costo en 0 en Cost Management. Sin cadena de conexión ni claves en la captura. Límites que se anotan: sin alta disponibilidad, sin respaldo automático, pausa tras 60 días sin conexiones.

### P2 — Punto en polígono, respaldo `$near` y fuera de cobertura
- **Pasa en los tres destinos** (Azure en la fase 1). Los 8 puntos interiores devuelven su colonia; el vértice de El Cielito devuelve El Cielito; fuera de Tula (Pachuca) `$geoIntersects` y `$near` vuelven vacíos; el respaldo `$near` (≤ 2000 m) ordena por distancia: Mastrantos 76 m, San Lorenzo 1360 m, Centro 1514 m, Bugambilias 1620 m, El Cielito 1956 m (iguales en mongo y documentdb-local).
- Polígono que se cruza a sí mismo: rechazado con `MongoWriteException` en mongo y documentdb-local; rechazado también en Azure (fase 1).
- **Hallazgo:** el seed traslapa Centro–La Huerta, Centro–San Lorenzo, El Cielito–San Lorenzo y El Llano 1a Sección–Tultengo; un punto ahí devuelve dos colonias. Lo decide TT-BD-06 (corregir polígonos o regla de desempate).

### P3 — Latencia de «colonia de un punto» (criterio: p95 de la consulta < 100 ms)
- Método: `DocumentDbLatencyTests` mide en cada muestra la **red** (`ping`) y el **total** de la consulta `$geoIntersects`; la **consulta** sale de `explain` con `executionStats.executionTimeMillis` cuando el destino lo expone (los tres lo exponen), y si no, total − red. N = 200 tras 10 de calentamiento (`SPIKE_P3_N`, por defecto 200). Origen por `SPIKE_ORIGEN` (por defecto «Tula, Hgo. — red doméstica»). P3 y P7 comparten colección de xUnit para no correr a la vez.

| Destino | Red p50 / p95 / p99 | Total p50 / p95 / p99 | Consulta p50 / p95 / p99 (servidor) |
|---|---|---|---|
| mongo:8.0 (Docker local) | 1.3 / 8.9 / — ms | 1.9 / 8.4 / 11.4 ms | 0 / 1.0 / 4.0 ms |
| documentdb-local (Docker local) | 1.3 / 2.4 / 3.3 ms | 2.6 / 4.3 / 8.6 ms | 0 / 0 / 0 ms |
| **Azure, West US** (desde Tula, Hgo., red doméstica) | 82.7 / 105.7 / 125.5 ms | 84.1 / 160.6 / 220.7 ms | **0.4 / 0.6 / 0.9 ms** |

- **Pasa en Azure:** el p95 de la consulta es 0.6 ms, frente al límite de 100 ms. El total (p95 160.6 ms) lo domina la red México–West US. Desde servicios en la misma región se espera una red mucho menor; no se midió. Según la spec, la latencia de red no descarta DocumentDB: se resuelve acercando los servicios al clúster.

### P4 — Transacción solicitud + outbox
- **Pasa en los tres destinos** (Azure en la fase 1): commit deja (1, 1), abort deja (0, 0) y una excepción dentro de `WithTransaction` no deja documentos.

### P5 — Texto en español sin acentos
- **Falla en documentdb-local y en Azure**; pasa en mongo. El criterio no cambia.
- mongo: `$text` con `spanish` no encuentra «plomeria» (el stemming no coincide), pero sí lo encuentran `$text` con `none` y la colación `es` fuerza 1. documentdb-local y Azure: ninguna de las tres estrategias encuentra «Plomería» (la colación se ignora sin error).
- **Alternativa (contorno recomendado):** campo `nombreBusqueda` calculado en la app (sin marcas diacríticas, en mayúsculas), índice ascendente normal y búsqueda por prefijo anclado (`^PLOM`). `NormalizedFieldPrefixSearch_IgnoresAccentsAndCase_TC_TT_BD_05_05`: `plomeria` y `PLOM` → Plomería; `electricidad` y `Electrí` → Electricidad; con `hint` al índice devuelve lo mismo.
  - **Pasa en los tres destinos.** mongo: el plan ganador es FETCH sobre el índice. documentdb-local: con 3 oficios el planificador elige COLLSCAN, pero el `hint` al índice funciona.
  - Límite: la «ñ» se normaliza a «N» («peña» y «pena» empatan); no hay stemming ni búsqueda por palabra interior (solo prefijo).

### P6 — TTL de tokens revocados
- **Pasa en los tres destinos** (Azure en la fase 1). mongo: `ttlMonitorSleepSecs` = 60, borrado entre 48 y 57 s según la corrida. documentdb-local: el parámetro no se expone, borrado entre 7 y 45 s. Azure: el tiempo hasta el borrado no se registró (cupo en la espera de 3 min de la prueba).

### P7 — 8 clientes con pool, concurrentes
- Método: 8 `MongoClient` independientes (4 servicios × 2 réplicas; `applicationName` distinto para que el driver no comparta el pool), 4 hilos por cliente con insert + find durante 120 s (`SPIKE_P7_SECONDS`, por defecto 120). Después de la carga estable se suben clientes de 8 en 8 (10 s por escalón) hasta el primer rechazo o hasta 64, y se reporta el máximo alcanzado sin errores. Se cuentan las conexiones que abren los clientes (eventos del driver) y `serverStatus().connections.current` si el destino lo expone.

| Destino | Carga estable (8 clientes, 120 s): operaciones / errores | Conexiones de los clientes (pico) | `serverStatus.connections.current` | Rampa: máximo sin errores |
|---|---|---|---|---|
| mongo:8.0 | 420 286 / 0 | 32 | antes 9 · pico 57 | **64** (tope de la prueba; 256 conexiones de cliente, 394 en el servidor) |
| documentdb-local | 134 768 / 0 | 32 | **no expuesto** | **16** (64 conexiones); a 24 clientes (~96 conexiones) rechaza con `MongoCommandException` |
| **Azure, West US** | 43 290 / 0 | 32 | **no expuesto** | **64** (tope de la prueba; 256 conexiones), sin errores ni rechazos |

- **Pasa en Azure.** Hay menos operaciones que en local porque cada una paga la latencia de red (P3). El límite real del nivel gratuito sigue sin conocerse: Microsoft no lo publica (revisado el 2026-10-20) y `serverStatus().connections` no se expone. Esta es una **medición, no una cifra oficial**: ≥ 256 conexiones sin rechazo. Atlas M0 permite 500.
- **Hallazgo:** documentdb-local agota las conexiones de su PostgreSQL interno («remaining connection slots are reserved for roles with the SUPERUSER attribute») entre 64 y 96 conexiones y no las libera en 30 s; después de la rampa ni siquiera se puede borrar la base (P7 lo reporta como medición, no como fallo del criterio). **Testcontainers con documentdb-local subestima el límite de Azure:** una prueba de conexiones que falle en local no implica que falle en Azure.

### P8 — `mongodump` / `mongorestore` (comando)
- `tests/Spike.DocumentDb/scripts/backup-restore.sh` (+ `backup-restore.js` para sembrar, comparar y borrar con mongosh). Siembra `colonias` (8 polígonos, `2dsphere`), `solicitudes` y `outbox`, `oficios` (texto `spanish`) y `tokens_revocados` (TTL, vencen en 1 h); hace el dump en un directorio temporal fuera del repositorio, restaura en `<base>_restored` y compara conteos e índices (nombre, clave, `expireAfterSeconds`, `default_language`, `weights`). MongoDB Database Tools 100.16.1, mongosh 2.13.0.
- mongo:8.0: **pasa** — colonias 8/8, solicitudes 1/1, outbox 1/1, oficios 3/3, tokens_revocados 3/3; índices `geom_2dsphere`, `nombre_text` y `expiraEn_1` iguales.
- documentdb-local: **pasa** con los mismos conteos e índices. Diferencias: `mongodump` avisa «non-primary readPreference with a connection to mongos» (el motor se presenta como `mongos`) y el índice de texto queda con `textIndexVersion` 2 (mongo: 3).
- Azure, primer intento: **falla.** Con el paralelismo por defecto de `mongorestore`, Azure DocumentDB responde «(WriteConflict) Could not acquire lock for operation due to deadlock» al crear colecciones en paralelo: se restauraron `oficios` y `colonias`; fallaron `tokens_revocados`, `solicitudes` y `outbox`.
- **Hallazgo: en DocumentDB la restauración serial es obligatoria.** El script usa ahora `--numParallelCollections=1 --numInsertionWorkersPerCollection=1`, sale con código distinto de 0 si `mongorestore` falla, si imprime cualquier «Failed:» o «N document(s) failed», o si no coinciden los conteos e índices, y borra las bases de prueba (original y `_restored`) aunque falle. Verificado contra mongo:8.0 local: pasa con restauración serial; un `mongorestore` simulado que imprime «Failed:» y sale con 0 hace que el script termine con 1 y borre las dos bases.
- Azure con restauración serial: **pasa.** 16 documentos restaurados y 0 fallidos; conteos iguales (colonias 8, solicitudes 1, outbox 1, oficios 3, tokens_revocados 3) e índices iguales (`geom_2dsphere`, `nombre_text`, `expiraEn_1` con TTL); las dos bases de prueba se borraron y el script salió con 0.

### P9 — Paridad entre destinos
- `DocumentDbParityTests.LocalAndAzureBehaveTheSame_TC_TT_BD_05_09` corre P2, P4, P5 (más la alternativa) y P6 en cada destino disponible y marca con ≠ lo que difiere. La columna de Azure se llenó con el resultado de sus pruebas: P2, P4 y P6 pasan (fase 1); en P5 Azure se comporta como documentdb-local. «No registrado» indica un dato que la corrida de Azure no conservó.

| Comportamiento | mongo:8.0 | documentdb-local | Azure DocumentDB |
|---|---|---|---|
| P2 índices 2dsphere sobre el seed | aceptados (12 docs) | aceptados (12 docs) | aceptados (12 docs) |
| P2 `$geoIntersects` en 8 puntos interiores | 8/8 | 8/8 | 8/8 |
| P2 punto en vértice | El Cielito | El Cielito | El Cielito |
| P2 punto en traslape del seed | El Llano 1a Sección + Tultengo | El Llano 1a Sección + Tultengo | El Llano 1a Sección + Tultengo |
| P2 fuera de Tula (`$geoIntersects` / `$near`) | vacío / vacío | vacío / vacío | vacío / vacío |
| P2 respaldo `$near` (primero) | Mastrantos | Mastrantos | Mastrantos |
| P2 polígono que se cruza | rechazado (`MongoWriteException`) | rechazado (`MongoWriteException`) | rechazado (tipo no registrado) |
| P4 commit / abort | (1, 1) / (0, 0) | (1, 1) / (0, 0) | (1, 1) / (0, 0) |
| P4 excepción en `WithTransaction` | (0, 0) | (0, 0) | (0, 0) |
| P5 `$text` spanish | —, Electricidad | —, Electricidad | igual que documentdb-local |
| **P5 `$text` none ≠** | Plomería, Electricidad | —, Electricidad | igual que documentdb-local |
| **P5 colación es fuerza 1 ≠** | Plomería, Electricidad | —, — | igual que documentdb-local |
| P5 alternativa normalizada + prefijo | Plomería, Electricidad | Plomería, Electricidad | Plomería, Electricidad |
| P6 TTL borra el vencido y conserva el vigente | sí | sí | sí |
| **P6 `ttlMonitorSleepSecs` ≠** | 60 | no expuesto | no registrado |
| P6 segundos hasta el borrado (medición) | 57 s | 35 s | no registrado |
| P8 `mongorestore` en paralelo (por defecto) | pasa | pasa | **≠ falla** (`WriteConflict`, deadlock al crear colecciones) |
| P8 `mongorestore` serial: conteos e índices iguales | sí | sí | sí |

- Fuera del set: **P8 ≠** — `mongorestore` en paralelo funciona en mongo y documentdb-local, pero en Azure provoca `WriteConflict` (deadlock) al crear colecciones: la restauración contra Azure debe ser serial (`--numParallelCollections=1 --numInsertionWorkersPerCollection=1`). documentdb-local no reproduce este conflicto. Con restauración serial, P8 pasa en los tres destinos (mismos conteos e índices). **P7 ≠** — documentdb-local rechaza desde 24 clientes; Azure y mongo llegan a 64 sin errores. `serverStatus().connections` no se expone ni en documentdb-local ni en Azure. documentdb-local usa otro formato de `explain` (`explainVersion` 2, contadores como enteros de 64 bits partidos), su planificador prefiere COLLSCAN en colecciones pequeñas, se presenta como `mongos` ante `mongodump` y crea el índice de texto con `textIndexVersion` 2.
- **Imagen para Testcontainers (TT-BD-07):** **documentdb-local**, porque reproduce el comportamiento de Azure en P5 y mongo no. Advertencia: su límite de conexiones es más bajo que el de Azure (P7), así que las pruebas de conexiones o de carga no deben tomar el local como límite real.

## Regla de decisión (spec §8)
- Si **P1, P2, P4 o P7** fallan sin solución razonable → **plan B (MongoDB Atlas)** y se actualiza ADR-002.
- Si fallan **P3, P5, P6 o P8** → se documenta el contorno (región o servicios más cerca del clúster, colación o campo normalizado, Redis según ADR-006, `mongodump` programado) antes de descartar. Un p95 que falla solo por la red no descarta DocumentDB.
- **Aplicación:** P2, P4 y P7 pasan en Azure; P1 está pendiente de la captura. P3 y P6 pasan. P5 falla y tiene contorno (campo normalizado, verde en los tres destinos). P8 pasa en Azure con restauración serial (en paralelo falla con `WriteConflict`). Ninguna condición activa el plan B → **seguir con Azure DocumentDB**, sujeto a P1.

## Riesgos
- **Búsqueda de texto con acentos:** P5 falla en Azure. El campo normalizado debe calcularse en cada escritura (y con una migración para los existentes); un servicio que lo olvide deja oficios sin encontrar. Solo cubre prefijos, y «ñ» = «n».
- **Límite de conexiones desconocido:** ≥ 256 conexiones medidas, sin cifra oficial ni `serverStatus`. Fijar `maxPoolSize` por servicio, para que 4 servicios × 2 réplicas no lleguen a 8 × 100 conexiones.
- **Pausa a los 60 días sin uso y sin respaldo automático:** el nivel gratuito no tiene alta disponibilidad ni respaldo. Hace falta un `mongodump` programado (P8 validado contra Azure) y un plan para reanudar el clúster tras una pausa. La restauración contra Azure tiene que ser serial (P8); en paralelo deja colecciones sin restaurar.
- **Latencia de red desde México:** red p95 105.7 ms y total p95 160.6 ms desde una red doméstica en Tula. Los servicios deben desplegarse cerca de West US (región de ADR-006).
- **documentdb-local de 2025-06:** la imagen fijada puede diferir del motor actual de Azure (ya difiere en P7); revisar la imagen nueva antes de TT-BD-07.
- **Traslapes del seed:** un punto puede devolver dos colonias hasta que TT-BD-06 decida.

## Pendiente de una persona
- P1: captura del portal por el líder (región, nivel Free, versión 8.0), sin cadena ni claves.
- Pedir que se quite la regla de firewall de la IP al terminar (el clúster se conserva).
