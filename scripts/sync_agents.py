#!/usr/bin/env python3
"""Genera los adaptadores de cada agente de IA a partir de la fuente neutral.

Fuente (lo único que se edita a mano):
  AGENTS.md                         manual operativo
  .specify/memory/constitution.md   reglas no negociables
  .agents/skills/<name>/SKILL.md    conocimiento reutilizable
  .agents/agents/<name>.md          roles (constructores y revisores)
  .agents/rules/<name>.md           reglas por tipo de archivo (frontmatter paths)
  scripts/hooks/knock_guard.py      guardián que invocan los hooks de todas las herramientas

Salidas (no editar a mano):
  constitution.md, CLAUDE.md, GEMINI.md, sección generada de AGENTS.md
  Claude Code   .claude/settings.json (permisos + hooks), .claude/skills, .claude/agents, .claude/rules
  opencode      .opencode/skills, .opencode/agents, .opencode/plugins/knock-guard.js
  Codex         .codex/hooks.json (skills: lee .agents/skills directamente)
  Gemini CLI    .gemini/settings.json (hooks)
  Cursor        .cursor/rules/*.mdc, .cursor/hooks.json
  Copilot       .github/copilot-instructions.md, .github/instructions/*, .github/agents/*, .github/hooks/knock-guard.json

Uso:
  python scripts/sync_agents.py          # escribe
  python scripts/sync_agents.py --check  # falla si algo está desactualizado (CI y pre-commit)
Variable KNOCK_PYTHON: intérprete en los hooks (por defecto python3; en Windows suele ser python).
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKILLS_DIR = ROOT / ".agents" / "skills"
AGENTS_DIR = ROOT / ".agents" / "agents"
RULES_DIR = ROOT / ".agents" / "rules"
CONSTITUTION = ROOT / ".specify" / "memory" / "constitution.md"
AGENTS_MD = ROOT / "AGENTS.md"
START = "<!-- skills-index:start -->"
END = "<!-- skills-index:end -->"
GEN = "GENERADO por scripts/sync_agents.py desde {src}. No editar a mano."
GEN_NOTE = "<!-- " + GEN + " -->"
PY = os.environ.get("KNOCK_PYTHON", "python3")
GUARD = "scripts/hooks/knock_guard.py"
EDIT_TOOLS_CLAUDE = "Edit|Write|MultiEdit|NotebookEdit"
CONFIG = json.loads((ROOT / ".agents" / "knock.json").read_text(encoding="utf-8")) if (ROOT / ".agents" / "knock.json").exists() else {}
REPO_NAME = CONFIG.get("repo", ROOT.name)
PROTECTED_DENY = ["/.github/**", "/.claude/**", "/.githooks/**", "/.specify/memory/**", "/scripts/**", "/constitution.md", "/CLAUDE.md",
                  "/.semgrep/**", "/stryker-config.json", "/security/exceptions.yaml", "/.agents/knock.json"] + \
                 ["/" + p.lstrip("/") for p in CONFIG.get("protected", [])]


# ------------------------------------------------------------------ lectura

def parse_frontmatter(text: str, path: Path) -> tuple[dict, str]:
    match = re.match(r"^---\n(.*?)\n---\n?(.*)$", text, re.S)
    if not match:
        sys.exit(f"ERROR: {path} no tiene frontmatter YAML")
    meta: dict = {}
    for line in match.group(1).splitlines():
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        key, _, raw = line.partition(":")
        value = raw.strip()
        if value.startswith("[") and value.endswith("]"):
            items = [v.strip().strip("\"'") for v in value[1:-1].split(",")]
            meta[key.strip()] = [i for i in items if i]
        elif value.lower() in ("true", "false"):
            meta[key.strip()] = value.lower() == "true"
        else:
            meta[key.strip()] = value.strip("\"'")
    return meta, match.group(2)


def load(directory: Path, pattern: str) -> list[tuple[Path, dict, str]]:
    items = []
    for path in sorted(directory.glob(pattern)):
        meta, body = parse_frontmatter(path.read_text(encoding="utf-8"), path)
        for required in ("name", "description"):
            if not meta.get(required):
                sys.exit(f"ERROR: {path} sin '{required}' en el frontmatter")
        items.append((path, meta, body))
    return items


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def yaml_str(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


# ------------------------------------------------------------------ agentes

def edit_scope_note(meta: dict) -> str:
    scope = meta.get("can_edit", False)
    if isinstance(scope, list):
        return "\n> **Permiso de edición:** solo puedes modificar " + ", ".join(f"`{s}`" for s in scope) + ".\n"
    if scope is False:
        return "\n> **Permiso de edición:** ninguno. Solo lectura y reporte.\n"
    return ""


def skills_note(meta: dict) -> str:
    skills = meta.get("skills", [])
    if not skills:
        return ""
    return "\n## Skills que debes cargar\n" + "\n".join(f"- `.agents/skills/{s}/SKILL.md`" for s in skills) + "\n"


def guard_cmd(event: str, tool: str, extra: str = "", project_var: str | None = None) -> str:
    base = f'"${project_var}/{GUARD}"' if project_var else f'"$(git rev-parse --show-toplevel)/{GUARD}"'
    return f"{PY} {base} {event} --tool {tool}{extra}"


def claude_agent(meta: dict, body: str, src: str) -> str:
    header = ["---", f"name: {meta['name']}", f"description: {yaml_str(meta['description'])}"]
    can_edit, can_run = meta.get("can_edit", False), meta.get("can_run_commands", False)
    if can_edit is False:
        header.append("tools: " + ", ".join(["Read", "Grep", "Glob"] + (["Bash"] if can_run else [])))
        header.append("disallowedTools: Edit, Write, MultiEdit, NotebookEdit")
        header.append("color: purple")
    elif not can_run:
        header.append("disallowedTools: Bash, PowerShell")
    if meta.get("skills"):
        header.append("skills:")
        header += [f"  - {s}" for s in meta["skills"]]
    if isinstance(can_edit, list):
        scope = ",".join(can_edit)
        allow_arg = f' --allow-edit "{scope}"'
        header += ["hooks:", "  PreToolUse:", f"    - matcher: {yaml_str(EDIT_TOOLS_CLAUDE)}", "      hooks:",
                   "        - type: command",
                   f"          command: {yaml_str(guard_cmd('pre-tool', 'claude', allow_arg, 'CLAUDE_PROJECT_DIR'))}"]
    header.append("---")
    return "\n".join(header) + "\n" + GEN_NOTE.format(src=src) + "\n" + edit_scope_note(meta) + body.rstrip() + "\n" + skills_note(meta)


def opencode_agent(meta: dict, body: str, src: str) -> str:
    can_edit = meta.get("can_edit", False)
    header = ["---", f"description: {yaml_str(meta['description'])}", "mode: subagent", "temperature: 0.2", "permission:",
              f"  edit: {'deny' if can_edit is False else 'allow'}", f"  bash: {'allow' if meta.get('can_run_commands') else 'deny'}", "---"]
    return "\n".join(header) + "\n" + GEN_NOTE.format(src=src) + "\n" + edit_scope_note(meta) + body.rstrip() + "\n" + skills_note(meta)


def copilot_agent(meta: dict, body: str, src: str) -> str:
    header = ["---", f"name: {meta['name']}", f"description: {yaml_str(meta['description'])}"]
    if meta.get("can_edit", False) is False:
        header.append("tools: ['read', 'search']")
    header.append("---")
    return "\n".join(header) + "\n" + GEN_NOTE.format(src=src) + "\n" + edit_scope_note(meta) + body.rstrip() + "\n" + skills_note(meta)


def skill_copy(text: str, src: str) -> str:
    meta_block, _, rest = text.partition("\n---\n")
    return meta_block + "\n---\n" + GEN_NOTE.format(src=src) + "\n" + rest.lstrip("\n")


# ------------------------------------------------------------------ reglas por ruta

def rule_outputs(path: Path, meta: dict, body: str) -> dict[Path, str]:
    paths = meta.get("paths", [])
    src, name = rel(path), meta["name"]
    note = GEN_NOTE.format(src=src)
    return {
        ROOT / ".claude" / "rules" / f"{name}.md": "---\npaths:\n" + "".join(f"  - {yaml_str(p)}\n" for p in paths) + "---\n" + note + "\n" + body.lstrip("\n"),
        ROOT / ".github" / "instructions" / f"{name}.instructions.md": f"---\napplyTo: {yaml_str(','.join(paths))}\n---\n" + note + "\n" + body.lstrip("\n"),
        ROOT / ".cursor" / "rules" / f"{name}.mdc": f"---\ndescription: {meta['description']}\nglobs: {','.join(paths)}\nalwaysApply: false\n---\n" + note + "\n" + body.lstrip("\n"),
    }


# ------------------------------------------------------------------ hooks por herramienta

def claude_settings() -> str:
    cmd = lambda event, extra="": guard_cmd(event, "claude", extra, "CLAUDE_PROJECT_DIR")
    settings = {
        "$schema": "https://json.schemastore.org/claude-code-settings.json",
        # Sin Co-Authored-By ni «Generated with Claude Code» en commits y PR (el uso de IA va en «Uso de IA» del PR).
        "attribution": {"commit": "", "pr": "", "sessionUrl": False},
        "permissions": {
            "deny": [f"Edit({p})" for p in PROTECTED_DENY] + [
                "Read(./.env)", "Read(./.env.*)", "Read(**/appsettings.Development.json)", "Read(**/*.keystore)",
                "Bash(git push --force *)", "Bash(git push -f *)", "Bash(git reset --hard *)", "Bash(* --no-verify *)"],
            "ask": ["Bash(git push *)", "Bash(dotnet ef database *)", "Bash(docker system prune *)"],
            "allow": ["Bash(python3 scripts/*)", "Bash(python scripts/*)", "Bash(dotnet restore *)", "Bash(dotnet build *)",
                      "Bash(dotnet test *)", "Bash(flutter analyze *)", "Bash(flutter test *)", "Bash(dart format *)",
                      "Bash(git status *)", "Bash(git diff *)", "Bash(git log *)"],
        },
        "hooks": {
            "SessionStart": [{"hooks": [{"type": "command", "command": cmd("session-start"), "timeout": 30}]}],
            "PreToolUse": [{"matcher": EDIT_TOOLS_CLAUDE + "|Bash|PowerShell",
                            "hooks": [{"type": "command", "command": cmd("pre-tool"), "timeout": 90}]}],
            "Stop": [{"hooks": [{"type": "command", "command": cmd("stop"), "timeout": 900,
                                 "statusMessage": "knock-guard: verificando antes de terminar"}]}],
            "SubagentStop": [{"hooks": [{"type": "command", "command": cmd("stop"), "timeout": 900}]}],
        },
    }
    return json.dumps(settings, ensure_ascii=False, indent=2) + "\n"


def codex_hooks() -> str:
    cmd = lambda e: guard_cmd(e, "codex")
    data = {"hooks": {
        "SessionStart": [{"hooks": [{"type": "command", "command": cmd("session-start"), "timeout": 30}]}],
        "PreToolUse": [{"hooks": [{"type": "command", "command": cmd("pre-tool"), "timeout": 90}]}],
        "Stop": [{"hooks": [{"type": "command", "command": cmd("stop"), "timeout": 900}]}],
    }}
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def gemini_settings() -> str:
    cmd = lambda e: guard_cmd(e, "gemini")
    data = {"hooks": {
        "SessionStart": [{"hooks": [{"type": "command", "command": cmd("session-start")}]}],
        "BeforeTool": [{"matcher": "write_file|replace|edit|run_shell_command",
                        "hooks": [{"type": "command", "command": cmd("pre-tool")}]}],
        "AfterAgent": [{"hooks": [{"type": "command", "command": cmd("stop")}]}],
    }}
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def cursor_hooks() -> str:
    cmd = lambda e: f"{PY} {GUARD} {e} --tool cursor"
    data = {"version": 1, "hooks": {
        "preToolUse": [{"command": cmd("pre-tool"), "timeout": 90}],
        "beforeShellExecution": [{"command": cmd("pre-tool"), "timeout": 30}],
        "stop": [{"command": cmd("stop"), "timeout": 900, "loop_limit": 3}],
    }}
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def copilot_hooks() -> str:
    def h(event: str, timeout: int) -> dict:
        return {"type": "command", "bash": f"{PY} {GUARD} {event} --tool copilot",
                "powershell": f"python {GUARD} {event} --tool copilot", "timeoutSec": timeout}
    data = {"version": 1, "hooks": {"sessionStart": [h("session-start", 30)], "preToolUse": [h("pre-tool", 90)],
                                    "agentStop": [h("stop", 900)]}}
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


OPENCODE_PLUGIN = """// {note}
// Guardián de Knock para opencode: delega en scripts/hooks/knock_guard.py.
import {{ spawnSync }} from "node:child_process";
import path from "node:path";

