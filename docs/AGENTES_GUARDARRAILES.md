# Guardarraíles para agentes de IA

> Investigación de octubre de 2026 sobre cómo obligar a los agentes a respetar la arquitectura y a verificar antes de terminar, y qué quedó configurado en Knock. Las capacidades de cada herramienta cambian rápido: si una deja de funcionar, se ajusta **solo** `scripts/sync_agents.py`.

## 1. La idea: defensa en capas

Ninguna instrucción escrita obliga a un modelo; solo lo orienta. Por eso Knock combina cinco capas, de la más suave a la más dura:

| Capa | Qué hace | ¿Se puede saltar? | Dónde está |
|---|---|---|---|
| 1. Instrucciones | Le dicen al agente cómo trabajar | Sí: el modelo puede ignorarlas | `AGENTS.md`, constitución, reglas por ruta |
| 2. Skills y subagentes | Conocimiento por tema y roles con permisos distintos (los revisores no editan) | Parcialmente | `.agents/skills`, `.agents/agents` |
| 3. Permisos y hooks del agente | Bloquean en el momento ediciones y comandos prohibidos, y no dejan terminar sin verificar | Solo si alguien desactiva el hook | `scripts/hooks/knock_guard.py` + configs generadas |
| 4. Git hooks | Validan cada commit y push de cualquier persona o agente | Con `--no-verify` (que el guardián bloquea) | `.githooks/` |
| 5. CI + protección de ramas + CODEOWNERS | Nada entra a `sprint-XX`, `develop`, `release` o `main` sin checks en verde y revisión humana | **No** | `.github/workflows`, `docs/BRANCH_PROTECTION.md` |

La capa 5 es la que realmente garantiza; las demás evitan perder tiempo llegando al CI con algo que va a fallar.

## 2. Qué ofrece cada herramienta (octubre 2026)

| Herramienta | Instrucciones | Reglas por ruta | Skills | Subagentes | Hooks que bloquean | «No terminar hasta verificar» |
|---|---|---|---|---|---|---|
| **Claude Code** | `CLAUDE.md` (importa `AGENTS.md`); también lee `AGENTS.md` directo si no hay `CLAUDE.md` | `.claude/rules/*.md` con `paths:` | `.claude/skills/` | `.claude/agents/` con `tools`, `disallowedTools`, `permissionMode`, `skills`, `hooks` | `PreToolUse` (exit 2 o `permissionDecision: deny`) + `permissions.deny` | `Stop`/`SubagentStop` con exit 2 → sigue trabajando |
| **Codex** | `AGENTS.md` | AGENTS.md anidados | `.agents/skills/` (nativo) | por prompt | `.codex/hooks.json`: `PreToolUse` (exit 2 / deny) | `Stop` con `decision: block` o exit 2 |
| **GitHub Copilot** (agente en la nube y CLI) | `.github/copilot-instructions.md` y `AGENTS.md` | `.github/instructions/*.instructions.md` con `applyTo` | `.github/skills`, `.claude/skills`, `.agents/skills` | `*.agent.md` | `.github/hooks/*.json`: `preToolUse` (falla cerrado) | `agentStop` con `decision: block` |
| **Cursor** | `.cursor/rules/*.mdc` (siempre activa) y `AGENTS.md` | `.mdc` con `globs` | índice en `AGENTS.md` | por prompt | `.cursor/hooks.json`: `preToolUse`, `beforeShellExecution` (exit 2 = deny) | `stop` con `followup_message` (límite de vueltas) |
| **Gemini CLI** | `GEMINI.md` (importa `AGENTS.md`) | — | índice en `AGENTS.md` | por prompt | `.gemini/settings.json`: `BeforeTool` (exit 2 / deny) | `AfterAgent` con deny → reintenta con el motivo |
| **opencode** (el que usa el profesor) | `AGENTS.md` | — | `.opencode/skills/` | `.opencode/agents/` con `permission.edit/bash` | plugin `.opencode/plugins/knock-guard.js` (`tool.execute.before` lanza error) | aviso en `session.idle` |

**Modos de Claude Code útiles aquí:** `plan` (solo lee y planea: ideal para refinar specs y para revisores), `acceptEdits` (edita sin preguntar, comandos sí preguntan), `auto` (un clasificador revisa cada acción; las reglas `deny` siguen aplicando) y `dontAsk` (CI: solo lo preaprobado). Las reglas `deny` aplican en todos los modos, incluso `bypassPermissions`. Los revisores generados usan herramientas de solo lectura.

