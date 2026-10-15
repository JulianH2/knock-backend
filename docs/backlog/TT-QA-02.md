---
id: "TT-QA-02"
tipo: "tarea técnica"
jira: "KNOCK-88"
titulo: "Prueba de carga con k6 y escalamiento con 2 instancias"
epica: "EP-13"
sprint: "Sprint 5 (17/11 – 20/11/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "2"
fuente: "TT-QA-02_PruebaCargaK6Escalamiento_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-QA-02_PruebaCargaK6Escalamiento_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-88); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-QA-02 — Prueba de carga con k6 y escalamiento con 2 instancias

## 1. Datos generales

- **ID de la tarea:** TT-QA-02
- **Puntos:** 2
- **Título:** Prueba de carga con k6 y escalamiento con 2 instancias
- **Área:** QA · Calidad
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Sprint 5 (17/11 – 20/11/2026)
- **Día sugerido:** Martes 17 – miércoles 18
- **Clave de Jira:** KNOCK-88
- **Rama:** chore/KNOCK-88-carga-k6  →  PR a sprint-05
- **Depende de:** TT-CD-02
- **Terminada cuando:** Reporte con tiempos y throughput.

## 2. Objetivo

Medir cuánto aguanta Knock y comprobar que escala con más instancias.

## 3. Alcance

- Escenarios k6 del recorrido principal.
- Comparación 1 contra 2 instancias.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Escribir escenarios: mapa, pedir cotización, cotizar, chat.
1. Correr contra Staging con 1 y 2 instancias.
1. Publicar el reporte con tiempos (p95) y throughput.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| k6 | Pruebas de carga |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Throughput con 2 instancias ≥ 1.6 veces el de 1 | Reporte |

## 8. Seguridad y reglas

- Datos ficticios.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Reporte k6.

## 11. Referencias

- DECISION_TECNICA.md §2.
