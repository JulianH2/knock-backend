# Checklist — TT-API-07

> Estado: borrador. Servicio afectado: Mercado (módulo Catalog). Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-141; dudas (a) a (h) de §8 resueltas
- [ ] TT-BD-06 (KNOCK-156) fusionada, con la consulta que entrega `id`, `cp` y `municipio` según la duda (a)
- [ ] Cada caso TC-TT-API-07-KK de este repo tiene prueba con su ID (01 a 14 con prueba; 15 y 16 de comando, evidencia en el PR) y `sdd_lint --trace TT-API-07` en OK
- [ ] Amenazas de §6 con control y prueba; riesgo residual de la query fuera de la API anotado en TT-ARQ-02 y TT-OBS-01 (duda (f))
- [ ] Firma de `ICatalogApi` sin cambios (TC-TT-BD-06-17 en verde)
- [ ] `docs/endpoints.md` y `docs/rate-limit-matrix.md` al día; `docs/openapi.v1.json` regenerado si existe `scripts/export_openapi.py`
- [ ] Build, análisis estático, formato y pruebas en verde (resultados reales)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
