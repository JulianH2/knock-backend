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
