---
name: api-contract-openapi
description: Mantiene el contrato REST de Knock (OpenAPI 3.1, docs/endpoints.md, ProblemDetails, versionado) y regenera el cliente Dart. Úsala cuando agregues o cambies un endpoint.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/api-contract-openapi/SKILL.md. No editar a mano. -->
# api-contract-openapi

## Qué hago
- El contrato se acuerda en la spec (§4) **antes** del código.
- `python scripts/export_openapi.py` arranca la API en modo de exportación y guarda `docs/openapi.v1.json`.
- `el workflow `contract-publish.yml` publica el contrato como artefacto y abre un PR en `knock-frontend` que actualiza `contracts/openapi.v1.json`; allí se regeneran los clientes Dart y TypeScript. **Nunca** se editan a mano.
- `python scripts/check_endpoints.py` compara `docs/endpoints.md` con los controllers.

## Convenciones
- Rutas en inglés, plural, kebab-case: `/api/v1/service-requests/{id}/quotes`.
- Acciones de estado como subrecurso con POST: `POST /quotes/{id}/accept`.
- Errores: ProblemDetails con `type`, `title`, `status`, `traceId`; validación 422 con `errors{campo:[...]}`.
- Códigos: 201 + `Location` al crear; 204 sin cuerpo; 409 por estado o concurrencia; 429 con `Retry-After`.
- Fechas ISO 8601 UTC; dinero `{ amount, currency: "MXN" }`.
- Cambio incompatible → nueva versión `v2`, nunca romper `v1`.

## Verificación
- `openapi-diff` en CI marca cambios incompatibles; deben estar justificados en la spec.
- Contract tests capturan el JSON real (`CONTRACT_CAPTURE=1`).
