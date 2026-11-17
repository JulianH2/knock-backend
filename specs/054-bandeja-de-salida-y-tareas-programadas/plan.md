# Plan 054 — Bandeja de salida y tareas programadas (INT-01)

> Parte de `spec.md` (estado Borrador). Este plan se confirma cuando la spec esté Aprobada.

## Enfoque
1. Scripts de `db/` con los índices y los TTL de `outbox` y `tareas_programadas` en las tres bases (idempotentes y con prueba de base de datos).
2. `Common/Outbox/` en Mercado e Identidad: escritura del evento en la misma transacción, `ProcesadorDeOutbox` y el cliente interno de avisos con el token de la spec 037.
3. `Common/TareasProgramadas/` en Identidad con las reglas del ejecutor de la spec 028; registro de manejadores por `tipo` y catálogo cerrado de plazos.
4. Limpieza por TTL, métricas, alerta y registros sin datos personales.
5. Pruebas con tiempo simulado y dos instancias, y documentación.

## Archivos que se crean o cambian
- `db/054-outbox-y-tareas-programadas.js`
- `src/Knock.Market.Api/Common/Outbox/` y `src/Knock.Identity.Api/Common/Outbox/`
- `src/Knock.Identity.Api/Common/TareasProgramadas/` (el de Mercado es de la spec 028)
- `tests/` (clases `Spec054*`)
- `docs/observabilidad.md`, `Memoria.md`

## Dependencias
- Specs 003, 006, 009, 011, 028, 029, 034 y 037; los manejadores de negocio llegan con las specs 025, 039, 050 y 051.

## Orden y paralelismo
- Los scripts de base de datos van primero; el procesador de la bandeja y el ejecutor de Identidad se pueden hacer a la vez; las pruebas de concurrencia y de plazos largos al final.

## Verificación
- `python scripts/sdd_lint.py --trace 054` sin faltantes; comandos de `AGENTS.md`; `Memoria.md` al día.
