---
id: "TT-AZ-02"
tipo: "tarea técnica"
jira: "KNOCK-34"
titulo: "Infraestructura como código (Bicep): Container Apps, PostgreSQL Flexible con PostGIS, Redis, Blob Storage y Key Vault"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "3"
fuente: "TT-AZ-02_InfraestructuraComoCodigoContainer_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-AZ-02_InfraestructuraComoCodigoContainer_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-34); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-AZ-02 — Infraestructura como código (Bicep): Container Apps, PostgreSQL Flexible con PostGIS, Redis, Blob Storage y Key Vault

## 1. Datos generales

- **ID de la tarea:** TT-AZ-02
- **Puntos:** 3
- **Título:** Infraestructura como código (Bicep): Container Apps, PostgreSQL Flexible con PostGIS, Redis, Blob Storage y Key Vault
- **Área:** AZ · Azure
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Lunes 19 – martes 20
- **Clave de Jira:** KNOCK-34
- **Rama:** chore/KNOCK-34-bicep-staging  →  PR a sprint-01
- **Depende de:** TT-AZ-01
- **Terminada cuando:** Staging se crea desde cero con un comando.

## 2. Objetivo

Crear Staging en Azure desde código, para poder destruirlo y recrearlo igual y no configurar nada a mano.

## 3. Alcance

- Plantillas Bicep para Container Apps (API), PostgreSQL Flexible con PostGIS, Cache for Redis, Storage (Blob) y Key Vault.
- Identidad administrada de la API con acceso a Key Vault y Blob.

## 4. Fuera de alcance

- Producción (TT-CD-02).
- Front Door + WAF (TT-CD-02).

## 5. Pasos

1. Crear deploy/bicep/main.bicep con módulos por servicio y parámetros por entorno (staging.bicepparam).
1. PostgreSQL Flexible: nivel más económico, extensión POSTGIS permitida (azure.extensions), acceso solo desde la API.
1. Container Apps: entorno, app de la API con escala mínima 0–1 en Staging, identidad administrada.
1. Key Vault con RBAC; dar a la identidad de la API permiso de lectura de secretos.
1. Storage con contenedores privados: perfiles, solicitudes, trabajos y verificacion (este último separado).
1. Redis en el nivel más bajo.
1. Documentar en docs/infra.md el comando de despliegue y el costo estimado mensual.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| deploy/bicep/main.bicep | Infraestructura |
| deploy/bicep/staging.bicepparam | Parámetros de Staging |
| az deployment group create | Aplicar |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Staging se crea desde cero con un comando | Ejecutarlo en un grupo vacío |
| V2 | SELECT postgis_version() responde en la base de Azure | psql |
| V3 | Los contenedores de Blob no permiten acceso anónimo | Intento sin firma → 403 |

## 8. Seguridad y reglas

- Sin contraseñas en los Bicep; secretos generados y guardados en Key Vault.
- Mínimo privilegio para la identidad de la API.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida del despliegue y captura del grupo de recursos.

## 11. Referencias

- DECISION_TECNICA.md §10.
