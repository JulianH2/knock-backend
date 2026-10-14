# Checklist — TT-BD-01

> Estado: borrador de preparación. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Mantener la spec en Borrador hasta aprobación humana.
- [ ] Completar entregable A independiente: Compose/PostGIS, imagen fijada, secreto requerido, loopback, healthcheck, volumen, verificación de PostGIS y reinicio que preserve datos.
- [ ] Completar entregable B después de TT-API-01: provider/contextos, convención de esquema/nombres y migración inicial de extensión y esquemas.
- [ ] Reutilizar readiness y Testcontainers de TT-API-01; no duplicar ni añadir `AspNetCore.HealthChecks.*`.
- [ ] Evaluar licencia, mantenimiento y vulnerabilidades de `EFCore.NamingConventions` antes de incorporarlo.
- [ ] Verificar las pruebas TC-TT-BD-01-01 a TC-TT-BD-01-04 con PostGIS local/Testcontainers.
- [ ] Verificar TC-TT-BD-01-05: Gitleaks, archivos de configuración y rechazo de host remoto en configuración local.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace TT-BD-01` y registrar el resultado real.
- [ ] Ejecutar gates aplicables de `AGENTS.md §5` y registrar cada resultado.
