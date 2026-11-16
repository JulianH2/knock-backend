# Tareas 013 — Almacenamiento de archivos, entrega con caché y moderación

> **Estado:** OI-1 y OI-2 implementadas · OI-3 pendiente.

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Crear las dos cuentas de almacenamiento y sus contenedores en Terraform, sin CDN | R-01, R-04 | pendiente |
| T-02 | Implementar el servicio de enlaces firmados | R-02 · CA-02 | pendiente |
| T-03 | Implementar la validación (5 MB, JPEG/PNG/WebP), la limpieza de metadatos y la reducción de tamaño | R-03 · CA-01, CA-06 | parcial: OI-2 procesa JPEG/PNG/WebP a JPEG sin metadatos, con fondo blanco y reducción por uso; falta el flujo HTTP de OI-3 |
| T-04 | Implementar el cliente de moderación compartido con falla cerrada y 503 reintentable | R-05, R-06, R-07 · CA-04, CA-05 | parcial: OI-2 conserva la subida pendiente ante moderación no disponible o tiempo agotado; faltan el cliente real y la respuesta HTTP 503 con Retry-After en OI-3 |
| T-05 | Escribir las pruebas de seguridad negativa (incluida la petición anónima a la cuenta privada) | R-04 · CA-03 | parcial: OI-2 cubre archivo malicioso con firma falsa y falla cerrada de moderación; falta la petición anónima a la cuenta privada (CA-03) |
| T-06 | Crear el índice TTL de `subidas_imagen` (vence solo lo pendiente o rechazado) | R-08 · CA-07 | pendiente |

## Avance de la implementación (KNOCK-208)

### Fases

| Fase | Alcance | Estado |
|---|---|---|
| **OI-1** | Contratos (`IContentModerator` con falla cerrada), estructura `Media`, dominio/reglas puras, validaciones tipo/tamaño, `MediaOptions`, `MediaModuleExtensions`, integración en `ModulesExtensions`, pruebas unitarias y arquitectura | **Hecha** |
| **OI-2** | Procesamiento de imágenes y orquestación local con moderador falso (sin Azure, sin SAS, sin endpoints) | **Hecha** |
| **OI-3** | Blob Storage, User Delegation SAS, endpoints `archivos/*`, OpenAPI, Azurite/RBAC, moderación real con falla cerrada | pendiente |

## Criterios pendientes por fase
- **CA-01** (limpieza de metadatos): cubierto en OI-2 (`ImageProcessor` y prueba de integración con Azurite).
- **CA-02** (enlace vencido a los 5 minutos): OI-3. En OI-1 solo existe la constante `SasLifetime`, sin servicio de enlaces.
- **CA-03** (imagen privada por la CDN): OI-3 (T-05).
- **CA-04** (contenido inapropiado rechazado): cubierto en OI-2 con moderador falso; la moderación real llega en OI-3.
- **CA-05** (moderación caída, no se publica): cubierto en OI-2 con moderador falso (`Unavailable` bloquea); el cliente real es OI-3.

## Dudas abiertas (rutas de subida y confirmación)
- Ni `docs/endpoints.md` ni las specs 017 o 020 fijan rutas para subir o confirmar archivos (la spec 020 no existe en `specs/`). Por eso OI-2 **no expone controllers ni rutas**: solo servicios de aplicación (`MediaService`, `IMediaApi`) detrás de interfaces del módulo, como pide el alcance de la fase.
- **Decide una persona en OI-3:** las rutas HTTP (`POST /api/v1/archivos/subidas` y `POST /api/v1/archivos/{id}/confirmar` son solo una propuesta del código legacy), las respuestas del contrato y su entrada en `docs/endpoints.md` y el OpenAPI. Hasta que se fijen, no se publica contrato de subida.

## CDN (OI-3)
- La CDN se propone con **Azure Front Door Standard, ~35 USD/mes**, **apagada por defecto** (variable de Terraform en `false`). No se crea en OI-2/OI-3 sin aprobación del líder; las imágenes públicas siguen servidas desde el contenedor público de Blob (ver ADR-007 y `Memoria.md`).

Las pruebas de OI-1 se nombran `*_OI1_NN` y no `*_CA_013_NN`, porque ninguna cubre todavía un criterio de aceptación completo.

## Notas de revisión (OI-1)
- **Excepción de «Media» en `ModuleSurfaceTests`:** la rama de `tests/UnitTest/Architecture/ModuleSurfaceTests.cs` que acepta una sola clase pública (`MediaModuleExtensions`) se quita cuando OI-2 agregue la interfaz del módulo (el test vuelve a exigir las dos superficies: clase de registro e interfaz). OI-1 no expone interfaz porque todavía no hay consumidor interno real del módulo.
- **Límites de las fotos:** 5 MB, 40 MP y 8 000 px por lado, en JPEG, PNG o WebP (decisión del Tech Lead, 2026-11-12; `MediaRules` en `src/Knock.Market.Api/Modules/Media/Domain/MediaRules.cs`). Reemplazan los 12 000 px y 24 MP de TT-API-05 legacy. Se rechaza antes de decodificar. La réplica de Market queda en 1 Gi con `MaxConcurrentProcessing=1` (se aplica en #63).

## Notas de revisión (OI-2)
- **Qué entró:** `ImageProcessor` (SkiaSharp), `MediaService` (cuarentena → procesar → moderar → contenedor final → borrar cuarentena), `IMediaBlobStore` con implementación de Azure (`AzureMediaBlobStore`) y en memoria (`InMemoryMediaBlobStore`), `IMediaFileStore` con `BlobMediaFileStore` (JSON por archivo en el contenedor de registros, temporal hasta TT-BD-06) e `InMemoryMediaFileStore`, y `IMediaApi`/`MediaApi` como frontera del módulo.
- **Fuera de OI-2 (pasa a OI-3):** SAS de delegación, controllers/rutas, Terraform, moderación real de Azure AI Content Safety, CDN y prueba de CA-02/CA-03.
- **Pruebas OI-2:** `ImageProcessorTests` cubre conversión, transparencia, metadatos y reducción por uso; `MediaServiceTests` verifica rechazo y conservación de cuarentena ante moderación caída, con reintento; `MediaConfirmationBlobTests` usa Azurite para el flujo de blobs. El 503 HTTP corresponde a OI-3 y aún no está probado.
- **Azurite de pruebas:** `mcr.microsoft.com/azure-storage/azurite:3.37.0` fijado por el digest de la lista de manifiestos (`sha256:830430c1...`), con `--skipApiVersionCheck` porque `Azure.Storage.Blobs` 12.30 envía una `x-ms-version` que 3.37 aún no reconoce.
- **Docker de Market:** construye con `SkiaSharp.NativeAssets.Linux.NoDependencies`; la imagen contiene `libSkiaSharp.so` para linux-x64/arm64 y no necesita paquetes del sistema adicionales.

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.

  Aclaración: esta definición aplica a la finalización integral de la spec 013. El cierre de cada fase OI-1/OI-2/OI-3 se evalúa contra su alcance y entregables asignados. OI-1 no declara cubiertos los criterios de aceptación completos que están asignados a fases posteriores.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
