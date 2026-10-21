# TT-BD-05 — tareas

## T1 — Arnés local
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-05-02 · TC-TT-BD-05-04 · TC-TT-BD-05-05 · TC-TT-BD-05-06 · TC-TT-BD-05-09
- **Pendiente:** Proyecto `tests/Spike.DocumentDb/` con `MongoDB.Driver` y Testcontainers (`documentdb-local` y `mongo:8.0` réplica de un nodo); carga del GeoJSON e índices del spike.

## T2 — Clúster en Azure
- **Estado:** pendiente (persona con acceso)
- **Verificar:** TC-TT-BD-05-01 · TC-TT-BD-05-03 · TC-TT-BD-05-07
- **Pendiente:** Crear el nivel gratuito en West US, firewall por IP, correr el arnés y medir latencia y conexiones.

## T3 — Respaldo y paridad
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-05-08 · TC-TT-BD-05-09
- **Pendiente:** `mongodump`/`mongorestore` y tabla de diferencias local vs Azure.

## T4 — Evidencia y recomendación
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-05-01 a TC-TT-BD-05-09
- **Pendiente:** `docs/evidencia/KNOCK-147-spike-documentdb.md` con P1–P9 y la recomendación final.
