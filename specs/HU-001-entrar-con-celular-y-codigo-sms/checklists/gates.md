# Checklist — HU-001

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (decisiones 1 a 9 del líder del 2026-10-24 escritas en §8; dudas abiertas 1 a 11 por responder).
- [ ] Confirmar que TT-BD-06 (#53) fusionó (`AddKnockMongo`, `Mongo:Databases:Identity`, fixture de `documentdb-local`) antes de implementar la base; si no, aplicar spec §3 «Dependencia de TT-BD-06» (no crear un cliente propio).
- [ ] Fusionar el `sub` v8 (caso 10) **antes** de guardar ninguna cuenta; ninguna cuenta con `sub` v5 persistida.
- [ ] Verificar el cambio de código y pruebas ya fusionados de TT-API-02 (derivación, canje, `UserIdDerivationTests`, `FirebaseSessionTests`, `FirebaseRequestValidationTests`); listarlo en el PR; sin SHA-1 en `src/`.
- [ ] Añadir y ejecutar una prueba por cada TC-HU-001 de este repositorio (01, 02, 05 y 06 se verifican en knock-frontend; 26 y 27 por comando con su salida en el PR).
- [ ] Cifrado AES-256-GCM y HMAC-SHA256 con llaves separadas y versión; la API no arranca sin llaves (también en Development); ninguna llave en el repositorio (gitleaks).
- [ ] Alta de cuenta en transacción (usuario, dos consentimientos, `USER_REGISTERED`, huella); índices únicos de `sub` y `celularHash`; canjes simultáneos sin duplicados.
- [ ] Refresh solo como huella SHA-256; ni el token en claro, ni el celular, ni el `uid`, `sub` o huella en logs ni respuestas.
- [ ] CA9 verificado por IP con el limitador de TT-API-03; desviación anotada en `docs/rate-limit-matrix.md`.
- [ ] `cache: no` declarado en las cuatro colecciones; paginación no aplica (sin listados); excepciones a los principios 11 y 15 documentadas en el OpenAPI y en `docs/endpoints.md`.
- [ ] Responder las dudas 1 a 11 de §8 (colecciones, cuerpo `deviceId`/`aceptaAviso`, conflicto 409, `code` de ProblemDetails, 400 o 422, `scripts/export_openapi.py`, constantes de versión, TC-TT-API-02-10, referencia `sub` en HU-009, tamaño, retención).
- [ ] `tests/ContractTest`, `docs/openapi.v1.json` y `docs/endpoints.md` creados y al día (HU-009 los espera); `docs/events.md` sin cambios.
- [ ] Build 0 errores / 0 warnings, `dotnet format` sin cambios, Unit (con Architecture), Integration, Security, Database y Contract en verde (resultados reales).
- [ ] Cobertura (60 % líneas / 50 % ramas) y mutación del módulo Identity en meta (≥ 80 % según la constitución §7); sin supresiones sin EXC-NNN.
- [ ] `python scripts/sdd_lint.py --trace HU-001`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check` y Semgrep en OK.
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift.
- [ ] `Memoria.md` actualizado · informe final en el PR con «Uso de IA»; firma real del canje contra `knock-0` con número de prueba (evidencia sin códigos ni tokens).
