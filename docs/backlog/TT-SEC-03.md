---
id: "TT-SEC-03"
tipo: "tarea técnica"
jira: "KNOCK-78"
titulo: "Modelo de amenazas (STRIDE) y escaneo ZAP contra Staging"
epica: "EP-13"
sprint: "Sprint 4 (09/11 – 13/11/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-SEC-03_ModeloAmenazasEscaneoZap_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-SEC-03_ModeloAmenazasEscaneoZap_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-78); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-SEC-03 — Modelo de amenazas (STRIDE) y escaneo ZAP contra Staging

## 1. Datos generales

- **ID de la tarea:** TT-SEC-03
- **Puntos:** 2
- **Título:** Modelo de amenazas (STRIDE) y escaneo ZAP contra Staging
- **Área:** SEC · Seguridad (DevSecOps)
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 4 (09/11 – 13/11/2026)
- **Día sugerido:** Miércoles 11 – viernes 13
- **Clave de Jira:** KNOCK-78
- **Rama:** chore/KNOCK-78-amenazas-zap  →  PR a sprint-04
- **Depende de:** TT-CD-01
- **Terminada cuando:** Sin hallazgos altos abiertos.

## 2. Objetivo

Revisar de forma ordenada qué puede salir mal en seguridad y probar la API como lo haría un atacante.

## 3. Alcance

- Modelo de amenazas STRIDE.
- Escaneo ZAP contra Staging.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Documentar el modelo de amenazas por módulo en docs/seguridad/stride.md.
1. Configurar ZAP (baseline o API scan con el OpenAPI) contra Staging de forma semanal.
1. Convertir hallazgos en issues BUG-NNN.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| OWASP ZAP | Escaneo dinámico |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Sin hallazgos altos abiertos | Reporte de ZAP |

## 8. Seguridad y reglas

- Solo contra Staging, nunca producción.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Reporte de ZAP.

## 11. Referencias

- SEGURIDAD_Y_PRIVACIDAD.md §10.
