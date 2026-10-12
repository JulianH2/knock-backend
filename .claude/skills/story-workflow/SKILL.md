---
name: story-workflow
description: Flujo obligatorio de Knock para implementar cualquier historia, tarea técnica, bug, refactor o cambio de arquitectura - leer, analizar amenazas, planear pruebas por criterio, implementar, verificar de verdad e informar con honestidad. Úsala siempre antes de cambiar código.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/story-workflow/SKILL.md. No editar a mano. -->
# story-workflow

Aplica a historias (HU-NNN), tareas técnicas (TT-ÁREA-NN), defectos (BUG-NNN), refactors y cambios de arquitectura.

## 1. Leer
- `Memoria.md`, `AGENTS.md`, la spec `specs/<ID>-*/spec.md` (debe estar **Aprobada** y tener su clave KNOCK-N) y su ficha.
- El código relacionado: `python scripts/codemap.py --query "<tema>"` y `docs/architecture/CODEMAP.md`.

## 2. Entender
- Comportamiento esperado por cada caso `TC-<ID>-KK` y efectos secundarios (qué más usa lo que vas a tocar: `docs/architecture/graph.html` o `--query`).

## 3. Amenazas (proporcional al riesgo)
- Cambio de texto o estilo: basta confirmar que no expone datos.
- Endpoint, permisos, datos personales, archivos, pagos o IA: STRIDE breve en la spec §6 (suplantación, alteración, repudio, fuga, denegación, elevación) con control y caso que lo prueba.

## 4. Riesgos
- Datos (migraciones, borrados), permisos (autorización por recurso), dependencias (no agregues sin evaluar), arquitectura (fronteras de módulo/feature).

## 5. Plan de pruebas
- Para cada caso: qué prueba lo verifica y en qué suite. Justifica los tipos que no aplican (no hagas pruebas de relleno).
- Las pruebas deben fallar si la lógica está mal: aserciones sobre resultado y efectos, no solo «no lanza excepción».

## 6. Implementar
- Reutiliza antes de crear (skill `reuse-before-create`). Sigue el molde del repositorio y las reglas por ruta.

## 7. Verificar (de verdad)
- Ejecuta lo que el cambio exige: build, formato, análisis estático/tipos, pruebas de la suite afectada, `scripts/sdd_lint.py --trace <ID>`, `scripts/codemap.py --check`, `scripts/ci/check_exceptions.py`.
- Si una herramienta no está disponible o no se puede ejecutar, **dilo**; no lo des por pasado.

## 8. Revisar el diff
- Secretos, datos personales, cambios accidentales, archivos generados editados a mano, pruebas desactivadas, umbrales bajados, supresiones sin `EXC-NNN`.

## 9. Informar (en tu respuesta y en el PR)
```
### Informe — <ID> (KNOCK-N)
Ejecutado: <comando> → <resultado real>  (uno por línea)
Pasó: …
Falló / no se pudo ejecutar: … (motivo)
Casos cubiertos: TC-… → prueba …
Amenazas y controles: …
Pendiente / riesgos: …
Uso de IA: herramienta, qué generó, cómo se validó
```
Actualiza `Memoria.md` (estado, decisiones con porqué, errores a evitar).

## Prohibido
Omitir pruebas para acelerar · desactivar reglas, escáneres o workflows · cambiar pruebas para ocultar fallos · agregar dependencias sin evaluar · exponer secretos o datos reales · afirmar que algo pasó sin ejecutarlo · marcar una vulnerabilidad como resuelta sin verificar · cambios destructivos, despliegues u operaciones sobre producción sin autorización explícita.
