---
id: "TT-OBS-01"
tipo: "tarea técnica"
jira: "KNOCK-51"
titulo: "Observabilidad: Application Insights en la API y Sentry en la app"
epica: "EP-13"
sprint: "Sprint 2 (26/10 – 30/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-OBS-01_ObservabilidadApplicationInsightsApi_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-OBS-01_ObservabilidadApplicationInsightsApi_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-51); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-OBS-01 — Observabilidad: Application Insights en la API y Sentry en la app

## 1. Datos generales

- **ID de la tarea:** TT-OBS-01
- **Puntos:** 2
- **Título:** Observabilidad: Application Insights en la API y Sentry en la app
- **Área:** OBS · Observabilidad
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 2 (26/10 – 30/10/2026)
- **Día sugerido:** Miércoles 28
- **Clave de Jira:** KNOCK-51
- **Rama:** chore/KNOCK-51-observabilidad  →  PR a sprint-02
- **Depende de:** TT-AZ-02
- **Terminada cuando:** Un error de prueba aparece en ambos tableros.

## 2. Objetivo

Ver errores, tiempos y uso de la API y la app para detectar problemas antes que los usuarios.

## 3. Alcance

- Application Insights en la API con OpenTelemetry.
- Sentry en la app.
- traceId visible en errores de la app.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Agregar OpenTelemetry con exportador a Application Insights.
1. Agregar sentry_flutter con el entorno y la versión.
1. Mostrar el traceId en los mensajes de error para soporte.
1. Crear alertas de errores 5xx y latencia alta.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Azure Monitor OpenTelemetry | Telemetría de la API |
| sentry_flutter | Errores de la app |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un error de prueba aparece en ambos tableros | Prueba manual |
| V2 | La alerta de 5xx se dispara | Prueba manual |

## 8. Seguridad y reglas

- No enviar datos personales en la telemetría (filtrar celulares, tokens y coordenadas).
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Capturas de los tableros.

## 11. Referencias

- DECISION_TECNICA.md §10.
