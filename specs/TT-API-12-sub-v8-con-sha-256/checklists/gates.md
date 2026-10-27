# Checklist — TT-API-12

> Estado: implementada en el PR #71. Las casillas se marcan con el resultado real de cada comando (en la descripción del PR); un gate sin marcar no se ejecutó o espera su revisor.

- [x] Obtener aprobación humana de la spec (decisión 1 de HU-001, D8 y D10 del líder, 2026-10-24; sin dudas de diseño abiertas).
- [ ] Fusionar esta tarea **antes** de HU-001 y de cualquier dato guardado; ningún `sub` v5 persistido.
- [x] Vectores v8 recalculados con una implementación independiente antes de fijarlos en la prueba.
- [x] Añadir y ejecutar una prueba por cada TC-TT-API-12 de este repositorio (08, 09 y 10 por comando, con su salida en el PR).
- [x] `UserIdDerivationTests` actualizado conservando el sufijo `_TC_TT_API_02_10`; el resto de TT-API-02 en verde sin editar; spec y evidencia de TT-API-02 intactas.
- [x] Sin SHA-1 en `src/` y sin `EXC-NNN` ni supresiones de CA5350 o S4790.
- [x] Sin paquetes nuevos; `dotnet list package --vulnerable` en OK (local) y gitleaks en el CI (no disponible en local); datos solo ficticios.
- [x] Sin cambios de contrato HTTP; ningún archivo de MongoDB ni de HU-001.
- [x] Build 0 errores / 0 warnings, `dotnet format` sin cambios, Unit (con Architecture), Integration y Security en verde (resultados reales, con el emulador de Firebase).
- [x] `python scripts/sdd_lint.py --trace TT-API-12` y `--trace TT-API-02`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check` en OK (local) y Semgrep en el CI (verde en los commits anteriores; confirmar en el último).
- [x] `security-reviewer` LIBERA (PASS, sin hallazgos medios o mayores) · `traceability-clerk` sin drift (segunda pasada tras corregir su primer informe).
- [x] `Memoria.md` actualizado (lo escribe quien hace el cambio) · informe final en el PR con «Uso de IA».
