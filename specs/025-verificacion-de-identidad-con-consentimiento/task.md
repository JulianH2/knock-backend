# Tareas 025 — Verificación de identidad con consentimiento

> **Estado:** en curso desde el 2026-11-20 (Sprint 5). Versión sin fotos: T-01, T-02, T-05 (creación y consulta), T-07 y T-08 (Erick). T-03 (José Julián), T-04 y T-06 esperan.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Script de `db/` con `verificaciones` y `consentimientos` e índices (único parcial por `curpVigente`); prueba de base de datos | §6 · R-05, R-06 | — | T-02, T-03 | Alexis | en revisión (KNOCK-79): `db/025-verificaciones.js` y `Spec025VerificacionesScriptTests` |
| T-02 | Dominio y validadores: niveles, estados, CURP, consentimiento, motivos y consejos (unitarias y mutación) | R-02 a R-04, R-07, R-15 · CA-01, CA-04, CA-05, CA-08, CA-19 | — | T-01, T-03 | Guillermo | en revisión (KNOCK-79): niveles, estados, CURP, motivos y validador; `Spec025DominioTests` |
| T-03 | `CurpHmac` y `CifradoDeVerificacion` (llave por verificación envuelta con Key Vault) | R-05, R-11 · CA-06 | — | T-01, T-02 | José Julián (llaves y Key Vault) | pendiente |
| T-04 | `FotoVerificacionStore`: SAS de 5 min, validación por contenido, cifrado, borrado de cuarentena | R-08, R-10, R-12 · CA-03, CA-09 a CA-11 | T-03 | T-05 | Guillermo | en revisión parcial (KNOCK-79 y 80): creación con consentimiento y `deviceId` de la sesión, y consulta del estado; confirmación y envío esperan T-03 y T-04 |
| T-05 | Servicio y controllers de creación, consentimiento con `deviceId` de la sesión, confirmación, envío y estado | R-01, R-06 a R-09, R-13, R-14, R-18 a R-20 · CA-02, CA-07, CA-12 a CA-15, CA-17, CA-20, CA-21 | T-01, T-02, T-04 | T-06 | Guillermo | pendiente |
| T-06 | Servicio de vencimiento horario con `tareas_programadas` y borrado idempotente | R-16, R-17 · CA-16 | T-01, T-04 | T-05 | Alexis | pendiente |
| T-07 | Pruebas de integración y seguridad negativa; registros sin CURP ni direcciones | CA-01 a CA-21 | T-05, T-06 | T-08 | Guillermo | en revisión parcial: OpenAPI de Identity, `docs/endpoints.md` y `docs/rate-limit-matrix.md`; falta la semilla de la demo |
| T-08 | OpenAPI de Identity, `docs/endpoints.md`, `docs/rate-limit-matrix.md` y semilla de una verificación decidida para la demo | §5 | T-05 | T-07 | Alexis | pendiente |
| T-09 | Gates, `Memoria.md` y revisión de par | §12 | T-07, T-08 | — | Guillermo (revisa José Julián) | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
