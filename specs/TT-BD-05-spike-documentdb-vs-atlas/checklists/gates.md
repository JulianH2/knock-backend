# Checklist — TT-BD-05

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Resolver las preguntas abiertas (a)–(d) de spec §8 y obtener aprobación humana.
- [ ] Evaluar y registrar licencia de los paquetes nuevos (`MongoDB.Driver`, módulo de Testcontainers).
- [ ] Fijar las etiquetas exactas de `documentdb-local` y `mongo` y anotarlas en la evidencia.
- [ ] Ejecutar P1–P9 en local y en Azure según la spec; registrar resultados reales (pasa / falla / medición).
- [ ] Confirmar que la cadena de conexión no aparece en el repositorio, logs, evidencia, PR ni prompts.
- [ ] Borrar la base `knock_spike`, los dumps temporales y la regla de firewall (o documentar por qué se conservan).
- [ ] Escribir `docs/evidencia/KNOCK-147-spike-documentdb.md` con la recomendación final.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace TT-BD-05` y los gates aplicables de `AGENTS.md §5`.
