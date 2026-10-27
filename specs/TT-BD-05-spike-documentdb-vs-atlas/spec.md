# TT-BD-05 — Spike: Azure DocumentDB (nivel gratuito) frente a MongoDB Atlas (plan B)

> **Jira:** KNOCK-147 · **Área:** Base de datos · **Sprint:** S1 · **Puntos:** 2
> **Repositorio:** knock-backend · **Rama:** `chore/KNOCK-147-documentdb-spike` → PR a `sprint-01`
> **Fuentes:** `docs/decisiones/ADR-002-base-de-datos-mongodb.md` («El spike debe probar») · `docs/decisiones/ADR-006-costos-y-ventana-de-evaluacion.md` (región y suscripción) · `docs/PLAN_ADAPTACION.md` (TT-BD-05)
> **Skills:** story-workflow, database-scripts, security-core, testing-strategy-backend
> **Estado:** Firmada

## 1. Objetivo
Decidir **con evidencia** si Knock sigue con **Azure DocumentDB en su nivel gratuito** (ADR-002) o pasa a **MongoDB Atlas** (plan B). Es un **spike**: se entregan mediciones y una recomendación, no código de producto.

**Terminada cuando:** existe `docs/evidencia/KNOCK-147-spike-documentdb.md` con el resultado de P1 a P9 (pasa / falla / medición) y una recomendación final, y ADR-002 puede confirmarse o cambiarse al plan B con esa evidencia.

