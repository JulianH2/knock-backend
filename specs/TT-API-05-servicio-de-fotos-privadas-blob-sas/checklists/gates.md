# Checklist — TT-API-05

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Resolver las preguntas abiertas (a)–(e) de spec §8 y obtener aprobación humana.
- [ ] Evaluar y registrar licencia de los paquetes nuevos (Azure.Storage.Blobs y librería de imágenes).
- [ ] Añadir y ejecutar una prueba por cada TC-TT-API-05-01 a 03 con Azurite real.
- [ ] Verificar que no hay contenedores públicos ni URLs con SAS en logs.
- [ ] Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `docs/openapi.v1.json`.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace TT-API-05` y los gates de `AGENTS.md §5`; registrar resultados reales.
