# Plan 021 — Comparar y elegir cotización

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Ampliar el script de `db/` con la colección `visitas` y sus índices, y los campos nuevos de `solicitudes`, `cotizaciones` y `tecnicos`.
2. Escribir `ComparadorDeCotizaciones` como función pura (orden, etiquetas, `cubre`, `tiposMixtos`, `puedeElegir`) con pruebas unitarias y de mutación.
3. Escribir `VisitaFactory` (código de 4 dígitos con generador criptográfico, cifrado con `CifradoDeCampo` y vencimiento).
4. Escribir `EleccionService` con una sesión de MongoDB: cuatro escrituras condicionadas y buzón.
5. Exponer `CotizacionesController` (listado) y `EleccionController` (elección) con la convención de errores de la spec 020.
6. Publicar contrato, endpoints, límites y semillas con contacto público.

## Archivos que se crean o cambian
- `db/021-requests-visitas.js`
- `src/Knock.Market.Api/Modules/Requests/Domain/` (`ComparadorDeCotizaciones`, `Visita`, `VisitaFactory`)
- `src/Knock.Market.Api/Modules/Requests/Services/EleccionService.cs`
- `src/Knock.Market.Api/Modules/Requests/Data/` (`TecnicosStore` de solo lectura, operaciones de `visitas`)
- `src/Knock.Market.Api/Modules/Requests/Controllers/` (`CotizacionesController` con el listado, `EleccionController`)
- `src/Knock.Market.Api/Modules/Requests/Events/` (`CotizacionAceptada`, `CotizacionRechazada`, `VisitaConfirmada`)
- `tests/` (clases `Spec021*` unitarias, de integración, concurrencia y seguridad negativa)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, OpenAPI regenerado, `Memoria.md`, semillas de KNOCK-246

## Dependencias
- Spec 020 fusionada (colecciones, `RequestsStore`, `MarketApiFactory`, `CifradoDeCampo`).
- Specs 003 (transacciones, outbox), 009, 010 y 013.

## Orden y paralelismo
- T-01 (modelo) y T-02 (comparador) se pueden empezar en cuanto la spec esté aprobada, incluso antes de que se fusione la 020, porque son funciones puras y un script.
- T-03 (código) va en paralelo con T-02.
- T-04 (elección) espera T-01, T-03 y la fusión de la 020.
- Contrato y semillas al final; solo esperan revisión.

## Riesgos
- Transacciones en DocumentDB; la prueba de concurrencia debe usar dos peticiones reales en paralelo.

## Verificación
- `python scripts/sdd_lint.py --trace 021` sin faltantes; comandos de `AGENTS.md`; `python scripts/export_openapi.py`; `Memoria.md` al día.
