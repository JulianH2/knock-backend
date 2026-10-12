# Guía SDD — cómo trabajar Knock con agentes de IA

> Método del profesor (repos `API_SDD` y `WebAPIDevSecOpsScallingSDD`) adaptado a Knock, a dos repositorios y a **cualquier agente**.

## 1. La idea
Las personas deciden **qué** se construye (constitución + specs aprobadas); los agentes lo construyen y lo revisan **dentro de esas reglas**; las pruebas, los hooks y el CI dicen cuándo está terminado.

## 2. Las piezas
| Pieza | Archivo | Quién la edita |
|---|---|---|
| Constitución (idéntica en ambos repos) | `.specify/memory/constitution.md` | Equipo, PR en los dos repos |
| Manual operativo | `AGENTS.md` | Tech Lead |
| Specs por issue | `specs/<ID>-<slug>/` (HU-NNN, TT-ÁREA-NN, BUG-NNN) | `spec-writer` + aprobación humana |
| Skills | `.agents/skills/*/SKILL.md` | Equipo |
| Agentes | `.agents/agents/*.md` | Equipo |
| Reglas por ruta | `.agents/rules/*.md` | Equipo |
| Guardián (hooks) | `scripts/hooks/knock_guard.py` + `.agents/knock.json` | Tech Lead |
| Mapa de código | `docs/architecture/` (generado) | `scripts/codemap.py` |
| Bitácora | `Memoria.md` | Agentes + equipo |

Detalle de cada herramienta y de lo que bloquea el guardián: `docs/AGENTES_GUARDARRAILES.md`.

## 3. Puesta en marcha (una vez por clon)
```bash
python scripts/install_githooks.py      # git hooks: pre-commit, commit-msg, pre-push
python scripts/sync_agents.py           # genera los archivos de cada agente
python scripts/codemap.py               # genera el mapa de código
```
En Windows consulta «Python (requisito)» en `CONTRIBUTING.md`. No uses `KNOCK_PYTHON` al correr `sync_agents.py`: cambia los comandos generados y el CI marcaría diferencias.

## 4. Flujo de un issue
```
Jira KNOCK-N (HU/TT/BUG)            rama <tipo>/KNOCK-N-<desc> desde sprint-XX
        │
        ▼
spec-writer refina specs/<ID>/  ──►  persona aprueba (Estado: Aprobada + KNOCK-N + revisor)
        │
        ▼
db-builder / api-builder  (backend)      flutter-builder / panel-builder  (frontend)
        │      el guardián bloquea lo que rompe reglas y no deja terminar sin verificar
        ▼
security-reviewer (PASS/FAIL) ──► traceability-clerk (drift) ──► docs-keeper
        │
        ▼
PR «KNOCK-N · <ID> título» a sprint-XX  ──►  ci-pr (gate) + 1 revisor + CODEOWNERS  ──► merge ──► spec Firmada
```
Historias que tocan ambos repositorios: un PR en cada uno con la misma clave; el backend va primero si cambia el contrato.

## 5. Prompts de ejemplo
- **Refinar spec:** «Usa el agente `spec-writer` para refinar `specs/HU-024-…`. La clave de Jira es KNOCK-45. Deja dudas en §8.»
- **Backend:** «Usa `api-builder` para implementar HU-024 (spec aprobada). Sigue `story-workflow` y entrega el informe.»
- **Base de datos:** «Usa `db-builder` para la capa [BD] de HU-024.»
- **App:** «Usa `flutter-builder` para la capa [Mobile] de HU-024; la pantalla es la de pedir cotización de la maqueta.»
- **Panel:** «Usa `panel-builder` para la capa [Panel] de HU-0NN.»
- **Revisar:** «Usa `security-reviewer` sobre el diff contra sprint-02.» · «Usa `traceability-clerk` sobre HU-024.»
- **Auditar el repositorio:** «Usa la skill `devsecops-audit`.»

## 6. Reglas de oro
1. Sin spec aprobada no hay código (el guardián lo impide).
2. Antes de crear, buscar (`codemap.py --query`).
3. Los revisores nunca editan.
4. Nadie edita archivos generados ni protegidos.
5. Las personas aprueban y firman.
6. Nada se declara pasado sin ejecutarlo; lo que no se pudo correr se informa.
7. Lo aprendido va a `Memoria.md`; si se repite, se vuelve regla o skill.

## 7. Agregar una skill, agente o regla
1. Crea el archivo en `.agents/skills/<n>/SKILL.md`, `.agents/agents/<n>.md` o `.agents/rules/<n>.md` (frontmatter `name`, `description`; agentes: `can_edit`, `can_run_commands`, `skills`; reglas: `paths`).
2. `python scripts/sync_agents.py` y commit en el mismo PR.
