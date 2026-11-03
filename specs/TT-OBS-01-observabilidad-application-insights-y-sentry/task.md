# TT-OBS-01 — tareas

> Servicio afectado: Transversal (`src/Knock.Api/Hosting/`). Borrador: nada implementado. Decisiones P1 a P9 en la spec §8. Paquetes de la spec §3: autorizados para la implementación (P8).

## T1 — Tubería de OpenTelemetry y exportador condicional
- **Estado:** pendiente de aprobación de la spec. Lista explícita de paquetes (P8); 100 % de las trazas y Live Metrics apagado (P7).
- **Verificar:** TC-TT-OBS-01-01 · TC-TT-OBS-01-02 · TC-TT-OBS-01-03 · TC-TT-OBS-01-07

## T2 — Enlace con `X-Correlation-Id` y logs de Serilog
- **Estado:** pendiente. Sin cambiar TT-API-03: `traceId` y `correlationId` siguen separados (decisión previa 1). La API ignora el `traceparent` externo e inicia siempre una traza nueva (P9).
- **Verificar:** TC-TT-OBS-01-04 · TC-TT-OBS-01-05 · TC-TT-OBS-01-06 · TC-TT-OBS-01-14

## T3 — Privacidad de la telemetría
- **Estado:** pendiente. Claves conocidas siempre; en texto libre solo pares de coordenadas (P6).
- **Verificar:** TC-TT-OBS-01-08 · TC-TT-OBS-01-09 · TC-TT-OBS-01-10 · TC-TT-OBS-01-11 · TC-TT-OBS-01-12

## T4 — Configuración sin secretos
- **Estado:** pendiente. La cadena de conexión solo por `APPLICATIONINSIGHTS_CONNECTION_STRING` (variable o Key Vault).
- **Verificar:** TC-TT-OBS-01-13

## T5 — Alertas y verificación manual en Azure
- **Estado:** pendiente del código. El líder crea a mano el recurso, el grupo de acciones y las alertas (P4) con los umbrales de P3; el 5xx se provoca con MongoDB detenido (P5). Son pruebas manuales con la evidencia en el PR.
- **Verificar:** TC-TT-OBS-01-15 · TC-TT-OBS-01-16

## T6 — App (knock-frontend)
- **Estado:** fuera de esta rama. Spec espejo en knock-frontend con KNOCK-51; la implementa Erick en el Sprint 3 (P1 y P2).
- **Verificar:** TC-TT-OBS-01-19 · TC-TT-OBS-01-20 · TC-TT-OBS-01-21 · TC-TT-OBS-01-22

## T7 — Paquetes, documentos vivos y gates
- **Estado:** pendiente
- **Verificar:** TC-TT-OBS-01-17 · TC-TT-OBS-01-18
