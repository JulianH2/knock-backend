# TT-STD-01 — Reglas de codificación: .editorconfig, analizadores de .NET, flutter_lints, Conventional Commits y convención de ramas

> **Jira:** KNOCK-18 · **Área:** Reglas de codificación · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 2
> **Repositorio:** knock-backend · **Ficha:** TT-STD-01_ReglasCodificacionEditorconfigAnalizadores_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Fijar las reglas de codificación y de trabajo: `.editorconfig`, analizadores de .NET (`Directory.Build.props`), plantilla de `flutter_lints`, Conventional Commits en inglés con clave `KNOCK-N` y convención de ramas, con hooks locales y el mismo validador en el CI.

## 2. Historia y casos de prueba
**TT-STD-01** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-STD-01-01 | Dado un PR con nombre de rama, commit o título fuera de las reglas, entonces el CI falla (job `pr-meta`). | PR de prueba con rama inválida una vez fusionado KNOCK-26; antes, `scripts/ci/branch_rules.py` local |
| TC-TT-STD-01-02 | Dado el hook `commit-msg`, cuando el mensaje no cumple `type(scope): description (KNOCK-N)` en inglés, no lleva clave, la clave no coincide con la rama o trae atribución de IA (`Co-Authored-By` de agentes, «Generated with…»), entonces se rechaza; un `Co-Authored-By` de una persona del equipo se permite. | Pruebas manuales del hook con mensajes válidos e inválidos (8 de 8 con el resultado esperado en KNOCK-18) |
| TC-TT-STD-01-03 | Dado el hook `pre-push`, cuando se intenta empujar a `main`, `release`, `develop` o `sprint-XX`, o con un nombre de rama inválido, entonces se bloquea. | Prueba manual del hook en una copia con remoto local |
| TC-TT-STD-01-04 | Dado `.editorconfig` y `Directory.Build.props`, cuando exista código, entonces las advertencias son errores y el formato se verifica (`dotnet build -warnaserror`, `dotnet format --verify-no-changes`; `flutter analyze` con la plantilla en frontend). | Se verifica con TT-API-01 y TT-MOB-01 (aún no hay código) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `.editorconfig`, `Directory.Build.props` (backend) y `docs/templates/app/analysis_options.yaml` (frontend).
  - `scripts/ci/branch_rules.py`, hooks `commit-msg` y `pre-push`, `lib-python.sh` e `install_githooks.py`.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Workflows que ejecutan estas reglas (TT-CI-01).
  - Hook `pre-commit` y guardián de agentes (TT-SDD-01).
  - Plantillas de capas, mutación y panel (TT-QA-01).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Atribución de IA no declarada o commits sin trazabilidad | El hook y el CI rechazan trailers de agentes y exigen la clave de la rama | TC-TT-STD-01-02 |
| Push directo a ramas fijas | Hook `pre-push` y protección de ramas | TC-TT-STD-01-03 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- Los hooks requieren Python 3.11+ y Git Bash en Windows; en Linux/macOS no probados.
- El caso 4 depende de que exista código.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
