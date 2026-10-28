# Checklist — TT-BD-03

> Estado: borrador. Servicios afectados: Identidad y confianza y Mercado. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-40
- [ ] Preguntas P1–P9 de spec §8 respondidas por el líder; §5 conciliada con HU-002, HU-017 y HU-024
- [ ] PR #73 (TT-BD-06) fusionado: `colonias.json` y `oficios.json` disponibles y sin cambios
- [ ] Cada caso TC-TT-BD-03-01 a 08 tiene prueba con su ID (`sdd_lint --trace TT-BD-03` en OK); 09 y 10 con su salida en el PR
- [ ] Solo datos ficticios: nombres revisados por una persona, celulares del rango acordado, sin campos sensibles
- [ ] Celular cifrado y con hash como HU-001; nunca en claro en la base ni en logs
- [ ] Cargador repetible (dos corridas, mismos conteos) y bloqueado en `Production`
- [ ] Conteos en Staging (o en el entorno que decida P2) en el PR, sin cadenas ni credenciales
- [ ] Sin dependencias nuevas sin evaluar (Bogus solo si P7 lo aprueba, registrado en `Memoria.md`)
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y pruebas en verde con resultado real (Unit, Database)
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
