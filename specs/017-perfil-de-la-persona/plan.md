# Plan 017 — Perfil de la persona

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Agregar al repositorio de `usuarios` la lectura por id con caché (`perfil:{usuarioId}`) y la actualización que invalida la clave.
2. Crear el validador del `PUT` con las reglas de los campos y la lista de dominios permitidos para la foto.
3. Definir en `Knock.Contracts` la interfaz que consulta si una colonia existe.
4. Crear el servicio de perfil y el controller con `ETag` y `Cache-Control`.
5. Escribir las pruebas desde los criterios de aceptación.

## Archivos que se crean o cambian
- `src/Knock.Identity.Api/Modules/Usuarios/Controllers/MeController.cs`
- `src/Knock.Identity.Api/Modules/Usuarios/Dtos/` (perfil y actualización)
- `src/Knock.Identity.Api/Modules/Usuarios/Validators/UpdateProfileValidator.cs`
- `src/Knock.Identity.Api/Modules/Usuarios/Services/ProfileService.cs`
- `src/Knock.Identity.Api/Modules/Usuarios/Data/` (repositorio con caché)
- `src/Knock.Contracts/Colonias/IColoniaCatalog.cs`
- `tests/` (unitarias, integración y seguridad negativa del módulo)

## Dependencias
- Specs 002, 003, 009 y 010 (estructura, datos, JWT y caché).
- Spec 015 (la colección `usuarios` y sus índices).
- Servicio de Mercado para validar la colonia (ver dudas abiertas de la spec).

## Decisiones de la spec que este plan sigue
- **Ruta `/me` sin identificador.**
- **Caché de 60 segundos con invalidación.**
- **`PUT` en lugar de `PATCH`.**

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
