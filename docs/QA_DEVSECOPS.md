# Calidad, pruebas y DevSecOps — knock-backend

> Matriz de controles realmente configurados en este repositorio. Si cambias una herramienta, actualiza esta tabla en el mismo PR.
> Nivel: **PR** = `ci-pr.yml` (rápido, bloquea vía `gate`) · **Nightly** = `nightly.yml` (exhaustivo, abre issue si falla) · **Deploy** = `deploy-staging.yml` / `deploy.yml` · **Local** = git hooks y guardián de agentes.

## 1. Comparación con los repositorios de referencia del profesor

| Práctica en los repos del profesor | Dónde la usan | En knock-backend |
|---|---|---|
| Unit, Integration (Testcontainers), Security, Database, Contract | WebAPIDevSecOpsScalling, API_SDD | ✅ mismas 5 suites (con PostGIS, Redis y Azurite) |
| Mutación con Stryker (PR incremental + nightly) | Scalling, ScallingSDD, API_SDD | ✅ PR informativo `--since` + nightly con umbral por módulo |
| Property-based (FsCheck) | Scalling | ✅ en la estrategia de unitarias |
| Pact / contrato | Scalling | ✅ captura del wire + OpenAPI + oasdiff (+ clientes generados en frontend) |
| Rendimiento (NBomber) | Scalling | ✅ k6 (lo fija la propuesta v2.0): humo, carga y estrés |
| Fuzzing (RESTler) | Scalling | ✅ RESTler nightly |
| Caos (caída de Redis/SQL, latencia) | Scalling | ✅ caída de Redis y PostgreSQL con recuperación |
| SAST: Semgrep, SonarAnalyzer, SonarCloud, CodeQL | todos | ✅ Semgrep con reglas de Knock, SonarAnalyzer + analizadores .NET como error, CodeQL, SonarCloud opcional |
| SCA: NuGetAudit, Dependabot, dependency-review, Snyk | todos | ✅ NuGetAudit, Dependabot, dependency-review con licencias, OSV-Scanner (en lugar de Snyk, sin cuenta) |
| Secretos: Gitleaks | WebDevSecOps | ✅ Gitleaks en PR + pre-commit + push protection de GitHub |
| Contenedores: Trivy, Dockle, Cosign, SBOM | Scalling, WebDevSecOps | ✅ Trivy, Dockle, SBOM y procedencia en la imagen, firma Cosign y verificación antes de desplegar |
| DAST: ZAP | Scalling, WebDevSecOps | ✅ ZAP baseline en PR + API scan nightly |
| Gates SDD (critic, endpoints, sdd-lint, cobertura) | API_SDD | ✅ sdd_lint + trazabilidad TC, mapa de código, excepciones, cobertura y actionlint |
| Agentes con skills y revisores sin edición | API_SDD (.opencode) | ✅ para 6 herramientas + hooks que bloquean |
| — | — | ➕ Nuevo: arquitectura (NetArchTest + codemap), duplicados, Checkov (IaC/Docker/Actions), sqlfluff, pinning a SHA, registro de excepciones, reglas de ramas/commits/PR |

## 2. Pruebas

| Tipo | Valida | Herramienta | Dónde | Nivel | Bloquea |
|---|---|---|---|---|---|
| Unitarias | Reglas de dominio, servicios, validadores | xUnit v2, AwesomeAssertions, FsCheck | `tests/UnitTest` | PR | Sí |
| Arquitectura | Fronteras de módulo, pipeline, controllers sin DbContext | NetArchTest.eNhancedEdition + `codemap.py --check` | `tests/UnitTest/Architecture` | PR + Local | Sí |
| Integración | Endpoints reales contra base y caché reales | WebApplicationFactory + Testcontainers | `tests/IntegrationTest` | PR | Sí |
| Seguridad negativa | 401/403/404 ajenos, roles, 429, entradas maliciosas, headers | xUnit + WAF | `tests/SecurityTest` | PR | Sí |
| Base de datos | Migraciones, semillas idempotentes, restricciones, PostGIS, respaldo | Testcontainers `postgis/postgis` | `tests/DatabaseTest` | PR | Sí |
| Contrato | JSON real, OpenAPI al día, sin cambios incompatibles | Captura + `export_openapi.py` + oasdiff | `tests/ContractTest`, `docs/openapi.v1.json` | PR | Sí |
| Aceptación | Un caso `TC-<ID>-KK` por criterio | Suites de arriba + `sdd_lint --trace` | specs | PR | Sí |
| Smoke | Salud, auth exigida, headers | `tests/smoke/smoke.sh` | PR (pila CI) + Deploy | Sí |
| Mutación | Que las pruebas detecten errores | Stryker.NET | `stryker-config.json` | PR (informativo) · Nightly (Identity ≥ 80 %, negocio ≥ 70 %) | Nightly |
| Fuzzing | 500, fugas, acceso a recursos ajenos | RESTler | `tests/fuzz` | Nightly | Nightly |
| Rendimiento | p95, 2 instancias ≥ 1.6× 1, degradación bajo estrés | k6 | `tests/perf` | Nightly | Nightly |
| Caos | Degradación controlada y recuperación | Docker Compose | `tests/chaos` | Nightly | Nightly |

