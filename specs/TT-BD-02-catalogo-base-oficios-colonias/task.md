# TT-BD-02 — tareas

## T1 — Esquema y seed de oficios
- **Verificar:** TC-TT-BD-02-05 · TC-TT-BD-02-09
- **Pendiente:** Implementar `catalog.oficios` como datos y seed idempotente con los valores activos y modalidades especificados.

## T2 — Catálogo geográfico y fuente
- **Verificar:** TC-TT-BD-02-01 · TC-TT-BD-02-02 · TC-TT-BD-02-03 · TC-TT-BD-02-04 · TC-TT-BD-02-07
- **Pendiente:** Verificar fuente/CP SEPOMEX; versionar sus nombres y CP bajo `db/sepomex/`; dibujar 8–12 polígonos aproximados con autor/fecha en `db/seeds/colonias_tula.geojson` y guardar centros para el resto.

## T3 — Consultas de colonia
- **Verificar:** TC-TT-BD-02-01 · TC-TT-BD-02-02 · TC-TT-BD-02-03 · TC-TT-BD-02-04 · TC-TT-BD-02-06 · TC-TT-BD-02-07 · TC-TT-BD-02-08
- **Pendiente:** Exponer consulta por punto y por CP en la interfaz pública del módulo Catalog, con `geography` + GiST, orden X=`lng`/Y=`lat`, desempate de borde definido y sin persistencia/log de coordenadas.
