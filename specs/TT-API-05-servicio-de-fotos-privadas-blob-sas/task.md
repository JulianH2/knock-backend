# TT-API-05 — tareas

## T1 — Infraestructura de Blob
- **Verificar:** TC-TT-API-05-01
- **Pendiente:** Azurite en compose y fixture de Testcontainers; contenedores privados `cuarentena` y `archivos`; comprobación `ready`.

## T2 — Subida y confirmación
- **Verificar:** TC-TT-API-05-02 · TC-TT-API-05-03
- **Pendiente:** Módulo `Media`, tabla `archivos`, `POST /archivos/subidas` con SAS de escritura de 5 min y `POST /archivos/{id}/confirmar` con validación por contenido, quitado de EXIF y versión reducida.

## T3 — Lectura con permiso
- **Verificar:** TC-TT-API-05-01
- **Pendiente:** `GET /archivos/{id}/lectura` para el propietario e `IMediaApi` para que otros módulos emitan SAS de lectura tras su autorización.
