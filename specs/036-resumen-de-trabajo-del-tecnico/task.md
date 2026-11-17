# Tareas 036 — Resumen de trabajo del técnico

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Índices de `visitas` (por técnico, estado y `terminadaEn`) y de `cotizaciones` (por técnico y `enviadaEn`) en `db/` y prueba de base de datos | §6 | spec 020 y 021 fusionadas | T-02, T-03 | Guillermo | pendiente |
| T-02 | Reglas puras: dirección según estado, `motivoRechazo`, `disponibleHoy` por omisión, orden y desempate de `terminadas` (unitarias y mutación acotada) | R-03, R-04, R-05 · CA-01, CA-04, CA-07 | — | T-04 | T-01, T-03 | Guillermo |
| T-03 | Lectura de `verificacion` y `nivel` desde `tecnicos` en `TrabajoStore`, con falla de la base mapeada a `unavailable` | R-06, R-07 · CA-05, CA-08 | — | T-01, T-02 | Alexis | hecha (lectura; respuesta HTTP y cabeceras corresponden a T-04) |
| T-04 | `TecnicoTrabajoService` y `TrabajoController`: cuatro consultas, respuesta única, cursor de `terminadas`, `Cache-Control: private, no-store` y límite de lectura | R-01, R-02, R-07, R-08, R-10 · CA-03, CA-05, CA-09 | T-02, T-03 | T-05 | — | Guillermo |
| T-05 | Pruebas de integración (MongoDB y Redis reales) y seguridad negativa (cliente, técnico inactivo, cursor manipulado, base caída) | CA-01, CA-02, CA-06 | T-04 | T-06, T-07 | Guillermo (revisa Alexis) | pendiente |
| T-06 | Humo en evaluación (401 sin token, 403 para cliente) y recorrido E2E de la pestaña Trabajo con datos de semilla (frontend 019) | CA-01, CA-02 | T-05 | T-07 | Guillermo | pendiente |
| T-07 | Contrato: `oasdiff` contra `contracts/openapi.v1.json`, `docs/endpoints.md` y `docs/rate-limit-matrix.md` | §4, R-10 | T-04 | T-05, T-06 | Alexis | pendiente |
| T-08 | Pasar las puertas de CI (análisis, formato, pruebas, cobertura, escáneres) y actualizar la memoria (`Memoria.md`), con revisión de par | §7 | T-05, T-06, T-07 | — | Guillermo | pendiente |

## Dependencias con otras specs
- **Spec 020 y 021** fijan técnico, cotizaciones y visita confirmada; sin ellas se usan datos de semilla.
- **Spec 025** publica el evento de la decisión de verificación que actualiza `tecnicos`; hasta que esté fusionada se usan datos de semilla.
- **Frontend 019** consume la respuesta; su cliente se regenera cuando T-07 esté lista.

## Ruta crítica y trabajo en paralelo
- Ruta crítica: T-02 y T-03 → T-04 → T-05 → T-06 → T-08.
- En paralelo: T-01 desde el inicio; T-07 mientras T-05 y T-06 corren.

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde (`python scripts/sdd_lint.py --trace 036` sin `FALTA`).
- El CI está en verde y el contrato OpenAPI no tiene cambios incompatibles fuera de la ruta nueva.
- Un par revisó el cambio y `Memoria.md` está al día.
