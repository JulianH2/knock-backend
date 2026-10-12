---
name: testing-strategy-backend
description: Estrategia de pruebas de knock-backend - unitarias, arquitectura, integración con Testcontainers, seguridad negativa, base de datos, contrato, aceptación por caso TC, mutación con Stryker, fuzzing con RESTler, carga con k6, smoke y caos. Úsala al planear y escribir pruebas.
---

# testing-strategy-backend

| Tipo | Dónde | Herramienta | Nivel CI |
|---|---|---|---|
| Unitarias (UB) | `tests/UnitTest` | xUnit v2, AwesomeAssertions, FsCheck (Moq/NSubstitute solo cuando una HU lo necesite, con evaluación) | PR |
| Arquitectura | `tests/UnitTest/Architecture` | NetArchTest.eNhancedEdition (fronteras de módulo, orden del pipeline, controllers sin DbContext ni try/catch), cada regla con un caso negativo | PR |
| Integración (INT) | `tests/IntegrationTest` | WebApplicationFactory + Testcontainers (`postgis/postgis`, Redis, Azurite) | PR |
| Seguridad negativa (NEG) | `tests/SecurityTest` | 401 sin token, 403/404 con token ajeno, 429, headers, entradas maliciosas | PR |
| Base de datos | `tests/DatabaseTest` | Migraciones, semillas, restricciones, PostGIS | PR |
| Contrato (CON) | `tests/ContractTest` + `docs/openapi.v1.json` | Captura del wire + oasdiff contra main | PR |
| Aceptación (ACC) | en la suite que corresponda | Una prueba por caso `TC-<ID>-KK` | PR (trazabilidad) |
| Mutación (MUT) | `stryker-config.json` | Stryker.NET: incremental en PR (informativo), completo nightly con umbral | PR + nightly |
| Fuzzing | `tests/fuzz` | RESTler sobre el OpenAPI (endpoints públicos y con sesión) | nightly |
| Rendimiento (PERF) | `tests/perf/*.js` | k6: humo en PR de release, carga y estrés nightly; meta 2 instancias ≥ 1.6× 1 | nightly / release |
| Smoke | `tests/smoke` (script) | `/health/ready`, login de prueba, 401 sin token tras desplegar | deploy |
| Caos | `tests/chaos` | Detener Redis/PostgreSQL: degradación controlada y recuperación | nightly |

## Paquetes de prueba aprobados (TT-API-01 · KNOCK-20; KNOCK-41 lo formaliza)
- **AwesomeAssertions** (Apache-2.0) en lugar de FluentAssertions: 8.x publica una licencia propia no abierta. No uses FluentAssertions.
- **NetArchTest.eNhancedEdition** (MIT) en lugar de NetArchTest.Rules: este último no se publica desde 2021.
- **xunit v2** por ahora (compatibilidad con Stryker.NET); xunit v3 se reevalúa en KNOCK-41.
- Cualquier otro paquete de prueba se evalúa (necesidad, licencia, mantenimiento, vulnerabilidades) antes de agregarse.

## Reglas
- Nombre: `Metodo_Escenario_Resultado_TC_HU_024_03`.
- **Nunca** EF InMemory para integración. Tiempo con `TimeProvider` falso. Eventos con el harness del bus.
- Builders para datos (`UserBuilder`); nada de datos reales; números de prueba de Firebase con el emulador.
- Mutación por módulo crítico: Identity (sesión) ≥ 80 %, Requests/Trust/Reputation ≥ 70 %; resto informativo.
- Cobertura mínima: líneas 60 %, ramas 50 % (no se bajan para que pase).
- Prohibido `Skip`, `[Ignore]` o asserts vacíos.
