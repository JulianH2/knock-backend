# TT-BD-07 — tareas

> Servicio afectado: Transversal (CI, pruebas y entorno local). Ninguna tarea empieza hasta que la spec esté Aprobada y #73 (TT-BD-06) esté fusionado. Lo marcado «Tech Lead» toca archivos protegidos.

## T1 — Pruebas de regresión de la configuración
- **Estado:** pendiente de aprobación. Escribir primero y ver fallar contra el estado actual.
- **Verificar:** TC-TT-BD-07-01 · TC-TT-BD-07-02 · TC-TT-BD-07-03 · TC-TT-BD-07-04 · TC-TT-BD-07-05 · TC-TT-BD-07-06 · TC-TT-BD-07-07

## T2 — Compose local, `.env.example` y README
- **Estado:** pendiente. `documentdb-local` con el digest de `DocumentDbFixture.Image`, puerto en `127.0.0.1`, contraseña obligatoria; README sin PostgreSQL.
- **Verificar:** TC-TT-BD-07-06 · TC-TT-BD-07-07

## T3 — Instrucciones de agentes
- **Estado:** pendiente. Fuentes en `.agents/agents/db-builder.md`, `.agents/rules/tests-backend.md` y `.agents/rules/database.md`; regenerar con `python scripts/sync_agents.py`. `.agents/knock.json` (sin la verificación de sqlfluff): **Tech Lead**.
- **Verificar:** TC-TT-BD-07-07

## T4 — Workflow y Dependabot (Tech Lead)
- **Estado:** pendiente. `ci-pr.yml`: renombrar pasos, quitar el `if` de `DatabaseTest`, borrar `sql-lint`, la salida `sql` y su entrada en `gate.needs`; `dependabot.yml`: quitar `efcore`, agregar `MongoDB.*`.
- **Verificar:** TC-TT-BD-07-01 · TC-TT-BD-07-02 · TC-TT-BD-07-03

## T5 — CI en verde y gates
- **Estado:** pendiente. Evidencia de la corrida y conteos de `trx` en el PR.
- **Verificar:** TC-TT-BD-07-08 · TC-TT-BD-07-09

## Fuera de esta tarea (hasta que el líder responda)
- Humo contra DocumentDB real y `$jsonSchema` de `modalidad` (P1); imagen `1.0` (P2); `docker-compose.ci.yml` y job `dast` (P3); skills con PostgreSQL (P4).
