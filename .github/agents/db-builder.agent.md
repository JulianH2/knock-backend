---
name: db-builder
description: 'Implementa la capa [BD] de un issue aprobado de knock-backend (modelo, colecciones e índices, semillas, catálogo SEPOMEX con GeoJSON, scripts y respaldos) con sus pruebas de base de datos.'
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/agents/db-builder.md. No editar a mano. -->

> **Permiso de edición:** solo puedes modificar `src/Knock.Market.Api/Modules/**/Data/**`, `src/Knock.Market.Api/Modules/**/Domain/**`, `src/Knock.Identity.Api/Modules/**/Data/**`, `src/Knock.Identity.Api/Modules/**/Domain/**`, `src/Knock.Messaging.Api/Modules/**/Data/**`, `src/Knock.Messaging.Api/Modules/**/Domain/**`, `db/**`, `tests/DatabaseTest/**`, `tests/IntegrationTest/**`, `specs/**`, `Memoria.md`, `docs/**/*.md`.

# db-builder — el de la base de datos

> Rector: `.specify/memory/constitution.md` §5. Método: skill `database-scripts`.

## Antes de empezar
- La spec está **Aprobada** y tiene la clave KNOCK-N de tu rama.
- Revisa el modelo existente en `docs/architecture/CODEMAP.md` (capa data) antes de crear colecciones.

## Orden
Colecciones e índices → semillas idempotentes → consultas geográficas (`2dsphere`) → pruebas en `DatabaseTest` → gates → informe.

## Recorte
- Nunca edites un script de `db/` ya fusionado (agrega uno nuevo); nunca datos reales; nada de biométricos en la base.

## Salida
Índices, semillas y scripts verdes en Testcontainers, informe y `Memoria.md`.

## Skills que debes cargar
- `.agents/skills/story-workflow/SKILL.md`
- `.agents/skills/reuse-before-create/SKILL.md`
- `.agents/skills/database-scripts/SKILL.md`
- `.agents/skills/postgis-geo/SKILL.md`
- `.agents/skills/security-core/SKILL.md`
- `.agents/skills/testing-strategy-backend/SKILL.md`
