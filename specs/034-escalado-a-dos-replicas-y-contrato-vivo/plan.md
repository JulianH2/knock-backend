# Plan 034 — Escalado a dos réplicas y contrato vivo

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
Dos frentes independientes que pueden avanzar a la vez y se juntan en el humo final.

**Frente A — Réplicas (KNOCK-76)**
1. Pruebas de Terraform de perfiles y sondas (`deploy/tests/`), y ajustes del módulo `service` (gracia, sonda de vida).
2. Apagado ordenado en los cuatro proyectos y sus pruebas.
3. `ci-deploy.yml`: `workflow_dispatch` con `image_tag` validado y entorno `evaluacion`.
4. Script k6 de despliegue sin corte y consulta del reparto en Application Insights.
5. Ensayo de reversión y humo con dos réplicas; documentación y evidencia.

**Frente B — Contrato vivo (KNOCK-137)**
1. Contrato con `ProblemDetails` completo y `X-Correlation-Id` declarado en los servicios.
2. `export_openapi.py`: Mensajería, contrato público combinado, conflictos y comprobación de datos internos, con pruebas.
3. CI del PR: regenerar todos los contratos y `oasdiff` del público con la etiqueta `contrato-incompatible`.
4. `tests/ContractTest` por servicio.
5. GitHub App y `contract-publish.yml`; primera ejecución real.

## Archivos que se crean o cambian
- `deploy/modules/service/main.tf`, `deploy/tests/replicas_evaluacion.tftest.hcl`, `deploy/tests/sondas.tftest.hcl`, `deploy/README.md`.
- `src/*/Hosting/` (apagado ordenado y disponibilidad) en los cuatro proyectos.
- `.github/workflows/ci-deploy.yml`, `.github/workflows/ci-pr.yml`, `.github/workflows/contract-publish.yml` (nuevo).
- `scripts/export_openapi.py` y `scripts/test_export_openapi.py`.
- `tests/ContractTest/` (fábricas por servicio, lector de contrato, validador), `tests/PerformanceTest/k6/despliegue-sin-corte.js`.
- `docs/openapi.publico.v1.json` (generado), `docs/OBSERVABILIDAD.md`, `docs/evidencia/KNOCK-76-reversion.md`, `docs/VERSIONES_DESPLEGADAS.md`, `Memoria.md`.
- Archivos protegidos para agentes (`.github/`, `scripts/`, `docs/openapi*.json`): los cambia una persona.

## Dependencias
- Specs 005, 006, 007, 008, 011, 012 y 035 (redes de confianza antes de dos réplicas).
- La GitHub App la crea el líder; el entorno `contrato` y `evaluacion` se configuran en GitHub.
- Confirmación del equipo de frontend de que `contracts/openapi.v1.json` pasa a ser el contrato combinado.

## Riesgos del plan
- El humo y el ensayo de reversión consumen crédito y requieren el entorno real: se agendan el 20 y el 23 de noviembre.
- Cambiar `ProblemDetails` en el contrato puede marcar cambios incompatibles: el primer PR del frente B lleva la etiqueta `contrato-incompatible` con la justificación.

## Decisiones de la spec que este plan sigue
- Réplicas fijas solo en la ventana; reversión por etiqueta de imagen; contrato público combinado; el backend empuja el contrato con una GitHub App acotada; sin Front Door ni WAF.

## Verificación
- `dotnet build Knock.slnx -c Release`, `dotnet test Knock.slnx -c Release`, `dotnet format Knock.slnx --verify-no-changes`.
- `terraform validate`, `terraform test` y `checkov -d deploy`.
- `python scripts/test_export_openapi.py`, `python scripts/export_openapi.py --publico` y `git diff` limpio.
- `actionlint` sobre los flujos; `k6 run` del despliegue sin corte; `python scripts/sdd_lint.py --trace 034`.
