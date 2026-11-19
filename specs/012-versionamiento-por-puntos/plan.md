# Plan 012 — Versionamiento por puntos

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Escribir el script de cálculo de versiones con pruebas.
2. Agregar la comprobación de la etiqueta de puntos al CI.
3. Integrar el cálculo en el despliegue y la creación de la etiqueta.
4. Crear el registro de versiones desplegadas.

## Archivos que se crean o cambian
- `scripts/ci/version.py` o equivalente
- `docs/VERSIONES_DESPLEGADAS.md`
- `.github/workflows/deploy.yml`

## Dependencias
- Ninguna adicional

## Decisiones de la spec que este plan sigue
- **Versión por componente:** un cambio en la landing no debe cambiar el número del backend.
- **Puntos por tipo de cambio y no por tamaño de código:** da una regla simple y verificable.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
