---
id: "TT-RT-01"
tipo: "tarea técnica"
jira: "KNOCK-62"
titulo: "SignalR con Redis como backplane"
epica: "EP-13"
sprint: "Sprint 3 (02/11 – 06/11/2026)"
responsable: "José Guillermo Rangel Hernández"
puntos: "3"
fuente: "TT-RT-01_SignalrRedisComoBackplane_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-RT-01_SignalrRedisComoBackplane_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-62); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-RT-01 — SignalR con Redis como backplane

## 1. Datos generales

- **ID de la tarea:** TT-RT-01
- **Puntos:** 3
- **Título:** SignalR con Redis como backplane
- **Área:** RT · Tiempo real
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Sprint 3 (02/11 – 06/11/2026)
- **Día sugerido:** Lunes 2 – martes 3
- **Clave de Jira:** KNOCK-62
- **Rama:** chore/KNOCK-62-signalr-redis  →  PR a sprint-03
- **Depende de:** TT-API-01, TT-AZ-02
- **Terminada cuando:** Dos instancias entregan el mismo mensaje en vivo.

## 2. Objetivo

Que el chat y los avisos en vivo funcionen aunque haya varias instancias de la API.

## 3. Alcance

- Hub de SignalR autenticado.
- Redis como backplane.

## 4. Fuera de alcance

- Lógica del chat (HU-029).

## 5. Pasos

1. Agregar SignalR con autenticación por la sesión de Knock.
1. Configurar el backplane de Redis.
1. Grupos por conversación con verificación de pertenencia.
1. Prueba con dos instancias.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Microsoft.AspNetCore.SignalR.StackExchangeRedis | Backplane |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Dos instancias entregan el mismo mensaje | Prueba de integración |
| V2 | Un usuario ajeno no se une a un grupo | Prueba negativa |

## 8. Seguridad y reglas

- Solo se une quien pertenece a la conversación.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de pruebas.

## 11. Referencias

- DECISION_TECNICA.md §7.
