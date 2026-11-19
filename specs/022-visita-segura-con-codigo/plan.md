# Plan 022 — Visita segura con código de 4 dígitos

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Agregar al script de `db/` los índices nuevos de `visitas` (`{ estado, codigoVenceEn }` y `{ tecnicoUsuarioId, estado, dia }`).
2. Escribir las reglas puras: ventana de la cita, motivo del código, `puedeLlegar`, `puedeAvisar` y la plantilla del aviso.
3. Escribir `VisitaService` con dos proyecciones (cliente y técnico) y el descifrado del código solo para el cliente.
4. Escribir `LlegadaService` con una sola actualización condicionada (intentos y bloqueo) y los eventos `VisitaEnCurso` y `VisitaBloqueada`.
5. Escribir `AvisoService` y los tres controllers.
6. Publicar contrato, endpoints y límites.

## Archivos que se crean o cambian
- `db/022-requests-visitas-indices.js`
- `src/Knock.Market.Api/Modules/Requests/Domain/` (`VentanaDeCita`, `ReglasDeCodigo`, `PlantillaAviso`)
- `src/Knock.Market.Api/Modules/Requests/Services/` (`VisitaService`, `LlegadaService`, `AvisoService`)
- `src/Knock.Market.Api/Modules/Requests/Controllers/` (`VisitasController`, `LlegadaController`, `AvisoController`)
- `src/Knock.Market.Api/Modules/Requests/Events/` (`VisitaEnCurso`, `VisitaBloqueada`, `AvisoConfianzaPreparado`)
- `tests/` (clases `Spec022*`)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, OpenAPI regenerado, `Memoria.md`

## Dependencias
- Spec 021 fusionada (crea `visitas`, el código cifrado y `CifradoDeCampo`).
- Specs 003, 009 y 010.

## Orden y paralelismo
- T-01 y T-02 se pueden empezar al aprobar la spec (script y reglas puras).
- T-03 (vista), T-04 (llegada) y T-05 (aviso) van en paralelo cuando la 021 esté fusionada.
- T-06 a T-08 al final.

## Riesgos
- El consumidor de `VisitaBloqueada` (reporte en Identidad) no existe; el evento queda en el buzón.

## Verificación
- `python scripts/sdd_lint.py --trace 022` sin faltantes; comandos de `AGENTS.md`; OpenAPI regenerado; `Memoria.md` al día.
