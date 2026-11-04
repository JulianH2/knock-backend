# Evidencia — KNOCK-158 · TT-AZ-05 Estado remoto de Terraform

> Spec: `specs/TT-AZ-05-estado-remoto-de-terraform/spec.md` (Aprobada). Los comandos los ejecuta el Tech Lead; ningún agente opera Azure.
> **Estado: pendiente de ejecutar.** Se completa después de seguir `deploy/README.md`, sección 1. Oculta el ID de suscripción, el tenant y los correos (`<id-de-suscripcion>`, `<id-del-tenant>`, `<correo-del-responsable>`).

## Resumen por caso

| Caso | Resultado | Evidencia |
|---|---|---|
| TC-TT-AZ-05-01 `terraform init` configura el backend remoto, sin `terraform.tfstate` en la raíz | Pendiente | Sección 1 |
| TC-TT-AZ-05-02 Nada de estado ni `*.tfvars` versionado; `.terraform.lock.hcl` no ignorado | Verificado en el PR (reglas de `.gitignore`); falta `git ls-files` tras el primer `init` | Sección 2 |
| TC-TT-AZ-05-03 Segundo acceso falla con «state blob is already locked» | Pendiente | Sección 3 |
| TC-TT-AZ-05-04 Cuenta con TLS 1.2, sin acceso público, versionado y sin llaves; rol en el contenedor | Pendiente | Sección 4 |
| TC-TT-AZ-05-05 Sin llaves, SAS ni cadenas de conexión; sin `subscription_id` ni `tenant_id` | Verificado en el diff; falta `gitleaks` del CI | Sección 5 |

## 1. Backend remoto (TC-01)
```text
(pega aquí la salida de terraform init y de ls, sin identificadores)
```

## 2. Archivos versionados (TC-02)
```text
(pega aquí git ls-files filtrado y git check-ignore deploy/.terraform.lock.hcl)
```

## 3. Bloqueo (TC-03)
```text
(pega aquí el error «state blob is already locked» de la segunda terminal)
```

## 4. Cuenta del estado (TC-04)
```text
(pega aquí las salidas de az storage account show, blob-service-properties show y az role assignment list, con los principales ocultos)
```

## 5. Secretos (TC-05)
`gitleaks` del CI limpio y revisión del diff: el bloque `backend` solo trae el grupo de recursos, el contenedor, el archivo de estado y `use_azuread_auth`.
