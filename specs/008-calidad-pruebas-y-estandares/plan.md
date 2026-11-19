# Plan 008 — Calidad, pruebas y estándares

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Agregar `.editorconfig` y `Directory.Build.props`.
2. Definir el esquema de nombres de pruebas con el ID del criterio.
3. Configurar la cobertura.

## Archivos que se crean o cambian
- `.editorconfig`, `Directory.Build.props`
- `tests/` (esquema de nombres con el ID del criterio)
- `CONTRIBUTING.md` (definición de terminado)

## Dependencias
- Analizadores de .NET
- Herramienta de cobertura

## Decisiones de la spec que este plan sigue
- **Warnings como errores:** evita acumular deuda sin verla.
- **Real primero:** las pruebas de integración con servicios reales detectan problemas que los dobles ocultan.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