const guard = (dir, event, payload) =>
  spawnSync(process.env.KNOCK_PYTHON || "{py}", [path.join(dir, "{guard}"), event, "--tool", "opencode"], {{
    input: JSON.stringify(payload), encoding: "utf8", cwd: dir,
  }});

export const KnockGuard = async ({{ directory, worktree }}) => {{
  const dir = worktree || directory;
  return {{
    "tool.execute.before": async (input, output) => {{
      const r = guard(dir, "pre-tool", {{ tool_name: input.tool, tool_input: output.args }});
      if (r.status === 2) throw new Error(r.stderr || r.stdout);
    }},
    event: async ({{ event }}) => {{
      if (event.type === "session.idle") {{
        const r = guard(dir, "stop", {{ session_id: event.properties?.sessionID }});
        if (r.status === 2) console.error(r.stderr);
      }}
    }},
  }};
}};
"""


# ------------------------------------------------------------------ índice y punteros

def skills_index(skills, agents, rules) -> str:
    rows = ["## 12. Índice de skills, agentes y reglas (generado)", "",
            "> Si tu herramienta no carga skills por sí sola, abre el `SKILL.md` indicado cuando la tarea coincida con su descripción.", "",
            "| Skill | Cuándo usarla | Archivo |", "|---|---|---|"]
    rows += [f"| `{m['name']}` | {m['description']} | `{rel(p)}` |" for p, m, _ in skills]
    rows += ["", "| Agente | Para qué | Archivo |", "|---|---|---|"]
    rows += [f"| `{m['name']}` | {m['description']} | `{rel(p)}` |" for p, m, _ in agents]
    rows += ["", "| Regla | Aplica a | Archivo |", "|---|---|---|"]
    rows += [f"| `{m['name']}` | {', '.join(f'`{x}`' for x in m.get('paths', []))} | `{rel(p)}` |" for p, m, _ in rules]
    return START + "\n" + "\n".join(rows) + "\n" + END


POINTER = """# Knock ({repo}) — instrucciones para el agente

