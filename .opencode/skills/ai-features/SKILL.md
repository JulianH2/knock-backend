---
name: ai-features
description: Reglas para las funciones de IA de Knock (asistente Knock, resumen del chat, bitácora, borrador de cotización con Azure OpenAI y ML.NET para «Recomendadas» y rango de precio). Úsala en el módulo Assistant y en Reputation (ML).
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/ai-features/SKILL.md. No editar a mano. -->
# ai-features

## Principio
**La IA propone; una persona o una regla decide.** Ningún endpoint de IA cambia estados, envía cotizaciones, aprueba verificaciones ni sanciona.

## IA generativa (Azure OpenAI)
- Salida estructurada con JSON Schema (`response_format`); se valida contra el esquema y se rechaza si no cumple.
- Prompt con lo mínimo: categoría, descripción, colonia, historial del chat **sin** teléfonos ni direcciones (filtrar antes de enviar).
- Imágenes: solo las fotos del problema, por SAS de corta vida.
- Plantillas de prompt versionadas en `Modules/Assistant/Prompts/*.md`.
- Límite de costo por usuario/día y timeout de 20 s; si falla, respuesta de respaldo por reglas (la de la maqueta).
- Registrar: versión de prompt, tokens, latencia, si el usuario editó la sugerencia (para medir calidad). Sin contenido sensible en logs.
- UI: siempre etiquetar «Sugerido por IA, revísalo» y permitir editar.

## Funciones
| Función | Entrada | Salida | Quién decide |
|---|---|---|---|
| Asistente Knock | texto + foto | categoría, descripción sugerida | El cliente envía la solicitud |
| Resumen del chat | conversación | lo que sabes / por confirmar / qué llevar | El técnico guarda o descarta |
| Borrador de cotización | solicitud + chat + precios de referencia | precio, tiempo, día, nota, «qué tomé en cuenta» | El técnico edita y envía |

## ML.NET
- «Recomendadas»: ranking por cumplimiento (puntualidad, reseñas, tasa de respuesta, verificación con ventaja leve y explicada).
- Rango de precio: regresión por categoría y tipo de trabajo; mostrar rango, nunca un precio único.
- Modelos entrenados offline, versionados en `ml/`, cargados con `PredictionEnginePool`.

## Pruebas
- Contrato del JSON de salida; respaldo cuando el servicio falla; filtro de datos personales (prueba con teléfono en el chat → no llega al prompt).
