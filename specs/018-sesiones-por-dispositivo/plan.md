# Plan 018 — Sesiones por dispositivo

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Agregar al repositorio de `sesiones` la consulta de sesiones abiertas de una persona y la búsqueda de una sesión por id con su dueña.
2. Reutilizar el servicio de revocación y de cancelación en Redis de la spec 016.
3. Crear el servicio y el controller de sesiones del usuario.
4. Escribir las pruebas desde los criterios de aceptación.

## Archivos que se crean o cambian
- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/MeSesionesController.cs`
- `src/Knock.Identity.Api/Modules/Sesiones/Dtos/` (elemento del listado)
- `src/Knock.Identity.Api/Modules/Sesiones/Services/SessionManagementService.cs`
- `src/Knock.Identity.Api/Modules/Sesiones/Data/` (consultas por persona y por id)
- `tests/` (unitarias, integración y seguridad negativa del módulo)

## Dependencias
- Specs 015 y 016 (colección `sesiones`, revocación y cancelación en Redis).
- Specs 002, 003 y 009.

## Decisiones de la spec que este plan sigue
- **Sin paginación:** máximo 10 sesiones por cuenta.
- **Mismo mecanismo de cancelación que el logout.**

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
