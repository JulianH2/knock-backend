---
id: "TT-GES-02"
tipo: "tarea técnica"
jira: "KNOCK-33"
titulo: "Retrospectiva de la semana de prueba y decisión de roles (mantener, cambiar o parejas)"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-GES-02_RetrospectivaSemanaPruebaDecision_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-GES-02_RetrospectivaSemanaPruebaDecision_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-33); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-GES-02 — Retrospectiva de la semana de prueba y decisión de roles (mantener, cambiar o parejas)

## 1. Datos generales

- **ID de la tarea:** TT-GES-02
- **Puntos:** 1
- **Título:** Retrospectiva de la semana de prueba y decisión de roles (mantener, cambiar o parejas)
- **Área:** GES · Gestión
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Viernes 16
- **Clave de Jira:** KNOCK-33
- **Rama:** chore/KNOCK-33-retro-roles  →  PR a sprint-00
- **Depende de:** TT-PRU-04
- **Terminada cuando:** Roles confirmados y anotados en la bitácora.

## 2. Objetivo

Decidir con datos si cada quien se queda en su rol, cambia o trabaja en pareja, antes de empezar el Sprint 1.

## 3. Alcance

- Revisión de la semana y retrospectiva.
- Llenar la tabla de la hoja Sprints del Sprint Planning.
- Ajustar responsables del Sprint 1 si cambian roles.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Revisión (30 min): cada quien muestra lo que terminó.
1. Medir la velocidad real: puntos terminados del Sprint 0 contra los 33 planeados.
1. Por persona, llenar: terminó lo comprometido, pruebas en verde, explica su código, avisó bloqueos a tiempo, cómo le fue fuera de su rol y si se siente cómodo (preguntarlo directo).
1. Retrospectiva (30 min): qué funcionó, qué no y qué cambiamos.
1. Decidir roles (mantener, cambiar o pareja) y anotarlo en la bitácora de la propuesta.
1. Si la velocidad salió por debajo, aplicar el orden de recorte del Sprint Planning al Sprint 1.
1. Cerrar sprint-00 con PR hacia develop y crear sprint-01 desde develop.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Sprint_Planning_Knock.xlsx → hoja Sprints | Tabla de evaluación de roles |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La tabla de roles está llena y hay decisión por persona | Hoja Sprints |
| V2 | sprint-01 existe y el Sprint 1 está planeado en Jira | GitHub y Jira |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Minuta breve de la retrospectiva.

## 11. Referencias

- Sprint_Planning_Knock.xlsx.
