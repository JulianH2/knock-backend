# Checklist — HU-009

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (preguntas (a)–(d) de §8 resueltas por el Tech Lead).
- [ ] Confirmar que se fusionó #48 (evidencia del spike TT-BD-05) antes de implementar.
- [ ] Esperar TT-BD-06 antes de implementar (no en este sprint).
- [ ] Confirmar que este cambio no define una ruta de colonia por punto (es `/colonias/en-punto` de TT-API-07) ni `PUT /me/zona` (Identidad).
- [ ] Añadir y ejecutar una prueba por cada TC-HU-009 de este repositorio (TC-HU-009-02 se verifica en knock-frontend).
- [ ] Integración con Testcontainers `documentdb-local`: índice único de `lista_espera` y ningún documento con coordenadas.
- [ ] Verificar `maxPoolSize` fijo en la conexión del servicio.
- [ ] Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `docs/openapi.v1.json`.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace HU-009` y los gates de `AGENTS.md §5`; registrar resultados reales.
