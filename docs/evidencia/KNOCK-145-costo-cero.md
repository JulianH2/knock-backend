# Evidencia — KNOCK-145 · TT-AZ-04 Revisión de costo cero: estimación de la ventana de evaluación

> Tarea de gestión: se resuelve con evidencia, sin spec propia. Fecha: 2026-10-24 · Solo lectura con `az` y consulta de tarifas públicas; no se creó ni cambió ningún recurso.
> Se ocultaron el ID de suscripción, el tenant y los nombres de grupos de recursos ajenos a Knock.

## Conclusión
1. **«Costo cero» no es «sin gasto»:** las dos réplicas fijas de la ventana **sí cuestan**, y se pagan con el crédito de Azure for Students (100 USD, compartido con otros proyectos de la suscripción), no con el nivel gratuito. Con 3 servicios, el gateway y Redis (9 réplicas) del 25-nov al 5-dic (11 días), la estimación va de **16 a 61 USD** con réplicas de 0.25 vCPU y 0.5 GiB (escenario S), de **20 a 75 USD** con Mercado de 0.5 vCPU y 1 GiB (escenario M) y de 35 a 125 USD si todo fuera de 0.5 vCPU y 1 GiB (escenario L), según cuánto trabajen las réplicas (de «siempre inactivas» a «siempre activas»).
2. **Una ventana más corta cabe en el presupuesto de 10 USD:** del **29-nov al 3-dic (5 días)** el escenario S cuesta de **5.5 a 9.6 USD** (inactivo o 20 % activo); el M, de 7.5 a 12.4 USD. Es la opción que recomiendo, porque la evaluación es el 2-dic y la regla del curso pide dos instancias, no once días.
3. **Fuera de la ventana todo se escala a cero** (0 USD de cómputo). Mantener una réplica mínima por app todo el mes costaría de 27 a 41 USD al mes, mucho más que la cuota gratuita; por eso el ADR-006 exige escalar a cero.
4. **Las alertas de presupuesto solo avisan, no frenan el gasto.** El presupuesto actual (`budget-knock-dev`, 10 USD al mes, gasto de 0.0 USD) tendría que subirse antes de la ventana, y la reducción a cero debe ser un paso manual comprobado (TT-AZ-02 y TT-AZ-03).

Decisiones que quedan para el líder (no se cambió ningún ADR): (a) ventana corta del 29-nov al 3-dic o la completa; (b) subir el presupuesto del mes de la ventana a unos 40 USD; (c) tamaños de réplica, que hoy no existen (TT-AZ-02).

