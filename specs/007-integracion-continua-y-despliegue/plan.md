# Plan 007 — Integración continua y despliegue

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Escribir el workflow con detección de cambios por carpeta.
2. Agregar los jobs de secretos y dependencias.
3. Escribir el workflow de despliegue.

## Archivos que se crean o cambian
- `.github/workflows/ci-pr.yml` y `deploy.yml`
- `scripts/ci/`

## Dependencias
- GitHub Actions
- Registro de contenedores

## Decisiones de la spec que este plan sigue
- **Un solo entorno desplegado desde `main`:** simplifica el costo y el proceso.
- **Acciones fijadas por SHA:** reduce el riesgo de cambios inesperados.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
