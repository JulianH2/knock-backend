---
id: "TT-BD-04"
tipo: "tarea técnica"
jira: "KNOCK-103"
titulo: "Prueba de restauración de respaldo"
epica: "EP-13"
sprint: "Sprint 6 (23/11 – 27/11/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "1"
fuente: "TT-BD-04_PruebaRestauracionRespaldo_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-BD-04_PruebaRestauracionRespaldo_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-103); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-BD-04 — Prueba de restauración de respaldo

## 1. Datos generales

- **ID de la tarea:** TT-BD-04
- **Puntos:** 1
- **Título:** Prueba de restauración de respaldo
- **Área:** BD · Base de datos
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Sprint 6 (23/11 – 27/11/2026)
- **Día sugerido:** Miércoles 25
- **Clave de Jira:** KNOCK-103
- **Rama:** chore/KNOCK-103-restaurar-respaldo  →  PR a sprint-06
- **Depende de:** TT-CD-02
- **Terminada cuando:** Restauración documentada con tiempos.

## 2. Objetivo

Comprobar que se pueden recuperar los datos si algo falla.

## 3. Alcance

- Restaurar un respaldo de PostgreSQL en un servidor nuevo.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Restaurar el respaldo a un punto en el tiempo.
1. Verificar que la API funciona con la base restaurada.
1. Documentar pasos y tiempos.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Restauración de PostgreSQL Flexible | Point-in-time restore |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Restauración documentada con tiempos | docs/infra.md |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Documento de la prueba.

## 11. Referencias

- Sin exclusiones adicionales para esta tarea.
