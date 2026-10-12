# plan.md — TT-FB-01 — Proyecto de Firebase: autenticación por teléfono con números de prueba, FCM y apps Android/iOS registradas

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- Ninguno (configuración en Firebase); la evidencia se adjunta al PR.
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-FB-01-01 | Inicio de sesión con el número de prueba en la consola o con el emulador de Firebase Auth | manual / comando |
| TC-TT-FB-01-02 | Revisión del diff y `.gitignore`/gitleaks | manual / comando |
| TC-TT-FB-01-03 | Captura de Project settings → Your apps; los archivos están en `.gitignore` | manual / comando |
| TC-TT-FB-01-04 | Nota con el plan, cuotas y costo (riesgo 1 de `docs/DECISION_TECNICA.md`) | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
