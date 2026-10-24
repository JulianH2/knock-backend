# Checklist — TT-BD-06

> Estado: borrador. Servicio afectado: Mercado. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-156
- [ ] Dudas (a)–(j) de spec §8 resueltas por el líder (#53, 2026-10-24); imagen de (a) confirmada (digest del spike o `1.0` tras repetir P5 y P9)
- [ ] `MongoDB.Driver` y `Testcontainers.MongoDb` evaluados (licencia, mantenimiento, vulnerabilidades) y registrados en `Memoria.md`
- [ ] `colonias.json` y `oficios.json` generados **antes** del retiro de PostGIS, con la salida del generador en el PR (casos 11 y 21)
- [ ] Cada caso TC-TT-BD-06-01 a 20 tiene prueba con su ID (`sdd_lint --trace TT-BD-06` en OK); 21 y 22 con su salida en el PR
- [ ] `ICatalogApi` con exactamente sus cuatro miembros: tres firmas existentes y `NeighborhoodExistsAsync` (TC-TT-BD-06-17 y 20); `FindNeighborhoodAtAsync` delega en la consulta interna (TC-TT-BD-06-19); consumidores en verde
- [ ] `MongoClient` único en `Hosting/` (`AddKnockMongo`) y base por módulo con `Mongo:Databases:<Módulo>` (TC-TT-BD-06-16 y 18); `maxPoolSize` = 20
- [ ] p95 de la consulta por punto en servidor < 100 ms (TC-TT-BD-06-12), con p50/p95/p99 en el PR
- [ ] Cadena `ConnectionStrings:KnockMongo` fuera del repositorio, logs y errores; imagen de pruebas fijada por digest
- [ ] Riesgo de `$jsonSchema` (`modalidad`) anotado para la prueba de humo de TT-BD-07
- [ ] Sin EF Core, Npgsql, NetTopologySuite, `Testcontainers.PostgreSql`, `Migrations/` del catálogo ni `db/seeds/catalog/oficios.sql` (autorización del líder escrita en la spec §3)
- [ ] PR del líder sobre `sdd_lint.py` («Reemplazada») fusionado antes de este PR; estado de TT-BD-01/02 sin tocar aquí
- [ ] Sin alcance de `archivos` (tarea «Colección archivos de Media») ni de la búsqueda por prefijo (HU-012)
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y pruebas en verde con resultado real (Unit, Integration, Security, Database)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
