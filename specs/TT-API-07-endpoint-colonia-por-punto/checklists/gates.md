# Checklist — TT-API-07

> Estado: borrador. Servicio afectado: Mercado (módulo Catalog). Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-141; dudas (b) a (f), (h) e (i) de §8 resueltas
- [ ] TT-BD-06 (KNOCK-156) fusionada, con la consulta interna `{ id, nombre, cp, municipio }` (TC-TT-BD-06-19)
- [ ] Cada caso TC-TT-API-07-KK de este repo tiene prueba con su ID (01 a 16 con prueba; 17 y 18 de comando, evidencia en el PR) y `sdd_lint --trace TT-API-07` en OK
- [ ] DTO con `Lat` y `Lon` de tipo `string[]`, sin parámetros tipados; parámetro repetido → 422 (casos 02, 06 y 15)
- [ ] Logs sin coordenadas con los niveles configurados y guarda del override `Microsoft.AspNetCore: Warning` (casos 08 y 16); decisión del líder sobre la duda (i)
- [ ] Amenazas de §6 con control y prueba; riesgo residual de la query fuera de la API anotado en TT-ARQ-02 y TT-OBS-01 (duda (f))
- [ ] `ICatalogApi` sin cambios desde esta tarea (TC-TT-BD-06-17 en verde); la validación de `coloniaId` queda en la duda (j) de TT-BD-06
- [ ] `docs/endpoints.md` y `docs/rate-limit-matrix.md` al día; `docs/openapi.v1.json` regenerado si existe `scripts/export_openapi.py`
- [ ] Build, análisis estático, formato y pruebas en verde (resultados reales)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
