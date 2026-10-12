# TT-REPO-02 — Ramas main, release, develop y sprint-00 (como ITACR); CONTRIBUTING.md con el flujo; plantillas de PR e issues; CODEOWNERS

> **Jira:** KNOCK-16 · **Área:** Repositorio · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 1
> **Repositorio:** knock-backend · **Ficha:** TT-REPO-02_RamasMainReleaseDevelop_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Dejar definido el flujo de ramas (`main` ← `release` ← `develop` ← `sprint-XX` ← `<tipo>/KNOCK-N-<descripcion>`), documentado en `CONTRIBUTING.md`, con plantillas de PR e issues, guía de protección de ramas y CODEOWNERS.

## 2. Historia y casos de prueba
**TT-REPO-02** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-REPO-02-01 | Dado el repositorio, entonces las ramas fijas existen y el flujo está documentado en `CONTRIBUTING.md` (sección 2). | `git ls-remote --heads origin` y revisión de `CONTRIBUTING.md §2` |
| TC-TT-REPO-02-02 | Dado un issue o PR nuevo, entonces GitHub ofrece los formularios de issue (bug, historia, tarea técnica) y la plantilla de PR con las secciones Criterios, Pruebas y Uso de IA. | Abrir un issue y un PR de prueba; `.github/ISSUE_TEMPLATE/*.yml` se cargan como YAML válido (verificado en KNOCK-16) |
| TC-TT-REPO-02-03 | Dado `docs/BRANCH_PROTECTION.md`, entonces la protección de `main`, `release`, `develop` y `sprint-*` (PR obligatorio, 1 aprobación, check `gate`, historia lineal) está documentada y aplicada en GitHub. | `gh api repos/JulianH2/<repo>/branches/<rama>/protection` (documentada; aplicación pendiente: la hace una persona, no un agente) |
| TC-TT-REPO-02-04 | Dado un `CODEOWNERS` con usuarios reales, cuando un PR toca zonas sensibles, entonces GitHub pide la revisión de su dueño. | PR de prueba que toque `.github/` (pendiente: faltan los usuarios de GitHub) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `CONTRIBUTING.md` (ramas, commits en inglés, PR, activación de hooks, requisito de Python).
  - Plantillas `.github/PULL_REQUEST_TEMPLATE.md` y `.github/ISSUE_TEMPLATE/*`.
  - `docs/BRANCH_PROTECTION.md`, `SECURITY.md` y `.gitignore` ampliado.
  - `CODEOWNERS` cuando se tengan los usuarios.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Workflows que validan nombres de rama y commits (TT-CI-01).
  - Configurar la protección en GitHub desde un agente (la aplica una persona).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Push directo o fusión sin revisión a ramas fijas | Protección de ramas (PR + 1 aprobación + `gate`); historia lineal | TC-TT-REPO-02-03 |
| Cambios sensibles sin revisor adecuado | CODEOWNERS en `.github/`, `scripts/`, `security/` y constitución | TC-TT-REPO-02-04 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- Usuarios de GitHub para `CODEOWNERS`.
- El check requerido `gate` solo puede exigirse cuando exista (KNOCK-26): la protección se completa después.
- Mientras no haya colaboradores, el Tech Lead fusiona su propio PR con un comentario que lo explique.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
