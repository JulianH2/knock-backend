# TT-BD-01 — PostgreSQL + PostGIS en Docker Compose y conexión con EF Core (Npgsql + NetTopologySuite) y migraciones

> **Jira:** KNOCK-22 · **Área:** Base de datos · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S0 · **Puntos:** Por confirmar
> **Repositorio:** knock-backend · **Ficha:** TT-BD-01_PostgresqlPostgisDockerCompose_Knock.docx
> **Skills:** Por confirmar
> **Estado:** Aprobada

## 1. Objetivo
Levantar PostgreSQL con la extensión PostGIS en Docker Compose para desarrollo local, conectar la API con EF Core usando Npgsql y NetTopologySuite, y crear la migración inicial. La base local debe poder migrarse con un solo comando. La cadena de conexión debe permanecer fuera del repositorio, mediante user-secrets o variables de entorno.

## 2. Historia y casos de prueba
**TT-BD-01** — Tarea técnica. La verificación se realiza en el entorno local; el comando concreto y el mecanismo de comprobación de migración quedan por confirmar.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-01-01 | Dado el entorno local preparado, cuando se ejecuta el único comando acordado, entonces la base local queda migrada. | Ejecutar el comando local acordado y comprobar el estado migrado de la base; literal del comando y comprobación por confirmar. |
| TC-TT-BD-01-02 | Dada la configuración de conexión, cuando se revisa el repositorio y se inicia la base local usando user-secrets o variables de entorno, entonces la cadena de conexión no está guardada en el repositorio y la configuración externa permite conectar. | Inspección del diff y del contenido versionado, más ejecución local con la configuración externa; nombres de variables y evidencia por confirmar. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Base de datos local PostgreSQL/PostGIS preparada para Docker Compose.
  - Conexión de la API mediante EF Core con Npgsql y NetTopologySuite.
  - Migración inicial.
  - Migración de la base local mediante un solo comando.
  - Cadena de conexión fuera del repositorio, mediante user-secrets o variables de entorno.
- **Out:**
  - Provisionamiento o migración de bases de datos remotas o de producción.
  - Cualquier otro alcance no especificado para esta tarea; confirmar antes de aprobar.
- **Dependencia:** TT-API-01 (KNOCK-20).

## 4. Contratos API y eventos
No se definen endpoints ni eventos en esta tarea.

## 5. Modelo de datos
No se especifican entidades, esquemas ni cambios de modelo en los hechos disponibles para esta tarea. La relación de la migración con el estado de TT-API-01 queda por confirmar.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Exposición de la cadena de conexión o credenciales en el repositorio. | Mantener la cadena fuera del repositorio mediante user-secrets o variables de entorno; no incluir valores secretos en archivos versionados. | TC-TT-BD-01-02 |
| Cambios de esquema aplicados accidentalmente a una base remota. | Limitar esta tarea a la base local y verificar la configuración antes de ejecutar la migración; la selección concreta del destino queda por definir junto con la configuración local. | TC-TT-BD-01-01 · detalle del control pendiente de confirmación. |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Migración / integración de base de datos | Sí | Verificar en local que el comando acordado deja la base migrada (TC-TT-BD-01-01). La forma concreta de inspeccionar el estado queda por confirmar. |
| Revisión de seguridad negativa | Sí | Verificar que no se versiona la cadena de conexión y que la configuración externa funciona (TC-TT-BD-01-02). |
| Unitarias | No por ahora | Los hechos de esta tarea no describen lógica de dominio que requiera pruebas unitarias. |
| Contrato API / eventos | No | No se definen endpoints ni eventos. |
| Mutación | No por ahora | No se ha especificado lógica de negocio ni meta de mutación para esta tarea de infraestructura. |
| Rendimiento, UI y accesibilidad | No | No forman parte del objetivo local de base de datos. |

**Gates:** ver `AGENTS.md §5` en lo que aplique; no se afirma aquí que se hayan ejecutado.

**Límites y dudas abiertas:**
- Responsable, Sprint, Puntos, ficha de Jira/backlog y Skills oficiales de la tarea.
- Literal del único comando, forma de migración y comprobación verificable del estado migrado.
- Configuración local requerida y nombres de las variables de entorno, si se eligen; confirmar también cuándo usar user-secrets.
- Efecto preciso de la dependencia TT-API-01 (KNOCK-20) sobre el esquema/migración de esta tarea.
- Confirmar si el Out propuesto es suficiente y cómo se identifica de forma verificable que el destino es local.
- No se declara una meta de mutación hasta confirmar si esta tarea incorpora lógica de producto.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-13
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
