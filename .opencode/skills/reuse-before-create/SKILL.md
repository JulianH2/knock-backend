---
name: reuse-before-create
description: Obliga a buscar en el mapa de código de Knock antes de crear una clase, función, servicio, widget, componente o archivo nuevo, para reutilizar o extender lo existente y evitar duplicados. Úsala siempre que vayas a crear algo nuevo.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/reuse-before-create/SKILL.md. No editar a mano. -->
# reuse-before-create

## Por qué
Los agentes tienden a escribir desde cero. Cada duplicado son dos lugares que corregir y dos pruebas que mantener, y el mapa de código lo marca como error en CI.

## Antes de crear
1. **Busca por intención:** `python scripts/codemap.py --query "enmascarar teléfono"` · `--query "paginación cursor"` · `--query "subir foto"`.
2. **Revisa nombres parecidos:** `python scripts/codemap.py --similar PhoneMasker MaskPhoneService`.
3. **Mira el índice** del módulo/feature en `docs/architecture/CODEMAP.md` y lo compartido (`Common/`, `core/`, `shared/`). Para ver quién usa qué: `docs/architecture/graph.html`.
4. **Decide y deja constancia:**
   - Reutilizar tal cual → úsalo.
   - Extender (parámetro, método nuevo en la misma clase) → hazlo ahí.
   - Extraer (existe en otro módulo/feature) → muévelo a lo compartido o expónlo por su interfaz pública; no lo copies.
   - Crear solo si nada aplica; explica por qué en `Memoria.md`.

## Al terminar
`python scripts/codemap.py` regenera el mapa y `python scripts/codemap.py --check` debe salir en OK (sin violaciones ni duplicados nuevos).

## El guardián
Si creas un archivo cuyo tipo se parece a uno existente, `knock_guard.py` bloquea el primer intento y te muestra las coincidencias. Revísalas; si de verdad es distinto, repite la creación y justifícalo en `Memoria.md`.
