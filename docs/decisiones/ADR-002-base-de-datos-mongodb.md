# ADR-002 — Base de datos: MongoDB en Azure DocumentDB

> **Estado:** decidida por el Tech Lead el 2026-10-20; pendiente de revisión en el PR que la incorpora. **La confirmación técnica depende del spike TT-BD-05.**
> **Reemplaza:** la sección 5 («Datos: PostgreSQL + PostGIS») y la fila «Base de datos» de la sección 1 de `docs/DECISION_TECNICA.md`.
> **Tarea:** TT-GES-06 · Impacto de implementación: TT-BD-05, TT-BD-06, TT-BD-07, TT-BD-08.

## Contexto
Los lineamientos del curso (punto 14) piden bases de datos **NoSQL**; el número de bases depende del proyecto. Knock usaba PostgreSQL 16 con PostGIS y EF Core. El equipo decide pasar a NoSQL y, como ahora hay un servicio por dominio, **una base lógica por servicio**.

## Decisión
**MongoDB sobre Azure DocumentDB** (antes «Azure Cosmos DB for MongoDB vCore»), en su nivel gratuito, dentro de la suscripción Azure for Students. **Plan B:** MongoDB Atlas (servicio externo; el punto 13 de los lineamientos lo permite).

## Por qué MongoDB
- Mapea directo lo que ya resolvíamos con PostGIS: índice `2dsphere` y consulta de punto dentro de polígono para «colonia de un punto» y «técnicos cerca» **(confirmar en el spike)**.
- Transacciones entre documentos para las máquinas de estado de solicitudes, cotizaciones y citas **(confirmar en el spike)**.
- Índice de texto para la búsqueda por texto (HU-012) **(confirmar)**.
- Controlador oficial para .NET; una base lógica por servicio dentro del mismo clúster.

## Opciones evaluadas
| Opción | Gratuito | Veredicto |
|---|---|---|
| **Azure DocumentDB (MongoDB)** | Clúster dedicado de 32 GB sin vencimiento; sin alta disponibilidad ni respaldos automáticos; disponible solo en algunas regiones; posible pausa por inactividad **(confirmar)** | ✅ Elegida |
| MongoDB Atlas M0 | Según la documentación oficial, 100 conexiones, 100 operaciones por segundo y 1 GB por semana de entrada y de salida; un clúster gratis por proyecto | Plan B. Con varios servicios abriendo conexiones se queda corta |
| Cosmos DB for NoSQL | 1000 RU/s y 25 GB por cuenta | Transacciones solo dentro de una partición; menos natural para nuestras máquinas de estado |
| Firestore | 1 GiB, 50 000 lecturas y 20 000 escrituras al día; se corta al pasarse | Sin polígonos ni texto nativos; el tope diario de lecturas no alcanza para un mapa; está fuera de Azure |
| MongoDB en un contenedor propio | Solo el costo de cómputo | Obliga a operar réplicas y respaldos nosotros mismos |

## Reglas
1. **Cada servicio es dueño de su base lógica**; ningún otro servicio la consulta (ADR-001).
2. Las colecciones, llaves e índices se definen en la spec de cada historia y se versionan como **scripts de índices y semillas** (TT-BD-09), que también son parte de la entrega.
3. **Sin respaldo automático:** se programa `mongodump` a un almacenamiento de Azure y se prueba la restauración (TT-BD-04).
4. **Redis** es un complemento opcional (caché, bloqueo de tokens, respaldo de SignalR), no una base principal.
5. Los secretos de conexión viven en Key Vault (TT-SEC-01); nunca en el repositorio, que es público.

## Qué se retira
EF Core, Npgsql, NetTopologySuite, las migraciones SQL, el esquema por módulo, la concurrencia con `xmin`, el job de `sqlfluff` y Testcontainers de PostgreSQL. Se agregan el controlador de MongoDB y Testcontainers de MongoDB **(confirmar que el contenedor de pruebas reproduce lo que hace DocumentDB)**.

## El spike (TT-BD-05) debe probar
1. Que DocumentDB gratuito se pueda crear en la suscripción de estudiante (región y restricciones).
2. Consulta de punto dentro de polígono con las colonias de Tula, y consulta de cercanía con `2dsphere`.
3. Una transacción real (crear solicitud + evento de outbox).
4. Índice de texto y de colación en español.
5. Conexiones de cuatro servicios al mismo clúster y pausa por inactividad.
6. `mongodump` y restauración.

Si falla alguno que no tenga solución razonable, se pasa al plan B y se registra el cambio aquí.

## Consecuencias
Se reemplazan TT-BD-01 y TT-BD-02 (ya terminadas con PostgreSQL) por TT-BD-06; se adaptan TT-API-01, TT-API-07, TT-BD-03, TT-BD-04, TT-QA-01 y el CI. Las historias de zona, mapa, búsqueda, solicitudes y panel cambian sus criterios (ver el plan de adaptación).
