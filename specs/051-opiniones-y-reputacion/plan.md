# Plan 051 — Opiniones y reputación del técnico

> Parte de `spec.md` (estado Borrador). Este plan se confirma cuando la spec esté Aprobada.

## Enfoque
1. Scripts de `db/` con las colecciones e índices de §6 (idempotentes, con prueba de base de datos).
2. Módulo `Reviews` en `Knock.Market.Api`: DTO, validadores, servicios y almacenes por colección.
3. Un controlador por recurso con las rutas de §5, filtrando siempre por el `sub` del token.
4. Caché, paginación por cursor y límites según las specs 009 y 010.
5. Pruebas por criterio, contrato OpenAPI, documentación y `Memoria.md`.

## Archivos que se crean o cambian
- `db/051-opiniones-y-reputacion.js`
- `src/Knock.Market.Api/Modules/Reviews/` (`Controllers`, `Dtos`, `Validators`, `Services`, `Data`)
- `tests/` (clases `Spec051*`)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, OpenAPI regenerado, `Memoria.md`

## Dependencias
- Spec 022.
- Spec 031.
- Spec 032.

## Verificación
- `python scripts/sdd_lint.py --trace 051` sin faltantes; comandos de `AGENTS.md`; OpenAPI regenerado; `Memoria.md` al día.
