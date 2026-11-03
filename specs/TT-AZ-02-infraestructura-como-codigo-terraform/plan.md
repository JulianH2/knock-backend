# plan.md — TT-AZ-02 — Infraestructura como código (Terraform)

## 1. Mapeo de archivos
- **Reutilizar:** ADR-001, ADR-002, ADR-003, ADR-006; `docs/evidencia/KNOCK-27-azure.md` (grupo de recursos, regiones permitidas y presupuesto); la raíz y el estado remoto de TT-AZ-05.
- **Archivos de la tarea:**
  - `deploy/main.tf`, `deploy/variables.tf`, `deploy/outputs.tf` y `deploy/modules/<servicio>/` más un módulo compartido (nuevos)
  - `deploy/persistent/main.tf`, `variables.tf` y `outputs.tf` (nuevos)
  - `deploy/README.md` (con los comandos por raíz)
  - `docs/evidencia/KNOCK-34-terraform.md` (nuevo)
  - `.github/workflows/ci-pr.yml` (Checkov de Bicep a Terraform; protegido, lo hace el Tech Lead)
  - `Memoria.md`
- **No tocar:** `scripts/`, `.agents/` ni el código de los servicios. `docker-compose.local.yml` es de TT-BD-07.

## 2. Riesgos y efectos secundarios
- Los comandos de Azure y de Terraform los ejecuta una persona; un agente no opera Azure.
- El estado debe ser remoto desde el primer `init` (TT-AZ-05): sin eso, no se aplica.
- No subir secretos, ID de suscripción, tenant ni correos; la evidencia los oculta.
- Un `apply` crea recursos que consumen crédito: revisar el `plan` y el costo antes de aplicar.
- Container Apps y Key Vault son recursos con nombre único: el nombre se confirma al crear.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-AZ-02-01 | `terraform fmt -check`, `init -backend=false` y `validate` | comando / CI |
| TC-TT-AZ-02-02 | `plan -destroy` en `deploy/` sin el clúster | comando |
| TC-TT-AZ-02-03 | `plan -var location=eastus` falla | comando |
| TC-TT-AZ-02-04 | `plan` y `az containerapp list` | comando |
| TC-TT-AZ-02-05 | `plan` con y sin la bandera de la ventana | comando |
| TC-TT-AZ-02-06 | `plan` y revisión de archivos | comando / manual |
| TC-TT-AZ-02-07 | `plan`, `az role assignment list` y `gitleaks` | comando / CI |
| TC-TT-AZ-02-08 | `az storage account show` | comando |
| TC-TT-AZ-02-09 | `plan` | comando |
| TC-TT-AZ-02-10 | `git ls-files` filtrado y `gitleaks` | comando / CI |
| TC-TT-AZ-02-11 | Checkov en el CI y `check_exceptions.py` | CI |
| TC-TT-AZ-02-12 | `apply` y segundo `plan` sin cambios | manual |

## 4. Secuencia
1. Esperar TT-AZ-05 (el backend remoto) y las decisiones de §8 de la spec.
2. Escribir `deploy/persistent/` y aplicarlo (clúster de DocumentDB).
3. Escribir el módulo compartido (Log Analytics, entorno de Container Apps, Key Vault, Blob, identidades) y un módulo por servicio y para Redis.
4. `fmt`, `validate` y `plan`; revisar el costo.
5. `apply` por el Tech Lead, segundo `plan` sin cambios y evidencia sin identificadores.
6. Pasar Checkov a Terraform en el CI, actualizar `Memoria.md` y abrir el PR.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
terraform -chdir=deploy fmt -check -recursive && terraform -chdir=deploy init -backend=false && terraform -chdir=deploy validate
terraform -chdir=deploy/persistent init -backend=false && terraform -chdir=deploy/persistent validate
```
