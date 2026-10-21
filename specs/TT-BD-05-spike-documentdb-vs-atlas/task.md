# TT-BD-05 — tareas

## T1 — Arnés local
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-05-02 · TC-TT-BD-05-04 · TC-TT-BD-05-05 · TC-TT-BD-05-06 · TC-TT-BD-05-09
- **Pendiente:** Proyecto `tests/Spike.DocumentDb/` (fuera de `Knock.slnx` y del CI) con `MongoDB.Driver` y Testcontainers (`documentdb-local` y `mongo:8.0` réplica de un nodo); scripts de carga del GeoJSON e índices dentro del arnés; fixtures sintéticos de centros, marcados como tales.

## T2 — Clúster en Azure
- **Estado:** pendiente (persona con acceso)
- **Verificar:** TC-TT-BD-05-01 · TC-TT-BD-05-03 · TC-TT-BD-05-07
- **Pendiente:** Verificar `knock-docdb` (West US, nivel gratuito, 32 GB, servidor 8.0); pedir la regla de firewall para la IP; correr el arnés; medir latencia (red y consulta por separado) y el límite de conexiones (sin cifra oficial; comparar con Atlas M0, 500).

## T3 — Respaldo y paridad
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-05-08 · TC-TT-BD-05-09
- **Pendiente:** `mongodump`/`mongorestore` y tabla de diferencias local vs Azure.

## T4 — Evidencia y recomendación
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-05-01 a TC-TT-BD-05-09
- **Pendiente:** `docs/evidencia/KNOCK-147-spike-documentdb.md` con P1–P9 y la recomendación final.
