---
id: "TT-PRU-01"
tipo: "tarea técnica"
jira: "KNOCK-29"
titulo: "Ejercicio fuera de rol (Erick): endpoint GET /api/v1/oficios con su prueba"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Erick Trejo Reséndiz"
puntos: "1"
fuente: "TT-PRU-01_EjercicioFueraRolEndpoint_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-PRU-01_EjercicioFueraRolEndpoint_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-29); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-PRU-01 — Ejercicio fuera de rol (Erick): endpoint GET /api/v1/oficios con su prueba

## 1. Datos generales

- **ID de la tarea:** TT-PRU-01
- **Puntos:** 1
- **Título:** Ejercicio fuera de rol (Erick): endpoint GET /api/v1/oficios con su prueba
- **Área:** PRU · Prueba de roles
- **Épica:** EP-13
- **Responsable:** Erick Trejo Reséndiz
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Jueves 15
- **Clave de Jira:** KNOCK-29
- **Rama:** feature/KNOCK-29-endpoint-oficios  →  PR a sprint-00
- **Depende de:** TT-API-01, TT-BD-02
- **Terminada cuando:** Endpoint fusionado con prueba en verde.

## 2. Objetivo

Ejercicio fuera de rol para Erick: hacer un endpoint pequeño del backend y ver qué tan cómodo se siente en .NET.

## 3. Alcance

- GET /api/v1/oficios que devuelve los oficios activos.
- Una prueba de integración.

## 4. Fuera de alcance

- Paginación y caché.

## 5. Pasos

1. Crear OficiosController en el módulo Catalog (o en el host llamando a la interfaz pública de Catalog).
1. Devolver [{ clave, nombre, modalidad }] ordenados por nombre.
1. Escribir la prueba de integración con WebApplicationFactory y Testcontainers.
1. Abrir el PR siguiendo CONTRIBUTING.md y pedir revisión a Guillermo.
1. Anotar en el PR cuánto tardó, qué fue difícil y si usó IA (y cómo validó lo generado).

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| feature/KNOCK-29-endpoint-oficios | Rama del ejercicio |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | GET /api/v1/oficios responde 200 con 3 oficios | Prueba de integración |
| V2 | El PR pasa todas las validaciones del CI | GitHub |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- PR fusionado con la nota de cómo le fue.

## 11. Referencias

- TT-API-01.
