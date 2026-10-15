---
id: "TT-STD-01"
tipo: "tarea técnica"
jira: "KNOCK-18"
titulo: "Reglas de codificación: .editorconfig, analizadores de .NET, flutter_lints, Conventional Commits y convención de ramas"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-STD-01_ReglasCodificacionEditorconfigAnalizadores_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-STD-01_ReglasCodificacionEditorconfigAnalizadores_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-18); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-STD-01 — Reglas de codificación: .editorconfig, analizadores de .NET, flutter_lints, Conventional Commits y convención de ramas

## 1. Datos generales

- **ID de la tarea:** TT-STD-01
- **Puntos:** 2
- **Título:** Reglas de codificación: .editorconfig, analizadores de .NET, flutter_lints, Conventional Commits y convención de ramas
- **Área:** STD · Reglas de codificación
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12 – martes 13
- **Clave de Jira:** KNOCK-18
- **Rama:** chore/KNOCK-18-reglas-de-codificacion  →  PR a sprint-00
- **Depende de:** TT-REPO-01
- **Terminada cuando:** El CI falla si no se respetan las reglas.

## 2. Objetivo

Que todo el código se vea y se escriba igual, sin importar quién o qué herramienta de IA lo escribió, y que el CI lo compruebe solo.

## 3. Alcance

- .editorconfig común.
- Reglas de análisis de .NET y de Dart.
- Convención de commits y ramas documentada.

## 4. Fuera de alcance

- El workflow que corre estas reglas (TT-CI-01).

## 5. Pasos

1. Crear .editorconfig en la raíz: UTF-8, fin de línea LF, 4 espacios en C#, 2 en Dart/YAML/JSON, sin espacios al final.
1. En api/ crear Directory.Build.props con Nullable=enable, TreatWarningsAsErrors=true, AnalysisLevel=latest-recommended y EnforceCodeStyleInBuild=true.
1. Definir en .editorconfig las reglas de estilo de C# más importantes (var cuando el tipo es evidente, llaves siempre, nombres en PascalCase, campos privados con _).
1. En app/ agregar flutter_lints (o very_good_analysis) en analysis_options.yaml; en panel/ configurar ESLint (angular-eslint) y Prettier.
1. Agregar a CONTRIBUTING.md la sección de estilo: dotnet format --verify-no-changes, dart format --set-exit-if-changed . y flutter analyze deben pasar antes del PR.
1. Agregar la convención de commits: type(scope): descripción (KNOCK-N). Tipos: feat, fix, docs, refactor, test, chore, ci, build, perf. Scopes: identity, catalog, requests, messaging, reputation, trust, notifications, assistant, app, panel, ci, infra, docs.
1. Agregar .gitattributes con * text=auto eol=lf para evitar diferencias por Windows.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| .editorconfig | Formato común |
| api/Directory.Build.props | Analizadores y advertencias como error |
| app/analysis_options.yaml · panel/eslint.config.js · .prettierrc | Reglas de Dart y de TypeScript |
| .gitattributes | Fin de línea LF |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | dotnet format --verify-no-changes pasa en api/ | Comando local |
| V2 | flutter analyze sin hallazgos en app/ y ng lint sin hallazgos en panel/ | Comando local |
| V3 | Un archivo mal formateado hace fallar el comando | Prueba con un archivo de ejemplo |

## 8. Seguridad y reglas

- Las reglas aplican igual al código generado por IA.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de los tres comandos en el PR.

## 11. Referencias

- CONTRIBUTING.md de ITACR.
- DECISION_TECNICA.md §4.
