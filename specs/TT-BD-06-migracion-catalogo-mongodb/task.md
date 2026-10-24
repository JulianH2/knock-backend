# TT-BD-06 — tareas

> Servicio afectado: Mercado. Ninguna tarea empieza hasta que la spec esté Aprobada.

## T1 — Paquetes, conexión compartida y readiness
- **Estado:** pendiente de aprobación de la spec (evaluar `MongoDB.Driver` y `Testcontainers.MongoDb`; registrar en `Memoria.md`). `AddKnockMongo` en `Hosting/` con `ConnectionStrings:KnockMongo`, `Mongo:MaxPoolSize` (duda (i)) y `Mongo:Databases:<Módulo>`.
- **Verificar:** TC-TT-BD-06-13 · TC-TT-BD-06-14 · TC-TT-BD-06-15 · TC-TT-BD-06-18

## T2 — Modelo, normalizador, índices y semilla
- **Estado:** pendiente (formato de la semilla y origen del centro según la duda (c); traslapes con el desempate por `nombreBusqueda` de spec §5 y corrección de polígonos según la duda (d); `modalidad` validada en el cargador, `$jsonSchema` como riesgo para TT-BD-07)
- **Verificar:** TC-TT-BD-06-01 · TC-TT-BD-06-02 · TC-TT-BD-06-03 · TC-TT-BD-06-10

## T3 — Consulta interna e `ICatalogApi` sobre MongoDB
- **Estado:** pendiente (consulta interna con `{ id, nombre, cp, municipio }` y `ICatalogApi` delegando en ella; firmas existentes sin cambios; miembro nuevo para validar `coloniaId` según la duda (j))
- **Verificar:** TC-TT-BD-06-04 · TC-TT-BD-06-05 · TC-TT-BD-06-06 · TC-TT-BD-06-07 · TC-TT-BD-06-08 · TC-TT-BD-06-09 · TC-TT-BD-06-17 · TC-TT-BD-06-19

## T4 — Búsqueda por prefijo y rendimiento
- **Estado:** pendiente (la búsqueda por prefijo depende de la duda (g))
- **Verificar:** TC-TT-BD-06-11 · TC-TT-BD-06-12

## T5 — Retiro de EF Core, Npgsql, NetTopologySuite y PostGIS
- **Estado:** pendiente (`oficios.sql` según la duda (e); estado «Reemplazada» de TT-BD-01/02 según la duda (h))
- **Verificar:** TC-TT-BD-06-16 · TC-TT-BD-06-20

## T6 — Gates de compilación y formato
- **Estado:** pendiente
- **Verificar:** TC-TT-BD-06-21

## Pendiente de decisión (no es tarea todavía)
- Colección `archivos` de TT-API-05 (duda (b)): si el líder confirma la recomendación, se agrega una tarea con sus casos antes de aprobar la spec.
