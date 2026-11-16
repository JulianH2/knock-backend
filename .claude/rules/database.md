---
paths:
  - 'db/**'
  - 'src/**/Migrations/**'
  - 'src/**/Data/**'
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/rules/database.md. No editar a mano. -->
# Reglas de base de datos

- Una base lógica por servicio y colecciones en `camelCase`; fechas en UTC; llaves e índices (únicos, TTL, `2dsphere`) en el `<Modulo>Store`, con `EnsureIndexesAsync` idempotente.
- Sin migraciones: un índice o semilla ya fusionado no se edita, se agrega uno nuevo; sin borrar colecciones con datos sin respaldo revisado.
- Semillas idempotentes; datos de demo solo ficticios y nunca en producción.
- Dirección y celular cifrados; CURP como HMAC; nada biométrico en la base.
- Scripts y semillas de `db/` cubiertos por las pruebas de `DatabaseTest`. Skill: `database-scripts`.
