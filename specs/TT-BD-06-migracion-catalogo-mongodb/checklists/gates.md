# Checklist — TT-BD-06

> Estado: borrador. Servicio afectado: Mercado. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-156
- [ ] Dudas (a)–(j) de spec §8 resueltas por el líder (incluida (b), colección `archivos`, y (j), validación de `coloniaId`)
- [ ] `MongoDB.Driver` y `Testcontainers.MongoDb` evaluados (licencia, mantenimiento, vulnerabilidades) y registrados en `Memoria.md`
- [ ] Cada caso TC-TT-BD-06-01 a 19 tiene prueba con su ID (`sdd_lint --trace TT-BD-06` en OK); 20 y 21 con su salida en el PR
- [ ] `ICatalogApi` con sus firmas existentes y solo los miembros que nombra la spec (TC-TT-BD-06-17); `FindNeighborhoodAtAsync` delega en la consulta interna (TC-TT-BD-06-19); consumidores en verde
- [ ] `MongoClient` único en `Hosting/` (`AddKnockMongo`) y base por módulo con `Mongo:Databases:<Módulo>` (TC-TT-BD-06-16 y 18)
- [ ] p95 de la consulta por punto en servidor < 100 ms (TC-TT-BD-06-12), con p50/p95/p99 en el PR
- [ ] Cadena `ConnectionStrings:KnockMongo` fuera del repositorio, logs y errores; `maxPoolSize` desde configuración
- [ ] Riesgo de `$jsonSchema` (`modalidad`) anotado para la prueba de humo de TT-BD-07
- [ ] Sin EF Core, Npgsql, NetTopologySuite, `Testcontainers.PostgreSql` ni `Migrations/` del catálogo
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y pruebas en verde con resultado real (Unit, Integration, Security, Database)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
