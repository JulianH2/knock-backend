# Checklist — TT-BD-01

> Seguimiento de implementación parcial. La aprobación humana de la spec se conserva; las casillas registran únicamente trabajo y verificaciones ejecutados.

- [x] Preservar la aprobación humana existente; no cambiar estado, revisor ni fecha.
- [x] Completar entregable A independiente: Compose/PostGIS con imagen fijada, secreto requerido, loopback, healthcheck y volumen; verificar `postgis_version()` y documentar reinicio que preserve datos.
- [ ] Completar entregable B después de TT-API-01: provider/contextos, convención de esquema/nombres y migración inicial de extensión y esquemas.
- [ ] Reutilizar readiness y Testcontainers de TT-API-01; no duplicar ni añadir `AspNetCore.HealthChecks.*`.
- [ ] Evaluar licencia, mantenimiento y vulnerabilidades de `EFCore.NamingConventions` antes de incorporarlo.
- [x] Verificar TC-TT-BD-01-01 con Docker Compose local y `SELECT postgis_version()`.
- [ ] Verificar TC-TT-BD-01-02 a TC-TT-BD-01-04 cuando TT-API-01 entregue API, contextos, readiness y fixture de Testcontainers.
- [x] Verificar la configuración básica de secretos de TC-TT-BD-01-05: `.env` ignorado y `.env.example` sin contraseña.
- [ ] Completar TC-TT-BD-01-05 con Gitleaks, revisión de configuración y rechazo de hosts remotos al implementar la migración local.
- [x] Ejecutar `python3 scripts/sdd_lint.py --trace TT-BD-01`; resultado: aviso de 5 casos aún sin pruebas.
- [x] Ejecutar gates de gobierno aplicables y registrar cada resultado; build/pruebas de API quedan pendientes porque `src/Knock.Api` no existe todavía.
