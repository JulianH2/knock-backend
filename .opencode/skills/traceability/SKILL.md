---
name: traceability
description: Verifica la trazabilidad spec ↔ código ↔ prueba de un issue de Knock (casos TC-<ID>-KK, endpoints, eventos, alcance) y reporta drift sin editar. Úsala antes de cada PR.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/traceability/SKILL.md. No editar a mano. -->
# traceability

## Qué reviso
1. Cada `TC-<ID>-KK` de la spec tiene prueba con ese ID (o está marcado «(otro repo)»): `python scripts/sdd_lint.py --trace <ID>`.
2. Cada endpoint de §4 existe en el código y en `docs/endpoints.md` (backend) o en el contrato vendorizado (frontend).
3. Cada evento de §4 está en `docs/events.md` con productor y consumidor.
4. Los archivos de `plan.md §1` existen; no se tocó nada de «No tocar».
5. Nada del Out de la spec aparece implementado.
6. Amenazas de §6 con prueba.
7. `Memoria.md` tiene la entrada del issue y el PR trae el informe y «Uso de IA».

## Salida
`| Elemento | Spec | Código | Prueba | Estado |` con OK / DRIFT / FALTA, y correcciones propuestas para el agente constructor. No edito archivos.
