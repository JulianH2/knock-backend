# TT-API-05 — tareas

> Servicio afectado: Mercado.

## T1 — Infraestructura de Blob
- **Verificar:** TC-TT-API-05-01
- **Pendiente:** Azurite en compose y fixture de Testcontainers; contenedores privados `cuarentena` y `archivos`; comprobación `ready`.

## T2 — Subida y confirmación
- **Verificar:** TC-TT-API-05-02 · TC-TT-API-05-03
- **Pendiente:** Módulo `Media` en el servicio de Mercado; `POST /archivos/subidas` con SAS de escritura de 5 min (`422` tipo/tamaño declarado, `413` cuerpo excesivo) y `POST /archivos/{id}/confirmar` con validación por contenido, quitado de EXIF y versión reducida.

## T3 — Lectura con permiso
- **Verificar:** TC-TT-API-05-01
- **Pendiente:** `GET /archivos/{id}/lectura` para el propietario e `IMediaApi` para que otros módulos y servicios emitan SAS de lectura tras su autorización.

## T4 — Persistencia en MongoDB
- **Estado:** pendiente de TT-BD-06
- **Pendiente:** implementar la interfaz interna sobre la colección `archivos` (base `market`) y sus pruebas con Testcontainers de MongoDB.
