# Plan 006 — Escalabilidad, balanceo y salud de los servicios

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Definir la variable de réplicas en Terraform.
2. Configurar las sondas de salud en Container Apps.
3. Configurar la lectura de encabezados reenviados.
4. Mover tokens cancelados, caché y contadores a Redis.

## Archivos que se crean o cambian
- `src/<Servicio>/Hosting/ForwardedHeaders.cs`
- `deploy/modules/<servicio>/` (réplicas y sondas)
- `tests/PerfTest/`

## Dependencias
- Redis
- Container Apps (balanceador integrado)
- k6

## Decisiones de la spec que este plan sigue
- **Escalado y no autoescalado:** nosotros fijamos cuántas copias hay; no es un requerimiento tener reglas que las suban solas.
- **Estado en Redis:** una caché en memoria daría respuestas distintas según la copia.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
