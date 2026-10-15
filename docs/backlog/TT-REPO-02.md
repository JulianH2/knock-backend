---
id: "TT-REPO-02"
tipo: "tarea técnica"
jira: "KNOCK-16"
titulo: "Ramas main, release, develop y sprint-00 (como ITACR); CONTRIBUTING.md con el flujo; plantillas de PR e issues; CODEOWNERS"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-REPO-02_RamasMainReleaseDevelop_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-REPO-02_RamasMainReleaseDevelop_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-16); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-REPO-02 — Ramas main, release, develop y sprint-00 (como ITACR); CONTRIBUTING.md con el flujo; plantillas de PR e issues; CODEOWNERS

## 1. Datos generales

- **ID de la tarea:** TT-REPO-02
- **Puntos:** 1
- **Título:** Ramas main, release, develop y sprint-00 (como ITACR); CONTRIBUTING.md con el flujo; plantillas de PR e issues; CODEOWNERS
- **Área:** REPO · Repositorio
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12
- **Clave de Jira:** KNOCK-16
- **Rama:** chore/KNOCK-16-ramas-y-contributing  →  PR a sprint-00
- **Depende de:** TT-REPO-01
- **Terminada cuando:** Ramas creadas y flujo documentado en CONTRIBUTING.md (sección 2).

## 2. Objetivo

Aplicar en Knock el mismo flujo de ramas de ITACR para que el trabajo de cada quien no pise el de otro y todo llegue a producción por PR.

## 3. Alcance

- Ramas main, release, develop y sprint-00.
- CONTRIBUTING.md con el flujo (sección 2).
- Plantillas de PR e issues y CODEOWNERS.
- Protecciones de rama si la cuenta lo permite.

## 4. Fuera de alcance

- Validación automática del nombre de rama (la hace TT-CI-01).
- release-please (TT-CD-03, Sprint 3).

## 5. Pasos

1. Crear main con el primer commit; desde main crear release y develop; dejar develop como rama por defecto.
1. Crear sprint-00 desde develop.
1. Escribir CONTRIBUTING.md: jerarquía <tipo>/KNOCK-N-descripcion → sprint-XX → develop → release → main; hotfix desde main; tipos permitidos (feature, fix, docs, refactor, test, chore); formato de commit type(scope): descripción (KNOCK-N); combinaciones origen → destino permitidas; reglas (una rama y un PR por issue, nunca ramas desde ramas personales, nunca commit directo a sprint-XX, develop, release ni main).
1. Agregar .github/pull_request_template.md con: issue de Jira, qué cambia, criterios cubiertos, pruebas agregadas, evidencia (capturas) y sección "Uso de IA" (herramienta, qué generó, cómo se validó).
1. Agregar plantillas de issue para defecto (BUG) y para tarea.
1. Agregar .github/CODEOWNERS: api/ → Guillermo y Ángel; app/ → Erick; panel/ → Ángel; deploy/ y .github/ → Julián; docs/ → Julián.
1. Intentar activar protecciones en main, release y develop: exigir PR, 1 aprobación y CI en verde. Si GitHub no lo permite en el repositorio privado, anotarlo y dejar la validación al CI.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| CONTRIBUTING.md | Flujo de ramas, commits y PR (sección 2, igual que ITACR) |
| .github/pull_request_template.md | Lo que todo PR debe traer, incluido el uso de IA |
| .github/ISSUE_TEMPLATE/ | Plantillas de defecto y tarea |
| .github/CODEOWNERS | Revisor automático por carpeta |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Existen main, release, develop y sprint-00; develop es la rama por defecto | git branch -r |
| V2 | Un PR nuevo muestra la plantilla y pide revisión al dueño de la carpeta | Abrir un PR de prueba |
| V3 | Las protecciones están activas o queda anotado por qué no | Settings → Branches |

## 8. Seguridad y reglas

- Nadie hace push directo a main, release ni develop.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- CONTRIBUTING.md fusionado y captura de las ramas.

## 11. Referencias

- Flujo de ramas de itacr-backend e itacr-frontend (CONTRIBUTING.md, sección 2).
- Sprint_Planning_Knock.xlsx, hoja Nomenclatura.
