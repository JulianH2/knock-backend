# Evidencia — KNOCK-27 · TT-AZ-01 Cuenta de Azure, grupo de recursos, presupuesto y alertas

> Spec: `specs/TT-AZ-01-cuenta-de-azure-grupo-de-recursos-presup/spec.md` (Aprobada) · Fecha: 2026-10-13
> Los comandos los ejecutó el Tech Lead en su terminal; ningún agente operó Azure. Esta nota resume sus salidas.
> Se ocultaron el ID de suscripción, el tenant y el correo (`<id-de-suscripcion>`, `<correo-del-responsable>`).

## Resumen por caso
| Caso | Resultado | Evidencia |
|---|---|---|
| TC-TT-AZ-01-01 Presupuesto con alertas por correo | Hecho y verificado. No se comprobó que el correo llegue (Cost Management evalúa con retraso). | Sección 3 |
| TC-TT-AZ-01-02 Grupo de recursos con nombre y etiquetas, una región | Hecho y verificado | Sección 2 |
| TC-TT-AZ-01-03 Roles mínimos | **Pendiente**: faltan los correos de las personas y cómo entran al directorio | — |

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
- Roles mínimos (TC-03): asignar `Contributor` sobre `rg-knock-dev` a quien corresponda; propietario solo el Tech Lead. Si son cuentas externas, primero se invitan como invitadas al directorio.
- Comprobar que llega el correo de alerta cuando el gasto cruce un umbral.
