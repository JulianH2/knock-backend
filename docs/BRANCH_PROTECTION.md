# Protección de ramas y reglas de merge (configuración manual en GitHub)

Los agentes y el CI no pueden activar esto: lo hace una persona con rol **Admin** en cada repositorio
(`knock-backend` y `knock-frontend`). Sin esto, los checks son solo informativos.

## 1. Ruleset para ramas fijas
**Settings → Rules → Rulesets → New branch ruleset** · Nombre: `ramas-fijas` · Enforcement: **Active**
Target branches: `main`, `release`, `develop` y el patrón `sprint-*`.

Activar:
- [x] **Restrict deletions**
- [x] **Block force pushes**
- [x] **Require linear history** (opcional: solo si el equipo hace *squash merge*)
- [x] **Require a pull request before merging**
  - Required approvals: **1**
  - [x] Dismiss stale pull request approvals when new commits are pushed
  - [x] Require review from Code Owners
  - [x] Require approval of the most recent reviewable push
  - [x] Require conversation resolution before merging
- [x] **Require status checks to pass**
  - [x] Require branches to be up to date before merging
  - Checks requeridos: **`gate`** (ci-pr) y **`CodeQL (actions)`**; en backend además `CodeQL (csharp)`, en frontend `CodeQL (javascript-typescript)` cuando exista el código.
- [x] **Require signed commits** (recomendado)
- Bypass list: **vacía** (nadie se salta las reglas, tampoco los admins).

## 2. Environments
**Settings → Environments**
- `staging`: sin uso (no hay entorno de Staging desplegado). Puede quedar vacío; el entorno de GitHub no cuesta nada.
- `production`: **Required reviewers** = Tech Lead + 1; *Deployment branches*: solo `main` y tags `api-v*` / `app-v*` / `panel-v*`; **Prevent self-review**.

## 3. Acciones y permisos
**Settings → Actions → General**
- Allow actions: **Allow select actions** (GitHub, verificadas y las usadas en los workflows).
- Workflow permissions: **Read repository contents** (cada job pide lo que necesita).
- [x] Require approval for all outside collaborators.

## 4. Seguridad del código
**Settings → Code security**: activar Dependency graph, Dependabot alerts, Dependabot security updates, **Secret scanning** y **Push protection**, Code scanning (CodeQL avanzado, ya hay workflow) y **Private vulnerability reporting**.

## 5. Secretos y variables (solo cuando se habiliten)
| Nombre | Tipo | Repo | Para qué |
|---|---|---|---|
| `AZURE_ENABLED` / `DEPLOY_ENABLED` | variable | backend / frontend | Activa los workflows de despliegue |
| `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | variables | backend | OIDC con Azure (sin secretos de larga vida) |
| `PRODUCTION_*` (`STAGING_*` sin uso) | variables | ambos | Recursos y URLs |
| `FRONTEND_CONTRACT_TOKEN` | secreto | backend | PR automático del contrato en knock-frontend (token de grano fino, solo ese repo) |
| `AI_REVIEW_API_KEY` + `AI_REVIEW=true` | secreto + variable | ambos | Pre-revisión automática opcional (DeepSeek por defecto; `AI_REVIEW_BASE_URL` y `AI_REVIEW_MODEL` opcionales) |
| `SONAR_TOKEN` + `SONAR_ENABLED=true` | secreto + variable | backend | SonarCloud opcional |
| Firma Android y Play | secretos del environment | frontend | Builds firmados y subida a Play |

## 6. Comprobación
Abre un PR de prueba desde una rama mal nombrada (`prueba`) hacia `sprint-00`: el check `gate` debe fallar en `pr-meta` y el botón de merge debe quedar bloqueado.
