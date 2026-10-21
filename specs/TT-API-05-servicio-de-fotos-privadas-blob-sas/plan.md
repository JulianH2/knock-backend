# plan.md — TT-API-05 — Servicio único de fotos privadas

> Borrador. Servicio afectado: Mercado. Se completa cuando la spec esté Aprobada y se resuelvan las preguntas abiertas de spec §8.

## 1. Mapeo de archivos
- **Reutilizar:** pipeline base de TT-API-01 (ProblemDetails, versionado, `RequestBodyLimitMiddleware`, `HealthExtensions` con la etiqueta `ready`), fixture de Testcontainers de IntegrationTest, `ModuleCatalog` de las pruebas de arquitectura.
- **Crear:** módulo `Media` del servicio de Mercado (`AddMediaModule`, `IMediaApi`, controller, servicio, validadores, interfaz interna de persistencia del archivo), servicio Azurite en compose y fixture de Azurite.
- **Pendiente de TT-BD-06:** implementación MongoDB de la interfaz de persistencia (colección `archivos` en la base `market`) y fixture de Testcontainers de MongoDB.
- **Modificar:** `Hosting/ModulesExtensions.cs` (registrar el módulo), `tests/UnitTest/Architecture/ModuleCatalog.cs`, `Directory.Packages.props` (Azure.Storage.Blobs y la librería de imágenes, tras evaluación), `src/Knock.Api/Dockerfile` si se elige SkiaSharp.
- **No tocar:** fotos de verificación (HU-021), infraestructura de Azure (TT-AZ-02), CDN (TT-API-10), moderación (TT-IA-02), `.github/`.

## 2. Riesgos y efectos secundarios
- SAS vencido debe probarse sin esperas reales (TC-TT-API-05-01): `TimeProvider` falso o vencimiento corto.
- Pérdida de orientación al quitar EXIF (TC-TT-API-05-02).
- Binarios nativos de SkiaSharp en la imagen Linux.
- Dependencia de la autenticación JWT (TT-API-02) y de la base (TT-BD-05/06): sin registro persistido no hay control de propietario (BOLA) completo.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite |
|---|---|---|
| TC-TT-API-05-01 | SAS vencido rechazado por Azurite. | `IntegrationTest/PrivateFilesTests.ExpiredSasLinkIsRejectedByBlobStorage_TC_TT_API_05_01` |
| TC-TT-API-05-02 | Foto con GPS confirmada sin EXIF. | `IntegrationTest/PrivateFilesTests.ConfirmedPhotoHasNoExifIncludingGps_TC_TT_API_05_02` |
| TC-TT-API-05-03 | 9 MB o PDF → 422 sin blob final. | `IntegrationTest/PrivateFilesTests.OversizedOrNonImageFileIsRejectedWith422_TC_TT_API_05_03` |
| Pendiente de TT-BD-06 | Propietario, estado, `404` a ajenos y `409` ya confirmado con MongoDB. | `IntegrationTest` con Azurite + MongoDB (Testcontainers) |

## 4. Secuencia
1. Resolver preguntas abiertas (a)–(f) y evaluar paquetes.
2. Contrato OpenAPI → servicio (SAS, validación, EXIF) + unitarias → controller + integración con Azurite, seguridad y contrato → `export_openapi.py` → gates → `Memoria.md`.
3. Con TT-BD-06: implementación MongoDB de la persistencia y sus pruebas.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace TT-API-05
dotnet test tests/UnitTest tests/IntegrationTest tests/SecurityTest tests/ContractTest -c Release
```
