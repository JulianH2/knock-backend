---
id: "TT-QA-04"
tipo: "tarea técnica"
jira: "KNOCK-100"
titulo: "Regresión completa y corrección de defectos"
epica: "EP-13"
sprint: "Sprint 6 (23/11 – 27/11/2026)"
responsable: "Todo el equipo"
puntos: "5"
fuente: "TT-QA-04_RegresionCompletaCorreccionDefectos_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-QA-04_RegresionCompletaCorreccionDefectos_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-100); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-QA-04 — Regresión completa y corrección de defectos

## 1. Datos generales

- **ID de la tarea:** TT-QA-04
- **Puntos:** 5
- **Título:** Regresión completa y corrección de defectos
- **Área:** QA · Calidad
- **Épica:** EP-13
- **Responsable:** Todo el equipo
- **Sprint:** Sprint 6 (23/11 – 27/11/2026)
- **Día sugerido:** Lunes 23 – viernes 27
- **Clave de Jira:** KNOCK-100
- **Rama:** chore/KNOCK-100-regresion  →  PR a sprint-06
- **Depende de:** —
- **Terminada cuando:** Suite completa en verde; cero defectos críticos.

## 2. Objetivo

Llegar al envío a tiendas sin defectos críticos.

## 3. Alcance

- Suite completa en release.
- Corrección de defectos.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Correr todas las pruebas y el E2E en release.
1. Clasificar defectos y corregir críticos y altos.
1. Congelar funciones nuevas durante la semana.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| release | Rama de estabilización |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Cero defectos críticos o altos | Jira |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Reporte de regresión.

## 11. Referencias

- Sin exclusiones adicionales para esta tarea.
