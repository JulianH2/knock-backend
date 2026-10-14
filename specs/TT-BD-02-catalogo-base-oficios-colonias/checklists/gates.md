# Checklist — TT-BD-02

> Estado: borrador de preparación. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] Preservar la aprobación humana existente; no cambiar estado, revisor ni fecha.
- [ ] Verificar y documentar fuente, fecha/licencia y CP SEPOMEX de Tula; no usar `42000`.
- [ ] Documentar como aproximados los 8–12 polígonos piloto, autor y fecha; incluir centros para el resto.
- [ ] Implementar esquema `catalog` con `geography(...,4326)` e índices GiST, oficios y seeds idempotentes.
- [ ] Implementar consulta por punto, respaldo de 2 km, fuera de cobertura, consulta por CP, privacidad, orientación de ejes y desempate en borde.
- [ ] Añadir y ejecutar una prueba con PostGIS/Testcontainers por cada TC-TT-BD-02-01 a TC-TT-BD-02-09.
- [ ] Ejecutar `python scripts/sdd_lint.py --trace TT-BD-02` y registrar los resultados reales.
- [ ] Adjuntar evidencia del mapa de polígonos y la salida real de las pruebas cuando se implemente la tarea.
- [ ] Ejecutar los gates aplicables de `AGENTS.md §5` y registrar cada resultado.
