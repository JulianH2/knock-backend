# Checklist — HU-001

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (decisiones 1 a 9 del líder y respuestas D1 a D11 del 2026-10-24 escritas en §8; sin dudas de diseño abiertas).
- [ ] Confirmar que TT-BD-06 (#53) fusionó (`AddKnockMongo`, `Mongo:Databases:Identity`, fixture de `documentdb-local`) antes de implementar la base; si no, aplicar spec §3 «Dependencia de TT-BD-06» (no crear un cliente propio).
- [ ] Fusionar la **tarea A** («`sub` v8 con SHA-256», 1 punto, por abrir) **antes** de guardar ninguna cuenta; ninguna cuenta con `sub` v5 persistida; el método de prueba conserva `_TC_TT_API_02_10` (D8).
- [ ] Verificar el cambio de código y pruebas ya fusionados de TT-API-02 que quedan en esta historia (canje, verificador, `FirebaseSessionTests`, `FirebaseRequestValidationTests` con TC-TT-API-02-11 a 422, fugas); listarlo en el PR (la derivación y su prueba son de la tarea A).
- [ ] Añadir y ejecutar una prueba por cada TC-HU-001 de este repositorio (01, 02, 05 y 06 se verifican en knock-frontend; 24 y 25 por comando con su salida en el PR).
- [ ] Cifrado AES-256-GCM y HMAC-SHA256 con llaves separadas y versión; la API no arranca sin llaves (también en Development); ninguna llave en el repositorio (gitleaks).
- [ ] Alta de cuenta en transacción (usuario, dos consentimientos, `USER_REGISTERED`, huella); índices únicos de `sub` y `celularHash`; canjes simultáneos sin duplicados.
- [ ] Refresh solo como huella SHA-256; ni el token en claro, ni el celular, ni el `uid`, `sub` o huella en logs ni respuestas.
- [ ] CA9 verificado por IP con el limitador de TT-API-03; desviación anotada en `docs/rate-limit-matrix.md`.
- [ ] `cache: no` declarado en las cuatro colecciones; paginación no aplica (sin listados); excepciones a los principios 11 y 15 documentadas en el OpenAPI y en `docs/endpoints.md`.
- [ ] Confirmar los puntos propuestos (HU-001 = 5; A = 1; B = 2), abrir las tareas A y B en Jira y fusionar #61 (`scripts/export_openapi.py`) y TT-BD-06; las 11 dudas ya están resueltas como D1 a D11.
- [ ] Tarea B («Contrato vivo»): `tests/ContractTest`, `docs/openapi.v1.json` y `docs/endpoints.md` creados (HU-009 y TT-API-03 los esperan); esta historia solo los regenera con el script de #61; `docs/events.md` sin cambios; no dar por existente `scripts/export_openapi.py` mientras no fusione.
- [ ] Build 0 errores / 0 warnings, `dotnet format` sin cambios, Unit (con Architecture), Integration, Security, Database y Contract en verde (resultados reales).
- [ ] Cobertura (60 % líneas / 50 % ramas) y mutación del módulo Identity en meta (≥ 80 % según la constitución §7, no el 70 % de la ficha); sin supresiones sin EXC-NNN.
- [ ] `python scripts/sdd_lint.py --trace HU-001`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check` y Semgrep en OK.
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift.
- [ ] `Memoria.md` actualizado · informe final en el PR con «Uso de IA»; firma real del canje contra `knock-0` con número de prueba (evidencia sin códigos ni tokens).
