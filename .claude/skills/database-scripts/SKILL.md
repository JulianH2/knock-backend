---
name: database-scripts
description: Modelo de datos, migraciones EF Core, semillas, catálogo SEPOMEX con polígonos PostGIS, respaldos y pruebas de base de datos de knock-backend. Úsala en tareas con capa [BD] (TT-BD-01..04, HU con tablas nuevas) y al escribir SQL.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/database-scripts/SKILL.md. No editar a mano. -->
# database-scripts

## Dónde va cada cosa
| Qué | Dónde | Cómo |
|---|---|---|
| Esquema de un módulo | `src/Knock.Api/Modules/<M>/Data/` + `Migrations/` | `dotnet ef migrations add <Nombre> --project src/Knock.Api --context <M>DbContext` |
| Catálogos base (oficios, colonias) | `db/seeds/catalog/*.sql` o seed de EF idempotente | Se aplican en todos los entornos |
| Datos ficticios de demo | `db/seeds/demo/*.sql` | **Nunca** en producción; sin datos reales |
| Carga SEPOMEX + polígonos | `db/sepomex/` (script de importación y README con la fuente y fecha) | Idempotente, por ciudad piloto |
| Scripts operativos | `db/scripts/` (verificar PostGIS, índices, mantenimiento) | Solo lectura salvo que la spec diga otra cosa |
| Respaldo y restauración | `db/backup/` | Probado en TT-BD-04 con tiempos documentados |

## Reglas
- Un esquema por módulo (`identity`, `catalog`, …), `snake_case`, `uuid` v7, `timestamptz` en UTC, dinero `numeric(12,2)`.
- Concurrencia optimista con `xmin`. Restricciones en la base (FK, CHECK, UNIQUE) para las reglas que no deben romperse nunca.
- Geografía `geography(Point|MultiPolygon, 4326)` con índice GiST; colonia de un punto con `ST_Contains`.
- Datos sensibles cifrados a nivel de campo (dirección, celular); CURP solo como HMAC; nada de biométricos en la base.
- Migraciones **hacia adelante y reversibles**; sin `DROP` de datos sin una migración de respaldo revisada; nunca editar una migración ya fusionada.
- SQL de `db/` pasa `sqlfluff lint --dialect postgres`; nada de SQL dinámico concatenado.
- Semillas idempotentes (`ON CONFLICT DO NOTHING`).

## Pruebas (DatabaseTest con Testcontainers `postgis/postgis`)
- Todas las migraciones aplican desde cero y el modelo coincide (`HasPendingModelChanges` falso).
- Semillas idempotentes (aplicar dos veces no duplica).
- Consultas PostGIS críticas devuelven lo esperado con colonias reales de la ciudad piloto.
- Restricciones: insertar datos inválidos falla en la base.
- Respaldo → restauración en contenedor limpio conserva conteos y sumas de control.
