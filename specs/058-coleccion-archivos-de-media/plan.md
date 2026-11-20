# Plan 058 — Colección `archivos` de Media: registro de archivos privados en MongoDB y tope de subidas pendientes

> Parte de `spec.md` (estado Borrador). Este plan queda sujeto a la aprobación de la spec por el Tech Lead.

## Enfoque
1. Base de datos primero: script de `db/` con la colección, el índice único, el compuesto y el TTL, y su prueba contra MongoDB real. Es la base de todo lo demás.
2. `MongoMediaFileStore` con `IMediaFileStore` en camelCase (`_id` ObjectId, `archivoId`, `version`), probado contra MongoDB y Azurite junto con las pruebas actuales del módulo (`MediaServiceTests`, `MediaConfirmationBlobTests`), sin tocar el contrato.
3. Migración desde `registros-temporales`: lectura de los JSON antiguos, traducción e inserción idempotente por `archivoId`, con conteos y sin datos personales en los registros.
4. Tope de subidas pendientes: `fecha de vigencia` en el documento, método de «insertar y verificar» en el almacén, resultado `LimitExceeded` en `MediaService` y pruebas de concurrencia.
5. Retirar `BlobMediaFileStore`, `Media:RecordsContainer` y sus referencias; registrar solo el almacén de MongoDB. El contenedor en Azure lo borra una persona tras verificar la migración.
6. Pruebas de seguridad (dueño solo del token, `archivoId` no enumerable, registros sin datos) y de concurrencia repetidas.
7. Contrato, documentación, codemap y `Memoria.md`.

## Archivos que se crean o cambian
- `db/NNN-market-archivos.js` (el siguiente número libre en `db/` al implementar): colección `archivos`, índices y TTL.
- `src/Knock.Market.Api/Modules/Media/Data/MongoMediaFileStore.cs` (nuevo), `IMediaFileStore.cs` (método de tope y documentación en camelCase), `InMemoryMediaFileStore.cs` (mismo método para pruebas) y `MediaOptions.cs` (sin `RecordsContainer`; tope y plazos configurables y validados).
- `src/Knock.Market.Api/Modules/Media/Domain/MediaFile.cs` (`ConfirmarAntesDe`, `ExpiraEn`), `MediaOutcome.cs` (`LimitExceeded`) y `Services/MediaService.cs` (`CreateUploadAsync`).
- Migrador interno del módulo `Media` (clase nueva en `Data` o `Services`) y su punto de invocación explícito.
- `src/Knock.Market.Api/Modules/Media/MediaModuleExtensions.cs` (registro de `MongoMediaFileStore`) y `src/Knock.Market.Api/appsettings.json` (quitar `RecordsContainer`).
- Se borra `src/Knock.Market.Api/Modules/Media/Data/BlobMediaFileStore.cs`.
- `tests/DatabaseTest`, `tests/UnitTest/Media`, `tests/IntegrationTest/Media`, `tests/SecurityTest`, `tests/ArchitectureTest` y `tests/ContractTest` (clases `Spec058*`).
- `docs/endpoints.md` (solo si el flujo privado ya tiene rutas), `docs/architecture/CODEMAP.md` (regenerado), `specs/README.md`, `Memoria.md`.
- Spec 020 (enmienda de R-04) solo si el Tech Lead aprueba D4; en otra tarjeta.

## Dependencias
- Specs 002, 003, 009, 011 y 013; coordinación con 032 y 020.
- Testcontainers con MongoDB compatible y Azurite (ya usados en el repositorio).
- Decisiones abiertas D1 a D8 de la spec contestadas por el Tech Lead antes de aprobar.
- Una persona con permiso en Azure para revisar y borrar el contenedor `registros-temporales` (y, si se aprueba D8, aplicar la regla de ciclo de vida).

## Riesgos del plan
- DocumentDB puede comportarse distinto a MongoDB en TTL, `explain` o índices parciales: se prueba contra el contenedor de pruebas y se anota lo que no se pueda confirmar.
- El tope por «insertar y verificar» puede rechazar de más bajo contención; se mide en las pruebas de concurrencia y se ajusta `Retry-After`.
- El conflicto del formato de `archivoId` con la spec 020 (D4) puede obligar a cambiar el tipo en `IMediaApi`.

## Decisiones de la spec que este plan sigue
- Colección propia separada de `subidas_imagen`; `_id` ObjectId y `archivoId` UUID v7; tope por insertar y verificar en MongoDB; migración explícita, idempotente y sin sobrescribir; un índice compuesto.

## Verificación
- `dotnet build Knock.slnx -c Release`, `dotnet test Knock.slnx -c Release` (con énfasis en `DatabaseTest`, `Spec058*` y `Media*`) y `dotnet format Knock.slnx --verify-no-changes`.
- `python scripts/sdd_lint.py --trace 058`, `python scripts/codemap.py --check` y `python scripts/ci/check_exceptions.py`.
- `python scripts/export_openapi.py` y `git diff` limpio de `docs/openapi.v1.json` (el contrato HTTP no cambia).
- Búsqueda de `BlobMediaFileStore`, `RecordsContainer` y `registros-temporales` en `src/`, `deploy/` y `appsettings*.json` sin resultados.
