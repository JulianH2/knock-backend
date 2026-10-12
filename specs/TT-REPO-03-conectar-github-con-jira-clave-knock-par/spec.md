# TT-REPO-03 — Conectar GitHub con Jira (clave KNOCK) para ligar ramas, commits y PR a cada issue

> **Jira:** KNOCK-17 · **Área:** Repositorio · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 1
> **Repositorio:** knock-backend · **Ficha:** TT-REPO-03_ConectarGithubJiraLigar_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Conectar GitHub con Jira (espacio `KNOCK`) para que las ramas, los commits y los PR queden ligados a su issue por la clave `KNOCK-N`.

## 2. Historia y casos de prueba
**TT-REPO-03** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-REPO-03-01 | Dado un PR cuya rama, commits y título llevan la clave `KNOCK-N`, entonces el PR aparece dentro de su issue de Jira. | Captura del panel «Desarrollo» del issue en Jira (p. ej. el PR de KNOCK-16) |
| TC-TT-REPO-03-02 | Dado que los dos repositorios están conectados al mismo sitio de Jira, cuando se hace un commit con `(KNOCK-N)`, entonces el commit aparece en el issue. | Jira → Configuración de aplicaciones → GitHub (ambos repos) y un commit visible en un issue |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Instalar y configurar la integración «GitHub for Jira» para `JulianH2/knock-backend` y `JulianH2/knock-frontend`.
  - Verificar con un PR real que se liga a su issue.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Automatizaciones que muevan issues según el estado del PR.
  - Reglas de nombres (ya en TT-STD-01).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| La integración tiene más acceso del necesario | Autorizarla solo sobre los dos repositorios, con permisos mínimos | TC-TT-REPO-03-02 |
| Tokens de la integración en el repositorio | Se gestionan en Jira y GitHub, nunca en el código | Revisión de diff (gitleaks en KNOCK-26) |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- Estado actual de la conexión: no verificado.
- Se necesita permiso de administración del sitio de Jira y de la cuenta de GitHub.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
