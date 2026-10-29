# TT-BD-01 — PostgreSQL + PostGIS en Docker Compose y conexión con EF Core (Npgsql + NetTopologySuite) y migraciones

> **Jira:** KNOCK-22 · **Área:** Base de datos · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S0 · **Puntos:** 2
> **Repositorio:** knock-backend · **Ficha:** TT-BD-01_PostgresqlPostgisDockerCompose_Knock.docx
> **Skills:** story-workflow, testing-strategy-backend, security-core, database-scripts, foundation-backend
> **Estado:** Reemplazada (por TT-BD-06)

## 1. Objetivo
Levantar PostgreSQL con PostGIS en Docker Compose para desarrollo local, conectar la API de TT-API-01 a PostgreSQL con EF Core, Npgsql y NetTopologySuite, y crear la migración inicial. La base local debe prepararse con Docker Compose y la API debe poder aplicar la migración con un comando EF. La cadena de conexión y contraseñas se configuran fuera del repositorio.

## 2. Historia y casos de prueba
**TT-BD-01** — Tarea técnica. La preparación del compose puede avanzar de forma independiente; la conexión EF y la migración requieren la API y los contextos de TT-API-01 (KNOCK-20).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-01-01 | Dada la configuración local, cuando se ejecuta `docker compose -f deploy/docker-compose.local.yml up -d`, entonces arranca PostgreSQL con PostGIS y `SELECT postgis_version()` devuelve la versión instalada. | Verificación local: ejecutar el comando y consultar `SELECT postgis_version()` en el servicio PostgreSQL de Compose. |
| TC-TT-BD-01-02 | Dado PostgreSQL/PostGIS local iniciado y la API/contexto disponible, cuando se ejecuta `dotnet ef database update --project src/Knock.Api --context <Modulo>DbContext`, entonces aplica la migración inicial y crea la extensión PostGIS y los esquemas de los módulos requeridos. | Verificar en PostgreSQL la extensión y los esquemas esperados, y confirmar que EF no deja migraciones pendientes. |
| TC-TT-BD-01-03 | Dada la API de TT-API-01 y PostgreSQL disponible, cuando se consulta `/health/ready`, entonces responde 200; cuando PostgreSQL no está disponible y las demás dependencias de readiness permanecen arriba, responde 503. | Reutilizar el endpoint/health checks de TT-API-01; prueba de disponibilidad contra la base local sin duplicar su implementación. |
| TC-TT-BD-01-04 | Dada la fixture de pruebas de integración de TT-API-01, cuando corre una prueba de integración con Testcontainers, entonces inicia y usa una base PostGIS real. | Reutilizar `tests/TestSupport` y ejecutar la prueba de integración con la imagen versionada `postgis/postgis`. |
| TC-TT-BD-01-05 | Dada la configuración local de conexión, cuando se revisan archivos y se ejecuta la migración local, entonces no hay credenciales reales en archivos versionados y el comando local rechaza un host distinto de `localhost` o `127.0.0.1`. | Revisar `.gitignore`, `.env.example` sin valores reales, `appsettings*.json` y el resultado de Gitleaks; comprobar la validación del host local. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Crear `deploy/docker-compose.local.yml` para el PostgreSQL local con `postgis/postgis:16-3.4`, imagen con versión fija.
  - Requerir credenciales mediante variables, con validación de valor ausente, por ejemplo `${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}`; no incluir contraseñas en archivos versionados.
  - Ligar el puerto publicado a `127.0.0.1`, definir `healthcheck` y volumen persistente, y documentar cómo reiniciar los servicios. `down`/`up` no debe borrar el volumen; no usar `down -v` para un reinicio que preserve datos.
  - Conectar el backend de TT-API-01 con EF Core/Npgsql/NetTopologySuite y habilitar la extensión con `HasPostgresExtension("postgis")`.
  - Definir un esquema por módulo con `HasDefaultSchema(...)` y nombres `snake_case` mediante `EFCore.NamingConventions`; evaluar paquete, licencia, mantenimiento y vulnerabilidades antes de agregar la dependencia.
  - Crear la migración inicial que habilita PostGIS y crea los esquemas requeridos por los contextos; documentar el comando de aplicación `dotnet ef database update --project src/Knock.Api --context <Modulo>DbContext`.
  - Mantener `.env` ignorado, proporcionar `.env.example` sin valores reales y proteger el comando de migración local contra hosts remotos.
