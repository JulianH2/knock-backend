# Propuesta de rulesets y comprobaciones obligatorias (KNOCK-233)

> Estado: **propuesta, sin aplicar**. Decisión del líder (2026-11-09): no aplicar rulesets mientras la organización tenga plan gratuito y los repositorios sean privados (GitHub no los hace cumplir y la API responde 403). Los archivos de `.github/rulesets/` no se modifican en este PR (zona protegida).

## Comprobación obligatoria recomendada: solo `gate`

| Check (nombre exacto en GitHub) | Workflow / job | ¿Obligatorio? | Por qué |
|---|---|---|---|
| `gate` | `ci-pr.yml` / `gate` | **Sí** | Corre siempre (`if: always()`) y falla si cualquier verificación bloqueante falla; su nombre no cambia aunque se agreguen o quiten jobs. |
| `Detectar proyecto, cambios y spec` | `ci-pr.yml` / `detect` | No | Lo cubre `gate`. |
| `Rama, commits y PR (branch-name)` | `ci-pr.yml` / `pr-meta` | No | Lo cubre `gate`; hoy se salta sin `ENFORCE_BRANCH_RULES=true`. |
| `Etiqueta de puntos del PR (spec 012)` | `ci-pr.yml` / `pr-points` | No | Lo cubre `gate`. |
| `governance / Gobierno SDD y arquitectura` | `_governance.yml` | No | Lo cubre `gate`. |
| `API — build, formato y pruebas` | `ci-pr.yml` / `build-test` | No | Lo cubre `gate`. |
| `SAST (Semgrep con reglas de Knock)` | `ci-pr.yml` / `sast` | No | Lo cubre `gate`. |
| `SCA (dependencias vulnerables, obsoletas y licencias)` | `ci-pr.yml` / `sca` | No | Lo cubre `gate`. |
| `Secretos (Gitleaks)` | `ci-pr.yml` / `secrets` | No | Lo cubre `gate`. |
| `Cambios incompatibles en el contrato (oasdiff)` | `ci-pr.yml` / `openapi-diff` | No | Lo cubre `gate`. |
| `IaC, Dockerfile y workflows (Checkov)` | `ci-pr.yml` / `iac` | No | Lo cubre `gate`. |
| `Contenedor (build, Trivy, Dockle) (…)` | `ci-pr.yml` / `container` | No | Su nombre lleva el servicio (matriz): **no es estable**; lo cubre `gate`. |
| `Smoke + DAST (ZAP baseline)` | `ci-pr.yml` / `dast` | No | Lo cubre `gate`; hoy nunca corre (BUG-001, KNOCK-238). |
| `Mutación incremental (Stryker, informativa)` | `ci-pr.yml` / `mutation-pr` | No | Informativo; `gate` no depende de él. |

Exigir cada job por separado obligaría a tocar el ruleset cada vez que cambie un nombre o la matriz de contenedores; `gate` agrupa todo con un nombre estable. Ojo: `gate` acepta `success` y `skipped`, así que un job que se salte por una condición mal escrita también pasa; hoy las condiciones dependen de `detect`, pero conviene revisarlas al cambiarlas.

## Diff propuesto

### `.github/rulesets/main.json`
En la regla `required_status_checks`, la lista hoy está vacía:
```diff
       "required_status_checks": [
+        { "context": "gate", "integration_id": 15368 }
       ]
```
(`15368` es el id de la integración de GitHub Actions; ata el check a Actions para que nadie lo «complete» desde otra app).

### `.github/rulesets/develop.json`
No tiene la regla de comprobaciones; se agrega completa en `rules`:
```diff
+    {
+      "type": "required_status_checks",
+      "parameters": {
+        "strict_required_status_checks_policy": false,
+        "do_not_enforce_on_create": false,
+        "required_status_checks": [
+          { "context": "gate", "integration_id": 15368 }
+        ]
+      }
+    }
```

Al aplicarlo, la regla anterior de `rules` debe terminar en coma (`}` pasa a `},`); si no, el JSON no se puede leer.

### Ramas `sprint-*`
Hoy no tienen ruleset. Si se quiere la misma protección, un ruleset nuevo con `ref_name.include: ["refs/heads/sprint-*"]`, las reglas `deletion`, `non_fast_forward`, `pull_request` (1 aprobación) y `required_status_checks` con `gate`.

## Diferencia detectada en la documentación
`.github/rulesets/README.md` dice que `main` y `develop` descartan aprobaciones con commits nuevos y exigen hilos resueltos, pero los JSON tienen `dismiss_stale_reviews_on_push: false` y `required_review_thread_resolution: false` (y la sección «Revisión de pares» del mismo README confirma que es intencional). Conviene corregir la tabla del README cuando se toque esa carpeta.

## Cómo aplicarlo (cuando haya plan compatible y rol de administrador)
1. Aplicar el diff anterior a los JSON de `.github/rulesets/` en un PR del líder.
2. GitHub: **Settings → Rules → Rulesets → Import a ruleset**, uno por archivo.
3. Comprobar en un PR de prueba que el botón de fusionar se bloquea mientras `gate` no esté en verde.
