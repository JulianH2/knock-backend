# plan.md — TT-AZ-01 — Cuenta de Azure, grupo de recursos, presupuesto y alertas de costo

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- Ninguno (configuración en Azure); la evidencia se adjunta al PR.
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-AZ-01-01 | Captura de Cost Management → Budgets con sus umbrales y destinatarios | manual / comando |
| TC-TT-AZ-01-02 | Captura del grupo de recursos o `az group show` (lo ejecuta una persona; los agentes no operan Azure) | manual / comando |
| TC-TT-AZ-01-03 | Captura de Access control (IAM) | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
