# Plan 009 — Seguridad base y configuración

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Definir la configuración tipada y su validación al arrancar.
2. Agregar los middlewares de cabeceras, CORS y límites.
3. Configurar la validación de JWT y la política de autorización por defecto.
4. Agregar el filtro que oculta datos personales en los registros.

## Archivos que se crean o cambian
- `src/<Servicio>/Hosting/Security.cs`
- `src/<Servicio>/Middleware/SecurityHeaders.cs`
- `tests/SecurityTest/`

## Dependencias
- Autenticación JWT de ASP.NET Core
- Key Vault

## Decisiones de la spec que este plan sigue
- **Denegar por defecto:** toda ruta exige autenticación salvo las marcadas como públicas.
- **Una sola entrada:** la seguridad, el límite de peticiones y el registro se aplican en un único punto.
- **Secretos fuera del código:** protege al repositorio aunque se haga público.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
