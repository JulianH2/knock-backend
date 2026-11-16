# Eventos de Knock

## CotizacionEnviada (spec 020)

Mercado inserta este evento en `outbox` dentro de la transacción que guarda la cotización. El documento contiene `_id` (igual al identificador de la cotización), `tipo`, `estado`, `creadoEn`, `solicitudId` y `cotizacionId`.

El despachador y la entrega del aviso al cliente aún no están implementados. El contrato de consumo debe acordarse antes de conectarlos; insertar el documento no demuestra la entrega.
