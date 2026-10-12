# plan.md — TT-CI-01 — GitHub Actions en cada PR: job branch-name (nombre y combinación origen → destino), commits, formato, build y pruebas de la API y la app

## 1. Mapeo de archivos
- **Reutilizar:** lo que ya existe en el repositorio (buscar con `python scripts/codemap.py --query "<tema>"`; el mapa está vacío mientras no haya código).
- **Archivos de la tarea:**
- `.github/workflows/ci-pr.yml`, `_governance.yml`, `ai-review.yml`.
- `.gitleaks.toml`, `.semgrep/`, `.semgrepignore`, `scripts/ci/pin_actions.py`, `scripts/check_coverage.py`, `docs/QA_DEVSECOPS.md`.
- **No tocar:** archivos protegidos fuera de lo que la tarea necesita (`AGENTS.md §4`).

## 2. Riesgos y efectos secundarios
- Configuración externa (GitHub, Jira, Azure, Firebase): la aplica una persona; un agente no opera servicios externos.
- No subir secretos ni datos reales; la evidencia (capturas) no debe mostrar claves.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-CI-01-01 | PR de prueba con una rama inválida (p. ej. `chore/KNOCK-26-Bad_Name`) después de fusionar KNOCK-26 | manual / comando |
| TC-TT-CI-01-02 | Ejecución de `ci-pr` en el PR de KNOCK-26 (enlace al run); localmente `actionlint` y `jscpd` en OK | manual / comando |
| TC-TT-CI-01-03 | Registro del job `sca` y del paso de Semgrep en el run del PR | manual / comando |
| TC-TT-CI-01-04 | `gh api repos/JulianH2/<repo>/branches/<rama>/protection` (lo configura una persona) | manual / comando |

## 4. Secuencia
1. Preparar la configuración o los archivos.
2. Verificar cada caso con su evidencia.
3. Actualizar `Memoria.md` y adjuntar la evidencia al PR (sección «Pruebas»).

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
