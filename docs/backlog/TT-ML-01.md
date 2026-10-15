---
id: "TT-ML-01"
tipo: "tarea técnica"
jira: "KNOCK-77"
titulo: "Datos de entrenamiento (sintéticos al inicio) y reentrenamiento semanal de modelos ML.NET"
epica: "EP-13"
sprint: "Sprint 4 (09/11 – 13/11/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "3"
fuente: "TT-ML-01_DatosEntrenamientoReentrenamientoSemanal_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-ML-01_DatosEntrenamientoReentrenamientoSemanal_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-77); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-ML-01 — Datos de entrenamiento (sintéticos al inicio) y reentrenamiento semanal de modelos ML.NET

## 1. Datos generales

- **ID de la tarea:** TT-ML-01
- **Puntos:** 3
- **Título:** Datos de entrenamiento (sintéticos al inicio) y reentrenamiento semanal de modelos ML.NET
- **Área:** ML · Aprendizaje automático
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Sprint 4 (09/11 – 13/11/2026)
- **Día sugerido:** Lunes 9 – jueves 12
- **Clave de Jira:** KNOCK-77
- **Rama:** chore/KNOCK-77-entrenamiento-mlnet  →  PR a sprint-04
- **Depende de:** TT-BD-03, TT-API-04
- **Terminada cuando:** El modelo se reentrena y se versiona solo.

## 2. Objetivo

Tener datos y un proceso de entrenamiento para los modelos de ML (orden por cumplimiento y rango de precio).

## 3. Alcance

- Dataset sintético inicial documentado.
- Entrenamiento con ML.NET y métricas.
- Reentrenamiento semanal y versión del modelo.

## 4. Fuera de alcance

- Uso en la app (HU-060 y HU-061).

## 5. Pasos

1. Generar un dataset sintético de precios por oficio y colonia y de cumplimiento por técnico; documentar sus supuestos.
1. Entrenar la regresión de precio y el puntaje de cumplimiento con ML.NET; registrar métricas (MAE, R²).
1. Guardar el modelo versionado en Blob.
1. Programar el reentrenamiento semanal con datos reales anonimizados cuando existan.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Microsoft.ML | Entrenamiento |
| Blob modelos/ | Modelos versionados |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | El modelo se entrena y se versiona solo | Ejecución programada |
| V2 | Las métricas quedan registradas | Reporte |

## 8. Seguridad y reglas

- Sin datos personales en el entrenamiento.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Reporte de métricas.

## 11. Referencias

- DECISIONES.md #16.
