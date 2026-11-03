# Checklist — TT-API-11

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-186
- [ ] Dudas 1 y 2 de spec §8 resueltas por el líder (2026-11-03) y reflejadas en la spec; dudas 3, 4 y 5 confirmadas al aprobar
- [ ] Cada caso TC-TT-API-11-01 a 04, 07 y 08 tiene prueba con su ID (`sdd_lint --trace TT-API-11` en OK); el 05 y el 06 con su salida en el PR
- [ ] Las pruebas de los casos 01 y 02 fallan antes del cambio y pasan después; las de 07 y 08 pasan (vigilan que el cliente HTTP y Kestrel no registren la query)
- [ ] El filtro queda antes de todos los sinks (consola y, si ya está fusionado TT-OBS-01, OpenTelemetry)
- [ ] `AspNetCoreLogLevelGuardTests`, `SensitiveLogTests` y las pruebas de logs de TT-API-03 sin editar y en verde
- [ ] Mutación manual del filtro (caso 05): salida en el PR y revertida
- [ ] Ninguna query, `lat=` ni `lon=` en los logs con los niveles bajados a `Debug`
- [ ] Sin cambios en `appsettings*.json`, contrato, paquetes ni archivos protegidos
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y gobierno en verde con resultado real
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
