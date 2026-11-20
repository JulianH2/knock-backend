# Checklist — Spec 056 (KNOCK-162)

> Estado: spec en Borrador. Las casillas las marca el revisor; no representan aprobación ni ejecución de pruebas.

- [ ] Spec con 8 secciones + Aprobación, clave KNOCK-162 y estado Aprobada por una persona con fecha
- [ ] Dudas de §8 (P-01 a P-08) resueltas o aceptadas por el líder y anotadas en la spec
- [ ] Cada caso CA-NN tiene prueba con `CA_056_NN` (01 a 11); `sdd_lint --trace 056` en OK
- [ ] Amenazas de §6 con control y prueba (suplantación, replay, rotación, escalada)
- [ ] Convivencia con las specs 037 (R-11) y 041 (R-17): sus pruebas en verde sin editarse
- [ ] Sin secretos en repositorio, registros ni respuestas; `gitleaks` limpio
- [ ] Contrato y docs vivos al día (`docs/endpoints.md` si cambia el esquema)
- [ ] Build, formato, análisis estático y pruebas en verde (resultados reales)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] Key Vault y Terraform resueltos por el líder (fuera de esta spec)
- [ ] `Memoria.md` actualizado · informe final en el PR
