# TT-SDD-01 — Constitución, AGENTS.md y skills base para los agentes de IA

> **Jira:** KNOCK-19 · **Área:** Desarrollo con IA · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 2
> **Repositorio:** knock-backend · **Ficha:** TT-SDD-01_ConstitucionAgentsMdSkills_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Dejar la base de desarrollo guiado por specs (SDD) con agentes de IA: constitución, `AGENTS.md`, skills, agentes y reglas por ruta como fuente única, adaptadores para cada herramienta, guardián de hooks y scripts de gobierno (`sdd_lint`, `codemap`, `check_exceptions`).

## 2. Historia y casos de prueba
**TT-SDD-01** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-SDD-01-01 | Dado cualquier agente (Claude Code, Codex, GitHub Copilot, Cursor, Gemini CLI, opencode), entonces lee las mismas reglas: `AGENTS.md` y la constitución son la fuente y los adaptadores están sincronizados. | `python scripts/sync_agents.py --check` en OK |
| TC-TT-SDD-01-02 | Dado el guardián, entonces bloquea la edición de archivos protegidos, la escritura de código sin spec Aprobada, los comandos peligrosos y la atribución de IA, y no deja terminar sin verificar. | Pruebas simuladas de `knock_guard.py` (`pre-tool` y `stop`) con entradas válidas e inválidas |
| TC-TT-SDD-01-03 | Dado `specs/`, entonces cada spec incluida tiene las secciones 1 a 8 y «Aprobación», un estado válido y su clave de Jira. | `python scripts/sdd_lint.py` en OK |
| TC-TT-SDD-01-04 | Dado el mapa de código, entonces `codemap.py --check` no escribe, detecta violaciones, clones y mapa desactualizado, y no cuenta la fecha ni la ruta. | `python scripts/codemap.py --check` y pruebas en una copia (mapa desactualizado, solo fecha o ruta distintas) |
| TC-TT-SDD-01-05 | Dado un agente, entonces nunca aprueba ni firma specs. | Guardián (los estados Aprobada y Firmada los escribe una persona) y revisión del PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `.specify/` (constitución y plantillas), `AGENTS.md`, `Memoria.md`, `.agents/**` y adaptadores generados (`.claude`, `.codex`, `.cursor`, `.gemini`, `.opencode`, `.github/{agents,instructions,hooks,copilot-instructions.md}`).
  - Guardián `scripts/hooks/knock_guard.py`, `sync_agents.py`, `sdd_lint.py`, `codemap.py`, `check_exceptions.py`, `import_backlog.py`, `security/exceptions.yaml`, `.jscpd.json` y el hook `pre-commit`.
  - Documentación `docs/{DECISION_TECNICA,SDD_GUIA,AGENTES_GUARDARRAILES}.md`, `docs/referencia/` y `docs/architecture/`.
  - Las specs de las tareas de gestión del Sprint 0 (KNOCK-15, 16, 17, 18, 19, 26, 27 y 28); el resto se incorpora por sprint.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Specs de las demás historias y tareas (se publican por sprint).
  - Activar los git hooks en cada equipo (`core.hooksPath`).
  - Workflows de CI (TT-CI-01).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Un agente edita archivos de seguridad, workflows o la constitución | Guardián con lista de archivos protegidos y `permissions.deny` generados | TC-TT-SDD-01-02 |
| Un agente aprueba o firma sus propias specs | El guardián lo impide; solo una persona escribe Aprobada/Firmada | TC-TT-SDD-01-05 |
| Código sin trazabilidad o con dependencias sin evaluar | Spec Aprobada obligatoria; `check_exceptions`; revisión en el PR | TC-TT-SDD-01-02 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- El guardián en Codex, Gemini CLI, Cursor y Copilot no está verificado en Windows (solo Claude Code, parcialmente).
- El hook `pre-commit` solo corre si se activa `core.hooksPath` (lo decide el Tech Lead).
- `sdd_lint.py --trace` no soporta casos verificados a mano: hoy pasa porque no hay pruebas en el repositorio.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
