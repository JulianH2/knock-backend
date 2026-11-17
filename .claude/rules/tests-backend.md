---
paths:
  - 'tests/**'
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/rules/tests-backend.md. No editar a mano. -->
# Reglas de pruebas (backend)

- Nombre con el caso: `Metodo_Escenario_Resultado_TC_HU_024_03`.
- Integración contra MongoDB (`documentdb-local`), Redis y Azurite reales (Testcontainers); nunca bases en memoria.
- Prohibido `Skip`, `[Ignore]`, asserts vacíos, bajar umbrales o reescribir una prueba para que acepte un comportamiento incorrecto.
- Fixtures de contrato (`tests/**/Fixtures`) se regeneran con `CONTRACT_CAPTURE=1`, no a mano.
- Skill: `testing-strategy-backend`.
