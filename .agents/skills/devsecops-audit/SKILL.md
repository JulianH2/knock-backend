---
name: devsecops-audit
description: Auditoría integral de calidad, pruebas, seguridad y DevSecOps de un repositorio de Knock por fases (inventario, propuesta priorizada, implementación incremental, verificación, informe). Úsala cuando pidan auditar el repositorio o revisar que los controles existen y funcionan.
---

# devsecops-audit

Inspecciona el estado **real** antes de proponer o cambiar nada. No supongas que faltan herramientas ni reemplaces configuraciones sin analizarlas.

## Fase 1 — Inventario (sin modificar)
- Lenguajes, frameworks, dependencias, arquitectura (`python scripts/codemap.py`), herramientas instaladas.
- Pruebas existentes por tipo; workflows (`.github/workflows`), permisos de cada job, acciones fijadas a SHA (`python scripts/ci/pin_actions.py --check`).
- Seguridad: `.semgrep/`, `.gitleaks.toml`, `security/exceptions.yaml` (`python scripts/ci/check_exceptions.py`), Dependabot, Dockerfile, IaC (Bicep), CODEOWNERS, protección de ramas.
- Riesgos: configuraciones inseguras, pruebas ausentes, controles que no fallan cuando deberían.

## Fase 2 — Informe priorizado
Tabla `| # | Hallazgo | Evidencia (archivo:línea) | Criticidad | Impacto | Probabilidad | Esfuerzo | Propuesta |` ordenada por riesgo. Dependencias entre tareas.

## Fase 3 — Implementación incremental
- Un cambio verificable por PR (rama `chore/KNOCK-N-…`), conservando trabajo existente.
- Pruebas solo si validan un comportamiento, requisito o riesgo real.
- Umbrales razonables; mutación con metas por módulo crítico (sesión 80 %, reglas de negocio 70 %), no un porcentaje global arbitrario.

## Fase 4 — Verificación
- Ejecuta cada control localmente y en CI. Un escáner sin alertas **no** prueba que el sistema sea seguro.

## Fase 5 — Informe final
Controles existentes y añadidos · vulnerabilidades y su estado · pruebas implementadas y resultados reales · comandos ejecutados · workflows creados o modificados · checks que bloquean el merge · herramientas que requieren configuración externa · riesgos, excepciones y limitaciones · cómo reproducir localmente.

## Referencias del repositorio
- Matriz de riesgos, controles y pruebas: `docs/QA_DEVSECOPS.md`
- Protección de ramas: `docs/BRANCH_PROTECTION.md`
- Excepciones: `security/exceptions.yaml` · Política: `SECURITY.md`
