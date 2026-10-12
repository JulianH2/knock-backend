# TT-CI-01 — GitHub Actions en cada PR: job branch-name (nombre y combinación origen → destino), commits, formato, build y pruebas de la API y la app

> **Jira:** KNOCK-26 · **Área:** Integración continua · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 2
> **Repositorio:** knock-backend · **Ficha:** TT-CI-01_GithubActionsCadaPr_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Poner en marcha el CI de cada PR (`ci-pr`): validación de rama, commits y PR (`pr-meta`), gobierno SDD y arquitectura, SAST, dependencias, secretos, IaC y el check `gate` que será el único requerido. Los jobs de código, contenedor y DAST se saltan solos mientras no exista el proyecto.

## 2. Historia y casos de prueba
**TT-CI-01** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-CI-01-01 | Dado un PR con nombre de rama, commits o título incorrectos, entonces `pr-meta` falla y el check `gate` queda en rojo. | PR de prueba con una rama inválida (p. ej. `chore/KNOCK-26-Bad_Name`) después de fusionar KNOCK-26 |
| TC-TT-CI-01-02 | Dado un PR correcto que solo cambia configuración, entonces pasan `governance` (sync_agents, sdd_lint, check_exceptions, acciones fijadas a SHA, codemap sin escribir, jscpd, actionlint), `sast`, `secrets`, `iac` y `gate`, y los jobs de código se saltan. | Ejecución de `ci-pr` en el PR de KNOCK-26 (enlace al run); localmente `actionlint` y `jscpd` en OK |
| TC-TT-CI-01-03 | Dado un repositorio privado sin GitHub Advanced Security, entonces la revisión de dependencias y la subida de SARIF se saltan (con la variable `GHAS_ENABLED=true` se activan) sin quitar ningún escáner. | Registro del job `sca` y del paso de Semgrep en el run del PR |
| TC-TT-CI-01-04 | Dado el CI, entonces `gate` es el único check requerido por la protección de ramas. | `gh api repos/JulianH2/<repo>/branches/<rama>/protection` (lo configura una persona) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `.github/workflows/{ci-pr,_governance,ai-review}.yml` (`ai-review` solo se activa con `vars.AI_REVIEW`).
  - Configuración que `ci-pr` usa: `.gitleaks.toml`, `.semgrep/`, `.semgrepignore`.
  - `scripts/ci/pin_actions.py`, `scripts/check_coverage.py` y `docs/QA_DEVSECOPS.md`.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - `docker-compose.ci.yml` y `nginx.ci.conf` (cuando exista la API).
  - Jobs de contenedor y DAST (cuando exista un Dockerfile).
  - Despliegue (KNOCK-35 y KNOCK-61), CodeQL, Dependabot, Trivy y ZAP (KNOCK-64), nightly (KNOCK-41).
  - Configurar `gate` como check requerido en GitHub (lo hace una persona).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Inyección o escalada desde un workflow de PR | `pull_request` (no `pull_request_target`), permisos mínimos por job, sin secretos en jobs que ejecutan código del PR | TC-TT-CI-01-02 |
| Acciones de terceros comprometidas | Todas fijadas a SHA (`pin_actions.py --check`) | TC-TT-CI-01-02 |
| Secretos subidos al repositorio | Gitleaks en cada PR con `.gitleaks.toml` | TC-TT-CI-01-02 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- OSV-Scanner en un repositorio sin archivos de dependencias: comportamiento no verificado.
- Sin GitHub Advanced Security en repos privados no hay revisión de dependencias ni carga de SARIF.
- `sdd_lint --trace` solo pasa mientras no existan pruebas en el repositorio (casos manuales).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
