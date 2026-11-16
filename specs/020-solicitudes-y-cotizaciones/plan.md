# Plan 020 — Solicitudes y cotizaciones (pedir y enviar)

> Parte de `spec.md` (estado Borrador). Este plan se confirma cuando la spec esté Aprobada.

## Enfoque
1. Traer a Mercado el módulo de archivos privados (`Media`, TT-API-05) desde `legacy/` o, si ya llegó con la spec 013, reutilizar su `IMediaApi`; sin él no hay fotos y la cadena se bloquea.
2. Crear el host de integración de Mercado (`MarketApiFactory` con `Testcontainers`) que también usan las specs 021, 022 y 023.
3. Escribir el script de `db/` con `solicitudes` y `cotizaciones` y sus índices (único `{ solicitudId, tecnicoId }`).
4. Crear el módulo `Requests` como rebanada vertical: dominio (estados, tope), validadores, servicios, almacén y controllers.
5. Integrar catálogo (oficios y colonias con caché de 24 h), moderación (spec 013), cifrado de campo de la dirección y buzón de salida.
6. Publicar el contrato (OpenAPI de Mercado), las filas de `docs/endpoints.md` y los límites en `docs/rate-limit-matrix.md`.
7. Extender las semillas (KNOCK-246) con solicitudes y cotizaciones ficticias.

## Archivos que se crean o cambian
- `db/020-requests-solicitudes-cotizaciones.js`
- `src/Knock.Market.Api/Modules/Requests/Controllers/` (`SolicitudesController`, `CotizacionesController`)
- `src/Knock.Market.Api/Modules/Requests/Dtos/` y `Validators/` (creación, cotización, consulta del listado)
- `src/Knock.Market.Api/Modules/Requests/Domain/` (`Solicitud`, `Cotizacion`, estados, `CotizacionTopeCalculator`)
- `src/Knock.Market.Api/Modules/Requests/Services/` (`SolicitudService`, `CotizacionService`)
- `src/Knock.Market.Api/Modules/Requests/Data/` (`RequestsStore`, `ICatalogoOficiosYColonias`)
- `src/Knock.Market.Api/Modules/Requests/Events/` (`SolicitudCreada`, `CotizacionEnviada`)
- `src/Knock.Market.Api/Modules/Media/` (traído de `legacy/` si no existe) y `src/Knock.Market.Api/Common/Crypto/CifradoDeCampo.cs`
- `src/Knock.Market.Api/appsettings.json` (precios del tope, límites, dominios)
- `tests/IntegrationTest/Market/MarketApiFactory.cs` y las clases `Spec020*`
- `tests/UnitTest/Architecture/ModuleCatalog.cs` (módulos `Requests` y `Media`)
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `docs/openapi.v1.json` (regenerado por script, nunca a mano), `Memoria.md`

## Dependencias
- Specs 002, 003, 009, 010, 013 y 017; TT-API-05 (KNOCK-42) y KNOCK-208 (fase de Blob de la spec 013).
- Catálogo de oficios y colonias del módulo `Catalog` (TT-BD-06, KNOCK-156).
- Llave de cifrado de campo en Key Vault y Azure AI Content Safety (con moderador falso en pruebas).

## Decisiones de la spec que este plan sigue
- Fotos privadas por `archivoId`; tope en el servidor; dirección cifrada; sin caché personal; cupo de 5 cotizaciones vivas; convención de errores común de §5.

## Orden y paralelismo
- **Bloquean todo:** T-01 (archivos privados) y T-02 (host de integración). Pueden hacerse a la vez por dos personas.
- **En paralelo después de T-03:** la rama de solicitudes (T-04 a T-07, Guillermo) y la de cotizaciones (T-08 a T-10, Alexis).
- **Al final:** contrato, semillas y gates (T-12 a T-14), que solo esperan revisión.

## Riesgos
- Transacciones de DocumentDB (se comprueban en el despliegue, como en la spec 015).
- El módulo `Media` de `legacy/` puede necesitar ajustes al pasar a .NET 11 y a la base `market`.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con la prueba de la tabla 4.1 (`python scripts/sdd_lint.py --trace 020`).
- `dotnet build Knock.slnx -c Release`, `dotnet test Knock.slnx -c Release`, `dotnet format Knock.slnx --verify-no-changes` y `python scripts/export_openapi.py`; se actualiza `Memoria.md`.

## Decisiones del líder del 2026-11-12 (KNOCK-59, rama `feature/KNOCK-59-requests-service`)
- **Fotos y moderación:** el servicio de solicitudes trabaja contra `IMediaApi` e `IContentModerator` (interfaces propias del módulo `Requests`, con dobles de prueba), sin esperar el #33. **CA-15 y CA-18 con el servicio real se cierran cuando llegue OI-2 del #33.**
- **Buzón de salida (`outbox`):** no existe en `src`; la spec 003 solo define la colección. Se hace una versión mínima en Mercado detrás de `IOutbox` (escritura en la misma transacción, `_id` determinista). El despachador y el transporte los cubre la spec 037. Sin él no se cierra CA-01.
- **Elegibilidad del técnico (R-23):** detrás de una interfaz con un doble hasta que exista la colección `tecnicos` (spec 027). **CA-13 y CA-23 no se cierran hasta entonces.**
- **`ICatalogApi`:** se amplía solo de lectura con `FindActiveTradeAsync` (oficio activo por clave) y `FindNeighborhoodAsync` (colonia por id), sin cambiar lo que ya expone. Devuelven tuplas porque la regla de arquitectura limita cada módulo a dos tipos públicos. El catálogo de colonias no tiene indicador de «activa»: R-02 se cumple comprobando que existe.

