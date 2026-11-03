---
name: database-scripts
description: Modelo de datos en MongoDB, índices, semillas JSON, catálogo SEPOMEX con polígonos GeoJSON, respaldos y pruebas de base de datos de knock-backend. Úsala en tareas con capa [BD] (TT-BD-01..09, HU con colecciones nuevas) y al escribir consultas o semillas.
---

# database-scripts

## Dónde va cada cosa
| Qué | Dónde | Cómo |
|---|---|---|
| Colecciones, llaves e índices de un módulo | `src/Knock.Api/Modules/<M>/Data/<M>Store.cs` (único punto con `IMongoCollection`) | Base lógica en `Mongo:Databases:<M>`; índices con `EnsureIndexesAsync` |
| Catálogos base (oficios, colonias) | `db/seeds/catalog/*.json` | Los aplica `CatalogSeeder`: upsert por clave natural, `_id` fijos y `version` que solo sube si cambia el contenido |
| Datos ficticios de demo | `db/seeds/demo/*.json` (TT-BD-03) | **Nunca** en producción; sin datos reales |
| Carga SEPOMEX + polígonos | `db/sepomex/` y `db/scripts/generate_catalog_seeds.py` (README con la fuente y fecha) | Idempotente, por ciudad piloto |
| Scripts operativos | `db/scripts/` (validar colonias, índices, mantenimiento) | Solo lectura salvo que la spec diga otra cosa |
| Respaldo y restauración | `db/backup/` (`mongodump` y `mongorestore` a Azure) | Probado en TT-BD-04 con tiempos documentados |

## Reglas
- **Una base lógica por módulo o servicio**; ninguno consulta la de otro (ADR-002, regla 1). Las colecciones, llaves e índices se definen en la spec de cada historia.
- Campos en camelCase como en `oficios` y `colonias`; `_id` explícito (fijo en las semillas) y fechas en UTC.
- Concurrencia optimista con un campo `version` y actualización condicional por la versión esperada → 409 en conflicto. Las reglas que no deben romperse nunca llevan **índice único**.
- Geografía en GeoJSON con índice `2dsphere`; colonia de un punto con `$geoIntersects` (ver `postgis-geo`).
- Datos sensibles cifrados a nivel de campo (dirección, celular); CURP solo como HMAC; nada de biométricos en la base.
- Los índices se crean «hacia adelante» y de forma repetible; cambiar la definición de uno es un índice nuevo y retirar el anterior, con la spec revisada. No se borran datos sin un respaldo probado.
- Las semillas validan todos los documentos antes de escribir el primero y nunca se editan ya fusionadas sin una versión nueva.
- Consultas con filtros tipados del driver y `maxTimeMS`; nada de JSON ni operadores armados con datos del usuario.

## Pruebas (DatabaseTest con Testcontainers y `documentdb-local`)
- Los índices se crean desde cero y repetir la creación no falla ni cambia nada.
- Semillas idempotentes (aplicar dos veces no duplica ni sube `version`).
- Consultas geográficas críticas devuelven lo esperado con colonias reales de la ciudad piloto.
- Restricciones: insertar datos que rompen un índice único falla en la base.
- Respaldo → restauración en un contenedor limpio conserva conteos y sumas de control.
- Una prueba de humo contra el DocumentDB real (ADR-002, P9).
