# TT-BD-05 — Spike: Azure DocumentDB (nivel gratuito) frente a MongoDB Atlas (plan B)

> **Jira:** KNOCK-147 · **Área:** Base de datos · **Sprint:** S1 · **Puntos:** 2
> **Repositorio:** knock-backend · **Rama:** `chore/KNOCK-147-documentdb-spike` → PR a `sprint-01`
> **Fuentes:** `docs/decisiones/ADR-002-base-de-datos-mongodb.md` («El spike debe probar») · `docs/decisiones/ADR-006-costos-y-ventana-de-evaluacion.md` (región y suscripción) · `docs/PLAN_ADAPTACION.md` (TT-BD-05)
> **Skills:** story-workflow, database-scripts, security-core, testing-strategy-backend
> **Estado:** Borrador

## 1. Objetivo
Decidir **con evidencia** si Knock sigue con **Azure DocumentDB en su nivel gratuito** (ADR-002) o pasa a **MongoDB Atlas** (plan B). Es un **spike**: se entregan mediciones y una recomendación, no código de producto.

**Terminada cuando:** existe `docs/evidencia/KNOCK-147-spike-documentdb.md` con el resultado de P1 a P9 (pasa / falla / medición) y una recomendación final, y ADR-002 puede confirmarse o cambiarse al plan B con esa evidencia.

