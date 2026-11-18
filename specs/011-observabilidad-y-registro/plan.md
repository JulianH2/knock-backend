# Plan 011 — Observabilidad y registro

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Configurar Serilog con el receptor de Application Insights.
2. Agregar el middleware de correlación y el enriquecedor de registros.
3. Agregar el filtro de datos personales.
4. Definir las alertas y la retención en Terraform.

## Archivos que se crean o cambian
- `src/<Servicio>/Hosting/Logging.cs`
- `src/<Servicio>/Middleware/CorrelationIdMiddleware.cs`
- `deploy/modules/shared/observability.tf`

## Dependencias
- Serilog y su receptor de Application Insights
- Application Insights y Log Analytics

## Decisiones de la spec que este plan sigue
- **Registro central:** buscar un error en cada pieza sería lento; en un solo lugar se ve dónde falló.
- **Identificador de correlación:** permite reconstruir el recorrido completo de una petición.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
