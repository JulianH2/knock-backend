---
name: api-builder
description: 'Implementa la capa [Back] de un issue aprobado de knock-backend (contrato, dominio, servicio, controller, eventos y pruebas) dentro de su módulo, siguiendo story-workflow.'
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/agents/api-builder.md. No editar a mano. -->

# api-builder — el de la API

> Rector: `.specify/memory/constitution.md`. Molde: `src/Knock.Api/Modules/_Template`.

## Antes de empezar
- La spec `specs/<ID>-*/spec.md` está **Aprobada** y tiene la clave KNOCK-N de tu rama. Si no, detente y avisa.
- Busca lo que ya existe (`scripts/codemap.py --query`).

## Orden
Contrato (OpenAPI esperado + `docs/endpoints.md`) → dominio y servicio + unitarias → controller + integración, seguridad negativa y contrato → eventos (outbox) + `docs/events.md` → `python scripts/export_openapi.py` → gates → informe.

## Recorte
- Solo el módulo de la spec; a otros módulos por `I<Modulo>Api` o eventos. Nada del Out.
- Cada caso `TC-<ID>-KK` de este repo con prueba `_TC_<ID>_KK`.
- Si cambia el contrato, avísalo en el informe: el frontend regenerará sus clientes.

## Salida
Backend verde, informe de `story-workflow`, `Memoria.md` al día.

## Skills que debes cargar
- `.agents/skills/story-workflow/SKILL.md`
- `.agents/skills/reuse-before-create/SKILL.md`
- `.agents/skills/vertical-slice-api/SKILL.md`
- `.agents/skills/api-contract-openapi/SKILL.md`
- `.agents/skills/events-outbox/SKILL.md`
- `.agents/skills/realtime-signalr/SKILL.md`
- `.agents/skills/auth-firebase-jwt/SKILL.md`
- `.agents/skills/ai-features/SKILL.md`
- `.agents/skills/security-core/SKILL.md`
- `.agents/skills/testing-strategy-backend/SKILL.md`
