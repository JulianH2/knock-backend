---
id: "TT-CD-01"
tipo: "tarea técnica"
jira: "KNOCK-35"
titulo: "Despliegue continuo a Staging: push a release → imagen → Container Apps, migraciones de EF Core y smoke test; app a la pista interna de Play"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "3"
fuente: "TT-CD-01_DespliegueContinuoStagingPush_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-CD-01_DespliegueContinuoStagingPush_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-35); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-CD-01 — Despliegue continuo a Staging: push a release → imagen → Container Apps, migraciones de EF Core y smoke test; app a la pista interna de Play

## 1. Datos generales

- **ID de la tarea:** TT-CD-01
- **Puntos:** 3
- **Título:** Despliegue continuo a Staging: push a release → imagen → Container Apps, migraciones de EF Core y smoke test; app a la pista interna de Play
- **Área:** CD · Despliegue continuo
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Martes 20 – miércoles 21
- **Clave de Jira:** KNOCK-35
- **Rama:** chore/KNOCK-35-cd-staging  →  PR a sprint-01
- **Depende de:** TT-AZ-02, TT-CI-01
- **Terminada cuando:** Cada merge a release queda desplegado en Staging y probado.

## 2. Objetivo

Que cada incremento que llega a release quede desplegado y probado en Staging sin pasos manuales.

## 3. Alcance

- Workflow que al hacer push a release construye la imagen, aplica migraciones y despliega a Container Apps.
- Smoke test.
- Build de la app a la pista interna de Play cuando exista la cuenta (TT-TIE-01).

## 4. Fuera de alcance

- Producción y release-please (TT-CD-02 y TT-CD-03).

## 5. Pasos

1. Crear .github/workflows/deploy-staging.yml que corre en push a release.
1. Autenticarse en Azure con OIDC (federated credentials), sin secretos de larga duración.
1. Construir y publicar la imagen de la API en Azure Container Registry con la etiqueta del commit.
1. Aplicar migraciones con un bundle de EF Core (dotnet ef migrations bundle) antes de actualizar la API.
1. Actualizar la Container App y correr el smoke test: /health/ready debe responder 200.
1. Si el smoke test falla, regresar a la revisión anterior.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| .github/workflows/deploy-staging.yml | Despliegue a Staging |
| azure/login con OIDC | Sin secretos guardados |
| efbundle | Migraciones |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un merge de develop a release despliega a Staging | Ver el workflow y la nueva revisión |
| V2 | Una migración nueva se aplica sola | Consulta a la base |
| V3 | Un smoke test fallido regresa a la revisión anterior | Prueba con /health roto |

## 8. Seguridad y reglas

- OIDC en lugar de claves guardadas en GitHub.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del workflow en verde.

## 11. Referencias

- Flujo de ramas: release = QA/Staging.
