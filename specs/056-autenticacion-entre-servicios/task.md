# Tareas 056 — Autenticación entre servicios (KNOCK-162)

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Resolver las dudas §8 P-01 a P-06 con el líder y registrar las decisiones en la spec | R-10 | pendiente |
| T-02 | Definir o extraer la validación de la credencial de servicio (según P-01) con falla cerrada | R-01 a R-06 · CA-02, CA-06 | pendiente |
| T-03 | Aplicar la regla común en Mensajería (`POST /api/v1/interno/avisos`, spec 037) y revisar Mercado (spec 041) | R-01 a R-05, R-09 · CA-03, CA-09 | pendiente |
| T-04 | Comprobar que el gateway no enruta `/api/v1/interno/*` | R-07 · CA-07 | pendiente |
| T-05 | Escribir las pruebas de seguridad negativa (credenciales inválidas, JWT cruzado, emisor cruzado, sin permiso) | CA-01, CA-03, CA-04, CA-05 | pendiente |
| T-06 | Pruebas de arquitectura y de no exposición de secretos | R-05, R-08 · CA-08 | pendiente |
| T-07 | Replay y rotación según P-03 y P-02 | CA-10, CA-11 | bloqueada por decisión |
| T-08 | Secretos y permisos en Key Vault y Terraform | P-04 | del líder (fuera de esta spec) |
| T-09 | `docs/endpoints.md` si cambia el esquema, `Memoria.md` e informe | — | pendiente |

## Definición de terminado
- Los casos CA de `spec.md` tienen prueba con su ID; `python scripts/sdd_lint.py --trace 056` en OK.
- Gates de `AGENTS.md §5` en verde, par revisor, `security-reviewer` LIBERA y `Memoria.md` al día.
