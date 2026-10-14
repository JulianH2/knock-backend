# plan.md — TT-BD-01 — PostgreSQL/PostGIS local con Docker Compose y EF Core

## 1. Mapeo de archivos
- **Reutilizar:** `deploy/docker-compose.local.yml` y el health/readiness y fixture `tests/TestSupport` provistos por TT-API-01; no duplicar `/health/ready` ni los checks de Npgsql.
- **Crear:** configuración local PostgreSQL/PostGIS, migración inicial para extensión y esquemas, y protección de la configuración local para que no acepte hosts remotos.
- **Modificar:** contextos EF Core de los módulos de TT-API-01 para PostGIS, esquema predeterminado y nombres `snake_case`, solo en el alcance aprobado.
- **No tocar:** contraseñas/cadenas reales versionadas, bases remotas/producción, paquetes `AspNetCore.HealthChecks.*` ni Redis para este issue.

## 2. Riesgos y efectos secundarios
- Exposición de credenciales: variables/user-secrets, `.env` ignorado, `.env.example` con placeholders y Gitleaks (TC-TT-BD-01-05).
- Migrar un destino remoto por error: rechazar hosts distintos a `localhost` y `127.0.0.1` en configuración local (TC-TT-BD-01-05).
- Perder datos por borrar el volumen: documentar reinicio que preserve volumen y no usar `down -v` (TC-TT-BD-01-01).
- Duplicar o contradecir la infraestructura de TT-API-01: reutilizar readiness y fixture de Testcontainers, y separar Compose independiente del trabajo EF dependiente de API (TC-TT-BD-01-03, 01-04).
- Dependencia nueva `EFCore.NamingConventions`: evaluar licencia, mantenimiento y vulnerabilidades antes de añadirla.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite / verificación |
|---|---|---|
| TC-TT-BD-01-01 | Levantar Compose y consultar `SELECT postgis_version()`. | Verificación local documentada. |
| TC-TT-BD-01-02 | Aplicar migración inicial; comprobar extensión, esquemas y migraciones pendientes. | `DatabaseTest`, con PostgreSQL/PostGIS real. |
| TC-TT-BD-01-03 | `/health/ready` responde 200 con PostgreSQL y 503 cuando solo PostgreSQL falla. | Reutilizar prueba/readiness de TT-API-01; no duplicar health checks. |
| TC-TT-BD-01-04 | La fixture de integración inicia un contenedor PostGIS y ejecuta una prueba. | Testcontainers y `tests/TestSupport` de TT-API-01. |
| TC-TT-BD-01-05 | Gitleaks/config review y rechazo de host remoto en perfil local. | Seguridad negativa y prueba de validación del host. |

## 4. Secuencia
1. **Entregable A, independiente de TT-API-01:** revisar Compose local; asegurar imagen fija, contraseña requerida por variable, puerto loopback, `healthcheck` y volumen persistente; documentar inicio y reinicio seguro; verificar `postgis_version()`.
2. Mantener fuera de esta tarea la configuración de Redis («para más adelante») y no borrar los servicios ya definidos en la composición local que pertenezcan a otras tareas.
3. **Entregable B, dependiente de TT-API-01:** conectar Npgsql/NetTopologySuite en los contextos existentes, establecer `HasDefaultSchema(...)`, PostGIS y `snake_case` tras evaluar paquete, y crear migración inicial de extensión y esquemas.
4. Reutilizar `/health/ready` y la fixture de Testcontainers que TT-API-01 ya incluye; comprobar readiness y ejecución con `postgis/postgis`.
5. Verificar `.env`/`.env.example`, Gitleaks y guardia de host local; registrar resultados por cada TC.

## 5. Verificación (comandos exactos)
```bash
docker compose -f deploy/docker-compose.local.yml up -d
dotnet ef database update --project src/Knock.Api --context <Modulo>DbContext
python scripts/sdd_lint.py --trace TT-BD-01
```
La ejecución de pruebas específicas de integración dependerá de las clases/contextos que provea TT-API-01; no se afirma que se hayan ejecutado en este borrador.
