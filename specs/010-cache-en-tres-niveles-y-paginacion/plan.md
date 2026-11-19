# Plan 010 — Caché en tres niveles y paginación

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Definir la interfaz de caché y su implementación con Redis.
2. Envolver los repositorios con la caché y la invalidación.
3. Agregar el filtro de `ETag` y 304.
4. Implementar la paginación por cursor y las proyecciones.
5. Configurar la compresión y los serializadores generados.

## Archivos que se crean o cambian
- `src/<Servicio>/Common/Caching/`
- `src/<Servicio>/Middleware/ETagMiddleware.cs`
- `tests/IntegrationTest/Caching/`
- `tests/PerfTest/`

## Dependencias
- Redis
- Generadores de código de `System.Text.Json`

## Decisiones de la spec que este plan sigue
- **Caché en la capa de datos y no en la memoria de cada copia:** con dos copias, una caché en memoria daría respuestas distintas.
- **Paginación por cursor y no por número de página:** contar desde el principio se vuelve más lento mientras más profundo se pagina.
- **No cachear lo personal que cambia al crear algo:** una lista cacheada mostraría datos viejos justo después de crear.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
