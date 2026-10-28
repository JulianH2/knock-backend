# TT-BD-03 — tareas

> Servicios afectados: Identidad y confianza y Mercado. Ninguna tarea empieza hasta que la spec esté Aprobada, #73 (TT-BD-06) esté fusionado y el líder responda P1–P9.

## T1 — JSON de demostración
- **Estado:** pendiente. 10 clientes, 20 técnicos y las solicitudes de P5, con el formato de `db/seeds/catalog/*.json`, `demo: true`, colonias piloto y oficios por `_id`, celulares del rango de P6.
- **Verificar:** TC-TT-BD-03-01 · TC-TT-BD-03-02 · TC-TT-BD-03-03 · TC-TT-BD-03-04

## T2 — Cargadores repetibles
- **Estado:** pendiente (forma según P3). Upsert por `_id`, validación previa, cifrado del celular como HU-001, guarda de entorno; uno por servicio.
- **Verificar:** TC-TT-BD-03-05 · TC-TT-BD-03-06 · TC-TT-BD-03-07 · TC-TT-BD-03-08

## T3 — Documentación
- **Estado:** pendiente. Sección de demostración en `db/seeds/README.md`.
- **Verificar:** revisión del PR

## T4 — Carga en Staging y gates
- **Estado:** pendiente (depende de P2). Conteos tras dos cargas y captura del mapa, sin cadenas.
- **Verificar:** TC-TT-BD-03-09 · TC-TT-BD-03-10
