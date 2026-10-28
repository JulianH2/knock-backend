# ADR-005 — Roles, asignación por carga y revisión de pares

> **Estado:** decidida por el Tech Lead el 2026-10-20.
> **Reemplaza:** la decisión de roles de `Memoria.md` («2026-10-19 — KNOCK-33») y la tabla «Quién revisa a quién» de `docs/FLUJO_EQUIPO.md`.
> **Tarea:** TT-GES-06.

## Contexto
La arquitectura pasó a microservicios (ADR-001) y la base a MongoDB (ADR-002). Con tres servicios y un gateway hace falta repartir el backend entre dos personas, y el trabajo de frontend (app, panel y landing) entre menos manos.

## Decisión
| Persona | Rol |
|---|---|
| José Julián | Líder del proyecto (Scrum Master, Product Owner y Tech Lead). Infraestructura, CI/CD, documentación y seguridad. **Apoya a todos**, toma tareas libres según su carga y es quien hace la **revisión final, aprueba y fusiona**. Dueño del **Gateway** junto con Guillermo (ADR-001). |
| Guillermo | Backend: servicios de **Identidad y confianza** y de **Mensajería y notificaciones**. |
| Alexis | Backend: servicio de **Mercado** (con el módulo **Asistente**: IA y ML), y la base de datos. |
| Erick | **Frontend:** app Flutter. Es además revisor de pares de los PR de backend. |

El panel de administración y la landing **no tienen dueño fijo** (ver «Tareas libres»).

## Tareas libres (asignación por carga)
- Una tarea libre lleva la etiqueta `resp-libre` en Jira.
- Se reparten en la planeación del sprint mirando la tabla de carga; quien la toma se la asigna **antes de empezar**.
- Máximo **2 tareas en curso** por persona.
- A mitad del sprint, lo que siga libre lo asigna el líder.
- **No se dejan libres** las tareas con fecha dura que bloquean otras cosas (por ejemplo, la landing y la URL de privacidad que pide Google Play): llevan dueño y fecha desde la planeación.

## Quién revisa a quién (revisión de pares)
Todo PR necesita al menos un comentario de revisión de otra persona **antes** de la revisión final del Tech Lead. El revisor de pares comenta qué revisó y qué falta; **no aprueba ni fusiona**.

| PR de… | Revisor de pares |
|---|---|
| Guillermo (Identidad, Mensajería) | Alexis · Erick |
| Alexis (Mercado, Asistente, base de datos) | Guillermo · Erick |
| Erick (app Flutter) | Guillermo o Alexis (un backend); José Julián queda para la revisión final, no como revisor de pares |
| Quien tome una tarea libre (panel, landing) | La persona con más contexto que no sea el autor |
| Gateway | Guillermo (dueño) · revisa Alexis |
| José Julián (gobierno, CI, Azure, Terraform, Firebase) | quien tenga más contexto |

**Excepción para documentación (2026-10-24, propuesta pendiente de la aprobación de 2 de los 4):** un PR que cambie **solo documentación** (specs, ADR, `Memoria.md`, evidencia) necesita la revisión final del Tech Lead y el reporte de la pre-revisión automática (`ai-review.yml`); el comentario de otro integrante es opcional. Todo PR que cambie código, workflows, scripts, infraestructura o seguridad sigue necesitando al menos un comentario de otro integrante, y la pre-revisión automática nunca lo sustituye. Si la pre-revisión no corrió o falló, se aplica la regla general. Porqué: la espera del par humano en cambios de bajo riesgo retrasaba los sprints; el riesgo se limita a documentación y se cubre con la revisión del Tech Lead más un revisor automático independiente.

## Consecuencias
- Se actualizan `docs/FLUJO_EQUIPO.md`, `.github/CODEOWNERS` por servicio (PR backend #45 y frontend #25) y las etiquetas `resp-*` en Jira (las etiquetas viejas `resp-angel` se corrigen a Alexis cuando se actualicen).
- Las historias del panel interno quedan como `resp-libre`, con la prioridad más baja frente a lo que evalúa el docente (nube, base de datos, backend, móvil y landing).
- Aprobar y fusionar sigue siendo del Tech Lead; los agentes no aprueban ni firman specs.
