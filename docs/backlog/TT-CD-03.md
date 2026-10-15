---
id: "TT-CD-03"
tipo: "tarea técnica"
jira: "KNOCK-61"
titulo: "release-please en main (versiones de api, app y panel), CHANGELOG, tag vX.Y.Z y deploy.yml a producción"
epica: "EP-13"
sprint: "Sprint 3 (02/11 – 06/11/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-CD-03_ReleasePleaseMainChangelog_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-CD-03_ReleasePleaseMainChangelog_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-61); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-CD-03 — release-please en main (versiones de api, app y panel), CHANGELOG, tag vX.Y.Z y deploy.yml a producción

## 1. Datos generales

- **ID de la tarea:** TT-CD-03
- **Puntos:** 2
- **Título:** release-please en main (versiones de api, app y panel), CHANGELOG, tag vX.Y.Z y deploy.yml a producción
- **Área:** CD · Despliegue continuo
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 3 (02/11 – 06/11/2026)
- **Día sugerido:** Jueves 5
- **Clave de Jira:** KNOCK-61
- **Rama:** chore/KNOCK-61-release-please  →  PR a sprint-03
- **Depende de:** TT-CD-01
- **Terminada cuando:** Un merge de release a main crea el tag y despliega.

## 2. Objetivo

Versionar y publicar a producción con un PR, igual que en ITACR.

## 3. Alcance

- release-please en modo monorepo (api, app y panel).
- CHANGELOG y tags.
- deploy.yml en push a main.

## 4. Fuera de alcance

- Sin exclusiones adicionales para esta tarea.

## 5. Pasos

1. Configurar release-please-config.json y .release-please-manifest.json con los tres componentes.
1. Workflow que abre el PR release-please--* hacia main.
1. deploy.yml: en push a main, migración con bundle de EF Core y luego despliegue de la API; build de la app a producción cuando aplique.
1. Back-merge automático main → release.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| release-please-action | Versiones y CHANGELOG |
| deploy.yml | Producción |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un merge de release a main genera el PR de versión y, al fusionarlo, el tag | Prueba real |

## 8. Seguridad y reglas

- Despliegue a producción con aprobación manual del entorno.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Tag y CHANGELOG.

## 11. Referencias

- Flujo de ramas de ITACR.
