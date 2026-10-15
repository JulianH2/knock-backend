# <ID> — <Título>

> **Jira:** KNOCK-<N> · **Épica:** EP-NN · **Módulo:** <módulo> · **Responsable:** <nombre> · **Sprint:** SN · **Puntos:** N · **Capas:** <BD/Back/Mobile/Panel>
> **Repositorio:** <knock-backend | knock-frontend> · **Otra parte en:** <repo o «—»>
> **Fuentes:** <DECISIONES.md #… · FUNCIONES_Y_FLUJOS.md §… · DECISION_TECNICA.md §…>
> **Skills:** <skill-1>, <skill-2>
> **Estado:** Borrador

## 1. Objetivo
<!-- Qué valor entrega y qué parte le toca a este repositorio. -->

## 2. Historia y casos de prueba
**<ID>** — Como <rol>, quiero <acción>, para <beneficio>.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-<ID>-01 | Dado …, cuando … → … | <Suite>/<Clase>.<Metodo>_TC_<ID>_01 · o «(otro repo)» · o «(comando)» |

## 3. Alcance / No-alcance
- **In (este repositorio):**
- **Out:**

## 4. Contratos API y eventos
<!-- Endpoints (docs/endpoints.md), eventos (docs/events.md). En frontend: endpoints que consume y versión del contrato. -->

## 5. Modelo de datos
<!-- Backend: esquema, tablas, índices, migración. Frontend: entidades de dominio y estado local. -->

## 6. Seguridad, privacidad y amenazas
<!-- Análisis proporcional al riesgo (STRIDE breve): suplantación, alteración, repudio, fuga de información, denegación, elevación de privilegios. Autorización por recurso, datos sensibles, límites. -->
| Amenaza | Control | Caso que lo prueba |
|---|---|---|

## 7. Pantallas
<!-- Frontend: pantallas, estados (cargando, vacío, error, datos), textos ARB/i18n, accesibilidad. Backend: «—». -->

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|

**Gates:** ver `AGENTS.md §5` (Definición de terminado).
**Límites y dudas abiertas:**

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
