---
id: "TT-MOB-03"
tipo: "tarea técnica"
jira: "KNOCK-38"
titulo: "Cliente de Dart generado desde OpenAPI e interceptor de sesión (dio)"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "Erick Trejo Reséndiz"
puntos: "2"
fuente: "TT-MOB-03_ClienteDartGeneradoDesde_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-MOB-03_ClienteDartGeneradoDesde_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-38); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-MOB-03 — Cliente de Dart generado desde OpenAPI e interceptor de sesión (dio)

## 1. Datos generales

- **ID de la tarea:** TT-MOB-03
- **Puntos:** 2
- **Título:** Cliente de Dart generado desde OpenAPI e interceptor de sesión (dio)
- **Área:** MOB · App móvil base
- **Épica:** EP-13
- **Responsable:** Erick Trejo Reséndiz
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Lunes 19 – martes 20
- **Clave de Jira:** KNOCK-38
- **Rama:** chore/KNOCK-38-cliente-openapi-dart  →  PR a sprint-01
- **Depende de:** TT-API-01, TT-MOB-01
- **Terminada cuando:** La app llama a la API sin código HTTP escrito a mano.

## 2. Objetivo

Que la app llame a la API con un cliente generado del contrato, sin escribir llamadas HTTP a mano, y que maneje la sesión en un solo lugar.

## 3. Alcance

- Generación del cliente Dart desde el OpenAPI de la API.
- Interceptor de dio para el token y la renovación (lo completa HU-003).
- Mapeo de ProblemDetails a errores de la app.

## 4. Fuera de alcance

- La lógica de renovación (HU-003).

## 5. Pasos

1. Exportar el documento OpenAPI en el build de la API (openapi.json versionado en api/).
1. Generar el cliente con openapi-generator (dart-dio) en app/packages/knock_api; script en app/tool/generate_api.sh.
1. Configurar dio con baseUrl por flavor (dev, staging, prod) e interceptor que agrega Authorization.
1. Convertir ProblemDetails en un tipo de error de la app con el mensaje del catálogo.
1. En CI, fallar si el cliente generado no coincide con el openapi.json actual.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| openapi-generator (dart-dio) | Cliente generado |
| app/packages/knock_api | Paquete del cliente |
| Flavors dev/staging/prod | URL por entorno |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La app llama /health y /me con el cliente generado | Prueba manual |
| V2 | Un cambio en el contrato sin regenerar hace fallar el CI | PR de prueba |

## 8. Seguridad y reglas

- La URL y llaves por entorno no se escriben en el código (dart-define).
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- PR con el paquete generado.

## 11. Referencias

- DECISION_TECNICA.md §3 y §9.
