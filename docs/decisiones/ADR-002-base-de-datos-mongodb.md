# ADR-002 — Base de datos: MongoDB en Azure DocumentDB

> **Estado:** decidida por el Tech Lead el 2026-10-20 y **confirmada por el spike TT-BD-05 el 2026-10-23** (backend #48; evidencia en `docs/evidencia/KNOCK-147-spike-documentdb.md`). El plan B (Atlas) no se activa.
> **Reemplaza:** la sección 5 («Datos: PostgreSQL + PostGIS») y la fila «Base de datos» de la sección 1 de `docs/DECISION_TECNICA.md`.
> **Tarea:** TT-GES-06 · Impacto de implementación: TT-BD-05, TT-BD-06†, TT-BD-07†, TT-BD-08†.

## Contexto
Los lineamientos del curso (punto 14) piden bases de datos **NoSQL**; el número de bases depende del proyecto. Knock usaba PostgreSQL 16 con PostGIS y EF Core. El equipo decide pasar a NoSQL y, como ahora hay un servicio por dominio, **una base lógica por servicio**.

## Decisión
**MongoDB sobre Azure DocumentDB** (antes «Azure Cosmos DB for MongoDB vCore»), en su nivel gratuito, dentro de la suscripción Azure for Students. **Plan B:** MongoDB Atlas (servicio externo; el punto 13 de los lineamientos lo permite).

## Por qué MongoDB
- Mapea directo lo que ya resolvíamos con PostGIS: índice `2dsphere` y consulta de punto dentro de polígono para «colonia de un punto» y «técnicos cerca» (confirmado por el spike: P2).
- Transacciones entre documentos para las máquinas de estado de solicitudes, cotizaciones y citas (confirmado por el spike: P4).
- Índice de texto para la búsqueda por texto (HU-012): el índice de texto en español **no** sirve (P5); se usa un campo normalizado con búsqueda por prefijo.
- Controlador oficial para .NET; una base lógica por servicio dentro del mismo clúster.

## Opciones evaluadas
| Opción | Gratuito | Veredicto |
|---|---|---|
| **Azure DocumentDB (MongoDB)** | Clúster dedicado de 32 GB sin vencimiento; **un clúster gratuito por suscripción**; **sin alta disponibilidad ni respaldo y restauración**; **se pausa tras 60 días de inactividad**; entre nuestras 5 regiones permitidas solo en **West US, France Central y Norway East** ([Microsoft](https://learn.microsoft.com/en-us/azure/documentdb/free-tier), actualizada 2026-06) | ✅ Elegida |
| MongoDB Atlas M0 (Free) | 0.5 GB de almacenamiento, 500 conexiones, 100 operaciones por segundo, 10 GB de entrada y 10 GB de salida por semana; un clúster gratuito por proyecto; copias diarias ([MongoDB](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/)) | Plan B viable, con límites estrechos: 0.5 GB y 100 operaciones por segundo (la transferencia se frena al excederlos) |
| Cosmos DB for NoSQL | 1000 RU/s y 25 GB por cuenta | Transacciones solo dentro de una partición; menos natural para nuestras máquinas de estado |
| Firestore | 1 GiB, 50 000 lecturas y 20 000 escrituras al día; se corta al pasarse | Sin polígonos ni texto nativos; el tope diario de lecturas no alcanza para un mapa; está fuera de Azure |
| MongoDB en un contenedor propio | Solo el costo de cómputo | Obliga a operar réplicas y respaldos nosotros mismos |

## Reglas
1. **Cada servicio es dueño de su base lógica**; ningún otro servicio la consulta (ADR-001).
2. Las colecciones, llaves e índices se definen en la spec de cada historia y se versionan como **scripts de índices y semillas** (TT-BD-09†), que también son parte de la entrega.
3. **Sin respaldo automático:** se programa `mongodump` a un almacenamiento de Azure y se prueba la restauración (TT-BD-04).
4. **Redis** es un complemento opcional (caché y respaldo de SignalR), no una base principal. **La lista de revocación de tokens** (cierre de sesión, lineamiento 10) vive en una colección de MongoDB del servicio de Identidad con **índice TTL** sobre la expiración del token; solo pasa a Redis si ADR-006 lo aprueba.
5. Los secretos de conexión viven en Key Vault (TT-SEC-01); nunca en el repositorio, que es público.

## Qué se retira
EF Core, Npgsql, NetTopologySuite, las migraciones SQL, el esquema por módulo, la concurrencia con `xmin`, el job de `sqlfluff` y Testcontainers de PostgreSQL. Se agregan el controlador de MongoDB y Testcontainers de MongoDB (el spike lo confirmó con matices: P9; las pruebas usan `documentdb-local`, que reproduce P5, y difiere en P7 y P8).

## El spike (TT-BD-05) debe probar
1. Que DocumentDB gratuito se pueda crear en la suscripción de estudiante (región y restricciones).
2. Consulta de punto dentro de polígono con las colonias de Tula, y consulta de cercanía con `2dsphere`.
3. Una transacción real (crear solicitud + evento de outbox).
4. Índice de texto y de colación en español.
5. Conexiones de cuatro servicios al mismo clúster y pausa por inactividad.
6. `mongodump` y restauración.

Si falla alguno que no tenga solución razonable, se pasa al plan B y se registra el cambio aquí.

## Resultado del spike (2026-10-23)

Regla de decisión de la spec: si fallaban P1, P2, P4 o P7 sin solución razonable, se pasaba a Atlas. **Las cuatro pasan en Azure**, así que se sigue con DocumentDB.

- **P1 (clúster gratuito):** `westus`, nivel Free, servidor 8.0, 32 GB y gasto de 0.0 USD frente al presupuesto de 10 USD.
- **P2 (geo), P4 (transacción y outbox):** pasan en Azure.
- **P3 (latencia):** la consulta tarda 0.6 ms p95 en el servidor; de extremo a extremo son **160.6 ms p95 desde Tula** (red p95 105.7 ms), sin medir desde West US. Pasa por la definición de la spec (separa la consulta de la red). Se vuelve a medir desde la misma región en TT-AZ-02.
- **P5 (texto sin acentos): falla** con el índice de texto en español y también con la colación `es`, igual que en `documentdb-local`. Contorno válido: un campo `nombreBusqueda` que calcula la aplicación, con búsqueda por prefijo anclado y «ñ» = «n», sin stemming (TT-BD-06).
- **P6 (TTL):** pasa. El tiempo de borrado en Azure no se registró y se repite antes de TT-BD-07; según la documentación de MongoDB, el TTL solo borra tarde, nunca antes (no se midió en Azure).
- **P7 (conexiones):** 64 clientes sin rechazos es el tope de la prueba (al menos 256 conexiones medidas; Microsoft no publica una cifra para el nivel gratuito). Condición de adopción: `maxPoolSize` por servicio.
- **P8 (`mongodump` y restauración):** pasa, pero la restauración debe ser **serial**; en paralelo da `WriteConflict` (TT-BD-04).
- **P9 (diferencias entre destinos):** registradas en la evidencia. `documentdb-local` reproduce a Azure en P5 y difiere en P7 y P8; las pruebas de integración de TT-BD-07 usan esa imagen y una prueba de humo contra el clúster real.
- **Hallazgo del seed de colonias:** cuatro pares de polígonos se traslapan (Centro–La Huerta, Centro–San Lorenzo, El Cielito–San Lorenzo y El Llano 1a Sección–Tultengo); un punto ahí devuelve dos colonias. TT-BD-06 decide entre corregir los polígonos o una regla de desempate.
- **Pendiente de decisión:** la lista de revocación de tokens. La regla 4 de arriba la deja en MongoDB con TTL, lo que P6 respalda; la constitución 2.2.0 (borrador, pendiente de ratificar) reparte así: el `jti` del access token en Redis y la huella del refresh, permanente, en MongoDB. Hasta que el líder decida entre ambas, este ADR no cambia la regla 4.

## Consecuencias
Se reemplazan TT-BD-01 y TT-BD-02 (ya terminadas con PostgreSQL) por TT-BD-06†; se adaptan TT-API-01, TT-API-07, TT-BD-03, TT-BD-04, TT-QA-01 y el CI. († tarea planificada que se crea en Jira; ver el plan en [`docs/PLAN_ADAPTACION.md`](../PLAN_ADAPTACION.md).) Las historias de zona, mapa, búsqueda, solicitudes y panel cambian sus criterios (ver el plan de adaptación).
