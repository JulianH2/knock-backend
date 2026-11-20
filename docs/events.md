# Eventos de Knock

## CotizacionEnviada (spec 020)

Mercado inserta este evento en `outbox` dentro de la transacción que guarda la cotización. El documento contiene `_id` (igual al identificador de la cotización), `tipo`, `estado`, `creadoEn`, `solicitudId` y `cotizacionId`.

El despachador y la entrega del aviso al cliente aún no están implementados. El contrato de consumo debe acordarse antes de conectarlos; insertar el documento no demuestra la entrega.

## Eventos de Mercado en el buzón de salida (specs 020, 021 y 022)

Mercado escribe cada evento en `outbox` en la misma transacción que el cambio de negocio, con `_id` determinista `{tipo}:{agregadoId}` (un reintento no lo duplica), `estado = pendiente` y `datos` con solo identificadores y datos no personales: nunca descripción, dirección, código, nombre ni teléfono. El despachador y los consumidores son de la spec 037; insertar el documento no demuestra la entrega.

| Evento | Spec | Cuándo | `datos` |
|---|---|---|---|
| `SolicitudCreada` | 020 | Al crear la solicitud | `solicitudId`, `oficioClave`, `coloniaId`, `venceEn` |
| `CotizacionAceptada` | 021 | Al elegir, para la cotización elegida | `cotizacionId`, `solicitudId`, `tecnicoUsuarioId` |
| `CotizacionRechazada` | 021 | Al elegir, una por cada cotización no elegida | `cotizacionId`, `solicitudId`, `tecnicoUsuarioId`, `motivo` (`other_quote_chosen`) |
| `VisitaConfirmada` | 021 | Al elegir, para la visita creada | `visitaId`, `solicitudId`, `cotizacionId`, `clienteId`, `tecnicoUsuarioId`, `modalidad`, `dia`, `hora` |
| `VisitaEnCurso` | 022 | Al confirmar la llegada con el código correcto | `visitaId`, `solicitudId`, `clienteId`, `tecnicoUsuarioId` |
| `VisitaBloqueada` | 022 | Al tercer intento fallido (Identidad abre un reporte de gravedad alta) | `visitaId`, `solicitudId`, `clienteId`, `tecnicoUsuarioId` |
| `VisitaCerrada` | 022 | Al pasar la visita a `cancelada`, `terminada`, `no_presentado` o bloqueada | `visitaId`, `motivo` (`cancelada`, `terminada`, `no_presentado`, `bloqueada`) |
| `AvisoConfianzaPreparado` | 022 | Cada vez que el cliente prepara el aviso (`_id` `{visitaId}:{n}`) | `visitaId`, `solicitudId`, `clienteId`, `tecnicoUsuarioId` |
| `SolicitudEnviadaATecnico` | 028 | Por cada envío de una oleada | `solicitudId`, `tecnicoId`, `oleada` |
| `SolicitudSinTecnicos` | 028 | Al completar la última oleada sin envíos anteriores | `solicitudId` |

## Tareas programadas de reparto (spec 028)

Estas tareas viven en `tareas_programadas` de Mercado; no son eventos ni entregan avisos:

| Tipo | Cuándo se programa | Efecto |
|---|---|---|
| `RepartirOleada` | Al crear la solicitud se programa la oleada 1; una revisión puede programar la siguiente. | Avanza la oleada de una solicitud abierta y no vencida, crea los envíos y sus eventos, y programa `RevisarOleada` en la misma transacción. En la última oleada, si ninguna tuvo envíos, escribe `SolicitudSinTecnicos`. |
| `RevisarOleada` | Al repartir, para `ahora + Oleadas:MinutosRevision` (15 minutos por omisión). | Si la solicitud sigue abierta, hay menos del mínimo de cotizaciones activas y no se alcanzó el máximo de oleadas, programa `RepartirOleada` para la siguiente. |

Cada tarea usa `claveNegocio = {solicitudId}:{oleada}`, `programadaPara` en UTC, `estado`, `intentos`, `creadoEn` y `actualizadoEn`. La reserva agrega `reservadaHasta` y `reservaId`; su confirmación transaccional incrementa `confirmacionesReserva`. Los estados son `pendiente`, `en_proceso`, `hecha` y `fallida`. El índice único por `tipo` y `claveNegocio`, la confirmación de la reserva y el avance condicionado protegen los efectos frente a reintentos y retomas; una reserva vencida no garantiza que solo haya un manejador físicamente en ejecución.

La spec 028 menciona `VencerSolicitud` como tarea de la spec 029, no como tarea implementada por el reparto. Los identificadores de eventos y tareas deben tratarse como datos sensibles al correlacionarlos: no incluir descripción, dirección, nombres, teléfonos ni `sub` en sus ejemplos o registros de reparto.
