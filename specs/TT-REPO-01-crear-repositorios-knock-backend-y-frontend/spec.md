# TT-REPO-01 — Crear los repositorios knock-backend y knock-frontend en GitHub e invitar al equipo y a los docentes

> **Jira:** KNOCK-15 · **Área:** Repositorio · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 1
> **Repositorio:** knock-backend · **Ficha:** TT-REPO-01_CrearMonorepoKnockGithub_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Crear los repositorios privados `knock-backend` (API, scripts de BD, infraestructura) y `knock-frontend` (app Flutter y panel Angular) en GitHub, con el commit base, las ramas fijas y los ajustes de fusión, e invitar al equipo y a los docentes. Decisión del equipo: dos repositorios en lugar de un monorepo (`docs/DECISION_TECNICA.md §9`).

## 2. Historia y casos de prueba
**TT-REPO-01** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-REPO-01-01 | Dado GitHub, cuando se consultan los dos repositorios, entonces existen privados con `main`, `release`, `develop` y `sprint-00`; `develop` es la rama por defecto y `main` solo contiene el commit base (`.gitignore`, `.gitattributes`, `README.md`). | `gh repo view JulianH2/<repo> --json visibility,defaultBranchRef` y `git ls-remote --heads origin` en cada repositorio |
| TC-TT-REPO-01-02 | Dado el ajuste de fusión, entonces solo se permite «Rebase and merge» (merge commit y squash desactivados) y las ramas se borran al fusionar, de modo que `sprint-00` queda con historia lineal. | `gh repo view JulianH2/<repo> --json mergeCommitAllowed,squashMergeAllowed,rebaseMergeAllowed,deleteBranchOnMerge` y `git rev-list --merges sprint-00` = 0 |
| TC-TT-REPO-01-03 | Dado el equipo y los docentes, cuando se revisan los colaboradores de cada repositorio, entonces cada persona tiene el permiso mínimo que necesita (equipo: escritura; docentes: lectura). | Captura de Settings → Collaborators o `gh api repos/JulianH2/<repo>/collaborators` (pendiente: aún no se ha invitado a nadie) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Crear `knock-backend` y `knock-frontend` como repositorios privados de la cuenta `JulianH2`.
  - Commit base en `main` (KNOCK-15), ramas `release`, `develop` y `sprint-00`, `develop` por defecto.
  - Ajustes de fusión (solo rebase) y borrado automático de ramas.
  - Invitar al equipo y a los docentes con el permiso mínimo.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Protección de ramas y CODEOWNERS (TT-REPO-02).
  - Integración con Jira (TT-REPO-03).
  - Workflows de CI (TT-CI-01).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Acceso indebido al código o a datos del proyecto | Repositorios privados; permiso mínimo por persona; revisión de la lista de colaboradores | TC-TT-REPO-01-01 · TC-TT-REPO-01-03 |
| Secretos o datos reales subidos al repositorio | `.gitignore` base desde el primer commit; gitleaks en CI (KNOCK-26) | Revisión en KNOCK-26 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- Lista de colaboradores y docentes con su permiso (pendiente de invitar).
- La ficha `TT-REPO-01_CrearMonorepoKnockGithub_Knock.docx` y el título de KNOCK-15 en Jira siguen diciendo «monorepo»: hay que corregirlos.
- El commit base usa el scope `repo`, que no está en la lista actual de scopes (se creó antes de fijar la regla).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
