# Checklist — TT-BD-07

> Estado: borrador. Servicio afectado: Transversal (CI, pruebas y entorno local). Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-157
- [ ] Preguntas P1–P6 de spec §8 respondidas por el líder; si entran P1 o P2, casos agregados y puntos reestimados
- [ ] PR #73 (TT-BD-06) fusionado en la rama destino antes de este PR
- [ ] Cambios en `.github/` y `.agents/knock.json` hechos o autorizados por escrito por el Tech Lead; ningún agente usó `KNOCK_GUARD_ALLOW_PROTECTED`
- [ ] Cada caso TC-TT-BD-07-01 a 07 tiene prueba con su ID (`sdd_lint --trace TT-BD-07` en OK); 08 y 09 con su salida en el PR
- [ ] Job `sql-lint`, salida `sql` y su entrada en `gate.needs` retirados; `gate` en verde
- [ ] Unit, Integration, Security y Database con más de 0 pruebas ejecutadas y 0 fallidas en el CI del PR
- [ ] Umbrales de cobertura sin cambio (60 % líneas / 50 % ramas)
- [ ] Compose local con el digest de `DocumentDbFixture.Image`, puerto en `127.0.0.1` y contraseña obligatoria; sin secretos en el repositorio
- [ ] Adaptadores de agentes regenerados con `sync_agents.py` (sin editar a mano)
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y gobierno en verde con resultado real
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
