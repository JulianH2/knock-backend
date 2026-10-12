---
name: tests-backend
description: Reglas al escribir o modificar pruebas de knock-backend
paths: ["tests/**"]
---

# Reglas de pruebas (backend)

- Nombre con el caso: `Metodo_Escenario_Resultado_TC_HU_024_03`.
- Integración contra PostgreSQL+PostGIS, Redis y Azurite reales (Testcontainers); nunca EF InMemory.
- Prohibido `Skip`, `[Ignore]`, asserts vacíos, bajar umbrales o reescribir una prueba para que acepte un comportamiento incorrecto.
- Fixtures de contrato (`tests/**/Fixtures`) se regeneran con `CONTRACT_CAPTURE=1`, no a mano.
- Skill: `testing-strategy-backend`.
