# Tareas 007 — Integración continua y despliegue

> **Estado:** implementada: los workflows `ci-pr.yml`, `ci-deploy.yml`, `_governance.yml` y `dependabot.yml` están activos y se ejecutan en cada PR (PR #26, #65, #74 y #96).

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Crear `.github/workflows/ci-pr.yml` | R-02 · CA-02 | hecha (workflows activos; PR #26, #65, #74, #96) |
| T-02 | Agregar la detección de servicio cambiado | R-01 · CA-01 | hecha (workflows activos; PR #26, #65, #74, #96) |
| T-03 | Agregar los jobs de secretos y dependencias | R-03, R-04 | hecha (workflows activos; PR #26, #65, #74, #96) |
| T-04 | Crear el workflow de construcción y despliegue | R-05, R-06 · CA-03 | hecha (workflows activos; PR #26, #65, #74, #96) |
| T-05 | Registrar los checks obligatorios | R-02 | hecha (workflows activos; PR #26, #65, #74, #96) |
| T-06 | Agregar al workflow Semgrep, Gitleaks, Trivy y Dependabot, y una prueba negativa con un secreto de ejemplo | R-07 · CA-04 | hecha (workflows activos; PR #26, #65, #74, #96) |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
