# plan.md — TT-AZ-05 — Estado remoto de Terraform (almacenamiento de Azure con bloqueo)

## 1. Mapeo de archivos
- **Reutilizar:** `docs/decisiones/ADR-003-terraform.md` (reglas 1 y 2), `docs/evidencia/KNOCK-27-azure.md` (grupo de recursos y restricciones de regiones) y el patrón de evidencia de TT-AZ-01.
- **Archivos de la tarea:**
  - `deploy/backend.tf` y `deploy/persistent/backend.tf` (nuevos)
  - `deploy/README.md` (nuevo)
  - `.gitignore` (reglas de estado, `.terraform/` y `*.tfvars`)
  - `docs/evidencia/KNOCK-158-estado-remoto.md` (nuevo)
  - `Memoria.md`
- **No tocar:** `.github/` ni `scripts/`. Los módulos de Terraform son de TT-AZ-02.

## 2. Riesgos y efectos secundarios
- Los comandos de Azure los ejecuta una persona; un agente no opera Azure.
- No subir secretos, ID de suscripción, tenant ni correos: la evidencia los oculta.
- El estado no debe quedar nunca en el repositorio (público): revisar `git status` antes de cada commit.
- Si `terraform init` se corre antes de crear la cuenta, falla; el orden de la secuencia importa.

## 3. Pruebas por caso
| Caso | Evidencia | Tipo |
|---|---|---|
| TC-TT-AZ-05-01 | Salida de `terraform init` y `ls` sin `terraform.tfstate` | comando |
| TC-TT-AZ-05-02 | `git ls-files` filtrado y reglas de `.gitignore` | comando |
| TC-TT-AZ-05-03 | Captura de dos terminales con el bloqueo | manual |
| TC-TT-AZ-05-04 | `az storage account show` y `blob-service-properties show` con `--query` | comando |
| TC-TT-AZ-05-05 | `gitleaks` limpio y revisión del diff | CI / manual |

## 4. Secuencia
1. Confirmar con el Tech Lead los nombres, la región y la autenticación (§8 de la spec).
2. El Tech Lead crea el grupo de recursos, la cuenta (TLS 1.2, sin acceso público, sin llaves compartidas, versionado) y el contenedor con los comandos del README, y se asigna el rol de datos de blobs.
3. Escribir `backend.tf`, el README y las reglas de `.gitignore`.
4. `terraform init` y la prueba de bloqueo con dos terminales.
5. Adjuntar la evidencia sin identificadores, actualizar `Memoria.md` y abrir el PR.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
git ls-files | grep -E "\.tfstate|\.tfvars|\.terraform/" || echo "sin archivos de estado versionados"
```
