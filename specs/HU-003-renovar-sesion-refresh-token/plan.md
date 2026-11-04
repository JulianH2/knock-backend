# plan.md — HU-003 — Mantener mi sesión segura (renovación del token)

> Spec en Borrador. No se escribe código hasta que el líder la apruebe.

## 0. Orden de implementación
1. Reabrir y fusionar HU-001 (PR #75, rama `feature/KNOCK-43-phone-login-code`): sin ella no existe `refresh_tokens` ni el JWT.
2. Esta historia (`feature/KNOCK-45-…`), antes de HU-004 (que revoca la misma familia).

## 1. Mapeo de archivos
- **Reutilizar:** `Services/SessionTokenIssuer.cs`, `Services/RefreshTokenFingerprint.cs`, `Controllers/V1/AuthController.cs`, `Dtos/SessionDtos.cs`, `Data/IIdentityStore.cs` e `IdentityStore.cs`, `Hosting/ValidationExtensions.cs` y los ayudantes de pruebas de `tests/IntegrationTest/Support/` (de la rama de HU-001).
- **Crear:** `Services/SessionRefreshService.cs`, `Validators/RefreshRequestValidator.cs`, `Dtos/RefreshRequest.cs`; pruebas en `tests/{UnitTest,IntegrationTest,SecurityTest,DatabaseTest,ContractTest}/Identity/`.
- **Modificar:** `AuthController` (acción `Refresh`), `IIdentityStore`/`IdentityStore` (rotación y revocación de familia), el inicializador de índices (`{ familiaId: 1 }`).
- **No tocar:** `.github/`, `scripts/`, `docs/openapi.v1.json` (protegidos).

## 2. Riesgos y efectos secundarios
- Mongo transaccional: la rotación depende de transacciones (confirmadas en el spike P4); probar con `documentdb-local`.
- Un cambio en `AuthController` choca con HU-004 (misma clase): coordinar el orden de fusión.
- Revocar la familia es destructivo para la sesión: probar el reuso con un reloj de pruebas.

## 3. Secuencia
Contrato (spec §4) → campos e índices → dominio y servicio con unitarias → controller con integración y seguridad negativa → regenerar documentos vivos → `Memoria.md`.

## 4. Verificación (comandos exactos)
```bash
dotnet build Knock.slnx -c Release --no-restore -warnaserror
python scripts/sdd_lint.py --trace HU-003 && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
