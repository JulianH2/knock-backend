---
id: "TT-API-04"
tipo: "tarea técnica"
jira: "KNOCK-50"
titulo: "Outbox transaccional con MongoDB y mensajes programados"
epica: "EP-13"
sprint: "Backlog (etiqueta sprint-2; se trae al sprint si hay capacidad)"
responsable: "José Guillermo Rangel Hernández"
puntos: "3"
fuente: "TT-API-04_OutboxTransaccionalMensajesProgramados_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-API-04_OutboxTransaccionalMensajesProgramados_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-50); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-API-04 — Outbox transaccional con MongoDB y mensajes programados

## 1. Datos generales

- **ID de la tarea:** TT-API-04
- **Puntos:** 3
- **Título:** Outbox transaccional con MongoDB y mensajes programados
- **Área:** API · Backend base
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Backlog (etiqueta sprint-2; se trae al sprint si hay capacidad)
- **Clave de Jira:** KNOCK-50
- **Rama:** chore/KNOCK-50-outbox-mensajes  →  PR a sprint-02
- **Depende de:** TT-API-01, TT-BD-06
- **Terminada cuando:** Un evento guardado con el cambio de estado sobrevive a un reinicio y se procesa una sola vez, aunque haya dos réplicas; los fallos se reintentan con un límite.

## 2. Objetivo

Que los eventos (avisos, oleadas, vencimientos) no se pierdan si el servicio se reinicia y se procesen una sola vez aunque haya varias réplicas, con reintentos acotados y sin duplicar efectos.

## 3. Alcance

- Outbox transaccional con transacciones de MongoDB: el evento se guarda junto con el cambio de estado.
- Procesamiento del outbox: cada tarea se reserva con una operación atómica y con un tiempo de expiración (lease), para que solo una réplica la ejecute y una tarea reservada por una réplica que se cae vuelva a quedar disponible.
- Reintentos con espera creciente, límite de intentos y estado de error visible.
- Idempotencia: procesar dos veces el mismo evento no repite su efecto.
- Mensajes programados (15 min, 48 h, 7 días).
- Decisión justificada de la biblioteca de mensajes programados, o implementación propia detrás de IEventBus e IScheduler.

## 4. Fuera de alcance

- Consumidores de cada HU.
- Transporte y comunicación entre servicios, y si se usa un servicio de mensajería con costo (TT-ARQ-03). No se asume Azure Service Bus.

## 5. Pasos

1. Decidir la biblioteca de mensajes programados con una comparación escrita (licencia, soporte del outbox sobre MongoDB, costo cero, facilidad de prueba) o una implementación propia detrás de IEventBus e IScheduler; registrar la decisión y su justificación en Memoria.md. No elegir antes de comparar.
1. Antes de implementar, confirmar que Azure DocumentDB y documentdb-local soportan transacciones de varios documentos (en MongoDB requieren réplica) y anotar el resultado en Memoria.md. Si no las soportan, plan B: guardar el evento dentro del mismo documento que el cambio de estado, porque la escritura de un solo documento ya es atómica.
1. Guardar el evento en la misma transacción de MongoDB que el cambio de estado (o, en el plan B, en el mismo documento).
1. Procesar el outbox reservando cada tarea con una operación atómica y con un tiempo de expiración (lease): si la réplica que la reservó se cae, al vencer la reserva otra réplica puede tomarla. El procesamiento idempotente del paso siguiente cubre que una tarea lenta se ejecute dos veces.
1. Reintentar con espera creciente y límite de intentos; al agotarlos, dejar el evento en estado de error visible.
1. Hacer el procesamiento idempotente (identificador del evento), de modo que repetirlo no duplique su efecto.
1. Configurar mensajes programados.
1. Dejar la biblioteca elegida detrás de IEventBus e IScheduler y documentar cómo se prueba en local, sin servicios de pago.
1. Pruebas de integración con MongoDB real: supervivencia a un reinicio, una sola ejecución con dos réplicas, reintentos acotados e idempotencia.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Biblioteca de mensajes programados (a decidir en el paso 1) | Mensajes programados; la decisión se justifica por escrito |
| Transacciones de MongoDB (requieren réplica; confirmar el soporte) | El evento y el cambio de estado se guardan juntos |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Evento persistido aunque se reinicie el servicio | Prueba de integración |
| V2 | Una sola ejecución con dos réplicas | Prueba de integración |
| V3 | Reintentos con límite: tras agotar los intentos el evento queda en estado de error | Prueba de integración |
| V4 | Repetir el mismo evento no duplica su efecto | Prueba de integración |
| V5 | La decisión de la biblioteca está justificada por escrito | Revisión en el PR (Memoria.md) |
| V6 | Una tarea reservada por una réplica que se cae se reasigna al vencer su reserva y se procesa una sola vez | Prueba de integración |
| V7 | El soporte de transacciones de varios documentos (o el plan B) está confirmado y anotado | Revisión en el PR (Memoria.md) |

## 8. Seguridad y reglas

- La cadena de conexión de MongoDB viene de configuración o de Key Vault, nunca del código.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de pruebas.
- Comparación de la biblioteca de mensajes programados registrada en Memoria.md.

## 11. Referencias

- DECISION_TECNICA.md §6; constitución §3 (eventos y plazos) y §12 (réplicas).