## 2. Historia y casos de prueba
**TT-BD-05** — Tarea técnica (spike). Cada pregunta tiene un criterio medible. Las que se prueban con código usan el arnés de §3 y corren primero en Docker local y después contra Azure. Las que dependen del portal o de una persona se marcan «(comando)» y su salida va en la evidencia y en el PR. Solo datos ficticios: colonias del seed versionado, puntos sintéticos, solicitudes y tokens inventados.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-05-01 | **P1.** Dado el clúster existente `knock-docdb` (creado por el líder en `rg-knock-dev`), cuando se revisa su configuración, entonces se verifica que cumple lo esperado: región **West US**, **nivel gratuito**, 32 GB y servidor **8.0**, sin cargo; se anotan sus límites (sin alta disponibilidad, sin respaldo, pausa tras 60 días sin conexiones). No se crea ni se borra ningún clúster. | (comando) `az` o portal por la persona responsable; captura sin cadena de conexión ni claves; costo en 0 en Cost Management. |
| TC-TT-BD-05-02 | **P2.** Dado `db/seeds/colonias_tula.geojson` cargado en una colección con índice `2dsphere`, cuando se consulta un punto sintético dentro de cada uno de los 8 polígonos con `$geoIntersects`, entonces se devuelve la colonia correcta; los polígonos inválidos se rechazan al crear el índice o al insertar; las 4 colonias del catálogo sin polígono se resuelven por el respaldo de centro más cercano con `$near` (`$maxDistance` 2000 m); un punto fuera de Tula no devuelve colonia. | `Spike/DocumentDbGeoTests.PointInPolygonNearFallbackAndOutOfCoverage_TC_TT_BD_05_02`; local y Azure. |
| TC-TT-BD-05-03 | **P3.** Dadas las consultas de P2 contra el clúster de West US, cuando se ejecutan al menos 200 veces desde México (después de calentar la conexión), entonces el p95 de la consulta es **< 100 ms**. Se reportan p50, p95 y p99, y se separa la latencia de red (ping) de la de la consulta. **Si el p95 falla por la red y no por la consulta**, la conclusión es «cambiar la región candidata o acercar los servicios al clúster», no «descartar DocumentDB». | `Spike/DocumentDbLatencyTests.PointQueryP95UnderOneHundredMilliseconds_TC_TT_BD_05_03`; solo Azure. Anotar el origen (ciudad y red) en la evidencia. |
| TC-TT-BD-05-04 | **P4.** Dado el nivel gratuito sin alta disponibilidad, cuando una sesión inserta una solicitud y su evento de outbox en dos colecciones dentro de una transacción multi-documento, entonces con `commit` ambos documentos existen y con `abort` (o una excepción a la mitad) no existe ninguno. | `Spike/DocumentDbTransactionTests.RequestAndOutboxCommitTogetherAndAbortTogether_TC_TT_BD_05_04`; local (réplica de un nodo) y Azure. |
| TC-TT-BD-05-05 | **P5.** Dada una colección de oficios con un índice de texto en español sin distinguir acentos, cuando se busca `plomeria`, entonces se encuentra «Plomería» (y `electricidad` encuentra «Electricidad»). Si `$text` no ignora acentos, se prueba una colación `es` con `strength: 1` y se registra cuál funciona. | `Spike/DocumentDbTextTests.SpanishTextSearchIgnoresAccents_TC_TT_BD_05_05`; local y Azure. |
| TC-TT-BD-05-06 | **P6.** Dada una colección de tokens revocados con índice TTL sobre la fecha de expiración, cuando vence un documento, entonces el servidor lo borra solo. Se mide el tiempo real hasta el borrado (el monitor TTL corre cada ~60 s). | `Spike/DocumentDbTtlTests.ExpiredRevokedTokenIsDeletedByTtlIndex_TC_TT_BD_05_06`; local y Azure, con espera acotada (≤ 3 min). |
| TC-TT-BD-05-07 | **P7.** Dados 8 clientes `MongoClient` independientes (4 servicios × 2 réplicas), cada uno con su pool, cuando ejecutan operaciones concurrentes durante al menos 2 minutos, entonces no hay errores de conexión ni de límite. Se documentan las conexiones abiertas observadas, la regla de pausa tras 60 días de inactividad (qué pasa al reanudar) y el **límite de conexiones del nivel gratuito**: Microsoft no lo publica (revisado el 2026-10-20 en [Free tier](https://learn.microsoft.com/en-us/azure/documentdb/free-tier) y [Service limits](https://learn.microsoft.com/en-us/azure/documentdb/limitations)), así que se **mide** (subir clientes y tamaño de pool hasta el primer rechazo, o leer `serverStatus().connections` si el servidor lo expone) y la evidencia dice que es una medición, no una cifra oficial. Se compara con **Atlas M0: 500 conexiones** ([MongoDB](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/)). | `Spike/DocumentDbConnectionTests.EightPooledClientsRunConcurrentlyWithoutErrors_TC_TT_BD_05_07`; Azure (local como referencia). |
| TC-TT-BD-05-08 | **P8.** Dada una base con los datos de P2, P4, P5 y P6, cuando se ejecuta `mongodump` y luego `mongorestore` en una base nueva, entonces los conteos por colección y las definiciones de índices (`2dsphere`, texto, TTL) coinciden. | (comando) `mongodump`/`mongorestore` de MongoDB Database Tools contra local y Azure; comparación de conteos e índices pegada en la evidencia. |
| TC-TT-BD-05-09 | **P9.** Dado el mismo set de pruebas P2 y P4 a P7, cuando corre contra el contenedor local y contra Azure DocumentDB, entonces se registra una tabla de diferencias (operador, resultado, error o tiempo) y se decide qué imagen usará Testcontainers en TT-BD-07. | `Spike/DocumentDbParityTests.LocalAndAzureBehaveTheSame_TC_TT_BD_05_09`; parametrizada por destino. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - **Método:** primero local en Docker y después en Azure, con el mismo arnés y las mismas pruebas.
  - **Imágenes locales (nombres verificados con `docker manifest inspect` el 2026-10-20; amd64 y arm64):**
    - `ghcr.io/microsoft/documentdb/documentdb-local` — imagen local del motor open source de DocumentDB, el más cercano a Azure DocumentDB. **Principal** para la paridad (P9).
    - `mongo:8.0` (oficial) iniciada como réplica de un nodo (`--replSet rs0`), necesaria para transacciones (P4). **Referencia** de comportamiento MongoDB y del plan B (Atlas).
    - Fijar la etiqueta exacta (no `latest`) al implementar y anotarla en la evidencia. El clúster de Azure usa servidor 8.0; `mongo:8.0` coincide en versión.
  - **Arnés:** proyecto de pruebas .NET aparte, `tests/Spike.DocumentDb/` (xUnit v2 + AwesomeAssertions, como el resto), con el **driver oficial `MongoDB.Driver`** y Testcontainers para el destino local. **Queda fuera de `Knock.slnx` y fuera del CI hasta TT-BD-07**; después se reutiliza como base de Testcontainers del producto. El destino se elige con la variable de entorno `KNOCK_SPIKE_MONGO` (sin ella, solo corre local; las pruebas de Azure se omiten). No toca `src/` de los módulos. P1 y P8 son comandos.
  - Scripts de índices y semillas del spike **dentro del arnés**; su ruta definitiva (`db/mongo/`) se fija en TT-BD-09.
  - Entregable: `docs/evidencia/KNOCK-147-spike-documentdb.md` con el formato de `docs/evidencia/KNOCK-21-spike-firebase.md` (condiciones, qué funcionó, qué falta, riesgos, pendiente de una persona) más una tabla P1–P9 con pasa / falla / medición y la recomendación.
- **Out:**
  - Migrar el catálogo ni los módulos a MongoDB (TT-BD-06), adaptar el CI y Testcontainers del producto (TT-BD-07), índices definitivos (TT-BD-08), scripts de entrega (TT-BD-09).
  - Crear, borrar o reconfigurar el clúster. `knock-docdb` es el único clúster gratuito de la suscripción: se conserva para TT-BD-06 y Terraform lo importará (TT-AZ-02 / TT-AZ-05).
  - Crear una cuenta de Atlas: solo se compara con la documentación pública y con `mongo:8.0`, salvo que DocumentDB falle y el equipo pida probarla.
- **Dependencias:** ADR-002 y ADR-006; cadena de conexión y regla de firewall para la IP de quien ejecuta, a cargo del líder (§8, (a)).

## 4. Contratos API y eventos
No se agrega endpoint ni evento. El «evento de outbox» de P4 es un documento de prueba en una colección del spike, no un contrato.

## 5. Modelo de datos
Colecciones **solo del spike**, en una base `knock_spike` que se borra al terminar:
- `colonias`: `{ _id, nombre, cp, municipio, piloto, geom: GeoJSON MultiPolygon | null, centro: GeoJSON Point }`; índices `2dsphere` en `geom` y en `centro`. Las 8 colonias con polígono salen de `db/seeds/colonias_tula.geojson`; las 4 sin polígono salen de `db/sepomex/tula_colonias.csv` (12 filas).
- `solicitudes` y `outbox`: documentos mínimos para P4.
- `oficios`: `{ clave, nombre }` con índice de texto `default_language: "spanish"` para P5.
- `tokens_revocados`: `{ jti, expiraEn }` con índice TTL `expireAfterSeconds: 0` sobre `expiraEn` para P6.

**Hallazgo:** las 4 colonias sin polígono **no tienen punto central en el repositorio**. El seed de PostgreSQL calcula el centro con `ST_PointOnSurface` y solo para las que tienen polígono. Para el respaldo `$near` de P2 se usan **fixtures sintéticos solo para el spike**, marcados como tales en el arnés; no se agregan coordenadas «reales» de esas colonias al repositorio (§8, (d)).

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| La cadena de conexión del clúster (usuario y contraseña) se filtra en el repositorio, que es público, o en logs, PR o prompts de IA. | El líder la comparte **solo por gestor de contraseñas**; en el equipo vive en variable de entorno o `dotnet user-secrets`; nunca en archivos versionados, salida de pruebas, capturas, evidencia, PR ni prompts a agentes; el arnés no imprime la cadena ni la incluye en mensajes de error. Gitleaks/Semgrep del CI como red de seguridad. | TC-TT-BD-05-01 · TC-TT-BD-05-07 |
| Clúster expuesto a Internet con credenciales débiles. | Firewall por IP: el líder agrega la IP de cada desarrollador a petición; contraseña generada y TLS obligatorio; al terminar se pide quitar la regla de esa IP (el clúster se conserva). | TC-TT-BD-05-01 |
| Uso de datos reales. | Solo el seed versionado, puntos sintéticos y documentos inventados; ningún teléfono, nombre ni ubicación de personas. | TC-TT-BD-05-02 · TC-TT-BD-05-08 |
| Un dump queda en el equipo o en el repositorio. | `mongodump` a un directorio temporal fuera del repositorio; se borra tras comparar. | TC-TT-BD-05-08 |
| Gasto inesperado del crédito de estudiante. | Solo el nivel gratuito; verificar el costo en 0 y respetar el presupuesto de ADR-006. | TC-TT-BD-05-01 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Pruebas del spike (local) | Sí | Testcontainers con `documentdb-local` y `mongo:8.0` réplica de un nodo: P2, P4–P7, P9. |
| Pruebas del spike (Azure) | Sí | Mismo arnés contra West US con `KNOCK_SPIKE_MONGO`: P2–P7, P9. Las ejecuta una persona con acceso. |
| Comandos | Sí | P1 (verificación del clúster existente) y P8 (`mongodump`/`mongorestore`); salida en la evidencia. |
| Rendimiento | Sí | P3: p95 < 100 ms con 200+ consultas tras calentar. |
| Unit, contrato, eventos, UI, mutación | No | El spike no agrega código de producto. |

**Gates:** `sdd_lint.py`, build y formato del proyecto del arnés, Semgrep y escaneo de secretos en verde; evidencia completa. `--trace TT-BD-05` fallará hasta que exista el arnés. Esta spec es un borrador: no se ha verificado el clúster ni se ha ejecutado ninguna prueba.

**Regla de decisión:** si P1, P2, P4 o P7 fallan sin solución razonable, se recomienda el plan B (Atlas) y se actualiza ADR-002. Si fallan P3, P5, P6 o P8, se documenta el contorno (región o servicios más cerca del clúster, colación, Redis según ADR-006, `mongodump` programado) antes de descartar. Un p95 que falla solo por la red no descarta DocumentDB (P3).

**Preguntas abiertas — Resuelto (revisión del Tech Lead, 2026-10-20):**
- **(a) Acceso a Azure — Resuelto.** El líder ya creó el clúster gratuito `knock-docdb` en West US, grupo `rg-knock-dev`, nivel gratuito, 32 GB, servidor 8.0. La cadena de conexión se comparte solo por gestor de contraseñas; nunca en el repositorio, PR, logs ni prompts. Firewall por IP: se agrega la IP del desarrollador a petición. Es el único clúster gratuito de la suscripción: se conserva para TT-BD-06 y Terraform lo importará. P1 verifica el clúster; no lo crea.
- **(b) Arnés — Resuelto.** Proyecto de pruebas .NET en `tests/Spike.DocumentDb/`, fuera de `Knock.slnx` y del CI hasta TT-BD-07; después se reutiliza para Testcontainers del producto.
- **(c) Scripts de índices y semillas — Resuelto.** Dentro del arnés; la ruta definitiva (`db/mongo/`) se fija en TT-BD-09.
- **(d) Centros de las 4 colonias sin polígono — Resuelto.** Fixtures sintéticos solo para el spike, marcados como tales; no se agregan coordenadas «reales» al repositorio.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-20
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
