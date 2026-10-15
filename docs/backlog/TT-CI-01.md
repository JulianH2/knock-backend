---
id: "TT-CI-01"
tipo: "tarea técnica"
jira: "KNOCK-26"
titulo: "GitHub Actions en cada PR: job branch-name (nombre y combinación origen → destino), commits, formato, build y pruebas de la API y la app"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-CI-01_GithubActionsCadaPr_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-CI-01_GithubActionsCadaPr_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-26); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-CI-01 — GitHub Actions en cada PR: job branch-name (nombre y combinación origen → destino), commits, formato, build y pruebas de la API y la app

## 1. Datos generales

- **ID de la tarea:** TT-CI-01
- **Puntos:** 2
- **Título:** GitHub Actions en cada PR: job branch-name (nombre y combinación origen → destino), commits, formato, build y pruebas de la API y la app
- **Área:** CI · Integración continua
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Martes 13 – miércoles 14
- **Clave de Jira:** KNOCK-26
- **Rama:** chore/KNOCK-26-github-actions-pr  →  PR a sprint-00
- **Depende de:** TT-REPO-02, TT-STD-01
- **Terminada cuando:** Un PR con nombre de rama, commits o pruebas incorrectos falla.

## 2. Objetivo

Que el CI haga cumplir el flujo de ramas y la calidad en cada PR, igual que en ITACR, aunque GitHub no permita proteger ramas.

## 3. Alcance

- Workflow de PR con los jobs branch-name, commits, formato, build y pruebas para api/ y app/.

## 4. Fuera de alcance

- Despliegues (TT-CD-01).
- Análisis de seguridad (TT-SEC-01 y TT-SEC-02).

## 5. Pasos

1. Crear .github/workflows/pr.yml que corre en pull_request.
1. Job branch-name: con github.head_ref y github.base_ref validar el nombre (^(feature|fix|docs|refactor|test|chore)/KNOCK-[0-9]+-[a-z0-9]+(-[a-z0-9]+)*$, o hotfix/KNOCK-N-…, sprint-NN, dependabot/…, release-please--…) y la combinación permitida: sprint-XX ← <tipo>/KNOCK-N · develop ← sprint-XX | hotfix | dependabot · release ← develop | main · main ← release | hotfix | release-please--*.
1. Job commits: cada commit del PR cumple type(scope): descripción (KNOCK-N).
1. Job api (solo si cambia api/): dotnet format --verify-no-changes, dotnet build y dotnet test.
1. Job app (solo si cambia app/): dart format --set-exit-if-changed ., flutter analyze y flutter test.
1. Job panel (solo si cambia panel/): npm ci, ng lint, ng test (headless) y ng build.
1. Usar filtros por carpeta (paths) para no correr lo que no cambió.
1. Probar con un PR que rompa cada regla y confirmar que falla con un mensaje claro.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| .github/workflows/pr.yml | Validaciones de cada PR |
| actions/setup-dotnet · subosito/flutter-action | Herramientas en el runner |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un PR desde una rama mal nombrada falla en branch-name | PR de prueba |
| V2 | Un PR de feature/… hacia develop falla (debe ir a sprint-XX) | PR de prueba |
| V3 | Un commit sin (KNOCK-N) falla | PR de prueba |
| V4 | Una prueba rota impide el merge | PR de prueba |

## 8. Seguridad y reglas

- El workflow no usa secretos en PR que vienen de forks.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Capturas de los PR de prueba fallando y uno pasando.

## 11. Referencias

- Job branch-name de ITACR.
- Sprint_Planning_Knock.xlsx, hoja Nomenclatura.