## Fuentes (consultadas el 2026-10-24)
- Tarifas de Container Apps, plan Consumption, West US, en USD: API pública de precios de Azure (`prices.azure.com/api/retail/prices`, con el filtro `serviceName eq 'Azure Container Apps'`, `armRegionName eq 'westus'` y `priceType eq 'Consumption'`).
- Reglas de facturación y cuotas gratuitas: [Billing in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/billing) (Microsoft Learn, actualizada el 2026-03-25).
- Almacenamiento Blob (Hot LRS, West US) y Log Analytics: la misma API de precios, filtrando por `serviceName` igual a `Storage` y a `Log Analytics`.
- Azure DocumentDB, nivel gratuito: [Free tier](https://learn.microsoft.com/en-us/azure/documentdb/free-tier) (citado en el ADR-002).

## Tarifas usadas (West US, plan Consumption)
| Concepto | Tarifa | Fuente |
|---|---|---|
| vCPU activo | 0.000024 USD por segundo | API de precios («Standard vCPU Active Usage») |
| vCPU inactivo | 0.000003 USD por segundo | API de precios («Standard vCPU Idle Usage») |
| Memoria (activa e inactiva) | 0.000003 USD por GiB-segundo | API de precios («Standard Memory Active/Idle Usage») |
| Solicitudes | 0.40 USD por millón | API de precios («Standard Requests») |
| Cuota gratuita por mes calendario y suscripción | 180 000 vCPU-s, 360 000 GiB-s y 2 millones de solicitudes | Microsoft Learn (billing) |
| Blob Hot LRS | 0.0208 USD por GB al mes | API de precios |
| Log Analytics (ingesta) | gratis hasta 5 GB; después 2.99 USD por GB | API de precios |
| Azure DocumentDB, nivel gratuito | 0 USD | Microsoft Learn (free tier) |

Reglas relevantes de la documentación de Microsoft: una réplica paga la tarifa **inactiva** solo si la revisión tiene mínimo de réplicas mayor que cero, está en ese mínimo y la réplica no procesa solicitudes, usa menos de 0.01 vCPU y recibe menos de 1 000 bytes por segundo; en cualquier otro caso paga la tarifa activa. Solo se cobran las solicitudes que vienen de **fuera** del entorno (las llamadas entre servicios no cuentan) y los sondeos de salud no se cobran. Una revisión escalada a cero réplicas no genera cargos de consumo.

## Supuestos
- **Réplicas:** 3 servicios (Identidad y confianza, Mensajería y notificaciones, Mercado) y el gateway con 2 réplicas fijas cada uno (8), más Redis con 1: **9 réplicas**.
- **Tamaños:** todavía no están definidos (TT-AZ-02), así que se usan tres escenarios: **S** = 0.25 vCPU y 0.5 GiB en todo; **M** = Mercado con 0.5 vCPU y 1 GiB (procesa imágenes, ver `MediaRules`) y lo demás en S; **L** = todo con 0.5 vCPU y 1 GiB.
- **Actividad:** «inactivo» (sin tráfico), «20 % activo» y «100 % activo» (el peor caso). Redis puede no cumplir las condiciones de inactividad; el caso «100 % activo» lo cubre.
- **Cuota gratuita:** se aplica por mes calendario (del 25 al 30 de noviembre es un mes y del 1 al 5 de diciembre es otro) y se descuenta del uso más barato primero, que es la estimación más alta.
- No se incluyen cargos del plan Dedicated ni de puntos de conexión privados (no se usarán). No se incluye el costo de solicitudes (menos de 2 millones por mes), de Key Vault (muy pocas operaciones; su tarifa de operaciones de secretos no se consultó) ni de Application Insights, porque la ingesta de 5 GB al mes es gratuita.

## Resultados de la ventana completa (25-nov al 5-dic, 11 días, 9 réplicas)
| Escenario | 100 % inactivo | 20 % activo | 100 % activo |
|---|---|---|---|
| S (0.25 vCPU / 0.5 GiB) | 16.0 USD | 25.0 USD | 60.9 USD |
| M (Mercado 0.5 / 1.0) | 20.3 USD | 31.3 USD | 75.2 USD |
| L (todo 0.5 / 1.0) | 35.3 USD | 53.2 USD | 125.1 USD |

## Resultados de la ventana corta (29-nov al 3-dic, 5 días)
| Escenario | 100 % inactivo | 20 % activo | 100 % activo |
|---|---|---|---|
| S | 5.5 USD | 9.6 USD | 25.9 USD |
| M | 7.5 USD | 12.4 USD | 32.4 USD |
| L | 14.3 USD | 22.4 USD | 55.1 USD |

Cifras con la cuota gratuita aplicada (sin ella, unos 3 USD más). Dejar 2 réplicas solo en el gateway y en Mercado (1 en Identidad y Mensajería) cuesta lo mismo que el escenario S de la ventana completa (16.0, 25.0 y 60.9 USD) y contradice el principio 12 de la constitución 2.2.0; no se recomienda.

## Fuera de la ventana
| Situación | Costo de cómputo al mes |
|---|---|
| Todo escalado a cero | 0 USD |
| 1 réplica mínima por app y Redis, escenario S, inactivas | 27.5 USD (con la cuota gratuita) |
| Lo mismo, 20 % activas | 41.2 USD (con la cuota gratuita) |

Otros servicios: Blob cuesta unos 0.21 USD al mes por cada 10 GB; la ingesta de Log Analytics es gratuita hasta 5 GB al mes; DocumentDB gratuito cuesta 0 USD (único por suscripción; no se borra). Redis gestionado y Front Door con WAF no se despliegan (ADR-006); el costo de la CDN se fija en TT-GES-10.

## Hallazgos de la suscripción (solo lectura)
- La suscripción es **Azure for Students**, habilitada.
- Presupuesto `budget-knock-dev`: 10 USD al mes, gasto actual **0.0 USD**. El gasto del mes en el grupo de recursos de Knock y en otro grupo de recursos ajeno a Knock de la misma suscripción es 0.0 USD. El **saldo del crédito de 100 USD no se puede leer con la CLI**: falta comprobarlo en el portal (Suscripciones, Créditos) antes de abrir la ventana.
- Los usuarios del tenant **pueden crear registros de aplicaciones** (`allowedToCreateApps = true`), lo que apunta a que las credenciales federadas de GitHub hacia Azure (OIDC) son viables. **No se probó** crearlas, porque eso escribe en el directorio. Plan B: entidad de servicio con secreto en GitHub Environments.

## Pendiente
- Comprobar el saldo del crédito en el portal y que otros proyectos de la suscripción no lo consuman.
- Crear una credencial federada de prueba cuando se autorice (TT-AZ-02) y confirmar el OIDC.
- Recalcular con los tamaños reales de TT-AZ-02 y con las tarifas del día de la ventana (las tarifas cambian).
- La fila 15a de la matriz queda en 🔶: hay estimación, pero no hay infraestructura ni medición real.

## Cómo reproducir el cálculo
Fórmula por mes calendario: costo = vCPU-s × (actividad × 0.000024 + (1 − actividad) × 0.000003) + GiB-s × 0.000003, menos la cuota gratuita, donde vCPU-s = réplicas × vCPU × segundos y GiB-s = réplicas × GiB × segundos. La cuota gratuita se valora, como estimación alta, a la tarifa inactiva: min(vCPU-s; 180 000) × 0.000003 + min(GiB-s; 360 000) × 0.000003 por mes. Segundos de la ventana completa: 6 días en noviembre (518 400 s) y 5 días en diciembre (432 000 s); de la corta: 2 y 3 días (172 800 s y 259 200 s).
