# Checklist — TT-BD-06

> Estado: borrador. Servicio afectado: Mercado. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-156
- [ ] Dudas (a)–(i) de spec §8 resueltas por el líder
- [ ] `MongoDB.Driver` y `Testcontainers.MongoDb` evaluados (licencia, mantenimiento, vulnerabilidades) y registrados en `Memoria.md`
- [ ] Cada caso TC-TT-BD-06-01 a 17 tiene prueba con su ID (`sdd_lint --trace TT-BD-06` en OK); 18 y 19 con su salida en el PR
- [ ] `ICatalogApi` sin cambios de firma (TC-TT-BD-06-17) y consumidores en verde
- [ ] p95 de la consulta por punto en servidor < 100 ms (TC-TT-BD-06-12), con p50/p95/p99 en el PR
- [ ] Cadena de conexión fuera del repositorio, logs y errores; `maxPoolSize` desde configuración
- [ ] Sin EF Core, Npgsql, NetTopologySuite, `Testcontainers.PostgreSql` ni `Migrations/` del catálogo
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y pruebas en verde con resultado real (Unit, Integration, Security, Database)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
