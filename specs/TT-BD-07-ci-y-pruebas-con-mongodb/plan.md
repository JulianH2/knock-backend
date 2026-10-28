# plan.md — TT-BD-07 — Adaptar CI y pruebas a MongoDB (Testcontainers); quitar sqlfluff y PostgreSQL

> Borrador. Servicio afectado: Transversal (CI, pruebas y entorno local). Falta la aprobación de la spec y las respuestas P1–P6 de spec §8. Los archivos protegidos los implementa o autoriza el Tech Lead.

## 1. Mapeo de archivos
- **Reutilizar:**
  - `tests/IntegrationTest/Support/DocumentDbFixture.cs` (de #73): `DocumentDbFixture.Image` es la única fuente del digest; el compose y la prueba del caso 05 la leen.
  - `tests/UnitTest/Architecture/` (de #73: `MongoAccessTests`, que ya prohíbe EF Core/Npgsql/NetTopologySuite en el ensamblado). Los casos de esta tarea revisan **archivos** (workflow, compose, paquetes), no el ensamblado; buscar con `python scripts/codemap.py --query "architecture repository file test"` si ya existe un ayudante para ubicar la raíz del repositorio.
  - `scripts/sync_agents.py` para regenerar los adaptadores (se ejecuta, no se edita).
- **Crear:** `tests/UnitTest/Architecture/PostgresRetirementTests.cs` (nombre por confirmar), con los casos 01 a 07.
- **Modificar (no protegidos):** `deploy/docker-compose.local.yml`, `.env.example`, `README.md`, `.agents/agents/db-builder.md`, `.agents/rules/tests-backend.md`, `.agents/rules/database.md`.
- **Modificar (protegidos, Tech Lead):** `.github/workflows/ci-pr.yml`, `.github/dependabot.yml`, `.agents/knock.json`; y los adaptadores generados que reescriba `sync_agents.py` (`.github/agents/db-builder.agent.md`, `.github/instructions/tests-backend.instructions.md`, `CLAUDE.md`/`.claude/` u otros si cambian).
- **No tocar:** `constitution.md`, `scripts/`, `security/`, `Directory.Build.props`, `.editorconfig`, `.jscpd.json`, `docs/architecture/`, `*.csproj`, `packages.lock.json` (#73 ya los dejó sin PostgreSQL), skills de `.agents/skills/` (pregunta P4), specs de otras tareas.

## 2. Riesgos y efectos secundarios
- **Orden:** si se implementa antes de fusionar #73, las pruebas de los casos 04 y 05 fallan (todavía hay `Testcontainers.PostgreSql` y no hay `DocumentDbFixture`).
- **`gate`:** borrar el job `sql-lint` sin quitarlo de `gate.needs` invalida el workflow; el PR debe mostrar `gate` en verde (caso 08).
- **Paso de `DatabaseTest` sin `if`:** si un PR futuro mueve el proyecto (TT-ARQ-01), el CI fallará en lugar de saltarlo; es lo buscado, pero TT-ARQ-01 debe actualizar la ruta.
- **Conexiones de documentdb-local (P7):** varias suites en paralelo contra contenedores propios pueden agotar recursos del runner; si pasa, serializar las suites, no subir reintentos.
- **Adaptadores generados:** `sync_agents.py` puede reescribir más archivos que los dos citados; todos van en el mismo PR y los revisa el Tech Lead.
- **Pruebas que leen archivos:** dependen de rutas relativas a la raíz; deben fallar con un mensaje claro si el archivo no existe (no pasar en vacío).
- **Compose:** la imagen de documentdb-local genera un certificado TLS autofirmado; el README debe explicar que `AllowInsecureTls` es solo local.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-BD-07-01 | `UnitTest/Architecture/PostgresRetirementTests` | arquitectura |
| TC-TT-BD-07-02 | `UnitTest/Architecture/PostgresRetirementTests` | arquitectura |
| TC-TT-BD-07-03 | `UnitTest/Architecture/PostgresRetirementTests` | arquitectura |
| TC-TT-BD-07-04 | `UnitTest/Architecture/PostgresRetirementTests` | arquitectura |
| TC-TT-BD-07-05 | `UnitTest/Architecture/PostgresRetirementTests` | arquitectura |
| TC-TT-BD-07-06 | `UnitTest/Architecture/PostgresRetirementTests` | arquitectura |
| TC-TT-BD-07-07 | `UnitTest/Architecture/PostgresRetirementTests` + `sync_agents.py --check` | arquitectura |
| TC-TT-BD-07-08 | corrida del CI del PR (conteos de `trx`) | comando (evidencia en el PR) |
| TC-TT-BD-07-09 | build, formato y scripts de gobierno | comando (evidencia en el PR) |

## 4. Secuencia
1. Aprobación de la spec y respuestas P1–P6; confirmar que #73 está fusionado en la rama destino.
2. Escribir las pruebas de los casos 01 a 07 y verlas fallar contra el estado actual.
3. Compose local, `.env.example` y README (casos 06 y 07).
4. Fuentes de `.agents/` y `python scripts/sync_agents.py` (caso 07).
5. Tech Lead: `ci-pr.yml`, `dependabot.yml` y `.agents/knock.json` (casos 01, 02, 03 y 07).
6. Abrir el PR y guardar la corrida del CI con los conteos de `trx` (caso 08); gates (caso 09); `Memoria.md`.

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build --filter "FullyQualifiedName~PostgresRetirementTests"
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest tests/SecurityTest tests/DatabaseTest -c Release --no-build   # requiere Docker
docker compose -f deploy/docker-compose.local.yml config --quiet
python scripts/sync_agents.py --check
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-BD-07
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
semgrep scan --config .semgrep/semgrep.yaml --error
```
