# Checklist — TT-API-05

> Estado: borrador. Servicio afectado: Mercado. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (preguntas (a)–(f) de §8 resueltas por el Tech Lead).
- [ ] Evaluar licencia, mantenimiento y vulnerabilidades de Azure.Storage.Blobs y SkiaSharp; registrar el resultado en `Memoria.md`.
- [ ] Confirmar que no se modificó `deploy/docker-compose.local.yml` (Azurite solo con Testcontainers).
- [ ] Añadir y ejecutar una prueba por cada TC-TT-API-05-01 a 03 con Azurite real.
- [ ] Pruebas con Testcontainers del registro del archivo en Azure DocumentDB (índices por `propietario_id` y `estado`, `maxPoolSize` fijo): pendiente de TT-BD-06.
- [ ] Verificar que no hay contenedores públicos, CDN ni URLs con SAS en logs.
- [ ] Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `docs/openapi.v1.json`.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace TT-API-05` y los gates de `AGENTS.md §5`; registrar resultados reales.
