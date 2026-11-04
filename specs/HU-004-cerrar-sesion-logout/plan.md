# plan.md — HU-004 — Cerrar sesión

> Spec en Borrador. No se escribe código hasta que el líder la apruebe.

## 0. Orden de implementación
1. HU-001 (reabrir y fusionar el PR #75) y HU-003 (`refresh_tokens` ampliada y rotación): esta historia revoca la misma familia.
2. Esta historia. La parte de Redis solo si el líder aprueba el paquete (spec §8, duda 1).

## 1. Mapeo de archivos
- **Reutilizar:** `Controllers/V1/AuthController.cs`, `Data/IIdentityStore.cs` e `IdentityStore.cs` (revocación de familia de HU-003), `RefreshTokenFingerprint.cs`, `Hosting/ValidationExtensions.cs`, `tests/IntegrationTest/Support/`.
- **Crear:** `Validators/LogoutRequestValidator.cs`, `Dtos/LogoutRequest.cs`; pruebas en `tests/{UnitTest,IntegrationTest,SecurityTest,ContractTest}/Identity/`. **Solo si se aprueba Redis:** `Services/AccessTokenRevocation.cs` (interfaz con implementación nula y de Redis) y su registro condicional por `ConnectionStrings:KnockRedis`.
- **Modificar:** `AuthController` (acción `Logout`), `IdentityModuleExtensions` (registro condicional) y la validación del JWT (evento `OnTokenValidated`) únicamente si hay Redis.
- **No tocar:** `.github/`, `scripts/`, `docs/openapi.v1.json`. Un paquete de Redis necesita evaluación (licencia, mantenimiento, vulnerabilidades) en el PR y en `Memoria.md`.

## 2. Riesgos y efectos secundarios
- `AuthController` es compartido con HU-003: fusionar en orden para no chocar.
- Sin Redis el access token vale 15 min tras el cierre: queda escrito y probado.
- Fallar abierto si Redis cae (duda 3) debe quedar registrado, no silencioso.

## 3. Secuencia
Contrato (spec §4) → revocación de la familia → controller con integración y seguridad negativa → (opcional) Redis → regenerar documentos vivos → `Memoria.md`.

## 4. Verificación (comandos exactos)
```bash
dotnet build Knock.slnx -c Release --no-restore -warnaserror
python scripts/sdd_lint.py --trace HU-004 && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
