# plan.md — TT-STD-01 — Reglas de codificación: .editorconfig, analizadores de .NET, flutter_lints, Conventional Commits y convención de ramas

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- `.editorconfig`, `scripts/ci/branch_rules.py`, `scripts/install_githooks.py`, `.githooks/{commit-msg,pre-push,lib-python.sh}`.
- Backend: `Directory.Build.props`. Frontend: `docs/templates/README.md` y `docs/templates/app/analysis_options.yaml`.
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-STD-01-01 | PR de prueba con rama inválida una vez fusionado KNOCK-26; antes, `scripts/ci/branch_rules.py` local | manual / comando |
| TC-TT-STD-01-02 | Pruebas manuales del hook con mensajes válidos e inválidos (8 de 8 con el resultado esperado en KNOCK-18) | manual / comando |
| TC-TT-STD-01-03 | Prueba manual del hook en una copia con remoto local | manual / comando |
| TC-TT-STD-01-04 | Se verifica con TT-API-01 y TT-MOB-01 (aún no hay código) | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
