# Tareas 024 — Cliente de IA con Azure OpenAI y plantillas locales de respaldo

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique. T-01 es del Sprint 4; las demás, del Sprint 5.
> «Depende de» = tareas que deben estar fusionadas antes; «Paralelo con» = tareas que se pueden trabajar a la vez.

| Tarea | Qué se hace | Cubre | Depende de | Paralelo con | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Decidir el tope de tokens y actualizar el ADR-008 (N-5) | R-07, R-09 | — | todas | José Julián | pendiente |
| T-02 | Módulo `Assistant`, `IAiClient`, `AddAssistantModule()` y prueba de arquitectura del SDK | R-01, R-15, R-16 · CA-12 | — | T-03, T-04 | José Julián | pendiente |
| T-03 | `FiltroDatosPersonales` y validación de imágenes por `archivoId` (unitarias y mutación) | R-02 a R-04 · CA-01 a CA-03, CA-16 | spec 020 T-01 (solo para imágenes) | T-02, T-04 | Guillermo | en curso (filtro de datos personales hecho: CA-01 a CA-03 y CA-13; falta la validación de imágenes por `archivoId`, CA-16, y las pruebas de mutación) |
| T-04 | `ConsumoIaStore` (reserva y ajuste atómicos) y `UsoIaStore` con `db/` | R-07, R-08, R-12 · CA-05 a CA-07, CA-11, CA-14 | — | T-02, T-03 | Alexis | hecha (integración con proveedor y plantilla corresponde a T-06) |
| T-05 | `ProveedorIa` (tiempo máximo, reintento, cortacircuito, salida estructurada) y `PlantillaIa` con plantillas y prompts versionados | R-05, R-06, R-10, R-11, R-13, R-14 · CA-04, CA-09, CA-10 | T-02 | T-03, T-04 | José Julián | pendiente |
| T-06 | `AiClient`: orquestación, apagado por omisión, registro de uso; pruebas de integración con el simulador | R-09 · CA-08, CA-13, CA-15 | T-03, T-04, T-05 | — | José Julián | pendiente |
| T-07 | Evaluación de `Azure.AI.OpenAI` en `Memoria.md`, gates y revisión de par | §12 | T-06 | — | José Julián (revisa Alexis) | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
