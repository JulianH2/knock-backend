# ADR-006 — Costos en Azure y ventana de evaluación

> **Estado:** decidida por el Tech Lead el 2026-10-20.
> **Complementa:** la sección 10 de `docs/DECISION_TECNICA.md`.
> **Tarea:** TT-GES-06, TT-AZ-04.

## Contexto
Los lineamientos piden desplegar «configurado con costo cero» y **al menos 2 instancias** del backend. Con tres servicios y un gateway, dos réplicas fijas de cada uno son ocho réplicas (más Redis; antes de pasar el Asistente a módulo de Mercado eran diez). Container Apps incluye por suscripción, cada mes, 180 000 vCPU-segundos, 360 000 GiB-segundos y 2 millones de solicitudes gratis ([Microsoft](https://learn.microsoft.com/en-us/azure/container-apps/billing)); con réplicas siempre encendidas se superan. La suscripción es **Azure for Students** (crédito de 100 USD, sin tarjeta).

## Decisión
1. **Escalar a cero** (o a una réplica) fuera de la ventana de evaluación. **«Costo cero» del lineamiento y «gasto del crédito de estudiante» no son lo mismo:** lo que sobrepase la capa gratuita se descuenta del crédito, y las alertas de presupuesto **solo avisan, no frenan**.
2. **Ventana de evaluación:** del **25 de noviembre al 5 de diciembre de 2026** se fijan **2 réplicas mínimas por servicio** y se prueba el balanceo (TT-QA-02, k6).
3. **Presupuesto y alertas** en Azure desde el primer día (TT-AZ-01); el consumo se revisa cada semana (TT-AZ-04).
4. Lo que no tenga capa gratuita equivalente (por ejemplo Front Door con WAF o Azure Cache for Redis) **no se despliega** sin pasar por esta decisión **(confirmar precios)**. El balanceo y HTTPS los da Container Apps mediante su entrada integrada y el gateway.
5. Observabilidad con Application Insights, dentro de la capa gratuita. Jira TT-CD-02 todavía dice «Front Door + WAF»; se corrige para que coincida con esta decisión.
6. Al terminar la evaluación se ejecuta `terraform destroy` (TT-AZ-03†).

## Verificar en TT-AZ-04
- Que la suscripción de estudiante permita las regiones y servicios necesarios.
- Costo real de una réplica inactiva (tarifa de inactividad) y de DocumentDB gratuito.
- Estimación para ocho réplicas en la ventana de evaluación frente al presupuesto de 10 USD y al crédito disponible (hay otros proyectos que descuentan del mismo crédito), **con tarifas citadas**. Si no cabe: 2 réplicas solo en los servicios críticos, o subir el presupuesto y automatizar la reducción a cero al llegar a un umbral.
- **Resultado de la estimación (2026-10-24):** ver `docs/evidencia/KNOCK-145-costo-cero.md`. Con 9 réplicas, la ventana del 25-nov al 5-dic cuesta de 16 a 61 USD (réplicas de 0.25 vCPU y 0.5 GiB) y la ventana corta del 29-nov al 3-dic, de 5.5 a 9.6 USD; todo se paga con el crédito de estudiante. La decisión sobre la ventana y el presupuesto sigue siendo del líder; este ADR no cambia hasta que decida.

## Hallazgos de la revisión de la suscripción (2026-10-20, solo lectura con `az`)
- La suscripción es **Azure for Students** y está habilitada. Hay un presupuesto de 10 USD (`budget-knock-dev`).
- Una política obliga a desplegar **solo en cinco regiones**: Chile Central, Norway East, West US, France Central y North Central US. El grupo de recursos actual está en North Central US. **Terraform debe usar una de esas regiones.**
- En esas regiones el catálogo de Azure lista Container Apps, Key Vault, Storage, Application Insights y Log Analytics. **Azure DocumentDB (MongoDB)**: el nivel gratuito solo está disponible, entre las cinco regiones permitidas, en **West US, France Central y Norway East** ([Microsoft](https://learn.microsoft.com/en-us/azure/documentdb/free-tier)); **no** en North Central US (donde está hoy el grupo de recursos) ni en Chile Central. **Región candidata del clúster: West US** (la más cercana a México de las tres); el grupo de recursos puede quedarse en North Central US, pero eso añade latencia entre servicios y base, y TT-BD-05 lo mide. El nivel gratuito no tiene alta disponibilidad ni respaldo, y se pausa tras 60 días de inactividad. Cognitive Services (moderación) figura en West US, France Central, North Central US y Norway East.
- Hay que **registrar los proveedores** de recursos antes de usarlos con Terraform: `Microsoft.App`, `Microsoft.DocumentDB`, `Microsoft.KeyVault`, `Microsoft.Storage`, `Microsoft.OperationalInsights`, `Microsoft.Insights`, `Microsoft.ManagedIdentity`, `Microsoft.Network`, `Microsoft.CognitiveServices`, `Microsoft.ContainerRegistry` y `Microsoft.Cdn` (si se usa CDN). El 2026-10-20 se registraron todos con `az provider register`, salvo `Microsoft.Cdn`, que se registra cuando se decida usar CDN.
- La suscripción tiene otros grupos de recursos que no son de Knock; el consumo de esos proyectos descuenta del mismo crédito.

