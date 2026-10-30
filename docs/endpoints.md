# Endpoints de la API

> AGENTS.md §5. Una fila por operación HTTP de negocio, con su spec. El contrato completo se genera del código (`GET /openapi/v1.json`, solo en Development); la exportación a `docs/openapi.v1.json` llega con TT-CI-02.
> Límites de peticiones: `docs/rate-limit-matrix.md` (la política global aplica a todas las filas salvo que se diga otra cosa).

| Método y ruta | Módulo | Auth | Respuesta | Caché | Spec | Notas |
|---|---|---|---|---|---|---|
| `GET /api/v1/oficios` | Catalog | pública (`[AllowAnonymous]`) | 200 `application/json`: arreglo de `{ clave, nombre, modalidad }` | `86400` s (declarada; sin implementar) | TT-PRU-01 | Solo oficios activos, ordenados por nombre sin importar mayúsculas y luego por clave. Arreglo sin paginar: excepción a la constitución §11 (catálogo acotado; si pasa de 50 elementos, `CursorPage<T>` en `v2`). `modalidad` es texto con valores conocidos `domicilio` y `taller`, no enum cerrado. |
| `GET /api/v1/colonias/en-punto` | Catalog | JWT Bearer | 200 `application/json`: `{ enCobertura, colonia: { id, nombre, cp, municipio } }` (`colonia` es `null` si el punto está fuera de cobertura); 401, 422 (`errors.lat` o `errors.lon`), 429 y 500 en ProblemDetails | sin caché (`Cache-Control: no-store`) | TT-API-07 | Query `lat` y `lon`: texto, un valor cada uno, patrón `^-?\d{1,3}(\.\d{1,10})?$`, máx. 16 caracteres y rangos [-90, 90] y [-180, 180]. No guarda ni registra coordenadas. Límite global por IP, sin exención. KNOCK-141, casos TC-TT-API-07-03 a 16. |
