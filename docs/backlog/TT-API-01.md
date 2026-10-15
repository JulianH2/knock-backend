---
id: "TT-API-01"
tipo: "tarea técnica"
jira: "KNOCK-20"
titulo: "Proyecto ASP.NET Core 10 con controllers, módulos vacíos, ProblemDetails, versionado /api/v1, health y Scalar"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Guillermo Rangel Hernández"
puntos: "3"
fuente: "TT-API-01_ProyectoAspNetCore_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-API-01_ProyectoAspNetCore_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-20); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-API-01 — Proyecto ASP.NET Core 10 con controllers, módulos vacíos, ProblemDetails, versionado /api/v1, health y Scalar

## 1. Datos generales

- **ID de la tarea:** TT-API-01
- **Puntos:** 3
- **Título:** Proyecto ASP.NET Core 10 con controllers, módulos vacíos, ProblemDetails, versionado /api/v1, health y Scalar
- **Área:** API · Backend base
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Martes 13 – miércoles 14
- **Clave de Jira:** KNOCK-20
- **Rama:** chore/KNOCK-20-proyecto-api-base  →  PR a sprint-00
- **Depende de:** TT-REPO-01, TT-STD-01
- **Terminada cuando:** La API compila, responde /health y tiene pruebas base.

## 2. Objetivo

Tener la base del backend con los módulos del sistema ya separados, para que cada HU se agregue en su módulo sin reorganizar nada después.

## 3. Alcance

- Solución .NET 10 con API (controllers) y un proyecto por módulo.
- Errores en formato ProblemDetails.
- Versionado /api/v1.
- Health checks.
- Documentación OpenAPI con Scalar en desarrollo.
- Proyectos de prueba.

## 4. Fuera de alcance

- Base de datos (TT-BD-01).
- Autenticación (TT-API-02).

## 5. Pasos

1. Crear api/Knock.sln con: src/Knock.Api (host con controllers), src/Knock.SharedKernel y un proyecto por módulo en src/Modules/: Identity, Catalog, Requests, Messaging, Reputation, Trust, Notifications, Assistant.
1. Cada módulo expone una clase pública de registro (AddXxxModule) y una interfaz pública; lo demás es internal.
1. En Program.cs: AddControllers, AddProblemDetails + UseExceptionHandler (sin try/catch en controllers), Asp.Versioning con /api/v{version}, AddOpenApi + MapOpenApi y MapScalarApiReference solo en Development.
1. Agregar /health (vivo) y /health/ready (listo, más adelante con la base de datos).
1. Agregar Serilog con salida a consola en JSON.
1. Crear tests/Knock.Api.Tests (xUnit + WebApplicationFactory) con una prueba de /health y tests/Knock.ArchitectureTests (NetArchTest) con la regla "un módulo no referencia los internos de otro".
1. Agregar api/Dockerfile multi-etapa.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| dotnet new sln / webapi --use-controllers | Crear la solución y el host |
| Asp.Versioning.Mvc | Versionado por URL |
| Microsoft.AspNetCore.OpenApi · Scalar.AspNetCore | Documento OpenAPI y visor |
| Serilog.AspNetCore | Registros estructurados |
| NetArchTest.Rules | Prueba de fronteras entre módulos |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | GET /health responde 200 | Prueba de integración |
| V2 | Una excepción no controlada responde ProblemDetails (RFC 9457) | Prueba con un endpoint de prueba |
| V3 | La prueba de arquitectura falla si un módulo usa clases internas de otro | Romperla a propósito y ver que falla |
| V4 | Scalar abre en https://localhost:<puerto>/scalar | Navegador |

## 8. Seguridad y reglas

- Scalar y el detalle de errores solo en Development.
- Nada de secretos en appsettings.json; se usan user-secrets.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura de Scalar y salida de dotnet test.

## 11. Referencias

- DECISION_TECNICA.md §2 a §4.
- Repos de referencia del profesor (WebAPIDevSecOpsScalling).
