# plan.md — TT-REPO-02 — Ramas main, release, develop y sprint-00 (como ITACR); CONTRIBUTING.md con el flujo; plantillas de PR e issues; CODEOWNERS

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- `CONTRIBUTING.md`, `SECURITY.md`, `.gitignore`.
- `.github/PULL_REQUEST_TEMPLATE.md`, `.github/ISSUE_TEMPLATE/*`, `docs/BRANCH_PROTECTION.md`.
- `.github/CODEOWNERS` (pendiente).
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-REPO-02-01 | `git ls-remote --heads origin` y revisión de `CONTRIBUTING.md §2` | manual / comando |
| TC-TT-REPO-02-02 | Abrir un issue y un PR de prueba; `.github/ISSUE_TEMPLATE/*.yml` se cargan como YAML válido (verificado en KNOCK-16) | manual / comando |
| TC-TT-REPO-02-03 | `gh api repos/JulianH2/<repo>/branches/<rama>/protection` (documentada; aplicación pendiente: la hace una persona, no un agente) | manual / comando |
| TC-TT-REPO-02-04 | PR de prueba que toque `.github/` (pendiente: faltan los usuarios de GitHub) | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
