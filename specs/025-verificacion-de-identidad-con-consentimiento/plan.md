# Plan 025 — Verificación de identidad con consentimiento

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Escribir el script de `db/` (base `identity`) con `verificaciones` y `consentimientos` y sus índices, incluido el único parcial sobre `curpVigente`.
2. Crear el módulo `Trust` en Identidad como rebanada vertical: dominio (estados, niveles, motivos), validadores (CURP, nivel, consentimiento), servicios y almacenes.
3. Escribir `CurpHmac` y `CifradoDeVerificacion` (llave de datos por verificación envuelta con Key Vault, AES-256-GCM sobre el blob).
4. Escribir `FotoVerificacionStore` (cuarentena y definitivo en la cuenta de almacenamiento de verificación) con validación por contenido.
5. Exponer los tres controllers y el servicio de vencimiento (cada hora, con `tareas_programadas` para no repetir entre réplicas).
6. Dejar `VerificacionDecisionService` sin endpoint para HU-067.

## Archivos que se crean o cambian
- `db/025-identity-verificaciones.js`
- `src/Knock.Identity.Api/Modules/Trust/` (`Controllers/`, `Dtos/`, `Validators/`, `Services/`, `Domain/`, `Data/`, `Events/`)
- `src/Knock.Identity.Api/Program.cs` (`AddTrustModule()`), `appsettings.json` (`Verificacion:*`, sin secretos)
- `tests/UnitTest/Architecture/ModuleCatalog.cs` y clases `Spec025*`
- `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `docs/openapi.identity.v1.json` (regenerado por script), `Memoria.md`

## Dependencias
- Specs 003, 009, 013, 015, 016 y 017; cuenta de almacenamiento de verificación y llaves en Key Vault (spec 005); Azurite y un doble de Key Vault en pruebas.

## Orden y paralelismo
- Esta spec no depende de la cadena de cotizaciones: se puede trabajar en paralelo con 020 a 023.
- T-01, T-02 y T-03 a la vez; T-04 después de T-03; T-05 y T-06 en paralelo cuando haya T-02 y T-04.

## Riesgos
- Sin HU-067 nada llega a `verificada`; la demo necesita una semilla o una decisión manual.

## Verificación
- `python scripts/sdd_lint.py --trace 025` sin faltantes; comandos de `AGENTS.md`; `python scripts/export_openapi.py --service identity`; `Memoria.md` al día.
