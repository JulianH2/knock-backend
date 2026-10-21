# Checklist — HU-009

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Resolver las preguntas abiertas (a)–(d) de spec §8 y obtener aprobación humana.
- [ ] Esperar TT-BD-05 y TT-BD-06 antes de implementar (no en este sprint).
- [ ] Añadir y ejecutar una prueba por cada TC-HU-009 de este repositorio (TC-HU-009-02 se verifica en knock-frontend).
- [ ] Verificar con `ConsoleCapture` que las coordenadas no aparecen en logs; y, tras TT-BD-05, en ninguna colección.
- [ ] Tras TT-BD-05: verificar que la consulta por punto usa el índice fijado por el spike y tarda < 100 ms.
- [ ] Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y `docs/openapi.v1.json`.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace HU-009` y los gates de `AGENTS.md §5`; registrar resultados reales.