{note}

Lee y sigue **`AGENTS.md`** (manual operativo) y **`.specify/memory/constitution.md`** (reglas no negociables; mandan ante conflicto).

Resumen mínimo:
- Sigue la skill `story-workflow` en todo issue: leer → amenazas → plan de pruebas → implementar → verificar de verdad → informe honesto.
- Sin spec **Aprobada** en `specs/<ID>-*/` (con la clave KNOCK-N de tu rama) no se escribe código.
- Antes de crear algo, busca si ya existe: `python scripts/codemap.py --query "<qué necesitas>"`.
- Cada caso `TC-<ID>-KK` tiene una prueba con ese ID; prohibido desactivar pruebas, reglas o escáneres, o bajar umbrales.
- No edites archivos protegidos ni generados; no agregues dependencias sin evaluarlas.
- Al terminar, actualiza `Memoria.md`; nunca firmes aprobaciones.
- Commits en inglés: `type(scope): description (KNOCK-N)`. Nunca agregues atribución a IA (`Co-Authored-By` de agentes, «Generated with…») en commits o PR; un `Co-Authored-By` de una persona del equipo sí se permite; el uso de IA se declara solo en «Uso de IA» del PR.
- Skills en `.agents/skills/`, reglas por archivo en `.agents/rules/` (índice en `AGENTS.md §12`).
"""


def build_outputs() -> dict[Path, str]:
    skills = load(SKILLS_DIR, "*/SKILL.md")
    agents = load(AGENTS_DIR, "*.md")
    rules = load(RULES_DIR, "*.md")
    out: dict[Path, str] = {ROOT / "constitution.md": CONSTITUTION.read_text(encoding="utf-8")}

    agents_md = AGENTS_MD.read_text(encoding="utf-8")
    index = skills_index(skills, agents, rules)
    if START in agents_md and END in agents_md:
        agents_md = re.sub(re.escape(START) + r".*?" + re.escape(END), lambda _: index, agents_md, flags=re.S)
    else:
        agents_md = agents_md.rstrip() + "\n\n" + index + "\n"
    out[AGENTS_MD] = agents_md

    note = GEN_NOTE.format(src="AGENTS.md")
    out[ROOT / "CLAUDE.md"] = f"@AGENTS.md\n\n{note}\n> Claude Code: skills en `.claude/skills/`, subagentes en `.claude/agents/`, reglas por ruta en `.claude/rules/`, permisos y hooks en `.claude/settings.json`.\n"
    out[ROOT / "GEMINI.md"] = f"@./AGENTS.md\n\n{note}\n"
    out[ROOT / ".github" / "copilot-instructions.md"] = POINTER.format(note=note, repo=REPO_NAME)
    out[ROOT / ".cursor" / "rules" / "knock.mdc"] = "---\ndescription: Reglas del proyecto Knock (SDD)\nalwaysApply: true\n---\n" + POINTER.format(note=note, repo=REPO_NAME)

    for path, meta, _ in skills:
        text, name = path.read_text(encoding="utf-8"), meta["name"]
        for tool in (".claude", ".opencode"):
            out[ROOT / tool / "skills" / name / "SKILL.md"] = skill_copy(text, rel(path))
            for extra in path.parent.iterdir():
                if extra.name != "SKILL.md" and extra.is_file():
                    out[ROOT / tool / "skills" / name / extra.name] = extra.read_text(encoding="utf-8")

    for path, meta, body in agents:
        name, src = meta["name"], rel(path)
        out[ROOT / ".claude" / "agents" / f"{name}.md"] = claude_agent(meta, body, src)
        out[ROOT / ".opencode" / "agents" / f"{name}.md"] = opencode_agent(meta, body, src)
        out[ROOT / ".github" / "agents" / f"{name}.agent.md"] = copilot_agent(meta, body, src)

    for path, meta, body in rules:
        out.update(rule_outputs(path, meta, body))

    out[ROOT / ".claude" / "settings.json"] = claude_settings()
    out[ROOT / ".codex" / "hooks.json"] = codex_hooks()
    out[ROOT / ".gemini" / "settings.json"] = gemini_settings()
    out[ROOT / ".cursor" / "hooks.json"] = cursor_hooks()
    out[ROOT / ".github" / "hooks" / "knock-guard.json"] = copilot_hooks()
    out[ROOT / ".opencode" / "plugins" / "knock-guard.js"] = OPENCODE_PLUGIN.format(note=GEN.format(src="scripts/sync_agents.py"), py=PY, guard=GUARD)
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="no escribe; falla si hay diferencias")
    args = parser.parse_args()

    outputs = build_outputs()
    stale = []
    for path, content in outputs.items():
        current = path.read_text(encoding="utf-8") if path.exists() else None
        if current != content:
            stale.append(rel(path))
            if not args.check:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content, encoding="utf-8", newline="\n")

    if args.check:
        if stale:
            print("DESACTUALIZADO (corre python scripts/sync_agents.py):")
            print("\n".join(f"  - {s}" for s in stale))
            return 1
        print(f"OK — {len(outputs)} archivos al día")
        return 0
    print(f"OK — {len(stale)} archivos escritos, {len(outputs) - len(stale)} sin cambios")
    return 0


if __name__ == "__main__":
    sys.exit(main())
