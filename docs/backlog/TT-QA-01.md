---
id: "TT-QA-01"
tipo: "tarea técnica"
jira: "KNOCK-41"
titulo: "Plantillas de pruebas: xUnit + Testcontainers, NetArchTest (fronteras de módulos), flutter_test y Stryker"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "3"
fuente: "TT-QA-01_PlantillasPruebasXunitTestcontainers_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-QA-01_PlantillasPruebasXunitTestcontainers_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-41); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-QA-01 — Plantillas de pruebas: xUnit + Testcontainers, NetArchTest (fronteras de módulos), flutter_test y Stryker

## 1. Datos generales

- **ID de la tarea:** TT-QA-01
- **Puntos:** 3
- **Título:** Plantillas de pruebas: xUnit + Testcontainers, NetArchTest (fronteras de módulos), flutter_test y Stryker
- **Área:** QA · Calidad
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Lunes 19 – miércoles 21
- **Clave de Jira:** KNOCK-41
- **Rama:** chore/KNOCK-41-plantillas-pruebas  →  PR a sprint-01
- **Depende de:** TT-API-01, TT-MOB-01, TT-CI-01
- **Terminada cuando:** Cada tipo de prueba tiene un ejemplo que corre en CI.

## 2. Objetivo

Que cada tipo de prueba que pide el profesor tenga un ejemplo funcionando en el CI, para que cada HU solo copie el patrón.

## 3. Alcance

- Ejemplos de prueba unitaria, integración (Testcontainers), arquitectura (NetArchTest), contrato, mutación (Stryker.NET) y widget (flutter_test).
- Reporte de cobertura y mutación en el CI.

## 4. Fuera de alcance

- Carga (TT-QA-02), usabilidad (TT-QA-03).

## 5. Pasos

1. tests/Knock.UnitTests con xUnit y FluentAssertions o Shouldly (revisar licencia vigente).
1. tests/Knock.IntegrationTests con WebApplicationFactory y Testcontainers (PostGIS y Redis).
1. Prueba de contrato: el openapi.json generado coincide con el versionado.
1. Stryker.NET configurado por módulo con meta de 70 % y reporte HTML como artefacto del CI.
1. En app/: ejemplo de prueba de widget y de proveedor de Riverpod con mocktail.
1. Documentar en docs/pruebas.md qué tipo de prueba aplica a cada HU (tabla UB, UM, W, INT, ACC, NEG, CON, MUT, E2E, PERF, USA, A11Y).

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Testcontainers | Base real en pruebas |
| NetArchTest.Rules | Fronteras de módulos |
| dotnet-stryker | Pruebas de mutación |
| mocktail | Dobles en Flutter |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Cada tipo de prueba tiene un ejemplo que corre en CI | Workflow en verde |
| V2 | El reporte de mutación se descarga desde el CI | Artefacto del workflow |

## 8. Seguridad y reglas

- Las pruebas usan solo datos ficticios.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura de los reportes.

## 11. Referencias

- DECISION_TECNICA.md §13.
