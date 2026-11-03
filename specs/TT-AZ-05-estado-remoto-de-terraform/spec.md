# TT-AZ-05 — Estado remoto de Terraform (almacenamiento de Azure con bloqueo)

> **Jira:** KNOCK-158 · **Área:** Azure · **Épica:** EP-13 · **Responsable:** José Julián Hernández Hernández · **Sprint:** S2 · **Puntos:** 1 · **Prioridad:** Técnica
> **Repositorio:** knock-backend · **Ficha:** TT-AZ-05 (Jira KNOCK-158; no existe `docs/backlog/TT-AZ-05.md`) · **Rama:** `chore/KNOCK-158-tt-az-05` → PR a `sprint-02`
> **Depende de:** TT-AZ-01 (KNOCK-27, firmada: suscripción, grupo de recursos y presupuesto). **Habilita:** TT-AZ-02 (KNOCK-34), TT-AZ-03 (KNOCK-165) y TT-CD-01 (KNOCK-35).
> **Fuentes:** ADR-003 (reglas 1, 2 y 5) · ADR-006 («Hallazgos de la revisión de la suscripción») · constitución 2.2.x (§6 «Secretos», §7 «Calidad») · `docs/PLAN_ADAPTACION.md` (A3, TT-AZ-05) · `docs/evidencia/KNOCK-27-azure.md`
> **Skills:** story-workflow, security-core, testing-strategy-backend
> **Estado:** Borrador

## 1. Objetivo
Que Terraform guarde su estado en una cuenta de almacenamiento de Azure con bloqueo, y nunca en el repositorio (ADR-003, regla 1), antes de que TT-AZ-02 escriba los módulos. Sin esto, dos personas o un pipeline pueden aplicar a la vez y pisarse, o un archivo de estado con datos de la infraestructura terminaría en un repositorio público.

## 2. Historia y casos de prueba
**TT-AZ-05** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura) y, donde se puede, con una comprobación de archivos del repositorio. Los comandos de Azure los ejecuta una persona: los agentes no operan Azure.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-AZ-05-01 | Dado el bloque `backend "azurerm"` de `deploy/terraform/`, cuando se ejecuta `terraform init`, entonces Terraform configura el backend remoto y no crea ningún `terraform.tfstate` local. | Salida de `terraform init` («Successfully configured the backend "azurerm"») y `ls` sin archivos de estado; la ejecuta una persona |
| TC-TT-AZ-05-02 | Dado el repositorio, entonces ningún archivo `*.tfstate`, `*.tfstate.*`, `*.tfvars` ni carpeta `.terraform/` está versionado y `.gitignore` los excluye. | `git ls-files` filtrado (sin resultados) y las reglas en `.gitignore`; el repositorio es público |
| TC-TT-AZ-05-03 | Dado un `terraform plan` que mantiene el bloqueo, cuando una segunda ejecución intenta tomar el estado, entonces falla con «state blob is already locked» y no modifica nada. | Captura de las dos terminales; la ejecuta una persona |
| TC-TT-AZ-05-04 | Dada la cuenta de almacenamiento del estado, entonces exige TLS 1.2 como mínimo, no permite acceso público a blobs, tiene el versionado de blobs activado y no permite llaves compartidas (solo identidad de Entra ID). | `az storage account show` y `az storage account blob-service-properties show` con `--query` de esos campos; la ejecuta una persona |
| TC-TT-AZ-05-05 | Dado el repositorio, entonces el bloque `backend` y la documentación no contienen llaves de acceso, tokens SAS ni cadenas de conexión, solo nombres (cuenta, contenedor, archivo de estado). | `gitleaks` limpio en el CI y revisión del diff |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `deploy/terraform/backend.tf` con el bloque `backend "azurerm"` (solo nombres, autenticación con Entra ID).
  - `deploy/terraform/README.md` con los comandos únicos de arranque (grupo de recursos aparte, cuenta de almacenamiento y contenedor) y cómo verificar cada caso.
  - Reglas de `.gitignore` para estado, `.terraform/` y `*.tfvars`.
  - Evidencia en `docs/evidencia/KNOCK-158-estado-remoto.md`, con el ID de suscripción, el tenant y los correos ocultos.
  - La misma tarea no se cubre en `knock-frontend`: no hay infraestructura allá.
