# Checklist — TT-BD-05

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Obtener aprobación humana de la spec (preguntas (a)–(d) de §8 ya resueltas).
- [ ] Evaluar y registrar licencia de los paquetes nuevos (`MongoDB.Driver`, módulo de Testcontainers).
- [ ] Fijar las etiquetas exactas de `documentdb-local` y `mongo` y anotarlas en la evidencia.
- [ ] Ejecutar P1–P9 en local y en Azure según la spec; registrar resultados reales (pasa / falla / medición).
- [ ] Confirmar que la cadena de conexión (recibida por gestor de contraseñas) no aparece en el repositorio, logs, evidencia, PR ni prompts.
- [ ] Confirmar que `tests/Spike.DocumentDb/` no está en `Knock.slnx` ni en el CI y que los centros sintéticos están marcados como tales.
- [ ] Documentar en la evidencia el límite de conexiones medido (sin cifra oficial de Microsoft) frente a Atlas M0 (500).
- [ ] Borrar la base `knock_spike` y los dumps temporales; pedir que se quite la regla de firewall de la IP. **No** borrar el clúster `knock-docdb` (se conserva para TT-BD-06).
- [ ] Escribir `docs/evidencia/KNOCK-147-spike-documentdb.md` con la recomendación final.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace TT-BD-05` y los gates aplicables de `AGENTS.md §5`.
