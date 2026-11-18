# Plan 023 — Zona por ubicación o código postal, con lista de espera

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Escribir el script de `db/` con `listas_espera` y sus índices (único `{ usuarioId, cp }`).
2. Ampliar el módulo `Catalog`: validador de ruta tipado para `cp`, `ZonaService` con la regla de cobertura y la caché positiva (24 h) y negativa (5 min), `ETag` y 304.
3. Crear `ListaEsperaService` y `ListaEsperaStore` con `updateOne` y `upsert` (201 o 200).
4. Crear `ZonasController` y `ListaEsperaController`, la opción `Catalogo:CiudadPiloto` y los límites.
5. Publicar contrato, endpoints y límites; marcar en la documentación que el borrador `_legacy/HU-009` queda reemplazado cuando una persona apruebe esta spec.

## Archivos que se crean o cambian
- `db/023-catalog-listas-espera.js`
- `src/Knock.Market.Api/Modules/Catalog/Controllers/` (`ZonasController`, `ListaEsperaController`)
- `src/Knock.Market.Api/Modules/Catalog/Dtos/` y `Validators/` (`CodigoPostalRoute`, `ListaEsperaRequest`)
- `src/Knock.Market.Api/Modules/Catalog/Services/` (`ZonaService`, `ListaEsperaService`)
- `src/Knock.Market.Api/Modules/Catalog/Data/` (`CatalogStore` ampliado, `ListaEsperaStore`)
- `src/Knock.Market.Api/Modules/Catalog/CatalogOptions.cs` (`CiudadPiloto`)
- `tests/` (clases `Spec023*`)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, OpenAPI regenerado, `Memoria.md`

## Dependencias
- TT-BD-06 (KNOCK-156) y TT-API-07 (KNOCK-141), ya en Listo; specs 003, 009 y 010.
- `MarketApiFactory` de la spec 020 (T-02); si aún no existe, esta spec lo puede crear primero y la 020 lo reutiliza.

## Orden y paralelismo
- Esta spec no depende de 020, 021 ni 022: se puede trabajar en paralelo con toda la cadena de cotizaciones.
- T-01, T-02 y T-03 se pueden hacer a la vez; T-04 y T-05 después.

## Verificación
- `python scripts/sdd_lint.py --trace 023` sin faltantes; comandos de `AGENTS.md`; OpenAPI regenerado; `Memoria.md` al día.
