# Checklist — TT-BD-03

> Estado: Aprobada (spec «Aprobación»: José Julián Hernández Hernández, 2026-11-02). Servicio afectado: Identidad y confianza. Casillas sin marcar: no representan implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-40
- [ ] Decisiones D1–D8 de spec §8 (2026-10-28) reflejadas; técnicos y solicitudes fuera de alcance hasta HU-017 y HU-024
- [ ] PR #73 (TT-BD-06) fusionado (ya lo está en `sprint-02`): patrón de cargador y `DocumentDbFixture` disponibles; `db/seeds/catalog/*.json` sin cambios
- [ ] PR #75 (HU-001) fusionado: el cifrado y hash del celular usan `Services/PhoneProtector` (si no llega antes del 30-oct-2026, pasa al siguiente sprint)
- [ ] Decisión D9 (P10, 2026-11-02) reflejada: cargador en `Modules/Identity/Data/` (ruta del caso 08) que escribe con un método nuevo de `IdentityStore`, coordinado con Guillermo y agregado tras fusionar #75; TC-HU-001-23 sin cambios
- [ ] Decisiones D10–D16 (revisión de pares, 2026-11-03) reflejadas; D11 (arranque con `Seed:Demo`) y D16 (Sprint 3) confirmadas por el líder
- [ ] Cada caso TC-TT-BD-03-01 a 08 y 11 tiene prueba con su ID (`sdd_lint --trace TT-BD-03` en OK); 09 y 10 con su salida en el PR
- [ ] Solo datos ficticios: nombres escritos a mano y revisados por una persona, celulares `+5200000NNNNN`, `uid` `demo-cliente-NN`, sin campos sensibles
- [ ] `sub` derivado con `UserIdDerivation.Derive(uid)` (TT-API-12)
- [ ] Celular cifrado y con hash como HU-001; nunca en claro en la base ni en logs
- [ ] Cargador repetible (dos corridas, mismos conteos, `celularHash` y `version`; `celularCifrado` se descifra con el dato asociado `usuarios|celular|<_id>`) y bloqueado en `Production` o sin `Seed:Demo`
- [ ] Choque con el índice único de `celularHash` o `sub`: error claro y nada escrito
- [ ] `docker run` manual publicado solo en `127.0.0.1`
- [ ] Conteos en `documentdb-local` en el PR, sin cadenas ni credenciales
- [ ] Sin dependencias nuevas (sin Bogus)
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y pruebas en verde con resultado real (Unit, Database)
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
