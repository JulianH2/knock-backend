---
id: "TT-CD-02"
tipo: "tarea técnica"
jira: "KNOCK-76"
titulo: "Producción con 2 instancias, Front Door + WAF y reversión probada"
epica: "EP-13"
sprint: "Sprint 4 (09/11 – 13/11/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "3"
fuente: "TT-CD-02_Produccion2InstanciasFront_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-CD-02_Produccion2InstanciasFront_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-76); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-CD-02 — Producción con 2 instancias, Front Door + WAF y reversión probada

## 1. Datos generales

- **ID de la tarea:** TT-CD-02
- **Puntos:** 3
- **Título:** Producción con 2 instancias, Front Door + WAF y reversión probada
- **Área:** CD · Despliegue continuo
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 4 (09/11 – 13/11/2026)
- **Día sugerido:** Lunes 9 – miércoles 11
- **Clave de Jira:** KNOCK-76
- **Rama:** chore/KNOCK-76-produccion  →  PR a sprint-04
- **Depende de:** TT-AZ-02, TT-CD-03
- **Terminada cuando:** Despliegue a producción con aprobación y rollback ensayado.

## 2. Objetivo

Tener Producción lista para la prueba cerrada y las tiendas, con varias instancias y protección en el borde.

## 3. Alcance

- Grupo de Producción con Bicep.
- 2 instancias mínimas de la API.
- Front Door + WAF.
- Reversión probada.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Parámetros de producción en Bicep.
1. Escala mínima de 2 réplicas.
1. Front Door con WAF delante de la API.
1. Entorno de GitHub con aprobación manual.
1. Ensayar una reversión a la revisión anterior.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| deploy/bicep/prod.bicepparam | Producción |
| Azure Front Door + WAF | Borde |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La API responde por Front Door con 2 instancias | Prueba manual |
| V2 | La reversión funciona | Ensayo |

## 8. Seguridad y reglas

- La API solo acepta tráfico desde Front Door.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura de las réplicas y del WAF.

## 11. Referencias

- DECISION_TECNICA.md §10.
