# plan.md — TT-REPO-01 — Crear los repositorios knock-backend y knock-frontend en GitHub e invitar al equipo y a los docentes

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- `README.md`, `.gitignore`, `.gitattributes` (commit base, ya en `main`).
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-REPO-01-01 | `gh repo view JulianH2/<repo> --json visibility,defaultBranchRef` y `git ls-remote --heads origin` en cada repositorio | manual / comando |
| TC-TT-REPO-01-02 | `gh repo view JulianH2/<repo> --json mergeCommitAllowed,squashMergeAllowed,rebaseMergeAllowed,deleteBranchOnMerge` y `git rev-list --merges sprint-00` = 0 | manual / comando |
| TC-TT-REPO-01-03 | Captura de Settings → Collaborators o `gh api repos/JulianH2/<repo>/collaborators` (pendiente: aún no se ha invitado a nadie) | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
