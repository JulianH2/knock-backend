---
id: "TT-API-03"
tipo: "tarea técnica"
jira: "KNOCK-37"
titulo: "Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "José Guillermo Rangel Hernández"
puntos: "2"
fuente: "TT-API-03_MiddlewaresErroresProblemdetailsSerilog_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-API-03_MiddlewaresErroresProblemdetailsSerilog_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-37); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-API-03 — Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones

## 1. Datos generales

- **ID de la tarea:** TT-API-03
- **Puntos:** 2
- **Título:** Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones
- **Área:** API · Backend base
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Lunes 19 – martes 20
- **Clave de Jira:** KNOCK-37
- **Rama:** chore/KNOCK-37-middlewares-base  →  PR a sprint-01
- **Depende de:** TT-API-01
- **Terminada cuando:** Errores con formato estándar y trazables.

## 2. Objetivo

Que todos los endpoints respondan errores iguales, se puedan rastrear y estén protegidos contra abuso.

## 3. Alcance

- Manejador global de errores a ProblemDetails.
- Serilog con CorrelationId.
- Límites de peticiones por IP y por usuario.
- Validación con FluentValidation.

## 4. Fuera de alcance

- Observabilidad en Azure (TT-OBS-01).

## 5. Pasos

1. Middleware que asigna o propaga X-Correlation-Id y lo agrega a cada log y a la respuesta.
1. IExceptionHandler que traduce excepciones de dominio a ProblemDetails con códigos del catálogo (400, 401, 403, 404, 409, 422, 429).
1. FluentValidation con filtro automático que responde 422 con los campos inválidos.
1. Rate limiting de ASP.NET Core: política global por IP y política estricta para /auth.
1. Orden de middlewares documentado en la constitución.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| FluentValidation.AspNetCore | Validación |
| Microsoft.AspNetCore.RateLimiting | Límites |
| Serilog.Enrichers.CorrelationId | Rastreo |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Una validación fallida responde 422 con ProblemDetails | Prueba de integración |
| V2 | Superar el límite de /auth responde 429 | Prueba de integración |
| V3 | Cada log trae el CorrelationId de la petición | Revisión de logs |

## 8. Seguridad y reglas

- Los errores no exponen trazas ni datos internos fuera de Development.
- Los logs no incluyen tokens, celulares ni coordenadas.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de pruebas.

## 11. Referencias

- DECISION_TECNICA.md §4.
