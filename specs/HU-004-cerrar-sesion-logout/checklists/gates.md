# Checklist — HU-004

> Casillas sin marcar: no representan implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (dudas de §8 resueltas por el líder).
- [ ] Confirmar que HU-001 (KNOCK-43) está fusionada en `sprint-02` (el PR #75 se cerró sin fusionar; su rama `feature/KNOCK-43-phone-login-code` se conserva).
- [ ] Añadir y ejecutar una prueba por cada TC-HU-004 de este repositorio (los marcados «(otro repo)» se verifican en knock-frontend; los «(comando)» con su salida en el PR).
- [ ] Ningún token, huella, `deviceId`, `sub` ni dato personal en logs ni en respuestas de error.
- [ ] `Cache-Control: no-store` y sin compresión en las respuestas de `/auth/*`; límite por IP de TT-API-03 verificado.
- [ ] Contrato regenerado (`docs/openapi.v1.json`, `docs/endpoints.md`) por TT-CI-02 o por el líder si toca archivos protegidos.
- [ ] Build, análisis estático, formato y pruebas en verde (resultados reales); cobertura y mutación en metas; sin supresiones sin EXC-NNN.
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
