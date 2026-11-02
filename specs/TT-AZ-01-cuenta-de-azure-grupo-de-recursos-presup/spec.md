# TT-AZ-01 — Cuenta de Azure, grupo de recursos, presupuesto y alertas de costo

> **Jira:** KNOCK-27 · **Área:** Azure · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 1
> **Repositorio:** knock-backend · **Ficha:** TT-AZ-01_CuentaAzureGrupoRecursos_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Firmada

## 1. Objetivo
Crear la cuenta de Azure del proyecto con un grupo de recursos, un presupuesto y alertas de costo, antes de provisionar infraestructura.

## 2. Historia y casos de prueba
**TT-AZ-01** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-AZ-01-01 | Dado el presupuesto del grupo de recursos, cuando el gasto alcance los umbrales definidos, entonces llega una alerta por correo. | Captura de Cost Management → Budgets con sus umbrales y destinatarios |
| TC-TT-AZ-01-02 | Dado el proyecto, entonces existe un grupo de recursos con nombre y etiquetas definidos, en una sola región. | Captura del grupo de recursos o `az group show` (lo ejecuta una persona; los agentes no operan Azure) |
| TC-TT-AZ-01-03 | Dado el acceso a la suscripción, entonces cada persona tiene el rol mínimo necesario (propietario solo el Tech Lead). | Captura de Access control (IAM) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Cuenta o suscripción de Azure, grupo de recursos, presupuesto y alertas de costo.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Infraestructura como código en Bicep (TT-AZ-02).
  - Key Vault y secretos (TT-SEC-01).
  - Despliegue (TT-CD-01 y TT-CD-02).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Gasto inesperado por recursos olvidados | Presupuesto con alertas a varios umbrales | TC-TT-AZ-01-01 |
| Acceso excesivo a la suscripción | Roles mínimos; propietario solo el Tech Lead; sin credenciales en el repositorio | TC-TT-AZ-01-03 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- Monto del presupuesto, umbrales (por ejemplo 50, 80 y 100 %) y correos que reciben la alerta.
- Región del grupo de recursos y tipo de suscripción.
- Un agente no opera Azure: la ejecuta una persona.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
