# Plan 016 — Renovación y cierre de sesión

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Agregar al repositorio de `sesiones` la rotación atómica (una operación condicionada a la huella vigente) y la revocación de todas las sesiones de una cuenta.
2. Implementar el servicio de renovación con detección de reuso.
3. Implementar el servicio de logout: revocar en MongoDB y escribir la cancelación en Redis con su tiempo de vida.
4. Agregar al gateway la consulta de `sesion-cancelada:{sid}` en la validación del token.
5. Crear los dos endpoints en el controller de autenticación.
6. Escribir las pruebas desde los criterios de aceptación.

## Archivos que se crean o cambian
- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/AuthController.cs` (agrega `refresh` y `logout`)
- `src/Knock.Identity.Api/Modules/Sesiones/Dtos/` (renovación)
- `src/Knock.Identity.Api/Modules/Sesiones/Validators/RefreshRequestValidator.cs`
- `src/Knock.Identity.Api/Modules/Sesiones/Services/` (renovación y cierre de sesión)
- `src/Knock.Identity.Api/Modules/Sesiones/Data/` (rotación atómica, revocación) y `Data/SesionCancelada.cs` para Redis
- `src/Knock.Gateway/` (validación del token con la lista de sesiones canceladas)
- `tests/` (unitarias, integración y seguridad negativa del módulo)

## Dependencias
- Spec 015 (creación de la sesión y colección `sesiones`).
- Specs 003, 009 y 010 (datos, JWT y respuestas sin compresión).
- Redis disponible para el gateway y el servicio de Identidad.

## Decisiones de la spec que este plan sigue
- **Cancelar por `sid`.**
- **Rotación con detección de reuso.**
- **La cancelación vive en Redis.**

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
