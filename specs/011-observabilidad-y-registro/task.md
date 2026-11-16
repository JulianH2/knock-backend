# Tareas 011 — Observabilidad y registro

> **Estado:** implementada: Serilog con Application Insights, correlación, redactor de datos personales, alertas en Terraform y `docs/OBSERVABILIDAD.md` (PR #10, #60, #62 y #67).

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Configurar Serilog y Application Insights en cada servicio | R-01, R-03 | hecha (PR #10, #60, #62, #67) |
| T-02 | Implementar el middleware de correlación | R-02 · CA-01 | hecha (PR #10, #60, #62, #67) |
| T-03 | Agregar el filtro de datos personales y su prueba | R-04 · CA-02 | hecha (PR #10, #60, #62, #67) |
| T-04 | Definir las alertas y la retención en Terraform | R-05, R-06 · CA-03 | hecha (PR #10, #60, #62, #67) |
| T-05 | Documentar cómo consultar los registros | R-07 | hecha (PR #10, #60, #62, #67) |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