- **Out:**
  - Los módulos de Terraform (TT-AZ-02).
  - El job `terraform destroy` (TT-AZ-03).
  - La identidad federada de GitHub hacia Azure y los permisos del pipeline (ADR-003, regla 5; se verifica en TT-AZ-04 y se usa en TT-CD-01). Hasta entonces el estado lo usa solo el Tech Lead desde su terminal.
  - Respaldo del estado más allá del versionado de blobs.

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin colecciones)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| El estado de Terraform, que puede contener datos de recursos y valores sensibles, termina en un repositorio público | Estado solo remoto; `.gitignore` de estado, `.terraform/` y `*.tfvars`; comprobación de que no hay archivos versionados | TC-TT-AZ-05-01, TC-TT-AZ-05-02 |
| Dos aplicaciones simultáneas corrompen el estado | Bloqueo nativo del backend `azurerm` (arrendamiento del blob) | TC-TT-AZ-05-03 |
| Lectura o borrado del estado por terceros | Sin acceso público a blobs, TLS 1.2, sin llaves compartidas y rol «Storage Blob Data Contributor» solo para quien aplica | TC-TT-AZ-05-04 |
| Pérdida o corrupción del estado | Versionado de blobs para recuperar una versión anterior; la cuenta vive en un grupo de recursos distinto del que borra `terraform destroy` | TC-TT-AZ-05-04 |
| Secretos en el bloque `backend` o en la documentación | Solo nombres; autenticación con Entra ID; `gitleaks` en el CI | TC-TT-AZ-05-05 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración de infraestructura sin código de producto. Cada caso se verifica con la evidencia indicada, que se adjunta en el PR. Checkov corre sobre los `.tf` en el CI (TT-CI-01).
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `gitleaks` limpio, `Memoria.md` al día y «Uso de IA» completo en el PR.
**Límites y decisiones** (propuestas confirmadas por el Tech Lead el 2026-11-03):
1. **Nombres.** La cuenta de almacenamiento necesita un nombre único en todo Azure (3 a 24 letras minúsculas y números). **Resuelto (líder, 2026-11-03):** grupo de recursos `rg-knock-tfstate` y cuenta `stknocktfstate` más un sufijo corto; el nombre definitivo se confirma al crearla.
2. **Grupo de recursos aparte.** **Resuelto (líder, 2026-11-03):** la cuenta vive en `rg-knock-tfstate`, no en `rg-knock-dev`, para que `terraform destroy` no se lleve el estado.
3. **Región.** Debe ser una de las cinco que permite la política de la suscripción (ADR-006). **Resuelto (líder, 2026-11-03):** la misma del grupo de recursos actual, North Central US.
4. **Autenticación.** **Resuelto (líder, 2026-11-03):** Entra ID con `use_azuread_auth = true`, rol «Storage Blob Data Contributor» para el Tech Lead y llaves compartidas desactivadas; el pipeline lo agrega TT-CD-01.
5. **Cuántos estados.** **Resuelto (líder, 2026-11-03):** un blob por entorno, hoy solo `dev` (`knock-dev.tfstate`), con los módulos por servicio dentro del mismo estado; TT-AZ-02 puede dividirlo.
6. **Costo.** Almacenamiento LRS estándar con un archivo pequeño; se confirma con tarifas citadas al ejecutar el arranque. Cuenta contra el crédito de estudiante, no es gratuito.
7. Un agente no opera Azure: el Tech Lead ejecuta los comandos y comparte la evidencia sin identificadores.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
