# plan.md — HU-002 — Completar mi perfil la primera vez (versión mínima)

> Spec en Borrador. No se escribe código hasta que el líder la apruebe.

## 0. Orden de implementación
1. HU-001 fusionada (PR #75 reabierto): crea `usuarios`, el JWT y `GET /me`.
2. Esta historia, independiente de HU-003 y HU-004.

## 1. Mapeo de archivos
- **Reutilizar:** `Controllers/V1/MeController.cs`, `Dtos/SessionDtos.cs` (`MeResponse`), `Data/IIdentityStore.cs` e `IdentityStore.cs`, `Domain/IdentityDomain.cs`, `Hosting/ValidationExtensions.cs`, `tests/IntegrationTest/Support/`.
- **Crear:** `Domain/PersonName.cs` (normalización), `Validators/ProfileRequestValidator.cs`, `Dtos/ProfileRequest.cs`; pruebas en `tests/{UnitTest,IntegrationTest,SecurityTest,ContractTest}/Identity/`.
- **Modificar:** `MeController` (acción `Put` y respuesta ampliada de `Get`), `MeResponse`, `IIdentityStore`/`IdentityStore` (`UpdateNameAsync`) y el modelo `usuarios` (campo `nombre`).
- **No tocar:** `.github/`, `scripts/`, `docs/openapi.v1.json`.

## 2. Riesgos y efectos secundarios
- `GET /me` cambia de forma (campos aditivos): comprobar con `oasdiff`.
- `perfilCompleto` cambia de significado cuando se agreguen zona y foto: queda anotado en la spec.

## 3. Secuencia
Contrato (spec §4) → campo en `usuarios` → validador y normalización con unitarias → controller con integración y seguridad negativa → regenerar documentos vivos → `Memoria.md`.

## 4. Verificación (comandos exactos)
```bash
dotnet build Knock.slnx -c Release --no-restore -warnaserror
python scripts/sdd_lint.py --trace HU-002 && python scripts/sync_agents.py --check
python scripts/codemap.py --check && python scripts/ci/check_exceptions.py
```