## 2. Historia y casos de prueba
**TT-BD-05** — Tarea técnica (spike). Cada pregunta tiene un criterio medible. Las que se prueban con código usan el arnés de §3 y corren primero en Docker local y después contra Azure. Las que dependen del portal o de una persona se marcan «(comando)» y su salida va en la evidencia y en el PR. Solo datos ficticios: colonias del seed versionado, puntos sintéticos, solicitudes y tokens inventados.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-05-01 | **P1.** Dada la suscripción Azure for Students, cuando se crea un clúster de Azure DocumentDB en nivel gratuito en **West US**, entonces queda aprovisionado sin cargo, se registra que es el único clúster gratuito permitido por suscripción y se anotan sus límites (almacenamiento, vCore, sin alta disponibilidad, sin respaldo, pausa a los 60 días). | (comando) `az` o portal por la persona responsable; captura sin cadena de conexión ni claves; costo estimado en 0 en Cost Management. |
| TC-TT-BD-05-02 | **P2.** Dado `db/seeds/colonias_tula.geojson` cargado en una colección con índice `2dsphere`, cuando se consulta un punto sintético dentro de cada uno de los 8 polígonos con `$geoIntersects`, entonces se devuelve la colonia correcta; los polígonos inválidos se rechazan al crear el índice o al insertar; las 4 colonias del catálogo sin polígono se resuelven por el respaldo de centro más cercano con `$near` (`$maxDistance` 2000 m); un punto fuera de Tula no devuelve colonia. | `Spike/DocumentDbGeoTests.PointInPolygonNearFallbackAndOutOfCoverage_TC_TT_BD_05_02`; local y Azure. |
| TC-TT-BD-05-03 | **P3.** Dadas las consultas de P2 contra el clúster de West US, cuando se ejecutan al menos 200 veces desde México (después de calentar la conexión), entonces el p95 de la consulta es **< 100 ms**. Se reportan p50, p95 y p99, y se separa la latencia de red (ping) de la de la consulta. | `Spike/DocumentDbLatencyTests.PointQueryP95UnderOneHundredMilliseconds_TC_TT_BD_05_03`; solo Azure. Anotar el origen (ciudad y red) en la evidencia. |
| TC-TT-BD-05-04 | **P4.** Dado el nivel gratuito sin alta disponibilidad, cuando una sesión inserta una solicitud y su evento de outbox en dos colecciones dentro de una transacción multi-documento, entonces con `commit` ambos documentos existen y con `abort` (o una excepción a la mitad) no existe ninguno. | `Spike/DocumentDbTransactionTests.RequestAndOutboxCommitTogetherAndAbortTogether_TC_TT_BD_05_04`; local (réplica de un nodo) y Azure. |
| TC-TT-BD-05-05 | **P5.** Dada una colección de oficios con un índice de texto en español sin distinguir acentos, cuando se busca `plomeria`, entonces se encuentra «Plomería» (y `electricidad` encuentra «Electricidad»). Si `$text` no ignora acentos, se prueba una colación `es` con `strength: 1` y se registra cuál funciona. | `Spike/DocumentDbTextTests.SpanishTextSearchIgnoresAccents_TC_TT_BD_05_05`; local y Azure. |
| TC-TT-BD-05-06 | **P6.** Dada una colección de tokens revocados con índice TTL sobre la fecha de expiración, cuando vence un documento, entonces el servidor lo borra solo. Se mide el tiempo real hasta el borrado (el monitor TTL corre cada ~60 s). | `Spike/DocumentDbTtlTests.ExpiredRevokedTokenIsDeletedByTtlIndex_TC_TT_BD_05_06`; local y Azure, con espera acotada (≤ 3 min). |
| TC-TT-BD-05-07 | **P7.** Dados 8 clientes `MongoClient` independientes (4 servicios × 2 réplicas), cada uno con su pool, cuando ejecutan operaciones concurrentes durante al menos 2 minutos, entonces no hay errores de conexión ni de límite. Se documentan el máximo de conexiones del nivel gratuito, las conexiones abiertas observadas y la regla de pausa tras 60 días de inactividad (qué pasa al reanudar). | `Spike/DocumentDbConnectionTests.EightPooledClientsRunConcurrentlyWithoutErrors_TC_TT_BD_05_07`; Azure (local como referencia). |
| TC-TT-BD-05-08 | **P8.** Dada una base con los datos de P2, P4, P5 y P6, cuando se ejecuta `mongodump` y luego `mongorestore` en una base nueva, entonces los conteos por colección y las definiciones de índices (`2dsphere`, texto, TTL) coinciden. | (comando) `mongodump`/`mongorestore` de MongoDB Database Tools contra local y Azure; comparación de conteos e índices pegada en la evidencia. |
| TC-TT-BD-05-09 | **P9.** Dado el mismo set de pruebas P2 y P4 a P7, cuando corre contra el contenedor local y contra Azure DocumentDB, entonces se registra una tabla de diferencias (operador, resultado, error o tiempo) y se decide qué imagen usará Testcontainers en TT-BD-07. | `Spike/DocumentDbParityTests.LocalAndAzureBehaveTheSame_TC_TT_BD_05_09`; parametrizada por destino. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - **Método:** primero local en Docker y después en Azure, con el mismo arnés y las mismas pruebas.
  - **Imágenes locales (nombres verificados con `docker manifest inspect` el 2026-10-20; amd64 y arm64):**
    - `ghcr.io/microsoft/documentdb/documentdb-local` — imagen local del motor open source de DocumentDB, el más cercano a Azure DocumentDB. **Principal** para la paridad (P9).
    - `mongo:8.0` (oficial) iniciada como réplica de un nodo (`--replSet rs0`), necesaria para transacciones (P4). **Referencia** de comportamiento MongoDB y del plan B (Atlas).
    - Fijar la etiqueta exacta (no `latest`) al implementar y anotarla en la evidencia.
  - **Arnés (propuesta, siguiendo el spike TT-API-02):** un proyecto de pruebas .NET aparte, `tests/Spike.DocumentDb/` (xUnit v2 + AwesomeAssertions, como el resto), con el **driver oficial `MongoDB.Driver`** y Testcontainers para el destino local. El destino se elige con la variable de entorno `KNOCK_SPIKE_MONGO` (sin ella, solo corre local; las pruebas de Azure se omiten). No toca `src/` de los módulos ni `Knock.slnx` del producto si el revisor prefiere mantenerlo fuera de la solución. P1 y P8 son comandos.
  - Script mínimo para cargar el GeoJSON en MongoDB y definir los índices del spike, dentro del arnés.
  - Entregable: `docs/evidencia/KNOCK-147-spike-documentdb.md` con el formato de `docs/evidencia/KNOCK-21-spike-firebase.md` (condiciones, qué funcionó, qué falta, riesgos, pendiente de una persona) más una tabla P1–P9 con pasa / falla / medición y la recomendación.
- **Out:**
  - Migrar el catálogo ni los módulos a MongoDB (TT-BD-06), adaptar el CI y Testcontainers del producto (TT-BD-07), índices definitivos (TT-BD-08), scripts de entrega (TT-BD-09).
  - Terraform del clúster (TT-AZ-02 / TT-AZ-05); en el spike el clúster se crea a mano y se borra o se deja documentado.
  - Crear una cuenta de Atlas: solo se compara con la documentación pública y con `mongo:8.0`, salvo que DocumentDB falle y el equipo pida probarla.
