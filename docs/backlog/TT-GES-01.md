---
id: "TT-GES-01"
tipo: "tarea técnica"
jira: "KNOCK-14"
titulo: "Cargar épicas, HU y tareas en Jira y crear los sprints semanales"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-GES-01_CargarEpicasHuTareas_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-GES-01_CargarEpicasHuTareas_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-14); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-GES-01 — Cargar épicas, HU y tareas en Jira y crear los sprints semanales

## 1. Datos generales

- **ID de la tarea:** TT-GES-01
- **Puntos:** 1
- **Título:** Cargar épicas, HU y tareas en Jira y crear los sprints semanales
- **Área:** GES · Gestión
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12
- **Clave de Jira:** KNOCK-14
- **Rama:** chore/KNOCK-14-cargar-jira  →  PR a sprint-00
- **Depende de:** —
- **Terminada cuando:** Tablero con S0 a S7 y todo el backlog cargado.

## 2. Objetivo

Tener en Jira todo el trabajo del proyecto (épicas, 72 HU y 49 tareas técnicas) repartido en los sprints semanales, para que desde el lunes cada quien tome sus issues y las ramas se liguen a una clave KNOCK-N.

## 3. Alcance

- Proyecto de Jira tipo Scrum con clave KNOCK.
- Sprints "Sprint 0" a "Sprint 7" con sus fechas.
- Importación de Knock_Jira_Import.csv.
- Adjuntar a cada issue del Sprint 0 su archivo individual.

## 4. Fuera de alcance

- Asignar personas en Jira (se hace después de que cada quien acepte la invitación).
- Adjuntar los archivos de los sprints siguientes (se hace al inicio de cada sprint).

## 5. Pasos

1. Crear el proyecto de Jira (plantilla Scrum) con la clave KNOCK e invitar a Erick, Guillermo y Ángel.
1. En el backlog del tablero crear los sprints "Sprint 0" a "Sprint 7" con las fechas de la hoja Sprints del Sprint Planning.
1. Importar Knock_Jira_Import.csv con la importación de issues desde CSV (codificación UTF-8, formato de fecha yyyy-MM-dd).
1. Mapear columnas: Issue Id → Issue Id, Parent → Parent, Summary, Issue Type, Priority, Story Points (o "Story point estimate"), Sprint, Due Date, las cuatro Labels y Description.
1. Si Jira no reconoce un sprint, importar sin esa columna y mover los issues filtrando por la etiqueta sprint-N.
1. Asignar en bloque filtrando por etiqueta (resp-guillermo, resp-erick, resp-angel, resp-julian).
1. Adjuntar a cada issue del Sprint 0 su archivo según Knock_Archivos_Adjuntos.csv.
1. Iniciar el Sprint 0 en el tablero.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Knock_Jira_Import.csv | Épicas, HU y tareas con prioridad, puntos, sprint, fecha y etiquetas |
| Knock_Archivos_Adjuntos.csv | Qué archivo va adjunto en cada issue |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Hay 13 épicas, 72 historias y 49 tareas | Filtro por tipo de issue en Jira |
| V2 | Cada historia y tarea cuelga de su épica | Vista de épicas del tablero |
| V3 | El Sprint 0 tiene 20 tareas y está iniciado | Tablero del sprint |
| V4 | Cada issue del Sprint 0 tiene su archivo adjunto | Revisión rápida de los 20 issues |

## 8. Seguridad y reglas

- No subir a Jira contraseñas, cadenas de conexión ni datos personales reales.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del tablero con el Sprint 0 iniciado.
- Captura de la vista de épicas.

## 11. Referencias

- Sprint_Planning_Knock.xlsx (hojas Sprints y Nomenclatura).
