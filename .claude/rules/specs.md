---
paths:
  - 'specs/**'
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/rules/specs.md. No editar a mano. -->
# Reglas de specs

- Carpeta `specs/<ID>-<slug>/` (HU-NNN, TT-ÁREA-NN, BUG-NNN) con spec, plan, tareas y checklist. Plantillas en `.specify/templates/`.
- Casos `TC-<ID>-KK` consecutivos, cada uno con su verificación o «(otro repo)».
- Estados: **Borrador** (agente) → **Aprobada** (persona; habilita el código; requiere KNOCK-N y revisor) → **Firmada** (tras el merge; no se edita).
- Los agentes nunca marcan Aprobada ni Firmada. Skill: `spec-first-writing`.