## 3. Qué bloquea el guardián (`knock_guard.py`)

Un solo script en Python para todas las herramientas. Recibe el JSON del hook y responde en el formato de cada una.

**Antes de editar**
- Archivos protegidos: `.github/`, `scripts/`, configuración de analizadores (`.semgrep/`, `stryker-config.json`, `.gitleaks.toml`, `Directory.Build.props`…), adaptadores generados, código generado, contrato vendorizado, constitución, registro de excepciones.
- Código sin spec **Aprobada** con la clave `KNOCK-N` de la rama.
- Specs firmadas, o un agente intentando aprobar o firmar.
- Fronteras: un módulo usando internos de otro; controllers con `DbContext` o `try/catch`; `domain/` de Flutter con dependencias de framework; `presentation/` con HTTP; componentes Angular con `HttpClient`; `innerHTML`/`bypassSecurityTrust`.
- Pruebas desactivadas (`Skip`, `[Ignore]`, `skip:`, `xit`, `fit`, `.only`), SQL interpolado, secretos en código.
- Supresiones de analizadores sin `EXC-NNN` registrada.
- **Crear algo que ya existe:** si el tipo nuevo se parece a uno del mapa de código, bloquea el primer intento y muestra las coincidencias.

**Antes de un comando**
- `push --force`, push a ramas fijas, `reset --hard`, `clean -f`, `--no-verify`, `DROP`, borrar recursos de Azure, `curl | sh`, merge/releases con `gh`, nombres de rama fuera del esquema, y agregar dependencias sin evaluación.

**Al terminar** (máx. 3 bloqueos seguidos para evitar bucles)
- `Memoria.md` actualizado si cambió código; `sdd_lint`, sincronía de agentes, excepciones y mapa de código en OK; build/formato/análisis del stack tocado; con `KNOCK_STOP=full` también pruebas y trazabilidad de la spec.

## 4. Grafo de arquitectura y reutilización

| Necesidad | Herramienta en Knock | Alternativas evaluadas |
|---|---|---|
| ¿Ya existe algo que haga esto? | `python scripts/codemap.py --query "…"` y `--similar Nombre` | Serena (MCP con servidores de lenguaje, soporta C# y Dart; opcional para búsqueda semántica), búsqueda del IDE |
| Índice de módulos, capas, clases y métodos | `docs/architecture/CODEMAP.md` y `codemap.json` | NDepend (de pago) |
| Ver dependencias y dónde está cada cosa | `docs/architecture/graph.html` (interactivo) y `modules.mmd` (Mermaid) | `dotnet-depends`, `lakos` (Dart) |
| Duplicados | Huella estructural de métodos (`codemap --check`) + jscpd (texto) | SonarCloud (opcional, ya previsto en CI) |
| Reglas de arquitectura | `codemap --check` + NetArchTest (backend) + prueba de capas (app) + Semgrep | ArchUnitNET |

`codemap.py` no tiene dependencias, lee C#, Dart y TypeScript, y lo regeneran el pre-commit y el hook al terminar. Se probó sobre el repositorio `WebAPIDevSecOpsScalling` del profesor: encontró `GenerateJwtToken` triplicado (un controller y dos servicios) y controllers que usan EF directamente.

## 5. Fuentes
- Claude Code: [hooks](https://code.claude.com/docs/en/hooks.md), [subagentes](https://code.claude.com/docs/en/sub-agents.md), [memoria y AGENTS.md](https://code.claude.com/docs/en/memory.md), [modos de permiso](https://code.claude.com/docs/en/permission-modes.md), [permisos](https://code.claude.com/docs/en/permissions.md), [GitHub Actions](https://code.claude.com/docs/en/github-actions.md)
- [Codex: hooks](https://learn.chatgpt.com/docs/hooks.md) · [Codex: skills](https://learn.chatgpt.com/docs/build-skills.md)
- [GitHub Copilot: hooks](https://docs.github.com/en/copilot/concepts/agents/hooks) · [referencia de hooks del CLI](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-hooks-reference) · [Copilot y AGENTS.md](https://github.blog/changelog/2026-06-23-copilot-coding-agent-now-supports-agents-md-custom-instructions/)
- [Cursor: hooks](https://cursor.com/docs/hooks.md)
- [Gemini CLI: referencia de hooks](https://geminicli.com/docs/hooks/reference/)
- [opencode: plugins](https://opencode.ai/docs/en/plugins/)
- [Serena](https://github.com/oraios/serena)
