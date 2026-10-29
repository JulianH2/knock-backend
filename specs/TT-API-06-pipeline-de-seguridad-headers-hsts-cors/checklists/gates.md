# Checklist — TT-API-06

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-138
- [ ] Dudas 1 a 13 de spec §8 resueltas por el líder (en especial 1, 4, 5, 6 y 8) y spec actualizada con lo decidido
- [ ] Cada caso TC-TT-API-06-01 a 18 tiene prueba con su ID (`sdd_lint --trace TT-API-06` en OK); 19 y 20 con su salida en el PR
- [ ] Pipeline en el orden de la constitución §4, igual en `UseKnockApi` y `docs/MIDDLEWARES.md` (TC-TT-API-06-17); `PipelineOrderTests` de TT-API-03 sin editar y en verde
- [ ] Encabezados de seguridad en 200, 401, 404, 413, 422, 429, 500 y `/health` (TC-TT-API-06-01 y 02); `Cache-Control` por defecto (03); sin `Server` (04, con el `curl -I` en el PR)
- [ ] HSTS solo en HTTPS y fuera de Development, sin `preload` (05 y 06); `X-Forwarded-Proto` solo de proxies de confianza y TC-TT-API-03-06 en verde (07)
- [ ] Redirección a HTTPS opt-in sin bucle detrás del ingress (08)
- [ ] CORS con orígenes explícitos, sin comodín ni credenciales, falla cerrada, también en errores (09 a 12); configuración validada y el arranque falla con valores inválidos (13 y 14)
- [ ] `appsettings*.json` sin orígenes reales ni secretos; valores reales solo por variables de entorno (15)
- [ ] Todos los endpoints públicos bajo límite salvo las exenciones documentadas (16); duda 8 resuelta y documentada en `docs/rate-limit-matrix.md`
- [ ] `/openapi` y `/scalar` siguen funcionando en Development con los encabezados (18)
- [ ] Amenazas de §6 con control y prueba
- [ ] `docs/MIDDLEWARES.md`, `docs/security-headers.md` y `docs/rate-limit-matrix.md` al día; sin paquetes nuevos (`*.csproj` y `packages.lock.json` sin cambios); `gitleaks` limpio
- [ ] Valores de despliegue (redes del ingress, origen del panel, `allowInsecure=false`) entregados a TT-AZ-02 sin escribirlos en el repositorio
- [ ] Build, análisis estático, formato y pruebas en verde con resultado real (Unit, Integration y Security)
- [ ] Cobertura y mutación en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
