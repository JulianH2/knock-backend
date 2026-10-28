# Endpoints

> Fuente única de verdad para rutas HTTP expuestas. Actualizar al añadir o cambiar endpoints.

| Método | Ruta | Autenticación | Entrada | Respuestas | Notas / Caso |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/colonias/en-punto` | JWT Bearer | Query: `lat` (string, requerido, un solo valor), `lon` (string, requerido, un solo valor). Formato `^-?\d{1,3}(\.\d{1,10})?$`, máx 16 caracteres, punto decimal, cultura invariante. Rangos `[-90,90]` y `[-180,180]`. | 200 `{enCobertura:boolean, colonia:{id,nombre,cp,municipio} o null}`; 401 ProblemDetails; 422 `errors.lat`/`errors.lon` (repetido, ausente, fuera de rango o mal formado); 429 ProblemDetails con `Retry-After`; 500 ProblemDetails genérico | Catálogo propio. Sin caché (`Cache-Control: no-store`). No guarda ni registra coordenadas. Usa la consulta interna de Catalog. Límite global por IP, sin exención. TT-API-07 (KNOCK-141), casos TC-TT-API-07-03 a 16. |
