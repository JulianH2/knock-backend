# Checklist — TT-OBS-01

> Estado: borrador. Servicio afectado: Transversal (`Hosting/`). Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-51; decisiones P1 a P9 de §8 aplicadas
- [ ] Paquetes de §3 agregados con la autorización del líder (P8), con versión fija, licencia y evaluación en `Memoria.md`; OSV-Scanner y dependency-review en verde
- [ ] Cada caso TC-TT-OBS-01-KK de este repo tiene prueba con su ID (01 a 14), evidencia manual (15 y 16) o salida de comando (17 y 18) en el PR; 19 a 22 en la spec espejo de knock-frontend (Sprint 3); `sdd_lint --trace TT-OBS-01` en OK
- [ ] Sin cadena de conexión no hay exportador; con la sintética no bloquea; almacenamiento local y Live Metrics apagados (casos 01 y 02)
- [ ] Spans, métricas y logs sin celulares, tokens, CURP, coordenadas, query ni `Authorization` (casos 08 a 12); requisito (f) de TT-API-07 cubierto
- [ ] La API ignora el `traceparent` externo e inicia siempre una traza nueva; el `traceparent` saliente se mantiene (casos 05 y 11)
- [ ] Un número suelto como `12.3456 ms` no se redacta; los pares de coordenadas y las claves conocidas sí (caso 12)
- [ ] `traceId` y `correlationId` enlazados en la telemetría sin cambiar TT-API-03; `docs/MIDDLEWARES.md` y `PipelineOrderTests` sin cambios (casos 04 y 14)
- [ ] Ninguna cadena de conexión ni DSN en el repositorio; gitleaks limpio (caso 13)
- [ ] `docs/OBSERVABILIDAD.md` con alertas (umbrales de P3, sin escribir el correo) y pasos manuales; costo de las reglas confirmado antes de crearlas; capturas de V1 y V2 en el PR (casos 15 y 16)
- [ ] Build, análisis estático, formato y pruebas en verde (resultados reales)
- [ ] Cobertura en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
