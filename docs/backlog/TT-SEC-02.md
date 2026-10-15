---
id: "TT-SEC-02"
tipo: "tarea técnica"
jira: "KNOCK-64"
titulo: "DevSecOps en CI: CodeQL, dependencias vulnerables y escaneo de contenedores"
epica: "EP-13"
sprint: "Sprint 3 (02/11 – 06/11/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-SEC-02_DevsecopsCiCodeqlDependencias_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-SEC-02_DevsecopsCiCodeqlDependencias_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-64); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-SEC-02 — DevSecOps en CI: CodeQL, dependencias vulnerables y escaneo de contenedores

## 1. Datos generales

- **ID de la tarea:** TT-SEC-02
- **Puntos:** 2
- **Título:** DevSecOps en CI: CodeQL, dependencias vulnerables y escaneo de contenedores
- **Área:** SEC · Seguridad (DevSecOps)
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 3 (02/11 – 06/11/2026)
- **Día sugerido:** Miércoles 4
- **Clave de Jira:** KNOCK-64
- **Rama:** chore/KNOCK-64-devsecops-ci  →  PR a sprint-03
- **Depende de:** TT-CI-01
- **Terminada cuando:** El CI falla con hallazgos graves.

## 2. Objetivo

Detectar vulnerabilidades en el código, las dependencias y los contenedores antes de fusionar.

## 3. Alcance

- CodeQL para C# y análisis de Dart.
- Dependabot.
- Escaneo de la imagen con Trivy.

## 4. Fuera de alcance

- ZAP (TT-SEC-03).

## 5. Pasos

1. Activar CodeQL en el repositorio.
1. Configurar Dependabot para NuGet, pub y GitHub Actions hacia develop.
1. Agregar Trivy al workflow de imagen.
1. Hacer que el CI falle con hallazgos altos o críticos.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| CodeQL | Análisis estático |
| Dependabot | Dependencias |
| Trivy | Contenedores |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Una dependencia vulnerable de prueba hace fallar el CI | PR de prueba |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Capturas de los reportes.

## 11. Referencias

- DECISION_TECNICA.md (DevSecOps).
