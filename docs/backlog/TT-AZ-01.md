---
id: "TT-AZ-01"
tipo: "tarea técnica"
jira: "KNOCK-27"
titulo: "Cuenta de Azure, grupo de recursos, presupuesto y alertas de costo"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-AZ-01_CuentaAzureGrupoRecursos_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-AZ-01_CuentaAzureGrupoRecursos_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-27); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-AZ-01 — Cuenta de Azure, grupo de recursos, presupuesto y alertas de costo

## 1. Datos generales

- **ID de la tarea:** TT-AZ-01
- **Puntos:** 1
- **Título:** Cuenta de Azure, grupo de recursos, presupuesto y alertas de costo
- **Área:** AZ · Azure
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12
- **Clave de Jira:** KNOCK-27
- **Rama:** chore/KNOCK-27-azure-cuenta-presupuesto  →  PR a sprint-00
- **Depende de:** —
- **Terminada cuando:** Alerta de presupuesto configurada.

## 2. Objetivo

Tener la cuenta de Azure lista y con alertas de costo antes de crear cualquier recurso, para no gastar el crédito sin darnos cuenta.

## 3. Alcance

- Suscripción de Azure para estudiantes.
- Grupos de recursos para Staging y Producción.
- Presupuesto con alertas.

## 4. Fuera de alcance

- Crear los servicios (TT-AZ-02, Sprint 1).

## 5. Pasos

1. Activar Azure for Students con el correo institucional (confirmar el crédito disponible y su vigencia).
1. Crear los grupos de recursos rg-knock-staging y rg-knock-prod en una sola región (la que tenga PostgreSQL Flexible y Container Apps disponibles; anotar si los datos quedan fuera de México para el aviso de privacidad).
1. En Cost Management crear un presupuesto mensual con alertas al 50 %, 80 % y 100 % al correo de Julián y del equipo.
1. Dar acceso a los compañeros con el mínimo rol necesario (Lector; Colaborador solo a quien despliegue).
1. Anotar en docs/infra.md la suscripción, la región, los grupos y quién tiene acceso (sin claves).

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Azure Portal → Cost Management → Budgets | Alertas de costo |
| docs/infra.md | Registro de la configuración |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Los dos grupos de recursos existen | Portal de Azure |
| V2 | El presupuesto envía una alerta de prueba | Correo recibido |

## 8. Seguridad y reglas

- Nadie comparte su contraseña de Azure; cada quien entra con su cuenta.
- Mínimo privilegio.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del presupuesto y de los grupos.

## 11. Referencias

- DECISION_TECNICA.md §10.
