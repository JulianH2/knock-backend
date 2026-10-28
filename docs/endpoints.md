# Endpoints de la API

> AGENTS.md §5. Una fila por operación HTTP de negocio, con su spec. El contrato completo se genera del código (`GET /openapi/v1.json`, solo en Development); la exportación a `docs/openapi.v1.json` llega con TT-CI-02.
> Límites de peticiones: `docs/rate-limit-matrix.md` (la política global aplica a todas las filas salvo que se diga otra cosa).

| Método y ruta | Módulo | Auth | Respuesta | Caché | Spec | Notas |
|---|---|---|---|---|---|---|
| `GET /api/v1/oficios` | Catalog | pública (`[AllowAnonymous]`) | 200 `application/json`: arreglo de `{ clave, nombre, modalidad }` | `86400` s (declarada; sin implementar) | TT-PRU-01 | Solo oficios activos, ordenados por nombre sin importar mayúsculas y luego por clave. Arreglo sin paginar: excepción a la constitución §11 (catálogo acotado; si pasa de 50 elementos, `CursorPage<T>` en `v2`). `modalidad` es texto con valores conocidos `domicilio` y `taller`, no enum cerrado. |
