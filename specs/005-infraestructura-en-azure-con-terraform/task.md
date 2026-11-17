# Tareas 005 — Infraestructura en Azure con Terraform

> **Estado (2026-11-13):** T-01 a T-05 y T-07 hechas; T-06 parcial. El código está en `deploy/`; la creación desde cero con `up.sh --apply` aún no se ha registrado.

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Crear el módulo compartido de Terraform | R-01, R-02 · CA-01 | hecha |
| T-02 | Crear un módulo por servicio | R-02, R-05 | hecha |
| T-03 | Configurar el estado remoto | R-03 · CA-02 | hecha |
| T-04 | Configurar identidades y permisos mínimos | R-04, R-05 | hecha |
| T-05 | Importar el clúster gratuito y excluirlo del destruir | R-07 · CA-03 | hecha |
| T-06 | Escribir los workflows de crear, desplegar, eliminar y destruir | R-01, R-06 · CA-01, CA-03 | parcial: `ci-deploy.yml` despliega; eliminar y destruir son comandos de `deploy/README.md`, no workflows (KNOCK-165) |
| T-07 | Documentar los comandos en `deploy/README.md` | R-01 · CA-04 | hecha |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
