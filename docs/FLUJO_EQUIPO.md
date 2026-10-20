# Flujo del equipo — cómo trabajar una tarea o historia

Guía corta para empezar. Detalle del método: [`docs/SDD_GUIA.md`](SDD_GUIA.md); ramas, commits y PR: [`CONTRIBUTING.md`](../CONTRIBUTING.md); reglas de los agentes: [`AGENTS.md`](../AGENTS.md). El texto es el mismo en `knock-backend` y `knock-frontend`.

## 1. Qué necesitas saber

- **Qué te toca:** tu tarjeta de Jira (`KNOCK-N`). Su ficha completa está en [`docs/backlog/`](backlog/INDEX.md) como `docs/backlog/<ID>.md` (por ejemplo `HU-024.md` o `TT-API-02.md`).
- **El estado** de la tarea (Por hacer, En curso, Listo) vive solo en Jira. La ficha es el requisito; la **spec** (`specs/<ID>-…/`) es el contrato que se aprueba y se implementa.
- **Una tarjeta = una rama = un PR.** La spec y el código van en el mismo PR, en este orden: primero la spec, luego la aprobación, luego el código.

## 2. Los 6 pasos

| # | Quién | Qué | Resultado |
|---|---|---|---|
| 1 | Autor | Crea la rama desde la rama del sprint (`sprint-00` esta semana): `git switch -c <tipo>/KNOCK-N-<english-kebab> origin/sprint-XX` | Rama con la clave de Jira |
| 2 | Autor + agente | El agente `spec-writer` crea o refina `specs/<ID>-…/` a partir de la ficha. **Solo spec, sin código.** Estado: Borrador. `python scripts/sdd_lint.py` en OK | Spec lista para revisar |
| 3 | Autor | Commit de la spec y **PR en *draft*** hacia `sprint-XX` (título `KNOCK-N · <ID> <título>`, plantilla completa) | PR en draft |
| 4 | **Revisor (Tech Lead)** | Lee la spec en el PR y comenta. Cuando está bien, **edita a mano las líneas de aprobación** (Estado: Aprobada, casillas marcadas, nombre y fecha) y sube ese commit a la misma rama | Spec Aprobada |
| 5 | Autor + agente | En la **misma rama** implementa con el agente constructor, con pruebas y evidencia real. Sin spec aprobada el guardián bloquea el commit de código | Código y pruebas |
| 6 | Autor | Con el CI en verde y la evidencia, marca el PR como *ready*. Otra persona lo revisa y deja su comentario (**obligatorio**, ver «Quién revisa a quién» abajo); **la aprobación final y el merge los hace el Tech Lead** con «Rebase and merge»; nadie más fusiona. Se exige el check `gate` y, en zonas sensibles, el Code Owner. Después la spec pasa a Firmada | Merge |

Reglas que no cambian:
- **El agente nunca aprueba ni firma una spec**, y el autor tampoco aprueba la suya: lo hace el revisor.
- Si una tarea toca los dos repositorios, hay un PR en cada uno con la misma clave; el backend va primero si cambia el contrato.
- Commits en inglés: `type(scope): description (KNOCK-N)`. Sin `Co-Authored-By` ni «Generated with…» de agentes; el uso de IA se declara solo en la sección «Uso de IA» del PR.
- Casos que solo se verifican con un comando (build, formato, auditoría de dependencias): en la spec, columna Verificación, se marcan **«(comando)»** y la salida del comando va en el PR. Lo agrega el revisor porque la spec ya está aprobada.
- Dependencias nuevas: se evalúan (necesidad, licencia, mantenimiento, vulnerabilidades) y se registran en `Memoria.md`; no las agrega el agente por su cuenta.
- Nunca secretos, claves, datos reales ni documentos de identidad, ni en el código ni en un prompt.
- Si el CI falla, corrige la causa. **No** bajes umbrales, desactives escáneres ni toques workflows: avisa al Tech Lead.

## 3. Plantillas para el agente (copia, cambia solo `<ID>` y `<KNOCK-N>` y envía)

Funcionan con cualquier agente (Claude Code, Copilot, Codex, Cursor, Gemini, opencode). Reemplaza `<ID>` por el identificador de la ficha (por ejemplo `HU-024` o `TT-API-02`) y `<KNOCK-N>` por la clave de tu tarjeta (por ejemplo `KNOCK-59`). El agente toma lo demás de la ficha.

### A. Empezar: redactar la spec (paso 2)
```text
Voy a empezar <ID> (<KNOCK-N>). Lee AGENTS.md, Memoria.md y la ficha docs/backlog/<ID>.md.
Usa el agente spec-writer. Si no existe specs/<ID>-*/, créala desde .specify/templates (un caso TC-<ID>-KK por cada criterio de la ficha); si existe, refínala.
Pon la clave <KNOCK-N> en el encabezado, completa verificación por caso, alcance, contrato, amenazas (STRIDE breve) y plan de pruebas. Lo que no sepas va a la sección 8 como duda; no inventes requisitos.
No escribas código. No marques la spec como Aprobada ni Firmada: eso lo hace una persona.
Propón el nombre de la rama (<tipo>/<KNOCK-N>-<descripción-en-inglés>) y dime antes de crearla.
Termina con python scripts/sdd_lint.py en OK, muéstrame la lista de archivos y espera mi confirmación antes del commit.
```

