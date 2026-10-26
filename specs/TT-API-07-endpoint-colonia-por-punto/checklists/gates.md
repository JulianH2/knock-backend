# Checklist — TT-API-07

> Estado: borrador. Servicio afectado: Mercado (módulo Catalog). Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-141; dudas de §8 resueltas por el líder en #54 ((e) e (i) el 2026-10-24; (b), (c), (d), (f), (h) y (j) el 2026-10-26)
- [ ] TT-BD-06 (KNOCK-156) fusionada, con la consulta interna `{ id, nombre, cp, municipio }` (TC-TT-BD-06-19)
- [ ] Cada caso TC-TT-API-07-KK de este repo tiene prueba con su ID (01 a 16 con prueba; 17 y 18 de comando, evidencia en el PR) y `sdd_lint --trace TT-API-07` en OK
- [ ] DTO con `Lat` y `Lon` de tipo `string` con binder de un solo valor y `[ValidateNever]`, sin parámetros tipados; parámetro repetido → 422, nunca el primero (casos 06 y 15)
- [ ] Logs sin coordenadas con los niveles configurados y guarda del override `Microsoft.AspNetCore: Warning` (casos 08 y 16); la redacción sin importar el nivel va en la tarea de seguimiento de TT-API-03 (decisión (i))
- [ ] OpenAPI con `lat` y `lon` como `string` con el patrón fijo, obligatorios y con `maxLength: 16`, sin transformador (caso 13, decisión (j))
- [ ] Amenazas de §6 con control y prueba; riesgo residual de la query fuera de la API anotado como requisito en TT-ARQ-02 y TT-OBS-01 (decisión (f))
- [ ] `ICatalogApi` sin cambios desde esta tarea (TC-TT-BD-06-17 en verde); la validación de `coloniaId` es `NeighborhoodExistsAsync` de TT-BD-06 (TC-TT-BD-06-20), que este endpoint no usa
- [ ] `docs/endpoints.md` y `docs/rate-limit-matrix.md` al día; `docs/openapi.v1.json` regenerado si existe `scripts/export_openapi.py`
- [ ] Build, análisis estático, formato y pruebas en verde (resultados reales)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
