---
id: "TT-SDD-01"
tipo: "tarea técnica"
jira: "KNOCK-19"
titulo: "Constitución, AGENTS.md y skills base para los agentes de IA"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-SDD-01_ConstitucionAgentsMdSkills_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-SDD-01_ConstitucionAgentsMdSkills_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-19); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-SDD-01 — Constitución, AGENTS.md y skills base para los agentes de IA

## 1. Datos generales

- **ID de la tarea:** TT-SDD-01
- **Puntos:** 2
- **Título:** Constitución, AGENTS.md y skills base para los agentes de IA
- **Área:** SDD · Desarrollo con IA
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Martes 13
- **Clave de Jira:** KNOCK-19
- **Rama:** chore/KNOCK-19-constitucion-y-agents  →  PR a sprint-00
- **Depende de:** TT-REPO-01, TT-STD-01
- **Terminada cuando:** Cualquier agente lee las mismas reglas.

## 2. Objetivo

Que cualquier agente de IA (Claude Code, opencode, Copilot…) siga las mismas reglas del proyecto, para que el código generado funcione junto y no duplique funciones.

## 3. Alcance

- Constitución del proyecto.
- AGENTS.md (manual operativo para agentes).
- Skills base.
- Plantilla de spec por rebanada.

## 4. Fuera de alcance

- Specs de cada HU (se escriben al inicio de cada sprint).

## 5. Pasos

1. Copiar al repositorio la constitución (.specify/memory/constitution.md), AGENTS.md y las skills (.agents/skills/) que ya se prepararon junto con DECISION_TECNICA.md.
1. Revisar que digan lo vigente: REST + OpenAPI, SignalR + Redis, Riverpod, fotos de verificación borradas al decidir o a los 7 días, no usar datos reales.
1. Agregar a la constitución el flujo de ramas de CONTRIBUTING.md y el formato de commit con KNOCK-N.
1. Agregar la regla: antes de crear una función, buscar en el repositorio si ya existe (mapa de código); nunca duplicar.
1. Agregar la regla: nunca pasar a la IA secretos, tokens, contraseñas, credenciales, documentos de identidad, datos personales reales ni cadenas de conexión.
1. Crear docs/specs/_plantilla/ con spec.md, plan.md, tasks.md y gates.md.
1. Generar con scripts/sync_agents.py los archivos para cada herramienta (CLAUDE.md, etc.) y comprobar que todos apuntan a AGENTS.md.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| .specify/memory/constitution.md | Lo que no se negocia |
| AGENTS.md | Cómo trabaja cualquier agente en el repo |
| .agents/skills/ | Conocimiento reutilizable (rebanada vertical, PostGIS, Flutter, seguridad) |
| docs/specs/_plantilla/ | Plantilla de spec por HU |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La constitución no contradice DECISIONES.md ni DECISION_TECNICA.md | Revisión de Julián con checklist |
| V2 | Un agente, al pedirle "¿cómo nombro mi rama?", responde con el formato correcto | Prueba con Claude Code u opencode |

## 8. Seguridad y reglas

- La constitución prohíbe compartir con la IA cualquier dato sensible o secreto.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura de la respuesta del agente de prueba.

## 11. Referencias

- DECISION_TECNICA.md §13.
- SEGURIDAD_Y_PRIVACIDAD.md §8.
