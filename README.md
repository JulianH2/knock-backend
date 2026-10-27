# knock-backend

API y base de datos de **Knock** — gente de confianza, cerca de ti.
ASP.NET Core 10 (monolito modular, REST + OpenAPI) · PostgreSQL 16 + PostGIS · Redis · SignalR · Azure.
La app móvil y el panel interno están en **knock-frontend**.

## Empieza aquí
| Si quieres… | Lee |
|---|---|
| Entender la arquitectura y por qué | [`docs/DECISION_TECNICA.md`](docs/DECISION_TECNICA.md) |
| Las reglas no negociables | [`.specify/memory/constitution.md`](.specify/memory/constitution.md) |
| Trabajar en el código (personas o agentes) | [`AGENTS.md`](AGENTS.md) · [`CONTRIBUTING.md`](CONTRIBUTING.md) |
| Usar tu agente de IA con SDD | [`docs/SDD_GUIA.md`](docs/SDD_GUIA.md) · [`docs/AGENTES_GUARDARRAILES.md`](docs/AGENTES_GUARDARRAILES.md) |
| Ver las specs del backlog | [`specs/README.md`](specs/README.md) |
| Controles de calidad y seguridad | [`docs/QA_DEVSECOPS.md`](docs/QA_DEVSECOPS.md) · [`SECURITY.md`](SECURITY.md) |
| Ver dónde está cada clase y qué depende de qué | [`docs/architecture/CODEMAP.md`](docs/architecture/CODEMAP.md) · `docs/architecture/graph.html` |
| Configurar GitHub (ramas, environments) | [`docs/BRANCH_PROTECTION.md`](docs/BRANCH_PROTECTION.md) |

## Primeros pasos
```bash
python scripts/install_githooks.py
python scripts/sync_agents.py
cp .env.example .env                 # define POSTGRES_PASSWORD localmente
docker compose --env-file .env -f deploy/docker-compose.local.yml up -d
```
El proyecto .NET se crea en **TT-API-01** y la base en **TT-BD-01** (ver `specs/`).

Para verificar PostGIS: `docker compose --env-file .env -f deploy/docker-compose.local.yml exec -T postgres sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "SELECT postgis_version();"'`.
Para reiniciar PostgreSQL sin borrar los datos: `docker compose --env-file .env -f deploy/docker-compose.local.yml restart postgres`. No uses `down -v` para un reinicio que deba conservar el volumen.

Desde TT-BD-06 el catálogo vive en MongoDB (base `market`) y la API ya no usa PostgreSQL ni EF Core. La API recibe `ConnectionStrings:KnockMongo` mediante .NET User Secrets o la variable `ConnectionStrings__KnockMongo`; nunca va en `appsettings*.json`:
```bash
dotnet user-secrets set "ConnectionStrings:KnockMongo" "<cadena de tu MongoDB local>" --project src/Knock.Api
```
Las semillas del catálogo son `db/seeds/catalog/oficios.json` y `colonias.json` (`_id` fijos; ver `db/seeds/README.md`). Las aplica el cargador del módulo (`Modules/Catalog/Data/CatalogSeeder`): es repetible, crea los índices y hace upsert por clave natural. Hoy lo invocan las pruebas (`tests/DatabaseTest`, que levantan documentdb-local con Testcontainers); el script de entrega con `mongosh` es de TT-BD-09. El `docker-compose.local.yml` con PostGIS se rehace en TT-ARQ-01.

## Estructura
`src/` API · `tests/` pruebas · `db/` scripts de base de datos · `deploy/` compose y Bicep · `specs/` · `docs/` · `security/` · `scripts/`
