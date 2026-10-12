# Knock (knock-backend) — instrucciones para el agente

<!-- GENERADO por scripts/sync_agents.py desde AGENTS.md. No editar a mano. -->

Lee y sigue **`AGENTS.md`** (manual operativo) y **`.specify/memory/constitution.md`** (reglas no negociables; mandan ante conflicto).

Resumen mínimo:
- Sigue la skill `story-workflow` en todo issue: leer → amenazas → plan de pruebas → implementar → verificar de verdad → informe honesto.
- Sin spec **Aprobada** en `specs/<ID>-*/` (con la clave KNOCK-N de tu rama) no se escribe código.
- Antes de crear algo, busca si ya existe: `python scripts/codemap.py --query "<qué necesitas>"`.
- Cada caso `TC-<ID>-KK` tiene una prueba con ese ID; prohibido desactivar pruebas, reglas o escáneres, o bajar umbrales.
- No edites archivos protegidos ni generados; no agregues dependencias sin evaluarlas.
- Al terminar, actualiza `Memoria.md`; nunca firmes aprobaciones.
- Commits en inglés: `type(scope): description (KNOCK-N)`. Nunca agregues atribución a IA (`Co-Authored-By` de agentes, «Generated with…») en commits o PR; un `Co-Authored-By` de una persona del equipo sí se permite; el uso de IA se declara solo en «Uso de IA» del PR.
- Skills en `.agents/skills/`, reglas por archivo en `.agents/rules/` (índice en `AGENTS.md §12`).
