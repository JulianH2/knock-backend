# Checklist — TT-OBS-01

> Estado: borrador. Servicio afectado: Transversal (`Hosting/`). Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-51; dudas P1 a P9 de §8 resueltas por el líder
- [ ] Paquetes de §3 agregados por el líder o con su autorización, con versión fija, licencia y evaluación en `Memoria.md`
- [ ] Cada caso TC-TT-OBS-01-KK de este repo tiene prueba con su ID (01 a 14), evidencia manual (15 y 16) o salida de comando (17 y 18) en el PR; 19 a 22 en knock-frontend; `sdd_lint --trace TT-OBS-01` en OK
- [ ] Sin cadena de conexión no hay exportador; con la sintética no bloquea; almacenamiento local apagado (casos 01 y 02)
- [ ] Spans, métricas y logs sin celulares, tokens, CURP, coordenadas, query ni `Authorization` (casos 08 a 12); requisito (f) de TT-API-07 cubierto
- [ ] `traceId` y `correlationId` enlazados en la telemetría sin cambiar TT-API-03; `docs/MIDDLEWARES.md` y `PipelineOrderTests` sin cambios (casos 04 y 14)
- [ ] Ninguna cadena de conexión ni DSN en el repositorio; gitleaks limpio (caso 13)
- [ ] `docs/OBSERVABILIDAD.md` con alertas (umbrales de P3) y pasos manuales; capturas de V1 y V2 en el PR (casos 15 y 16)
- [ ] Build, análisis estático, formato y pruebas en verde (resultados reales)
- [ ] Cobertura en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
