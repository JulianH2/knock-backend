# Evidencia — KNOCK-27 · TT-AZ-01 Cuenta de Azure, grupo de recursos, presupuesto y alertas

> Spec: `specs/TT-AZ-01-cuenta-de-azure-grupo-de-recursos-presup/spec.md` (Aprobada) · Fecha: 2026-10-13
> Los comandos los ejecutó el Tech Lead en su terminal; ningún agente operó Azure. Esta nota resume sus salidas.
> Se ocultaron el ID de suscripción, el tenant y el correo (`<id-de-suscripcion>`, `<correo-del-responsable>`).

## Resumen por caso
| Caso | Resultado | Evidencia |
|---|---|---|
| TC-TT-AZ-01-01 Presupuesto con alertas por correo | Hecho y verificado. No se comprobó que el correo llegue (Cost Management evalúa con retraso). | Sección 3 |
| TC-TT-AZ-01-02 Grupo de recursos con nombre y etiquetas, una región | Hecho y verificado | Sección 2 |
| TC-TT-AZ-01-03 Roles mínimos | **Hecho (2026-10-26)**: el Tech Lead es el único propietario y nadie más tiene acceso a Azure; el acceso del equipo llega con TT-AZ-02 | Sección 7 |

## 1. Suscripción y restricciones
- Suscripción: **Azure for Students** (`Enabled`), ID `<id-de-suscripcion>`.
- Política de Azure «Allowed resource deployment regions»: solo se puede desplegar en `chilecentral`, `norwayeast`, `westus`, `francecentral` y `northcentralus`. **No** están permitidas `eastus2` ni `mexicocentral`.
- Consecuencia: todos los datos quedarían fuera de México. Hay que reflejarlo en el aviso de privacidad (pregunta 6 para el abogado en `SEGURIDAD_Y_PRIVACIDAD.md` de la maqueta).

## 2. Grupo de recursos (TC-02)
```powershell
az group create --name rg-knock-dev --location northcentralus --tags proyecto=knock entorno=dev responsable=julian
az group show --name rg-knock-dev --query "{nombre:name, region:location, estado:properties.provisioningState}" -o json
```
Resultado: `nombre: rg-knock-dev`, `region: northcentralus`, `estado: Succeeded`; etiquetas `proyecto=knock`, `entorno=dev`, `responsable=julian`.

## 3. Presupuesto y alertas (TC-01)
Presupuesto `budget-knock-dev` creado sobre el grupo de recursos con `az rest --method put` (API `Microsoft.Consumption/budgets`, versión `2023-05-01`) y verificado con `az rest --method get`:

| Campo | Valor |
|---|---|
| Monto | **10 USD** al mes (se subirá cuando haya infraestructura real) |
| Periodo | 2026-10-01 a 2027-10-01 |
| Gasto actual | 0.0 USD |
| Alertas sobre lo gastado | 50 %, 80 % y 100 % |
| Alerta sobre lo previsto | 100 % |
| Destinatarios | `<correo-del-responsable>` |

Cost Management sí funciona en la suscripción Azure for Students.

## 4. Disponibilidad de servicios en la región elegida
- `Microsoft.App` (Container Apps), `Microsoft.DBforPostgreSQL` (Flexible) y `Microsoft.Cache` (Redis) listan **North Central US** entre sus regiones. Aparecer en la lista no garantiza que la suscripción de estudiante pueda desplegarlos; se prueba en KNOCK-34.
- PostgreSQL Flexible en `northcentralus`: ediciones `Burstable` (`Standard_B1ms` hasta `Standard_B20ms`), `GeneralPurpose` y `MemoryOptimized`, sin restricciones en los tamaños; solo `UltraDisk` no está soportado en la región.
- Para desarrollo se usará **Burstable B1ms** (el más económico); los tamaños General Purpose cuestan mucho más que el presupuesto.

## 5. Decisiones y riesgos
- **Región `northcentralus`** (de las cinco permitidas, está en los tres servicios). Alternativa si PostgreSQL falla al desplegar: `westus`.
- **Presupuesto USD 10**; el crédito de estudiante es el tope real y el presupuesto es un aviso temprano.
- Riesgo: datos fuera de México (ver sección 1). Para usar Mexico Central haría falta otra suscripción o un tenant sin esa política.
- Riesgo: restricciones por servicio en suscripciones de estudiante (por ejemplo, regiones restringidas en PostgreSQL); se confirma al crear los recursos en KNOCK-34.

## 6. Pendiente
- ~~Roles mínimos (TC-03)~~: resuelto (sección 7).
- Comprobar que llega el correo de alerta **cuando el gasto cruce un umbral**: con 0.0 USD de gasto no se puede provocar sin generar costo, así que queda como verificación de la ventana de evaluación (TT-AZ-04 y TT-AZ-03). No bloquea el cierre de esta tarea porque el presupuesto y sus cuatro alertas están creados y habilitados (sección 8).

## 7. Roles mínimos (TC-TT-AZ-01-03) — 2026-10-26
Decisión (Tech Lead): hoy **solo el Tech Lead tiene acceso a Azure**. El equipo no necesita roles de Azure para trabajar: el despliegue sale de GitHub Actions con una identidad federada (TT-AZ-02) y Alexis llega a la base de datos por la regla de firewall de su IP, no por un rol de Azure. Dar roles ahora agregaría acceso que nadie usa. Cuando TT-AZ-02 cree la identidad del pipeline, se le asignará `Contributor` solo sobre `rg-knock-dev` y se agregará aquí.

Comprobación con la CLI, solo lectura (los datos se ocultaron: `<id-de-suscripcion>`):
```text
az role assignment list --resource-group rg-knock-dev      → []   (sin asignaciones propias en el grupo de recursos)
az role assignment list --all                              → Owner · User · /subscriptions/<id>     (el Tech Lead, único propietario)
                                                             otras asignaciones: solo de un grupo de recursos ajeno a Knock
```
Captura de **Access control (IAM)** de `rg-knock-dev` (con el correo y el ID de suscripción tapados): se adjunta en el PR.

## 8. Alertas de presupuesto — 2026-10-26
`az consumption budget show --budget-name budget-knock-dev --resource-group rg-knock-dev` (solo lectura): monto 10 USD al mes, **gasto 0.0 USD** y **cuatro notificaciones habilitadas** (umbrales 50 %, 80 % y 100 % sobre lo gastado y 100 % sobre lo previsto). El correo no se ha comprobado porque ningún umbral se ha cruzado.
