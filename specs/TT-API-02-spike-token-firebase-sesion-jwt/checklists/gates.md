# Checklist — TT-API-02

- [x] spec con 8 secciones + Aprobación, estado Aprobada y clave KNOCK-N
- [ ] Cada caso TC-TT-API-02-KK de este repo tiene prueba con su ID (01 a 14 con prueba y `sdd_lint --trace` en OK; 15 a 17 son de comando: la evidencia va en el PR y está pendiente)
- [x] Amenazas de §6 con control y prueba
- [ ] Contrato y docs vivos actualizados (N/A en esta tarea: contrato vivo pospuesto a HU-001)
- [x] Build, análisis estático, formato y pruebas en verde (resultados reales: build 0/0; Unit 26, Integration 25 y Security 56 contra el emulador; `dotnet format` en OK sobre el commit en LF)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN (sin supresiones nuevas; cobertura de ramas de los archivos nuevos 89 % según el constructor, sin medir el reporte del CI)
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift (security-reviewer LIBERA; traceability-clerk sin drift bloqueante: queda drift documental por decisión del revisor: guardia del emulador, versión de `firebase-tools` y nombre del caso 12)
- [ ] `Memoria.md` actualizado · informe final en el PR (Memoria.md actualizado; informe y evidencias de los casos 15 a 17 pendientes en el PR)
