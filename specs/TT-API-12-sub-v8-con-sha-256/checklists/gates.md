# Checklist — TT-API-12

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (decisión 1 de HU-001, D8 y D10 del líder, 2026-10-24; sin dudas de diseño abiertas).
- [ ] Fusionar esta tarea **antes** de HU-001 y de cualquier dato guardado; ningún `sub` v5 persistido.
- [ ] Vectores v8 recalculados con una implementación independiente antes de fijarlos en la prueba.
- [ ] Añadir y ejecutar una prueba por cada TC-TT-API-12 de este repositorio (08, 09 y 10 por comando, con su salida en el PR).
- [ ] `UserIdDerivationTests` actualizado conservando el sufijo `_TC_TT_API_02_10`; el resto de TT-API-02 en verde sin editar; spec y evidencia de TT-API-02 intactas.
- [ ] Sin SHA-1 en `src/` y sin `EXC-NNN` ni supresiones de CA5350 o S4790.
- [ ] Sin paquetes nuevos; `dotnet list package --vulnerable` y gitleaks en OK; datos solo ficticios.
- [ ] Sin cambios de contrato HTTP; ningún archivo de MongoDB ni de HU-001.
- [ ] Build 0 errores / 0 warnings, `dotnet format` sin cambios, Unit (con Architecture), Integration y Security en verde (resultados reales, con el emulador de Firebase).
- [ ] `python scripts/sdd_lint.py --trace TT-API-12` y `--trace TT-API-02`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check` y Semgrep en OK.
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift.
- [ ] `Memoria.md` actualizado (lo escribe el líder) · informe final en el PR con «Uso de IA».
