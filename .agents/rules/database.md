---
name: database
description: Reglas al editar migraciones, semillas y scripts de base de datos de Knock
paths: ["db/**", "src/**/Migrations/**", "src/**/Data/**"]
---

# Reglas de base de datos

- Un esquema por módulo, `snake_case`, `uuid` v7, `timestamptz` UTC, `xmin` para concurrencia, restricciones en la base.
- Migraciones reversibles; nunca editar una ya fusionada; sin `DROP` de datos sin respaldo revisado.
- Semillas idempotentes; datos de demo solo ficticios y nunca en producción.
- Dirección y celular cifrados; CURP como HMAC; nada biométrico en la base.
- SQL de `db/` con `sqlfluff --dialect postgres`. Skill: `database-scripts`.
