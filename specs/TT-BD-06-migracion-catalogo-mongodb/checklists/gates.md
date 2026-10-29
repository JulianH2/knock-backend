# Checklist — TT-BD-06

> Estado: implementación terminada, sin commit (2026-10-27). Servicio afectado: Mercado. Marcado solo lo cumplido y verificado; lo que depende del PR, del emulador de Firebase o de mediciones que no se corrieron queda sin marcar con su pendiente.

- [x] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-156
- [x] Dudas (a)–(j) de spec §8 resueltas por el líder (#53, 2026-10-24); imagen de (a) confirmada: digest del spike (decisión del 2026-10-26)
- [x] `MongoDB.Driver` evaluado y registrado en `Memoria.md`. `Testcontainers.MongoDb` no se usa (su `MongoDbBuilder` no sirve para documentdb-local); en su lugar va `Testcontainers` 4.16.0 genérico, de la misma serie ya evaluada (Memoria, primera parte)
- [ ] `colonias.json` y `oficios.json` generados **antes** del retiro de PostGIS (hecho: 12 colonias y 3 oficios) — pendiente: la salida del generador en el PR (casos 11 y 21)
- [ ] Cada caso TC-TT-BD-06-01 a 20 tiene prueba con su ID (`sdd_lint --trace TT-BD-06` en OK: hecho) — pendiente: la salida de 21 y 22 en el PR
- [x] `ICatalogApi` con exactamente sus cuatro miembros: tres firmas existentes y `NeighborhoodExistsAsync` (TC-TT-BD-06-17 y 20); `FindNeighborhoodAtAsync` delega en la consulta interna (TC-TT-BD-06-19); consumidores en verde
- [x] `MongoClient` único en `Hosting/` (`AddKnockMongo`) y base por módulo con `Mongo:Databases:<Módulo>` (TC-TT-BD-06-16 y 18); `maxPoolSize` = 20
- [ ] p95 de la consulta por punto en servidor < 100 ms (TC-TT-BD-06-12: en verde; documentdb-local informa 0 ms en el servidor, P9) — pendiente: p50/p95/p99 en el PR (valores en `Memoria.md`)
- [x] Cadena `ConnectionStrings:KnockMongo` fuera del repositorio, logs y errores; imagen de pruebas fijada por digest
- [x] Riesgo de `$jsonSchema` (`modalidad`) anotado para la prueba de humo de TT-BD-07
- [x] Sin EF Core, Npgsql, NetTopologySuite, `Testcontainers.PostgreSql`, `Migrations/` del catálogo ni `db/seeds/catalog/oficios.sql` (autorización del líder escrita en la spec §3)
- [x] PR del líder sobre `sdd_lint.py` («Reemplazada», #58) fusionado antes de este PR. Nota: TT-BD-01/02 sí pasan aquí a «Reemplazada (por TT-BD-06)», según lo que escribió el líder en #53 («cuando fusiones»); se aparta del texto de la decisión (h) y se declara en el PR
- [x] Sin alcance de `archivos` (tarea «Colección archivos de Media») ni de la búsqueda por prefijo (HU-012)
- [x] Amenazas de §6 con control y prueba (`security-reviewer`, 2026-10-27)
- [ ] Build, formato y pruebas en verde con resultado real (build 0/0, formato, Unit y Database en verde) — pendiente: los 8 casos de TT-API-02 de Integration y Security con el emulador de Firebase
- [ ] Cobertura y mutación en metas (no medidas en esta tarea) — sin supresiones sin EXC-NNN: cumplido
- [ ] `security-reviewer` LIBERA (cumplido, 2026-10-27) · `traceability-clerk` sin drift — pendiente: repetirlo tras corregir el drift documental
- [ ] `Memoria.md` actualizado (cumplido) · informe final en el PR — pendiente