- **Out:**
  - Implementar de nuevo `/health/ready` o duplicar los checks de Npgsql y la fixture Testcontainers ya incluidos en TT-API-01; esta tarea los reutiliza y los verifica con PostGIS.
  - Agregar paquetes `AspNetCore.HealthChecks.*`; TT-API-01 decidió no usarlos.
  - Configurar Redis para esta tarea («para más adelante»); el archivo Compose local creado por KNOCK-22 no debe agregar Redis.
  - Provisionar, conectar o migrar bases de datos remotas o de producción.
- **Dependencia:** TT-API-01 (KNOCK-20). El compose/PostGIS es independiente; EF Core, contextos y migración dependen de la API de TT-API-01.

## 4. Contratos API y eventos
No se agregan endpoints ni eventos. `/health/ready` pertenece al alcance de TT-API-01 y se reutiliza, sin duplicar su implementación ni sus paquetes.

## 5. Modelo de datos
- Migración inicial de EF Core para habilitar la extensión PostGIS y crear los esquemas correspondientes a los módulos/contextos de API definidos en TT-API-01.
- Los contextos de módulo configuran `HasDefaultSchema(...)`; los identificadores físicos siguen `snake_case`.
- El proveedor es PostgreSQL mediante Npgsql y NetTopologySuite. No se fija aquí un modelo de entidades de negocio adicional.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Exposición de contraseñas/cadena de conexión en el repositorio. | Variables de entorno o user-secrets; `.env` ignorado y `.env.example` solo con nombres/placeholders; sin secretos en `appsettings*.json`; Gitleaks en verificación. | TC-TT-BD-01-05 |
| Migración accidental a una base remota. | La configuración de migración local permite solo `localhost` o `127.0.0.1` y rechaza un host distinto antes de conectarse; no aplicar este límite a configuración de otros entornos. | TC-TT-BD-01-05 |
| Exposición del servicio de base de datos a la red local. | Publicar el puerto solo en loopback (`127.0.0.1`) y proteger la contraseña con variable obligatoria. | TC-TT-BD-01-01 · TC-TT-BD-01-05 |
| API marcada lista aunque PostgreSQL no esté disponible. | Reutilizar el readiness existente de TT-API-01 y verificar respuesta 200/503 con la dependencia arriba/abajo. | TC-TT-BD-01-03 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Compose/PostGIS | Sí | Iniciar el servicio local y verificar `postgis_version()` (TC-TT-BD-01-01). |
| Migración/base de datos | Sí | Aplicar migración con EF Core, comprobar extensión/esquemas y ausencia de migraciones pendientes (TC-TT-BD-01-02). |
| Readiness | Sí | Comprobar 200 con PostgreSQL disponible y 503 al detenerlo, manteniendo las otras dependencias requeridas arriba; reutilizar los checks de TT-API-01 (TC-TT-BD-01-03). |
| Integración | Sí | Reutilizar `tests/TestSupport` de TT-API-01 y ejecutar prueba contra PostGIS con Testcontainers (TC-TT-BD-01-04). |
| Seguridad negativa/configuración | Sí | Gitleaks y revisión de configuración/host local; nunca incluir credenciales reales (TC-TT-BD-01-05). |
| Contrato de API/eventos | No | No se agrega endpoint ni evento. |
| Mutación | Según la meta vigente del módulo | La infraestructura de base de datos no define meta nueva de mutación. |
| Rendimiento, UI y accesibilidad | No | No pertenecen al alcance local de esta tarea. |

**Comandos locales de referencia:**
```bash
docker compose -f deploy/docker-compose.local.yml up -d
dotnet ef database update --project src/Knock.Api --context <Modulo>DbContext
```

**Gates:** aplicar `AGENTS.md §5` en lo que corresponda después de implementar; no se afirma aquí que los casos se hayan ejecutado.

**Límites y dudas abiertas:**
- Determinar qué `<Modulo>DbContext` aplica cada migración inicial en función de los contextos efectivamente entregados por TT-API-01.
- Evaluar `EFCore.NamingConventions` (necesidad, licencia, mantenimiento y vulnerabilidades) antes de añadir el paquete.
- Usar el nombre real del servicio de PostgreSQL definido en el compose para las instrucciones de verificación/reinicio.
- Redis se posterga; esta tarea verifica readiness de PostgreSQL reutilizando el health check existente sin alterar la cobertura total de dependencias de `/health/ready`.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-13
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
