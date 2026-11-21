# Estado de las tareas de las specs 046 y 025 al día con los PR fusionados (KNOCK-57)

## Qué cambió
- `specs/046-tecnico-perfil-horarios-y-exploracion/task.md`: T-02, T-04, T-06, T-07, T-09, T-10, T-11 y T-12 pasan de «en revisión» a «hecha», con su tarjeta y el PR fusionado (#158, #174, #165 y #173). T-09, T-11 y T-12 son de KNOCK-57 y se entregaron dentro del PR #173, que lleva la clave KNOCK-58.
- `specs/025-verificacion-de-identidad-con-consentimiento/task.md`: T-01 y T-02 pasan a «hecha» (#208); T-04 y T-07 pasan de «en revisión parcial» a «parcial» (#208). Solo cambia el principio de la celda de estado; el resto del texto de cada fila no se toca.
- Solo documentación: ningún archivo de código ni de pruebas.

## Por qué
- Los task.md seguían diciendo «en revisión» para tareas cuyos PR ya están fusionados en `sprint-05`; con eso el avance de las specs parecía menor del real y no coincidía con las tarjetas de Jira. Para las 8 filas de la spec 046 se comprobó que todos los casos que citan (CA-01 a CA-69 según la fila) tienen su prueba en `sprint-05`.

## A evitar
- Dejar «en revisión» en un task.md después de fusionar el PR: la fila se actualiza en el mismo PR que la entrega.
- Citar en el título de un PR una clave distinta de la de la tarea que entrega (T-09, T-11 y T-12 de KNOCK-57 salieron bajo KNOCK-58).
