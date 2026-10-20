# ADR-003 — Infraestructura como código: Terraform

> **Estado:** decidida por el Tech Lead el 2026-10-20; pendiente de revisión en el PR que la incorpora.
> **Reemplaza:** el uso de Bicep en `deploy/` (sección 10 de `docs/DECISION_TECNICA.md`).
> **Tarea:** TT-GES-06 · Impacto de implementación: TT-AZ-02, TT-AZ-03, TT-AZ-05.

## Contexto
Los lineamientos (punto 13) recomiendan Terraform, y el punto 15 pide que la **creación y la eliminación** de la infraestructura y del despliegue sean un paso del pipeline. TT-AZ-02 (Bicep) todavía no empezó, así que el cambio ahora no tiene costo de rehacer.

## Decisión
**Terraform** con el proveedor `azurerm` (y `azapi` solo cuando falte un recurso). Un módulo por servicio y un módulo compartido para red, Key Vault, observabilidad y base de datos.

## Por qué
- Terraform guarda **estado** y tiene `plan`, `apply` y `destroy` como comandos de primera clase; `destroy` cubre el requisito de eliminar la infraestructura.
- Bicep es solo para Azure y no guarda estado ([comparación de Microsoft](https://learn.microsoft.com/fi-fi/azure/developer/terraform/comparing-terraform-and-bicep)).
- Si algún día hubiera que cambiar de nube, el flujo y la herramienta se conservan y solo cambian el proveedor y los recursos.

## Reglas
1. **Estado remoto** en una cuenta de almacenamiento de Azure con bloqueo; nunca en el repositorio (TT-AZ-05).
2. `terraform plan` corre en cada PR que toque `deploy/`; `apply` solo desde la rama de despliegue, con aprobación.
3. `terraform destroy` es un job manual del pipeline (TT-AZ-03), protegido contra producción accidental.
4. Checkov se mantiene sobre los archivos de Terraform (TT-CI-01); las excepciones siguen en `security/exceptions.yaml`.
5. Ninguna credencial en variables del repositorio: identidad federada de GitHub hacia Azure.

## Consecuencias
- Se descartan los archivos Bicep que se hubieran empezado; no hay trabajo perdido porque TT-AZ-02 no empezó.
- Dependabot agrega el ecosistema `terraform`.
- El costo de la infraestructura se controla con las reglas de ADR-006.
