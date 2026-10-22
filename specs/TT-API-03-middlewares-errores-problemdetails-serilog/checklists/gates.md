# Checklist — TT-API-03

- [x] spec con 8 secciones + Aprobación, estado Aprobada (revisor, 2026-10-21) y clave KNOCK-N
- [x] Cada caso TC-TT-API-03-KK de este repo tiene prueba con su ID (01 a 16 con prueba y `sdd_lint --trace` en OK; 17 y 18 son de comando: la evidencia va en el PR)
- [x] Amenazas de §6 con control y prueba
- [x] Contrato y docs vivos actualizados (sin endpoints nuevos; `docs/MIDDLEWARES.md` y `docs/rate-limit-matrix.md` creados)
- [x] Build, análisis estático, formato y pruebas en verde (resultados reales: build 0/0; Unit 50, Integration 48, Security 80 y Database 8, con el emulador; `dotnet format` en OK sobre el árbol en LF)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN (cobertura de ramas de los archivos nuevos 86,8 % según el constructor; sin supresiones en código: CA1515 exento por ruta en `.editorconfig` solo para `DomainException.cs`, decisión del revisor)
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift (security-reviewer: sin vulnerabilidades; su único bloqueo, la supresión de `DomainException`, se resolvió con la exención aprobada por el revisor; traceability-clerk: 18 casos con prueba o comando)
- [ ] `Memoria.md` actualizado · informe final en el PR (Memoria.md actualizado; informe y evidencia de los casos 17 y 18 en el PR)