Cobertura mínima: **60 % líneas, 50 % ramas** (combinada, `check_coverage.py`).

## 3. Seguridad y cadena de suministro

| Control | Herramienta | Nivel | Bloquea |
|---|---|---|---|
| SAST semántico | CodeQL `security-extended` (C# y Actions) | PR + semanal | Sí (check requerido) |
| SAST con reglas de Knock | Semgrep (`.semgrep/semgrep.yaml` + p/csharp, p/secrets, p/owasp-top-ten, p/dockerfile) | PR | Sí (ERROR) |
| Analizadores .NET + SonarAnalyzer como error | `Directory.Build.props`, `.editorconfig` | PR + Local | Sí |
| Dependencias vulnerables | NuGetAudit, `dotnet list --vulnerable`, OSV-Scanner, dependency-review | PR | Sí (altas/críticas) |
| Licencias | dependency-review (deny GPL/AGPL/SSPL) + OSV nightly | PR + Nightly | Sí |
| Dependencias obsoletas | `dotnet list --deprecated`, Dependabot | PR (informativo) + semanal | No |
| Secretos | Gitleaks (PR y pre-commit), push protection | PR + Local | Sí |
| Contenedor | Trivy (críticas/altas con fix), Dockle | PR + Deploy | Sí |
| IaC, Dockerfile, workflows | Checkov, actionlint, pinning a SHA | PR | Sí |
| DAST | ZAP baseline (PR), ZAP API scan (nightly) | PR + Nightly | Sí |
| SQL | sqlfluff | PR + Local | Sí |
| Firma y procedencia | SBOM + provenance (BuildKit), Cosign keyless, verificación antes de desplegar | Deploy | Sí |
| Excepciones | `check_exceptions.py` (vencidas, supresiones sin EXC) | PR + Local | Sí |

## 4. OWASP API Security Top 10 → control → prueba

| Riesgo | Control en Knock | Prueba |
|---|---|---|
| API1 Autorización a nivel de objeto | Servicio filtra por dueño; técnico solo ve colonia | SecurityTest: recurso ajeno → 404 |
| API2 Autenticación rota | Firebase validado en servidor, JWT 15 min, refresh rotativo con reuso, blacklist | IntegrationTest de sesión, token alterado/vencido → 401 |
| API3 Propiedades expuestas | DTOs explícitos; teléfono enmascarado | ContractTest (campos exactos) |
| API4 Consumo sin límites | Rate limits, paginación máxima, tamaño de cuerpo | SecurityTest → 429 / 413 |
| API5 Autorización por función | Políticas por permiso en el panel | SecurityTest: otro rol → 403 |
| API6 Flujos de negocio sensibles | Oleadas con tope, máx. 5 cotizaciones, idempotencia | IntegrationTest de casos TC |
| API7 SSRF | Sin URLs del usuario; lista permitida | Semgrep + revisión |
| API8 Mala configuración | Pipeline fijo, headers, CORS de un origen, ProblemDetails | Smoke + ZAP + PipelineOrder |
| API9 Inventario | `docs/endpoints.md` vs controllers, versionado `/api/v1` | `check_endpoints` en CI (TT-API-01) |
| API10 Consumo inseguro de APIs | Firebase/Azure con SDK oficial, timeouts, Polly | IntegrationTest con fallos simulados |

## 5. Matriz de riesgos

| Riesgo | Impacto | Prob. | Control | Verificación |
|---|---|---|---|---|
| Fuga de biométricos | Crítico | Media | Llave por verificación, borrado 7 días, permiso + 2FA, registro de accesos | Prueba del borrado programado, 403 a otros roles |
| Acceso a datos de otro usuario | Alto | Media | Autorización por recurso | SecurityTest por endpoint |
| Duplicar eventos o perderlos con 2 instancias | Alto | Media | Outbox + consumidores idempotentes | IntegrationTest con 2 consumidores, caos |
| Inyección SQL | Alto | Baja | EF parametrizado, Semgrep, CA2100 | Semgrep + RESTler |
| Secretos en el repo | Alto | Media | Key Vault, Gitleaks, push protection, guardián | Gitleaks |
| Dependencia vulnerable | Medio | Alta | NuGetAudit, OSV, Dependabot | CI |
| Agente rompe arquitectura o duplica código | Medio | Alta | Guardián, codemap, NetArchTest, revisión | `codemap --check` |

## 6. Checks que bloquean el merge
`gate` (pr-meta, governance, build-test, sast, sca, secrets, openapi-diff, sql-lint, iac, container, dast) y `CodeQL`. Configuración: `docs/BRANCH_PROTECTION.md`.

## 7. Requiere configuración externa
Azure (OIDC, recursos, Key Vault) · Firebase (proyecto y números de prueba) · `FRONTEND_CONTRACT_TOKEN` · SonarCloud (opcional) · `ANTHROPIC_API_KEY` (revisión IA opcional) · protección de ramas y environments.

## 8. Limitaciones conocidas
- Hasta TT-API-01 no hay código: los jobs de API se saltan y la cobertura no se mide.
- La imagen de RESTler se usa como en los repos de referencia (`:latest`); fijarla por digest al validarla.
- k6, RESTler, ZAP API y caos corren de noche: un fallo no bloquea el PR, abre un issue.
