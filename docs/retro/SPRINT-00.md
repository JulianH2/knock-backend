# Sprint 0 — semana de prueba (12 al 16 de octubre de 2026)

> Resumen de lo que hizo cada persona, día a día, y de lo que quedó pendiente. Fuente: los commits y PR de `knock-backend` y `knock-frontend` (hora local) más lo conversado en las revisiones. Lo que se hizo solo en consolas (Azure, Firebase) o en el chat no deja commit y se anota según lo reportado. El estado de cada tarea vive en Jira. Insumo de la retrospectiva (KNOCK-33, TT-GES-02); la decisión de roles la toma el equipo y no se registra aquí.

## José Julián Hernández (Tech Lead)

| Día | Qué hizo |
|---|---|
| **Lun 12** | Creó los dos repositorios (`knock-backend` y `knock-frontend`) con sus ramas (KNOCK-15). Dejó el flujo de ramas, `CONTRIBUTING` y las plantillas de PR (KNOCK-16); las reglas de código y commits (KNOCK-18); y la constitución, `AGENTS.md`, skills y guardián para los agentes (KNOCK-19). Cargó las tareas en Jira (KNOCK-14). |
| **Mar 13** | Conectó GitHub con Jira (KNOCK-17). Registró el grupo de recursos y el presupuesto de Azure (KNOCK-27). Aprobó las specs de Alexis (KNOCK-22 y KNOCK-23). |
| **Mié 14** | Configuró Firebase con números de prueba y FCM, y registró las apps Android e iOS (KNOCK-28). Puso en marcha el CI de cada PR en los dos repos (KNOCK-26) y corrigió los fallos de su primera ejecución, con la excepción EXC-001. Añadió CODEOWNERS (KNOCK-135) y Dependabot (KNOCK-136). Aprobó las specs de Guillermo y de Alexis (panel). Revisó y fusionó los PR de Alexis y de Guillermo. |
| **Jue 15** | Convirtió las 121 fichas del backlog a Markdown y escribió la guía del flujo del equipo con plantillas para agentes. Creó el marcador `(comando)` para los casos de spec que se verifican con comando. Revisó los PR de Erick, Alexis y Guillermo. Ajustó el CI para la API: pasos condicionales, cobertura sin código generado, excepciones EXC-002 y EXC-003, orden del mapa de código y traza solo con código. Fusionó el PR de TT-API-01. |

## Guillermo Rangel

| Día | Qué hizo |
|---|---|
| **Mar 13** | Primer intento de TT-API-01: su agente escribió código antes de que la spec estuviera aprobada; se apartó en un stash y se rehízo con la spec primero. |
| **Mié 14** | Redactó la spec de TT-API-01 y resolvió sus puntos abiertos; el PR de la spec (#10) se fusionó. |
| **Jue 15** | Escribió la API base de ASP.NET Core 10: 8 módulos vacíos, `/health`, ProblemDetails, versionado, Scalar y Serilog. Añadió pruebas de arquitectura, integración y seguridad (32 en total) y el Dockerfile multi-etapa. Atendió el CI: el falso positivo de Gitleaks, pruebas para la cobertura y el `skip` de Checkov. Subió la evidencia de Docker (TC-15) y la captura de Scalar. Abrió el PR #17, ya fusionado. |
| **Vie 16** | Subió la spec de TT-API-02 (KNOCK-21, spike del token de Firebase) en borrador: PR #26. |
| **Aparte** | Mandó dos PR de documentación del proyecto ITACR (ITACR-42): diseño de pedidos públicos para recoger. |

## Alexis Blas

| Día | Qué hizo |
|---|---|
| **Mar 13** | Redactó los borradores de spec de TT-BD-01 (PostGIS), TT-BD-02 (catálogo de oficios y colonias) y TT-PRU-03 (panel), y refinó el catálogo de Tula. |
| **Mié 14** | Alineó las specs con las fichas y con los comentarios de la revisión. Creó el Compose local con PostGIS, verificó el orden de coordenadas y corrigió el flujo del `.env`. Hizo el scaffold del panel Angular: componentes, menú estático, pruebas y formato, con 100 % de cobertura. Resolvió conflictos de `Memoria.md` en sus PR. |
| **Jue 15** | Marcó TC-01 como comando y rebasó; el PR del panel (#7) quedó listo y se fusionó esa tarde. Empezó los datos del catálogo (KNOCK-23) y abrió el PR #18. |

## Erick Trejo

| Día | Qué hizo |
|---|---|
| **Mié 14** | Creó el proyecto Flutter base con Clean Architecture, Riverpod, go_router y las 5 pestañas. Redactó la spec de TT-MOB-01 y las pruebas (13, con 98,7 % de cobertura). |
| **Jue 15** | Alineó el identificador de la app con Firebase (`app.knock.mobile`), probó en el emulador de Android, grabó el video de TC-01 y abrió el PR #13. |
| **Pendiente** | Rebase del PR, firma de la spec por el revisor y revisión de otra persona. |

## Cifras de la semana

- Backend: 19 PR fusionados y 1 abierto (#18, Alexis). Frontend: 13 PR fusionados y 2 abiertos (#13, Erick; #14, mapa de código).
- El CI de cada PR nació el miércoles 14 y esa misma semana se corrigieron los fallos de su primera ejecución real.
- La API base (TT-API-01) quedó fusionada el jueves 15 por la noche, con 32 pruebas en verde y cobertura del código propio del 92 %.

## Lo que no se alcanzó

| Persona | Pendiente |
|---|---|
| Guillermo | KNOCK-21 (spike del token de Firebase: spec en revisión) y KNOCK-30 (pantalla Flutter con los oficios). |
| Alexis | Entregable B de KNOCK-22 (EF Core, migración, readiness y Testcontainers) y la mayor parte de KNOCK-23. |
| Erick | Cierre de KNOCK-24 (rebase y firma de la spec), KNOCK-25 y KNOCK-29. |
| José Julián | Inicio de sesión con número de prueba de Firebase (TC-01 de KNOCK-28), roles y alerta de correo de Azure (KNOCK-27), la demo de la semana (KNOCK-32) y el secreto de la revisión con IA. |

## Lo aprendido (para la retrospectiva)

- La spec va primero y la aprueba una persona: el código que un agente escribió antes de eso hubo que apartarlo y rehacerlo.
- El primer CI real destapó varios falsos positivos (Gitleaks, Semgrep, Dockle, Checkov) y un error del propio script del mapa de código; todos se resolvieron sin bajar umbrales y con excepciones registradas y con vencimiento.
- Un conflicto casi seguro en `Memoria.md` cada vez que dos PR se abren en paralelo: conviene fusionar en orden y rebasar con frecuencia.
- La guía `docs/FLUJO_EQUIPO.md` y las fichas en `docs/backlog/` salieron esta semana para que cada persona sepa por dónde empezar en el Sprint 1.
