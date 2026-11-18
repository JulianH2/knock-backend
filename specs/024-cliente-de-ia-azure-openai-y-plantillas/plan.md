# Plan 024 — Cliente de IA con Azure OpenAI y plantillas locales de respaldo

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec. **La implementación es del Sprint 5**; en el Sprint 4 solo se cierran la decisión del tope y el ADR.

## Enfoque
1. Decidir el tope de tokens y actualizar el ADR-008 (Azure OpenAI con plantillas locales de respaldo).
2. Crear el módulo `Assistant` con `IAiClient` y la prueba de arquitectura que impide referenciar el SDK fuera de él.
3. Escribir `FiltroDatosPersonales` (puro, con mutación) y el validador de imágenes por `archivoId`.
4. Escribir `ConsumoIaStore` (reserva atómica en Redis con script) y `UsoIaStore` (MongoDB).
5. Escribir `ProveedorIa` (SDK, tiempo máximo, un reintento, cortacircuito) y `PlantillaIa` (plantillas versionadas).
6. Integrar todo en `AiClient` con el apagado por omisión.

## Archivos que se crean o cambian
- `db/024-assistant-uso-ia.js`
- `src/Knock.Market.Api/Modules/Assistant/` (`Services/IAiClient.cs`, `AiClient.cs`, `FiltroDatosPersonales.cs`, `ProveedorIa.cs`, `PlantillaIa.cs`; `Data/ConsumoIaStore.cs`, `UsoIaStore.cs`; `Dtos/`, `Validators/`, `Prompts/`, `Plantillas/`)
- `src/Knock.Market.Api/Program.cs` (`AddAssistantModule()`), `appsettings.json` (`Asistente:*` sin secretos)
- `tests/UnitTest/Architecture/ModuleCatalog.cs` y clases `Spec024*`
- `docs/decisiones/` (ADR-008 actualizado), `Memoria.md` (dependencia `Azure.AI.OpenAI`)

## Dependencias
- Módulo de archivos privados (spec 020, T-01) para las imágenes.
- Specs 003, 011 y 013; recurso de Azure OpenAI en Terraform (spec 005) solo para la prueba manual.

## Orden y paralelismo
- T-01 (decisión y ADR) no bloquea el código: el módulo arranca apagado.
- T-02, T-03 y T-04 en paralelo; T-05 después de T-02; T-06 integra.

## Verificación
- `python scripts/sdd_lint.py --trace 024` sin faltantes; comandos de `AGENTS.md`; prueba manual única contra el servicio real fuera del CI.
