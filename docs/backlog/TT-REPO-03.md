---
id: "TT-REPO-03"
tipo: "tarea técnica"
jira: "KNOCK-17"
titulo: "Conectar GitHub con Jira (clave KNOCK) para ligar ramas, commits y PR a cada issue"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-REPO-03_ConectarGithubJiraLigar_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-REPO-03_ConectarGithubJiraLigar_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-17); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-REPO-03 — Conectar GitHub con Jira (clave KNOCK) para ligar ramas, commits y PR a cada issue

## 1. Datos generales

- **ID de la tarea:** TT-REPO-03
- **Puntos:** 1
- **Título:** Conectar GitHub con Jira (clave KNOCK) para ligar ramas, commits y PR a cada issue
- **Área:** REPO · Repositorio
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12
- **Clave de Jira:** KNOCK-17
- **Rama:** chore/KNOCK-17-conectar-github-jira  →  PR a sprint-00
- **Depende de:** TT-GES-01, TT-REPO-01
- **Terminada cuando:** Un PR de prueba aparece dentro de su issue de Jira.

## 2. Objetivo

Que cada rama, commit y PR aparezca dentro de su issue de Jira usando la clave KNOCK-N, para tener trazabilidad sin capturar nada a mano.

## 3. Alcance

- Instalar la integración de GitHub con Jira y conectar el repositorio knock.

## 4. Fuera de alcance

- Automatizaciones de Jira (mover issues al hacer merge) — opcional después.

## 5. Pasos

1. En Jira, instalar la aplicación oficial "GitHub for Jira" desde el Marketplace de Atlassian.
1. Conectar la cuenta u organización de GitHub y dar acceso solo al repositorio knock.
1. Crear una rama de prueba chore/KNOCK-17-prueba-integracion desde sprint-00, hacer un commit "chore(ci): prueba de integración (KNOCK-17)" y abrir un PR.
1. Verificar que la rama, el commit y el PR aparecen en el panel "Desarrollo" del issue KNOCK-17.
1. Cerrar el PR de prueba sin fusionar y borrar la rama.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| GitHub for Jira | Liga ramas, commits y PR por la clave KNOCK-N |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | El issue de prueba muestra rama, commit y PR | Panel Desarrollo del issue en Jira |

## 8. Seguridad y reglas

- Dar acceso solo al repositorio knock, no a toda la cuenta.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del panel Desarrollo del issue de prueba.

## 11. Referencias

- Documentación de GitHub for Jira (Atlassian).