### B. Implementar (paso 5, solo cuando el revisor ya aprobó la spec en la rama)
```text
La spec de <ID> (<KNOCK-N>) ya está Aprobada en esta rama. Impleméntala con el agente constructor de tu capa (api-builder o db-builder en knock-backend; flutter-builder o panel-builder en knock-frontend), siguiendo la skill story-workflow.
Antes de crear algo, busca si ya existe (python scripts/codemap.py --query "<tema>"). No agregues dependencias sin preguntarme. No edites archivos protegidos ni generados.
Escribe cada prueba con el ID del caso en su nombre (TC-<ID>-KK). Verifica de verdad: build, formato, análisis, pruebas del plan, python scripts/sdd_lint.py --trace <ID>, scripts/codemap.py --check y scripts/ci/check_exceptions.py. Lo que no puedas ejecutar, dilo.
Si un caso solo se verifica con un comando, avísame para que el revisor agregue «(comando)» en la spec.
Muéstrame la lista de archivos antes de cada commit. Commits en inglés: type(scope): description (<KNOCK-N>), sin Co-Authored-By.
Al final entrega el informe de story-workflow y actualiza Memoria.md.
```

### C. Revisar antes de marcar *ready* (paso 6)
```text
Prepara <ID> (<KNOCK-N>) para revisión. Usa security-reviewer sobre el diff contra la rama del sprint y traceability-clerk sobre <ID>; luego docs-keeper.
Dame el veredicto PASS/FAIL de cada uno y qué casos TC siguen sin prueba. No edites nada en esta revisión.
Después redacta la descripción del PR con .github/PULL_REQUEST_TEMPLATE.md: criterios con su evidencia, pruebas que SÍ se ejecutaron (y las que no, con el motivo), amenazas, riesgos y la sección «Uso de IA».
```

### D. Atender comentarios del PR
```text
Lee los comentarios del PR de <KNOCK-N> (gh pr view <número> --comments) y atiéndelos uno por uno. Dime qué cambiaste y qué no y por qué.
No cambies reglas, umbrales ni workflows para que pase el CI. Si un comentario cambia el alcance de la spec ya aprobada, no la edites: avísame para hablarlo con el revisor.
```

## 4. Para el revisor: aprobar una spec

1. Lee la spec en el PR (`specs/<ID>-…/spec.md`): casos de §2 con prueba o `(comando)`, alcance, amenazas y dudas de §8 resueltas.
2. Responde las dudas en el PR; el autor las pasa a «Resuelto» en la spec.
3. Edita a mano en la spec: `Estado: Aprobada`, marca las dos casillas, y pon tu nombre y la fecha en la línea del revisor.
4. Corre `python scripts/sdd_lint.py`, haz el commit `docs(<scope>): approve <ID> spec (KNOCK-N)` en la rama del autor y súbelo.
5. Avisa al autor: ya puede pasar a la plantilla B.

## 5. Cuando algo no sale

- **El guardián bloquea un commit:** lee el mensaje; casi siempre falta la spec aprobada, la clave de Jira en la rama o hay un archivo protegido. No lo rodees.
- **`sdd_lint --trace` marca FALTA:** o falta la prueba con el ID del caso, o el caso es por comando y le toca «(comando)» al revisor.
- **Conflicto en `Memoria.md`:** rebase sobre la rama del sprint y conserva las entradas de todos.
- **Dudas de la ficha o del alcance:** a la sección 8 de la spec y al PR; no decidas solo.

### Quién revisa a quién (revisión de pares)
Todo PR necesita al menos un comentario de revisión de otra persona **antes** de que el Tech Lead haga la revisión final, apruebe y fusione. El revisor de pares comenta (qué revisó, qué falta); no aprueba ni fusiona.

| PR de… | Revisor de pares |
|---|---|
| Guillermo (servicios de Identidad y de Mensajería) | Alexis · Erick |
| Alexis (servicios de Mercado y de Asistente, base de datos) | Guillermo · Erick |
| Erick (app Flutter) | Guillermo o Alexis (un backend); José Julián queda para la revisión final |
| Quien tome una tarea libre (panel, landing) | La persona con más contexto que no sea el autor |
| José Julián (gobierno, CI, Azure, Terraform, Firebase) | quien tenga más contexto del cambio |

La tabla rota cuando el equipo lo decida (retrospectiva); si el revisor asignado no puede, el autor pide a otra persona y lo anota en el PR. Detalle y motivos en [ADR-005](decisiones/ADR-005-roles-carga-y-revision.md).

### Tareas libres (asignación por carga)
El panel de administración y la landing no tienen dueño fijo: llevan la etiqueta `resp-libre` en Jira. Se reparten en la planeación del sprint mirando la carga de cada persona; quien toma una tarea se la asigna **antes de empezar**, con un máximo de 2 tareas en curso por persona. A mitad del sprint, lo que siga libre lo asigna el líder. Las tareas con fecha dura que bloquean otras (por ejemplo, la landing y la URL de privacidad de Google Play) llevan dueño y fecha desde la planeación.
