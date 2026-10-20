# ADR-006 — Costos en Azure y ventana de evaluación

> **Estado:** decidida por el Tech Lead el 2026-10-20; pendiente de revisión en el PR que la incorpora.
> **Complementa:** la sección 10 de `docs/DECISION_TECNICA.md`.
> **Tarea:** TT-GES-06, TT-AZ-04.

## Contexto
Los lineamientos piden desplegar «configurado con costo cero» y **al menos 2 instancias** del backend. Con cuatro servicios y un gateway, dos réplicas fijas de cada uno son diez réplicas. Container Apps incluye por suscripción, cada mes, 180 000 vCPU-segundos, 360 000 GiB-segundos y 2 millones de solicitudes gratis ([Microsoft](https://learn.microsoft.com/en-us/azure/container-apps/billing)); con réplicas siempre encendidas se superan. La suscripción es **Azure for Students** (crédito de 100 USD, sin tarjeta).

## Decisión
1. **Escalar a cero** (o a una réplica) fuera de la ventana de evaluación.
2. **Ventana de evaluación:** del **25 de noviembre al 5 de diciembre de 2026** se fijan **2 réplicas mínimas por servicio** y se prueba el balanceo (TT-QA-02, k6).
3. **Presupuesto y alertas** en Azure desde el primer día (TT-AZ-01); el consumo se revisa cada semana (TT-AZ-04).
4. Lo que no tenga capa gratuita equivalente (por ejemplo Front Door con WAF o Azure Cache for Redis) **no se despliega** sin pasar por esta decisión **(confirmar precios)**. El balanceo y HTTPS los da Container Apps mediante su entrada integrada y el gateway.
5. Observabilidad con Application Insights, dentro de la capa gratuita.
6. Al terminar la evaluación se ejecuta `terraform destroy` (TT-AZ-03).

## Verificar en TT-AZ-04
- Que la suscripción de estudiante permita las regiones y servicios necesarios.
- Costo real de una réplica inactiva (tarifa de inactividad) y de DocumentDB gratuito.
- Estimación para diez réplicas en la ventana de evaluación frente al crédito disponible.

## Hallazgos de la revisión de la suscripción (2026-10-20, solo lectura con `az`)
- La suscripción es **Azure for Students** y está habilitada. Hay un presupuesto de 10 USD (`budget-knock-dev`).
- Una política obliga a desplegar **solo en cinco regiones**: Chile Central, Norway East, West US, France Central y North Central US. El grupo de recursos actual está en North Central US. **Terraform debe usar una de esas regiones.**
- En esas regiones el catálogo de Azure lista Container Apps, Key Vault, Storage, Application Insights y Log Analytics. **Azure DocumentDB (MongoDB)** figura en West US, North Central US, France Central y Norway East, pero **no en Chile Central** **(confirmar que el nivel gratuito se pueda crear en la región elegida: lo prueba TT-BD-05)**. Cognitive Services (moderación) figura en las mismas cuatro, menos Chile Central.
- Hay que **registrar los proveedores** de recursos antes de usarlos con Terraform: `Microsoft.App`, `Microsoft.DocumentDB`, `Microsoft.KeyVault`, `Microsoft.Storage`, `Microsoft.OperationalInsights`, `Microsoft.Insights`, `Microsoft.ManagedIdentity`, `Microsoft.Network`, `Microsoft.CognitiveServices`, `Microsoft.ContainerRegistry` y `Microsoft.Cdn` (si se usa CDN). Hoy ninguno está registrado.
- La suscripción tiene otros grupos de recursos que no son de Knock; el consumo de esos proyectos descuenta del mismo crédito.

