# Checklist — HU-009

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Resolver las preguntas abiertas (a)–(e) de spec §8 y obtener aprobación humana.
- [ ] Añadir y ejecutar una prueba por cada TC-HU-009 de este repositorio (TC-HU-009-02 se verifica en knock-frontend).
- [ ] Verificar con `EXPLAIN` que la consulta por punto usa GiST y tarda < 100 ms.
- [ ] Verificar que las coordenadas no aparecen en tablas ni logs.
- [ ] Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `docs/openapi.v1.json`.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace HU-009` y los gates de `AGENTS.md §5`; registrar resultados reales.
