# plan.md — TT-SDD-01 — Constitución, AGENTS.md y skills base para los agentes de IA

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- Ver «Alcance». Sin código de producto.
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-SDD-01-01 | `python scripts/sync_agents.py --check` en OK | manual / comando |
| TC-TT-SDD-01-02 | Pruebas simuladas de `knock_guard.py` (`pre-tool` y `stop`) con entradas válidas e inválidas | manual / comando |
| TC-TT-SDD-01-03 | `python scripts/sdd_lint.py` en OK | manual / comando |
| TC-TT-SDD-01-04 | `python scripts/codemap.py --check` y pruebas en una copia (mapa desactualizado, solo fecha o ruta distintas) | manual / comando |
| TC-TT-SDD-01-05 | Guardián (los estados Aprobada y Firmada los escribe una persona) y revisión del PR | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
