# Plan 002 — Estructura interna de cada servicio y estilo de la API

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Definir la plantilla de módulo y crear un módulo vacío de ejemplo.
2. Implementar el manejo común de excepciones.
3. Configurar el versionado y la generación del OpenAPI.
4. Implementar `/health` y `/health/ready`.
5. Escribir las pruebas de capas con NetArchTest.

## Archivos que se crean o cambian
- `src/<Servicio>/Modules/<Modulo>/{Controllers,Dtos,Validators,Services,Domain,Data,Events}`
- `src/<Servicio>/Common/`, `Hosting/` y `Middleware/`
- `docs/openapi.v1.json`

## Dependencias
- FluentValidation
- Generador de OpenAPI de ASP.NET Core

## Decisiones de la spec que este plan sigue
- **REST y no GraphQL ni gRPC:** hay un solo tipo de cliente (app y panel); REST es más fácil de probar, de cachear con HTTP y de documentar, y OpenAPI permite generar el cliente.
- **Capas y rebanadas verticales:** cada función atraviesa las cuatro capas con sus pruebas en lugar de construir capa por capa.
- **ProblemDetails en toda la API:** un solo formato de error para clientes y pruebas.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
