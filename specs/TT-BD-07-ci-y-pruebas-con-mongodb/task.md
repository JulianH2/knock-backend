# TT-BD-07 — tareas

> Servicio afectado: Transversal (CI, pruebas y entorno local). Ninguna tarea empieza hasta que la spec esté Aprobada y #73 (TT-BD-06) esté fusionado. Lo marcado «Tech Lead» toca archivos protegidos.

## T1 — Pruebas de regresión de la configuración
- **Estado:** pendiente (spec Aprobada). Escribir primero y ver fallar contra el estado actual.
- **Verificar:** TC-TT-BD-07-01 · TC-TT-BD-07-02 · TC-TT-BD-07-03 · TC-TT-BD-07-04 · TC-TT-BD-07-05 · TC-TT-BD-07-06 · TC-TT-BD-07-07

## T2 — Compose local, `.env.example` y README
- **Estado:** pendiente. `documentdb-local` con el digest de `DocumentDbFixture.Image`, puerto en `127.0.0.1`, contraseña obligatoria; README sin PostgreSQL y con la plantilla de la cadena (marcadores, `tlsInsecure=true` solo local).
- **Verificar:** TC-TT-BD-07-06 · TC-TT-BD-07-07

## T3 — Instrucciones de agentes
- **Estado:** pendiente. Fuentes en `.agents/agents/db-builder.md`, `.agents/rules/tests-backend.md` y `.agents/rules/database.md`; regenerar con `python scripts/sync_agents.py` los nueve adaptadores (todos protegidos: **Tech Lead** o quien él autorice). `.agents/knock.json` (sin la verificación de sqlfluff): **Tech Lead**.
- **Verificar:** TC-TT-BD-07-07

## T4 — Workflow y Dependabot (Tech Lead)
- **Estado:** pendiente. `ci-pr.yml`: renombrar pasos, quitar el `if` de `DatabaseTest`, borrar `sql-lint`, la salida `sql` y su entrada en `gate.needs`; el comentario de las líneas 10 y 11; `dependabot.yml`: quitar `efcore`, agregar `MongoDB.*`; `.gitleaks.toml`: la regla para `mongodb://` la agrega el líder en el PR #101 (pregunta P7, resuelta).
- **Verificar:** TC-TT-BD-07-01 · TC-TT-BD-07-02 · TC-TT-BD-07-03

## T5 — CI en verde y gates
- **Estado:** pendiente. Evidencia de la corrida y conteos de `trx` en el PR.
- **Verificar:** TC-TT-BD-07-08 · TC-TT-BD-07-09

## Fuera de esta tarea (ya resuelto por el líder, 2026-10-28)
- Humo contra DocumentDB real y `$jsonSchema` de `modalidad` (P1: tarea aparte); imagen `1.0` (P2: se mantiene el digest del spike); `docker-compose.ci.yml` y job `dast` (P3: TT-ARQ-01 o TT-CD-01); skills con PostgreSQL (P4: tarea de documentación de agentes).
