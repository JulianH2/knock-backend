# Plan 047 — Servicios y precios de cada técnico

> Parte de `spec.md` (estado Borrador). Este plan se confirma cuando la spec esté Aprobada.

## Enfoque
1. Scripts de `db/` con las colecciones e índices de §6 (idempotentes, con prueba de base de datos).
2. Módulo `TechnicianServices` en `Knock.Market.Api`: DTO, validadores, servicios y almacenes por colección.
3. Un controlador por recurso con las rutas de §5, filtrando siempre por el `sub` del token.
4. Caché, paginación por cursor y límites según las specs 009 y 010.
5. Pruebas por criterio, contrato OpenAPI, documentación y `Memoria.md`.

## Archivos que se crean o cambian
- `db/047-servicios-del-tecnico.js`
- `src/Knock.Market.Api/Modules/TechnicianServices/` (`Controllers`, `Dtos`, `Validators`, `Services`, `Data`)
- `tests/` (clases `Spec047*`)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, OpenAPI regenerado, `Memoria.md`

## Dependencias
- Spec 046.
- Spec 020.

## Verificación
- `python scripts/sdd_lint.py --trace 047` sin faltantes; comandos de `AGENTS.md`; OpenAPI regenerado; `Memoria.md` al día.
