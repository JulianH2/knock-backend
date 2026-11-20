# Gates 059 — Suspensión preventiva en casos críticos (KNOCK-97)

> Se marca cada línea solo con el resultado real de ejecutar el comando. Lo que no se pueda ejecutar se anota con el motivo. Estado actual: spec en Borrador, nada ejecutado.

## Antes de escribir código
- [ ] La spec está **Aprobada** por una persona y trae la clave `KNOCK-97`.
- [ ] Dudas 1 a 6 de `spec.md §15` respondidas (disparo, cliente reportado, apelación del suspendido, quién reactiva, tipo de aviso y valores de la regla gradual).
- [ ] Enmiendas pedidas a las specs 037 y 054 aprobadas o decididas en contra.
- [ ] Specs 033 y 053 implementadas, o decisión documentada de probar `Suspender` solo como operación interna.

## Compilación y estilo
- [ ] `dotnet build Knock.slnx -c Release --no-restore` → 0 errores, 0 advertencias
- [ ] `dotnet format Knock.slnx --verify-no-changes`

## Pruebas (resultado real)
- [ ] `dotnet test tests/UnitTest -c Release --no-build` (incluye Architecture)
- [ ] `dotnet test tests/IntegrationTest -c Release --no-build`
- [ ] `dotnet test tests/SecurityTest tests/DatabaseTest tests/ContractTest -c Release --no-build`
- [ ] Cobertura ≥ 60 % líneas / 50 % ramas; mutación del módulo `Safety` en su meta (spec 008)
- [ ] `python scripts/sdd_lint.py --trace 059` sin faltantes (CA-01 a CA-19)

## Seguridad y privacidad (spec §9)
- [ ] IDOR y asignación masiva (CA-10), permiso y doble factor (CA-09) y autosuspensión (CA-11) con prueba negativa
- [ ] Reportes en masa sin decidir no suspenden (CA-12)
- [ ] Sesiones cerradas en la app, el panel y Redis (CA-01, CA-02, CA-07, CA-08, CA-18)
- [ ] Eventos, avisos, auditoría y registros sin datos personales ni identidad de quien reportó (CA-17)
- [ ] `security-reviewer` LIBERA

## Contrato, trazabilidad y gobierno
- [ ] `docs/events.md` con `CuentaSuspendida` y `CuentaReactivada`; `docs/endpoints.md` con el efecto de REP-05
- [ ] `python scripts/sdd_lint.py` · `python scripts/codemap.py --check` · `python scripts/ci/check_exceptions.py` · `python scripts/sync_agents.py --check`
- [ ] `semgrep scan --config .semgrep/semgrep.yaml --error`
- [ ] `traceability-clerk` sin drift
- [ ] `Memoria.md` con estado, decisiones con porqué y errores a evitar
- [ ] Informe de `story-workflow` y sección «Uso de IA» en el PR
