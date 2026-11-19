# Plan 019 — MCP del proyecto (backend)

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Listar los servidores aprobados en `docs/mcp.md` con propósito, permisos y quién los aprobó.
2. Agregar la configuración de cada agente soportado, sin secretos.
3. Restringir por defecto las herramientas de escritura.
4. Probar con una cuenta de prueba y con la búsqueda de secretos.

## Archivos que se crean o cambian
- `docs/mcp.md`
- `.mcp.json` (Claude Code), `opencode.json` (OpenCode) y `.vscode/mcp.json` (editor)
- `AGENTS.md` (regla de uso de MCP)

## Dependencias
- Cuentas de Atlassian de cada integrante con acceso al proyecto.
- Specs 007 y 009 (búsqueda de secretos y reglas de seguridad).

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
