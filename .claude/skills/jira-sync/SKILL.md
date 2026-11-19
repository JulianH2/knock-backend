---
name: jira-sync
description: Mantiene la tarjeta de Jira (KNOCK-N) al día con el estado real de la rama y del PR - En curso al abrir la rama, En revisión al sacar el PR de borrador con CI verde, y un comentario de criterios cubiertos y pendientes al fusionar. Úsala al abrir una rama KNOCK-N, al sacar un PR de borrador y al fusionar.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/jira-sync/SKILL.md. No editar a mano. -->
# jira-sync

> Regla del equipo (cierre del Sprint 4): Jira refleja lo que de verdad pasa en GitHub, sin depender de acordarse. La clave `KNOCK-N` en la rama, el título del PR y los commits es lo que lo hace posible.

## Antes de empezar
1. El conector MCP de Atlassian debe estar conectado y autorizado con la sesión de la persona que invoca la skill (en Claude Code: `claude mcp add --transport http atlassian https://mcp.atlassian.com/v2/mcp` y `/mcp` para autorizar; la dirección `/v1/sse` está obsoleta y se retira el 2027-03-01). Prueba leyendo la tarjeta de tu rama. **Si no puedes leerla, detente y dilo**: no inventes el estado de una tarjeta.
2. Saca la clave `KNOCK-N` de la rama (`git branch --show-current`, formato `<tipo>/KNOCK-<N>-<english-description>`), del título del PR (`KNOCK-N · …`) y de los commits (`(KNOCK-N)`). **Si no hay clave o las fuentes no coinciden, no muevas nada**: avisa y pide la clave real.
3. Lee la tarjeta antes de moverla: estado actual, responsable y etiquetas.
4. `docs/mcp.md` y `AGENTS.md` dejan el conector en solo lectura y con las herramientas de escritura denegadas por defecto: mover o comentar la tarjeta solo se hace cuando la persona que invoca la skill (el Tech Lead o quien él autorice) aprueba esas escrituras en su propia sesión. Si están denegadas y no hay aprobación, no escribas: dime qué movimiento o comentario haría y detente.

## Qué movimiento hace cada hecho
| Hecho en GitHub | Qué haces en Jira |
|---|---|
| Se abre la rama `<tipo>/KNOCK-N-…` y empiezas a trabajar | Pasa a **En curso** (solo si estaba en Por hacer; no antes de trabajar de verdad) |
| El PR sale de borrador, **sin conflictos** y con el **CI vigente en verde** | Pasa a **En revisión** y comenta el enlace del PR y su alcance (plantilla A) |
| El PR se fusiona | **No muevas a Listo.** Comenta qué criterios se cubrieron y cuáles faltan, con la ruta de la prueba de cada uno (plantilla B) y deja la decisión de Listo a una persona con la evidencia a la vista |
| Algo te bloquea | Etiqueta **bloqueada** y comenta qué necesitas para seguir y de quién depende (plantilla C); avisa al líder |

Si el PR vuelve a borrador, tiene conflictos o el CI se pone rojo, **no** muevas la tarjeta a En revisión (y si ya estaba, comenta el motivo y pide que se regrese a En curso).

## Comprobar antes de mover a En revisión
```bash
gh pr view <N> --json isDraft,mergeStateStatus,statusCheckRollup,headRefName,title
```
Todo debe cumplirse: `isDraft` falso, `mergeStateStatus` distinto de `DIRTY`, ningún check en fallo (los `skipped` están bien) y la clave `KNOCK-N` en la rama y el título. No muevas la tarjeta por una suposición: lee el resultado.

## Plantillas de comentario
**A. En revisión**
```
PR: <url>
Alcance: <qué cubre, con la sección o tareas de la spec: specs/<ID>/spec.md §3 y T-NN>
Fuera de este PR: <lo que no entra y quién lo hace>
CI: verde al <fecha y hora>. Sin conflictos con <rama base>.
```
**B. Fusionado**
```
PR fusionado: <url> (<fecha>)
| Criterio | Prueba | Estado |
|---|---|---|
| CA-01 … | <Clase.Método o ruta> | cubierto / parcial / pendiente |
Falta para cerrar: <criterios sin prueba, pendientes de otro equipo, límites conocidos>
Listo solo cuando todos los criterios estén cubiertos con evidencia; no basta con que el PR esté fusionado.
```
**C. Bloqueo**
```
Bloqueada por: <qué o quién>
Necesito: <acción concreta para seguir>
Impacto: <qué tarjetas o PR dependen de esto>
```
Los criterios y las rutas de prueba salen de la spec (`specs/<ID>/spec.md`, §4.1) y de `python scripts/sdd_lint.py --trace <ID>`; no copies de memoria.

## Reglas
- Nunca pegues tokens, contraseñas ni datos personales en la tarjeta o sus comentarios; solo ficticios.
- No agregues atribución de IA en commits ni PR (lo manda `AGENTS.md`); el uso de IA va solo en la sección «Uso de IA» del PR.
- Una tarjeta tiene un responsable: si otra persona hizo una tarea, anótalo en el comentario (quién hizo cada tarea).
- No cambies puntos, sprint ni responsable: eso lo decide el líder.
- Informa con honestidad: si falta un criterio o el CI no corrió, dilo en el comentario en lugar de redondear hacia «listo».

## Verificación de la skill
Probarla es abrir una rama de prueba con una tarjeta de pruebas y comprobar que cada transición y comentario queda en Jira como en la tabla. Si el conector no está disponible, la skill no hace nada y lo informa.
