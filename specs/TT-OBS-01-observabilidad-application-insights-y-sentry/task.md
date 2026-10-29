# TT-OBS-01 — tareas

> Servicio afectado: Transversal (`src/Knock.Api/Hosting/`). Borrador: nada implementado. Paquetes de la spec §3: los agrega el líder o se agregan con su autorización.

## T1 — Tubería de OpenTelemetry y exportador condicional
- **Estado:** pendiente de aprobación de la spec y de la decisión P8 (paquetes).
- **Verificar:** TC-TT-OBS-01-01 · TC-TT-OBS-01-02 · TC-TT-OBS-01-03 · TC-TT-OBS-01-07

## T2 — Enlace con `X-Correlation-Id` y logs de Serilog
- **Estado:** pendiente. Sin cambiar TT-API-03: `traceId` y `correlationId` siguen separados (decisión previa 1); P9 (`traceparent` del cliente) abierta.
- **Verificar:** TC-TT-OBS-01-04 · TC-TT-OBS-01-05 · TC-TT-OBS-01-06 · TC-TT-OBS-01-14

## T3 — Privacidad de la telemetría
- **Estado:** pendiente. P6 (coordenadas en texto libre) abierta.
- **Verificar:** TC-TT-OBS-01-08 · TC-TT-OBS-01-09 · TC-TT-OBS-01-10 · TC-TT-OBS-01-11 · TC-TT-OBS-01-12

## T4 — Configuración sin secretos
- **Estado:** pendiente. La cadena de conexión solo por `APPLICATIONINSIGHTS_CONNECTION_STRING` (variable o Key Vault).
- **Verificar:** TC-TT-OBS-01-13

## T5 — Alertas y verificación manual en Azure
- **Estado:** pendiente de P3 (umbrales), P4 (quién crea el recurso y las alertas) y P5 (cómo se provoca el error).
- **Verificar:** TC-TT-OBS-01-15 · TC-TT-OBS-01-16

## T6 — App (knock-frontend)
- **Estado:** pendiente de P1 y P2 (spec aparte o en esta).
- **Verificar:** TC-TT-OBS-01-19 · TC-TT-OBS-01-20 · TC-TT-OBS-01-21 · TC-TT-OBS-01-22

## T7 — Paquetes, documentos vivos y gates
- **Estado:** pendiente
- **Verificar:** TC-TT-OBS-01-17 · TC-TT-OBS-01-18
