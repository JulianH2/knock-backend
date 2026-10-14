# Cómo contribuir a Knock

Aplica a personas y a agentes de IA. Reglas completas: [`.specify/memory/constitution.md`](.specify/memory/constitution.md) y [`AGENTS.md`](AGENTS.md).

## 1. Antes de empezar (una vez)
```bash
python scripts/install_githooks.py     # pre-commit, commit-msg y pre-push
python scripts/sync_agents.py          # adaptadores de tu agente de IA
```
Activa los git hooks de este clon (si no usaste `install_githooks.py`):
```bash
git config core.hooksPath .githooks
```
El hook `commit-msg` rechaza el commit si el mensaje no cumple el formato de la sección 3, si no lleva `(KNOCK-N)`, si la clave no coincide con la de tu rama o si incluye atribución a IA (trailers `Co-Authored-By` de agentes o líneas «Generated with…»). Un `Co-Authored-By` de una persona del equipo se permite. Funciona con Git Bash en Windows y en Linux/macOS (necesita Python 3.11+; ver «Python (requisito)»; los hooks eligen solos un Python válido y descartan el alias de la Microsoft Store). En Linux/macOS los archivos de `.githooks/` deben ser ejecutables (`git update-index --chmod=+x .githooks/*`). No uses `--no-verify`.

## Python (requisito)
- Los scripts de `scripts/` requieren **Python 3.11 o superior** (el CI usa 3.12). Compruébalo con `python --version`.
- **Windows:** instala Python desde python.org (marca «Add to PATH») y comprueba con `py -0p`. Si `python3` abre la Microsoft Store, desactiva *Configuración → Aplicaciones → Configuración avanzada → Alias de ejecución de aplicaciones* para `python.exe` y `python3.exe`.
- Los hooks de los agentes generados por `sync_agents.py` llaman a `python3`. En Windows, si no existe, el hook falla y el guardián no corre: no lo ignores, avisa o aplica la propuesta de lanzador (`_propuestas/hooks-multiplataforma`).
- **No definas `KNOCK_PYTHON` al correr `sync_agents.py`**: esa variable cambia los comandos generados y el CI (`sync_agents.py --check`, con `python3`) marcaría diferencias. Para el resto de scripts basta con invocarlos con tu intérprete (`python scripts/sdd_lint.py` o `py -3.12 scripts/sdd_lint.py`). `KNOCK_PYTHON` solo lo lee hoy el plugin de opencode.

## 2. Ramas
| Rama | Rol | Recibe PR de |
|---|---|---|
| `main` | Producción. release-please crea tag, CHANGELOG y release; `deploy.yml` despliega con aprobación | `release`, `hotfix/*`, `release-please--*` |
| `release` | QA en Staging (`deploy-staging.yml`) | `develop`, `main` (back-merge) |
| `develop` | Integración (rama por defecto) | `sprint-XX`, `hotfix/*`, `dependabot/*`, `contract/api-*` (solo frontend) |
| `sprint-XX` | Una por sprint semanal (`sprint-00` … `sprint-07`); nadie hace commit directo | ramas personales |
| `<tipo>/KNOCK-<N>-<english-kebab-description>` | Personal, una por issue de Jira (`feature`, `fix`, `docs`, `refactor`, `test`, `chore`); descripción en inglés y kebab-case, p. ej. `chore/KNOCK-20-aspnet-core-project`; sale del `sprint-XX` actual y se borra al fusionar | — |
| `hotfix/KNOCK-<N>-<english-kebab-description>` | Arreglo urgente: PR a `main` y la misma rama a `develop`; luego back-merge `main → release` | — |

El job `pr-meta` del CI rechaza nombres y combinaciones fuera de este esquema.

## 3. Commits y PR
- Commit: mensaje **en inglés**, Conventional Commits: `type(scope): description in english, imperative, lowercase (KNOCK-N)`.
  Ejemplo: `feat(requests): upload up to 5 photos with the request (KNOCK-45)`.
  - Types: feat, fix, docs, refactor, test, chore, ci, build, perf.
  - Scopes: identity, catalog, requests, messaging, reputation, trust, notifications, assistant, app, panel, ci, infra, docs (fuente: `.agents/knock.json`).
  - La clave `(KNOCK-N)` es obligatoria, va al final y debe coincidir con la de la rama.
  - Descripción en imperativo, empieza en minúscula, sin punto final; encabezado de hasta 100 caracteres.
  - **Prohibida** la atribución a IA en commits y PR: trailers `Co-Authored-By` de agentes (Claude, Copilot, Codex, Gemini, Cursor, opencode, `noreply@anthropic.com`) y líneas «Generated with…». El uso de IA se declara solo en la sección «Uso de IA» de la plantilla del PR. Un `Co-Authored-By` de una persona del equipo (trabajo en parejas) sí se permite.
  - Excepción para bots (sin clave KNOCK-N): release-please (`chore(main|release)`), Dependabot (`build(deps)`) y el commit automático del contrato de la API (`chore(deps): update api contract to <sha>`).
- PR: título `KNOCK-N · <ID> <título>` y la plantilla completa (criterios, pruebas, amenazas, riesgos, **Uso de IA**, evidencia). Sin atribución a IA en la descripción salvo en esa sección.
- 1 aprobación de otro integrante + Code Owners en zonas sensibles + check `gate` en verde.

## 4. Definición de terminado
Criterios de la spec cumplidos · pruebas del plan en verde (resultados reales) · PR revisado · CI en verde · «Uso de IA» completo · `Memoria.md` al día · spec firmada tras el merge.

## 5. Verificaciones locales (las mismas del CI)
Ver la sección «Comandos» de `AGENTS.md`. Matriz de controles y qué bloquea el merge: [`docs/QA_DEVSECOPS.md`](docs/QA_DEVSECOPS.md).

## 6. Dependencias nuevas
Justifica necesidad, licencia (permitidas: MIT, Apache-2.0, BSD, ISC, MPL-2.0…), mantenimiento y vulnerabilidades en el PR y en `Memoria.md`. Los agentes no agregan dependencias por su cuenta.

## 7. Seguridad
Vulnerabilidades: [`SECURITY.md`](SECURITY.md). Nunca subas secretos, datos reales ni documentos de identidad (ni de prueba reales).

## 8. Revisión con IA en el CI (opcional)
El workflow `ai-review.yml` revisa cada PR con los agentes `security-reviewer` y `traceability-clerk`. **No sustituye a la revisión humana y no es parte del check requerido `gate`.** Mientras falte cualquiera de las dos piezas de abajo, el job `review` se **omite solo** (aparece como *skipped*, sin poner el PR en rojo).

Para activarlo cuando exista la cuenta, en **cada repositorio** (knock-backend y knock-frontend), en GitHub → Settings → Secrets and variables → Actions:
1. **Secreto** (pestaña *Secrets* → New repository secret): nombre `ANTHROPIC_API_KEY`, con la clave de API generada en la consola de Anthropic. Fija un límite de gasto en la cuenta.
2. **Variable** (pestaña *Variables* → New repository variable): nombre `AI_REVIEW`, valor `true`.

La clave nunca va en el código, un issue, un PR ni un chat. Los PR de forks no reciben secretos, por lo que la revisión se omite en ellos.

Si el repositorio es privado y no tiene GitHub Advanced Security, la revisión de dependencias y la subida de SARIF se omiten; para activarlas, crea la variable `GHAS_ENABLED=true`.

