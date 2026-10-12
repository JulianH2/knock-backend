---
name: spec-first-writing
description: Refina el borrador de spec de una historia (HU-NNN), tarea técnica (TT-ÁREA-NN) o defecto (BUG-NNN) de Knock antes de escribir código - alcance por capa, contrato, amenazas, verificación por caso y plan de pruebas. Úsala al iniciar cualquier issue o cuando pidan algo sin spec.
---

# spec-first-writing

## Dónde están
- `specs/<ID>-<slug>/` con `spec.md`, `plan.md`, `task.md`, `checklists/gates.md`. Índice en `specs/README.md`.
- Los borradores se generaron del backlog con `scripts/import_backlog.py` (criterios reales del Product Backlog, plan de pruebas y trazabilidad). **Refínalos, no los reinventes.**
- Plantillas: `.specify/templates/`. Fuentes de producto: `docs/referencia/`.

## Qué refinar
1. **Jira:** escribe la clave real `KNOCK-<N>` en el encabezado (la rama es `<tipo>/KNOCK-<N>-<english-description>`).
2. **§2 Verificación:** para cada `TC-<ID>-KK`, la prueba concreta (`Suite/Clase.Metodo_TC_<ID>_KK` en C#, `'TC-<ID>-KK …'` en Dart/TS) o «(otro repo)» si se verifica en el otro repositorio.
3. **§3 Alcance** de este repositorio y Out explícito.
4. **§4 Contrato:** endpoints y eventos (backend los define; frontend indica la versión del contrato que consume).
5. **§6 Amenazas** proporcionales al riesgo, cada una con control y caso.
6. **§8 Plan de pruebas:** confirma Sí/No con motivo; meta de mutación del módulo.
7. Dudas abiertas en §8 «Límites»: no las resuelvas inventando.

## Reglas
- Casos `TC-<ID>-KK` consecutivos; no renumeres casos de una spec aprobada.
- Estado inicial **Borrador**; los agentes nunca marcan Aprobada ni Firmada.
- Defectos nuevos: crea `specs/BUG-NNN-<slug>/` con la plantilla, ligado a la HU o TT donde se encontró.

## Verificación
`python scripts/sdd_lint.py` → OK.
