# Checklist — TT-API-01

- [x] spec con 8 secciones + Aprobación, estado Aprobada y clave KNOCK-N
- [ ] Cada caso TC-TT-API-01-KK de este repo tiene prueba con su ID (01 a 14 con prueba; 15 y 16 son de comando/manuales: evidencia en el PR)
- [x] Amenazas de §6 con control y prueba (la amenaza de IA la cubre `security-reviewer`, sin caso)
- [ ] Contrato y docs vivos actualizados (N/A en esta tarea según la spec: sin endpoints de negocio)
- [x] Build, análisis estático, formato y pruebas en verde (resultados reales: build 0/0, format sin cambios, Unit 11, Integration 9, Security 12)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN (cobertura no medida; reglas de `.editorconfig` autorizadas por el revisor)
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift (security-reviewer LIBERA; trazabilidad con drift menor: casos 15 y 16, sin prueba de código por diseño)
- [ ] `Memoria.md` actualizado · informe final en el PR (Memoria.md actualizado; informe en el PR pendiente)
