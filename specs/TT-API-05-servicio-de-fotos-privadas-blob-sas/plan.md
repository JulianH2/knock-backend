# plan.md — TT-API-05 — Servicio único de fotos privadas

> Borrador. Servicio afectado: Mercado. Preguntas de spec §8 resueltas por el Tech Lead; falta la aprobación de la spec.

## 1. Mapeo de archivos
- **Reutilizar:** pipeline base de TT-API-01 (ProblemDetails, versionado, `RequestBodyLimitMiddleware`, `HealthExtensions` con la etiqueta `ready`), emisor y validación JWT reales de TT-API-02 (KNOCK-21), fixture de Testcontainers de IntegrationTest, `ModuleCatalog` de las pruebas de arquitectura.
- **Crear:** módulo `Media` del servicio de Mercado (`AddMediaModule`, `IMediaApi`, controller, servicio, validadores, interfaz interna de persistencia del archivo, punto de enganche documentado para moderación en `confirmar`) y fixture de Azurite con Testcontainers.
- **Pendiente de TT-BD-06:** implementación de la interfaz de persistencia sobre Azure DocumentDB (recomendación del spike TT-BD-05): colección `archivos` en la base `market`, índices por `propietario_id` y por `estado`, `maxPoolSize` fijo; y su fixture de Testcontainers.
- **Modificar:** `Hosting/ModulesExtensions.cs` (registrar el módulo), `tests/UnitTest/Architecture/ModuleCatalog.cs`, `Directory.Packages.props` (Azure.Storage.Blobs y SkiaSharp, tras evaluación de licencia, mantenimiento y vulnerabilidades registrada en `Memoria.md`), `src/Knock.Api/Dockerfile` (paquete nativo de SkiaSharp para Linux).
- **No tocar:** `deploy/docker-compose.local.yml` (se rehace en TT-ARQ-01), fotos de verificación (HU-021), infraestructura de Azure y Blob real (TT-AZ-02), CDN (TT-API-10), lógica de moderación (TT-IA-02), `Knock.Contracts` (TT-ARQ-03), `.github/`.

## 2. Riesgos y efectos secundarios
- SAS vencido debe probarse sin esperas reales (TC-TT-API-05-01): `TimeProvider` falso o vencimiento corto.
- Pérdida de orientación al quitar EXIF (TC-TT-API-05-02).
- Binarios nativos de SkiaSharp en la imagen Linux.
- Dependencia de la base (TT-BD-06): sin registro persistido no hay control de propietario (BOLA) completo.

## 3. Pruebas por caso
| Caso | Prueba planeada | Suite |
|---|---|---|
| TC-TT-API-05-01 | SAS vencido rechazado por Azurite. | `IntegrationTest/PrivateFilesTests.ExpiredSasLinkIsRejectedByBlobStorage_TC_TT_API_05_01` |
| TC-TT-API-05-02 | Foto con GPS confirmada sin EXIF. | `IntegrationTest/PrivateFilesTests.ConfirmedPhotoHasNoExifIncludingGps_TC_TT_API_05_02` |
| TC-TT-API-05-03 | 9 MB o PDF → 422 sin blob final. | `IntegrationTest/PrivateFilesTests.OversizedOrNonImageFileIsRejectedWith422_TC_TT_API_05_03` |
| Pendiente de TT-BD-06 | Propietario, estado, `404` a ajenos y `409` ya confirmado con la base de documentos. | `IntegrationTest` con Azurite + base compatible con la API de MongoDB (Testcontainers) |

## 4. Secuencia
1. Aprobación de la spec; evaluar paquetes (SkiaSharp, Azure.Storage.Blobs) y registrar el resultado en `Memoria.md`.
2. Contrato OpenAPI → servicio (SAS, validación, EXIF) + unitarias → controller + integración con Azurite, seguridad y contrato → `export_openapi.py` → gates → `Memoria.md`.
3. Con TT-BD-06: implementación de la persistencia sobre Azure DocumentDB y sus pruebas.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace TT-API-05
dotnet test tests/UnitTest tests/IntegrationTest tests/SecurityTest tests/ContractTest -c Release
```
