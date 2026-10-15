---
id: "TT-IA-01"
tipo: "tarea técnica"
jira: "KNOCK-63"
titulo: "Cliente de Azure OpenAI con filtro de datos personales, límites de costo y respaldo manual"
epica: "EP-13"
sprint: "Sprint 3 (02/11 – 06/11/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "3"
fuente: "TT-IA-01_ClienteAzureOpenaiFiltro_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-IA-01_ClienteAzureOpenaiFiltro_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-63); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-IA-01 — Cliente de Azure OpenAI con filtro de datos personales, límites de costo y respaldo manual

## 1. Datos generales

- **ID de la tarea:** TT-IA-01
- **Puntos:** 3
- **Título:** Cliente de Azure OpenAI con filtro de datos personales, límites de costo y respaldo manual
- **Área:** IA · IA generativa
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 3 (02/11 – 06/11/2026)
- **Día sugerido:** Lunes 2 – miércoles 4
- **Clave de Jira:** KNOCK-63
- **Rama:** chore/KNOCK-63-cliente-azure-openai  →  PR a sprint-03
- **Depende de:** TT-API-01, TT-AZ-02
- **Terminada cuando:** Prueba: INE, CURP, dirección y teléfono nunca llegan al modelo.

## 2. Objetivo

Tener un solo punto de acceso a la IA con filtro de datos personales, límites de costo y respaldo, para que todas las HU de IA lo usen igual.

## 3. Alcance

- Cliente de Azure OpenAI en el módulo Assistant.
- Filtro de datos personales antes del modelo.
- Salida estructurada con esquema JSON.
- Límites por usuario y respaldo.

## 4. Fuera de alcance

- Funciones de IA (HU-057, 058 y 059).

## 5. Pasos

1. Crear el recurso de Azure OpenAI en Bicep y el despliegue del modelo.
1. Cliente con reintentos y tiempo máximo de 10 s.
1. Filtro: enmascarar teléfonos, CURP, direcciones y correos; rechazar imágenes de documentos.
1. Registrar uso (tokens) por usuario sin guardar el contenido.
1. Límite configurable por usuario y por día.
1. Confirmar en la documentación de Azure que los datos no se usan para entrenar y cuánto tiempo se retienen.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Azure.AI.OpenAI | Cliente |
| assistant.uso | Registro de consumo |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | INE, CURP, dirección y teléfono nunca llegan al modelo | Prueba del filtro con textos de ejemplo |
| V2 | Si la IA falla, la función responde con el respaldo | Prueba con el servicio caído |

## 8. Seguridad y reglas

- Nunca enviar datos de identificación.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de la prueba del filtro.

## 11. Referencias

- DECISION_TECNICA.md §12.
- SEGURIDAD_Y_PRIVACIDAD.md §7.