- **Dependencias:** ADR-002 y ADR-006; acceso a la suscripción Azure for Students (§8, pregunta a).

## 4. Contratos API y eventos
No se agrega endpoint ni evento. El «evento de outbox» de P4 es un documento de prueba en una colección del spike, no un contrato.

## 5. Modelo de datos
Colecciones **solo del spike**, en una base `knock_spike` que se borra al terminar:
- `colonias`: `{ _id, nombre, cp, municipio, piloto, geom: GeoJSON MultiPolygon | null, centro: GeoJSON Point }`; índices `2dsphere` en `geom` y en `centro`. Las 8 colonias con polígono salen de `db/seeds/colonias_tula.geojson`; las 4 sin polígono salen de `db/sepomex/tula_colonias.csv` (12 filas).
- `solicitudes` y `outbox`: documentos mínimos para P4.
- `oficios`: `{ clave, nombre }` con índice de texto `default_language: "spanish"` para P5.
- `tokens_revocados`: `{ jti, expiraEn }` con índice TTL `expireAfterSeconds: 0` sobre `expiraEn` para P6.

**Hallazgo:** las 4 colonias sin polígono **no tienen punto central en el repositorio**. El seed de PostgreSQL calcula el centro con `ST_PointOnSurface` y solo para las que tienen polígono. Para el respaldo `$near` de P2 hay que dar coordenadas aproximadas a esas 4 (autor y fecha documentados, igual que los polígonos) o probarlo con fixtures sintéticos (§8, pregunta d).

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| La cadena de conexión del clúster (usuario y contraseña) se filtra en el repositorio, que es público, o en logs, PR o prompts de IA. | Solo por variable de entorno o `dotnet user-secrets`; nunca en archivos versionados, salida de pruebas, capturas, evidencia, PR ni prompts a agentes; el arnés no imprime la cadena ni la incluye en mensajes de error. Gitleaks/Semgrep del CI como red de seguridad. | TC-TT-BD-05-01 · TC-TT-BD-05-07 |
| Clúster expuesto a Internet con credenciales débiles. | Regla de firewall solo para la IP de quien ejecuta el spike, contraseña generada y TLS obligatorio; borrar la regla (o el clúster) al terminar. | TC-TT-BD-05-01 |
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
| Comandos | Sí | P1 (creación del clúster) y P8 (`mongodump`/`mongorestore`); salida en la evidencia. |
| Rendimiento | Sí | P3: p95 < 100 ms con 200+ consultas tras calentar. |
| Unit, contrato, eventos, UI, mutación | No | El spike no agrega código de producto. |

**Gates:** `sdd_lint.py`, build y formato del proyecto del arnés, Semgrep y escaneo de secretos en verde; evidencia completa. `--trace TT-BD-05` fallará hasta que exista el arnés. Esta spec es un borrador: no se ha creado ningún clúster ni se ha ejecutado ninguna prueba.

**Regla de decisión:** si P1, P2, P4 o P7 fallan sin solución razonable, se recomienda el plan B (Atlas) y se actualiza ADR-002. Si fallan P3, P5, P6 o P8, se documenta el contorno (región, colación, Redis según ADR-006, `mongodump` programado) antes de descartar.

**Preguntas abiertas:**
- **(a) Acceso a Azure.** ¿Quién crea el clúster gratuito en West US y comparte la cadena de conexión fuera del repositorio (por ejemplo, por Key Vault o en persona)? Solo cabe un clúster gratuito por suscripción: decidir si se conserva para TT-BD-06 o se borra.
- **(b) Arnés.** ¿Proyecto de pruebas .NET (propuesta; reutilizable en TT-BD-07) o scripts de `mongosh`? ¿El proyecto entra en `Knock.slnx` y en el CI, o queda fuera hasta TT-BD-07?
- **(c) Scripts de índices y semillas.** ¿Dónde viven: dentro del arnés del spike, o ya en la ruta definitiva de TT-BD-09 (por ejemplo `db/mongo/`)?
- **(d) Centros de las 4 colonias sin polígono.** ¿Se dibujan coordenadas aproximadas versionadas o se usan fixtures sintéticos solo para el spike?

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
