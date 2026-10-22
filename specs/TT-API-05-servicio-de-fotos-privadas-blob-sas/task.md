# TT-API-05 — tareas

> Servicio afectado: Mercado.

## T1 — Infraestructura de Blob
- **Estado:** hecho (2026-10-21, sin commit). Azurite 3.37.0 con Testcontainers (`tests/IntegrationTest/Support/AzuriteFixture.cs`); contenedores privados `cuarentena` y `archivos` creados por el módulo si faltan; comprobación `blob` en `/health/ready`.
- **Verificar:** TC-TT-API-05-01
- **Pendiente:** fixture de Azurite con Testcontainers (sin tocar `deploy/docker-compose.local.yml`, TT-ARQ-01; Blob real hasta TT-AZ-02); contenedores privados `cuarentena` y `archivos`; comprobación `ready`.

## T2 — Subida y confirmación
- **Estado:** hecho (2026-10-21, sin commit). `src/Knock.Api/Modules/Media/` (mientras exista `Knock.Market.Api`, TT-ARQ-01); hook de moderación vacío `NoModerationHook` (TT-IA-02). Pruebas: `IntegrationTest/Media/PrivateFilesTests` (TC-02 y TC-03), `UnitTest/Media/MediaRulesTests`, `SecurityTest/Media/PrivateFilesSecurityTests`.
- **Verificar:** TC-TT-API-05-02 · TC-TT-API-05-03
- **Pendiente:** Módulo `Media` en el servicio de Mercado; `POST /archivos/subidas` con SAS de escritura de 5 min (`422` tipo/tamaño declarado, `413` cuerpo excesivo) y `POST /archivos/{id}/confirmar` con validación por contenido, quitado de EXIF (SkiaSharp) y versión reducida, autenticados con la sesión JWT real de TT-API-02; punto de enganche de moderación documentado (TT-IA-02).

## T3 — Lectura con permiso
- **Estado:** hecho (2026-10-21, sin commit). `GET /archivos/{id}/lectura` solo para el dueño (`sub` del JWT) e `IMediaApi.CreateReadLinkAsync` para otros módulos. Pruebas: `PrivateFilesTests` (TC-01), `UnitTest/Media/MediaSasTests`.
- **Verificar:** TC-TT-API-05-01
- **Pendiente:** `GET /archivos/{id}/lectura` para el propietario e `IMediaApi` (pasa a `Knock.Contracts` al separar servicios, TT-ARQ-03) para que otros módulos y servicios emitan SAS de lectura tras su autorización.

## T4 — Persistencia en Azure DocumentDB
- **Estado:** pendiente de TT-BD-06. Mientras tanto, implementación temporal `BlobMediaFileStore` (un JSON por archivo en el contenedor privado `registros-temporales`, ETag como versión); la colección `archivos` y sus índices quedan documentados en `IMediaFileStore`.
- **Pendiente:** implementar la interfaz interna sobre la colección `archivos` (base `market`; índices por `propietario_id` y por `estado`; `maxPoolSize` fijo) y sus pruebas con Testcontainers.
