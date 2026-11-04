# Evidencia — KNOCK-34 · TT-AZ-02 Infraestructura como código (Terraform)

> Spec: `specs/TT-AZ-02-infraestructura-como-codigo-terraform/spec.md` (Aprobada). Los comandos de Azure y de Terraform los ejecuta el Tech Lead; ningún agente opera Azure.
> **Estado: pendiente de ejecutar.** Se completa después de seguir `deploy/ENTORNO.md`. Oculta el ID de suscripción, el tenant y los correos (`<id-de-suscripcion>`, `<id-del-tenant>`, `<correo-del-responsable>`).

## Resumen por caso

| Caso | Resultado | Evidencia |
|---|---|---|
| TC-TT-AZ-02-01 `fmt`, `init -backend=false` y `validate` | Verificado en el PR (imagen `hashicorp/terraform` 1.9.8 por digest) | Descripción del PR |
| TC-TT-AZ-02-02 El clúster solo está en `deploy/persistent/` | Verificado en el código; falta `plan -destroy` | Sección 2 |
| TC-TT-AZ-02-03 Validación de región en las dos raíces | Pendiente | Sección 3 |
| TC-TT-AZ-02-04 Solo la API (y el gateway) con ingreso externo; `allowInsecure = false` | Pendiente | Sección 4 |
| TC-TT-AZ-02-05 Réplicas con y sin la bandera de la ventana | Pendiente | Sección 5 |
| TC-TT-AZ-02-06 Redis: digest, TCP interno, secreto de Key Vault | Verificado en el código; falta `plan` | Sección 6 |
| TC-TT-AZ-02-07 Key Vault por roles y roles mínimos | Pendiente | Sección 7 |
| TC-TT-AZ-02-08 Blob: TLS 1.2, sin acceso público, contenedores privados | Pendiente | Sección 8 |
| TC-TT-AZ-02-09 Etiquetas | Pendiente | Sección 9 |
| TC-TT-AZ-02-10 Sin estado, `*.tfvars` ni secretos | Verificado en el PR; falta `gitleaks` del CI | Sección 10 |
| TC-TT-AZ-02-11 Checkov | Pendiente (cambio protegido de `ci-pr.yml`) | Sección 11 |
| TC-TT-AZ-02-12 `apply` y segundo `plan` sin cambios | Pendiente | Sección 12 |

## Hallazgos (diferencias con la spec, que no se cambia)

1. **El clúster gratuito ya existía.** `knock-docdb` (Free, West US, 32 GB, versión 8.0) ya estaba creado y la suscripción solo permite un clúster gratuito. `deploy/persistent/` lo **adopta** con un bloque `import` en lugar de crearlo (decisión 4).
2. **Una sola aplicación por ahora.** Por la evaluación del 3 y 4 de noviembre, `deploy/` despliega solo `ca-knock-api` con la imagen del monolito; el gateway y los tres servicios quedan detrás de `enable_other_services=false` (la decisión 5 pedía crear las cuatro desde ya) y Redis detrás de `enable_redis=false`.

## 2 a 12

```text
(pega aquí la salida de cada comando indicado en deploy/ENTORNO.md, sin identificadores)
```
