---
id: "TT-API-04"
tipo: "tarea técnica"
jira: "KNOCK-50"
titulo: "Outbox transaccional y mensajes programados (MassTransit 8 o Wolverine)"
epica: "EP-13"
sprint: "Sprint 2 (26/10 – 30/10/2026)"
responsable: "José Guillermo Rangel Hernández"
puntos: "3"
fuente: "TT-API-04_OutboxTransaccionalMensajesProgramados_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-API-04_OutboxTransaccionalMensajesProgramados_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-50); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-API-04 — Outbox transaccional y mensajes programados (MassTransit 8 o Wolverine)

## 1. Datos generales

- **ID de la tarea:** TT-API-04
- **Puntos:** 3
- **Título:** Outbox transaccional y mensajes programados (MassTransit 8 o Wolverine)
- **Área:** API · Backend base
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Sprint 2 (26/10 – 30/10/2026)
- **Día sugerido:** Lunes 26 – martes 27
- **Clave de Jira:** KNOCK-50
- **Rama:** chore/KNOCK-50-outbox-mensajes  →  PR a sprint-02
- **Depende de:** TT-API-01, TT-BD-01
- **Terminada cuando:** Un evento sobrevive a un reinicio y se procesa una sola vez.

## 2. Objetivo

Que los eventos (avisos, oleadas, vencimientos) no se pierdan si el servidor se reinicia y se procesen una sola vez aunque haya varias instancias.

## 3. Alcance

- Outbox transaccional con EF Core.
- Mensajes programados (15 min, 48 h, 7 días).
- Transporte en memoria en local y Azure Service Bus en la nube.

## 4. Fuera de alcance

- Consumidores de cada HU.

## 5. Pasos

1. Elegir MassTransit 8 (licencia libre) o Wolverine; anotar la decisión en Memoria.md.
1. Configurar el outbox de EF Core para que el evento se guarde en la misma transacción que el cambio.
1. Configurar mensajes programados.
1. Crear el Service Bus de Staging en Bicep.
1. Prueba: un evento sobrevive a un reinicio; dos instancias lo procesan una sola vez.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| MassTransit 8 o Wolverine | Eventos y programados |
| Azure Service Bus | Transporte en la nube |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Evento persistido aunque se reinicie la API | Prueba de integración |
| V2 | Una sola ejecución con dos instancias | Prueba de integración |

## 8. Seguridad y reglas

- Conexión con identidad administrada.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de pruebas.

## 11. Referencias

- DECISION_TECNICA.md §6.
