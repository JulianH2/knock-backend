# Checklist — TT-PRU-01

> Estado: implementación en curso (2026-10-27). Solo se marcan las casillas con evidencia; el resto depende del CI del PR #31 o de la revisión.

- [x] spec con 8 secciones + Aprobación, estado Aprobada y clave KNOCK-29 (lo marca el revisor)
- [x] Dudas D1–D9 de §8 respondidas por el revisor y pasadas a «Resuelto»
- [ ] Cada caso TC-TT-PRU-01-01 a 06 tiene prueba con su ID; 07 con la evidencia de los checks de GitHub en el PR
- [ ] Regresión en verde: TC-TT-API-01-05, TC-TT-API-01-07, TC-TT-API-01-09, TC-TT-API-01-10
- [ ] Amenazas de §6 con control y prueba; riesgo residual de rate limiting declarado en el PR
- [ ] `docs/endpoints.md` con el endpoint (si D6); PR declara «cambia el contrato» (publicación pendiente, D5); `docs/openapi.v1.json` no editado a mano
- [ ] Build Release 0/0, `dotnet format` sin cambios, Unit, Integration, Security y Database en verde (resultados reales; lo que no se pudo ejecutar localmente, dicho)
- [ ] Cobertura ≥ 60 % líneas / 50 % ramas; sin supresiones sin EXC-NNN ni cambios en analizadores sin autorización
- [ ] `sdd_lint.py --trace TT-PRU-01`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check` y Semgrep en OK
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR con «Uso de IA», tiempo y dificultades (ficha)

## Evidencia local (2026-10-27, parcial; no marca casillas compuestas)
- `dotnet build Knock.slnx -c Release`: 0 advertencias, 0 errores. `dotnet format Knock.slnx --verify-no-changes` sobre una copia en LF: sin cambios.
- `dotnet test tests/UnitTest -c Release`: 91/91 (incluye TC-TT-API-01-07 y TC-TT-API-01-09 sin modificar).
- `sdd_lint.py` OK · `sdd_lint.py --trace TT-PRU-01`: 01–06 con prueba, 07 comando · `codemap.py --check` OK · `check_exceptions.py` OK · `sync_agents.py --check` OK · jscpd: 0 clones.
- **No ejecutado en local:** IntegrationTest (casos 01–06, regresión TC-TT-API-01-05 y -10), SecurityTest y DatabaseTest (Docker Desktop no responde), cobertura y Semgrep (no instalado). Se verifican en el CI.
