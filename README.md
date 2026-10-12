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
cp deploy/.env.example deploy/.env   # y define las contraseñas locales
docker compose -f deploy/docker-compose.local.yml up -d
```
El proyecto .NET se crea en **TT-API-01** y la base en **TT-BD-01** (ver `specs/`).

## Estructura
`src/` API · `tests/` pruebas · `db/` scripts de base de datos · `deploy/` compose y Bicep · `specs/` · `docs/` · `security/` · `scripts/`
