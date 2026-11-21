# Memoria por tarea

Desde 2026-11-21 cada tarea registra su memoria en un archivo propio de esta carpeta, en lugar de agregar una entrada a `Memoria.md`. Así dos PR que registran memoria al mismo tiempo tocan archivos distintos y no chocan al fusionarse. `Memoria.md` conserva el historial hasta 2026-11-20.

## Nombre

`AAAA-MM-DD-KNOCK-N[-slug].md`

- `AAAA-MM-DD`: fecha en que se abre la entrada.
- `KNOCK-N`: la clave de la tarea en Jira (se valida contra Jira antes de usarla).
- `-slug` (opcional): tema en minúsculas, números y guiones.
- Ejemplo: `2026-11-21-KNOCK-189.md`.

## Contenido

Una entrada por PR. Si una tarea tiene varios PR, cada uno agrega su propio archivo con un `slug` distinto (`2026-11-21-KNOCK-295-diagnostico.md`) y no edita el de otro PR, para que no choquen. Primera línea: título con `#`. Después, estas secciones:

| Sección | Obligatoria | Qué lleva |
|---|---|---|
| `## Qué cambió` | Sí | Lo que quedó distinto en el código, la documentación o la configuración. |
| `## Por qué` | Sí | La razón de cada decisión. |
| `## A evitar` | Sí | Errores y trampas que la siguiente persona no debe repetir. |
| `## Decidido` | No | Decisiones tomadas y quién las tomó, si hubo alguna. |

## Reglas

- Cada PR agrega o edita solo el archivo de su propia tarea; no se editan las entradas de otras tareas.
- No se agregan entradas nuevas a `Memoria.md`.
- Sin secretos, cadenas de conexión ni datos personales (el repositorio es público).
- `python scripts/memoria.py --check` valida nombres, secciones y claves repetidas; el CI lo corre en cada PR.

## Índice

`python scripts/memoria.py --index` genera `docs/memoria/INDEX.md` con el número de entradas históricas de `Memoria.md` y la lista de archivos de esta carpeta, de la más reciente a la más antigua. El índice es generado: no se versiona (está en `.gitignore`) y el CI lo publica en el artefacto `arquitectura`.
