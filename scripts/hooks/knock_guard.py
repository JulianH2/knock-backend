#!/usr/bin/env python3
"""Guardián de Knock para agentes de IA (un solo script para todas las herramientas).

Lo invocan los hooks de Claude Code, Codex, Gemini CLI, Cursor, GitHub Copilot y opencode
(configuraciones generadas por scripts/sync_agents.py). Lee el JSON del hook por stdin.
La configuración propia de cada repositorio está en .agents/knock.json.

Eventos:
  session-start  imprime el contexto de trabajo (issue y spec activos, reglas clave)
  pre-tool       bloquea ediciones y comandos que rompen la constitución
  stop           no deja terminar si faltan verificaciones (Memoria, lint, mapa, build, pruebas)

Uso:
  python3 scripts/hooks/knock_guard.py pre-tool --tool claude [--allow-edit "specs/**,Memoria.md"]
  python3 scripts/hooks/knock_guard.py stop --tool cursor

Variables:
  KNOCK_STOP=quick|standard|full   profundidad de la verificación al terminar (standard)
  KNOCK_SPEC=HU-024                spec activa si la rama no permite deducirla
  KNOCK_GUARD_ALLOW_PROTECTED=1    solo personas: permite editar archivos protegidos
  KNOCK_GUARD_OFF=1                desactiva el guardián (emergencias; el CI sigue aplicando todo)
"""
from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(os.environ.get("CLAUDE_PROJECT_DIR") or os.environ.get("KNOCK_ROOT") or Path(__file__).resolve().parents[2])
STATE = ROOT / ".git" / "knock-guard" if (ROOT / ".git").is_dir() else Path(os.environ.get("TMPDIR", "/tmp")) / "knock-guard"
CONFIG_PATH = ROOT / ".agents" / "knock.json"
CONFIG = json.loads(CONFIG_PATH.read_text(encoding="utf-8")) if CONFIG_PATH.exists() else {}

PROTECTED = [
    "constitution.md", "CLAUDE.md", "GEMINI.md", ".claude/**", ".opencode/**", ".codex/**", ".gemini/**", ".cursor/**",
    ".github/**", ".githooks/**", "scripts/**", ".specify/memory/**", ".semgrep/**", "stryker-config.json", "**/stryker.config.*",
    ".jscpd.json", ".gitleaks.toml", ".trivyignore", "Directory.Build.props", ".editorconfig", "security/exceptions.yaml",
    "docs/architecture/codemap.json", "docs/architecture/CODEMAP.md", "docs/architecture/graph.html", "docs/architecture/modules.mmd",
    "docs/architecture/clones-baseline.json", ".agents/knock.json", "release-please-config.json", ".release-please-manifest.json",
] + CONFIG.get("protected", [])
CODE_DIRS = CONFIG.get("code_dirs", ["src/**"])
SHELL_TOOLS = re.compile(r"bash|shell|terminal|exec|run_shell_command|local_shell|powershell", re.I)
EDIT_TOOLS = re.compile(r"edit|write|replace|create|patch|str_replace|notebook", re.I)
DECL = {
    ".cs": re.compile(r"\b(?:class|record|interface|struct|enum)\s+([A-Z]\w+)"),
    ".dart": re.compile(r"^\s*(?:abstract\s+|sealed\s+|final\s+|base\s+)*(?:class|mixin|enum)\s+([A-Za-z]\w+)", re.M),
    ".ts": re.compile(r"\b(?:class|interface|enum|type)\s+([A-Z]\w+)|\bfunction\s+([a-zA-Z]\w+)|export\s+const\s+([a-zA-Z]\w+)\s*=\s*(?:\(|async)"),
}
SUPPRESSION = re.compile(r"nosemgrep|eslint-disable|#pragma\s+warning\s+disable|//\s*ignore(?:_for_file)?:|\[SuppressMessage|@ts-ignore|@ts-expect-error|NOSONAR|gitleaks:allow")
SPEC_ID = re.compile(r"^(HU-\d{3}|TT-[A-Z]+-\d{2}|BUG-\d{3})")


# ------------------------------------------------------------------ entrada

def read_payload() -> dict:
    raw = sys.stdin.read() if not sys.stdin.isatty() else ""
    try:
        return json.loads(raw) if raw.strip() else {}
    except json.JSONDecodeError:
        return {}


def tool_info(data: dict) -> tuple[str, dict]:
    name = str(data.get("tool_name") or data.get("toolName") or data.get("tool") or "")
    args = data.get("tool_input") or data.get("toolArgs") or data.get("args") or data.get("input") or {}
    if isinstance(args, str):
        try:
            args = json.loads(args)
        except json.JSONDecodeError:
            args = {"command": args}
    if not name and "command" in data:
        name, args = "shell", {"command": data["command"]}
    return name, args if isinstance(args, dict) else {}


def relative(p: str) -> str:
    path = Path(p)
    if path.is_absolute():
        try:
            path = path.resolve().relative_to(ROOT.resolve())
        except ValueError:
            return path.as_posix()
    rel = path.as_posix()
    return rel[2:] if rel.startswith("./") else rel


def edit_targets(args: dict) -> list[tuple[str, str]]:
    """[(ruta relativa, contenido nuevo)] para cualquier herramienta de edición."""
    content = ""
    for key in ("content", "new_string", "newString", "new_str", "newText", "text", "code_edit", "patch", "input"):
        if isinstance(args.get(key), str):
            content = args[key]
            break
    if isinstance(args.get("edits"), list):
        content += "\n".join(str(e.get("new_string") or e.get("newString") or e.get("newText") or "") for e in args["edits"] if isinstance(e, dict))
    paths = [args[k] for k in ("file_path", "path", "filePath", "absolute_path", "target_file", "notebook_path", "filename") if isinstance(args.get(k), str)]
    if not paths and content.startswith("*** Begin Patch"):
        paths = re.findall(r"^\*\*\* (?:Add|Update|Delete) File: (.+)$", content, re.M)
    return [(relative(p), content) for p in paths]


def matches(rel: str, patterns: list[str]) -> bool:
    for p in patterns:
        if fnmatch.fnmatch(rel, p) or (p.startswith("**/") and fnmatch.fnmatch(rel, p[3:])):
            return True
        if p.endswith("/**") and rel == p[:-3]:
            return True
    return False


# ------------------------------------------------------------------ contexto

def git(*args: str) -> str:
    try:
        return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, timeout=20).stdout.rstrip()
    except (OSError, subprocess.SubprocessError):
        return ""


def spec_dirs() -> list[Path]:
    return sorted(p for p in (ROOT / "specs").glob("*") if p.is_dir() and SPEC_ID.match(p.name))


def active_spec() -> tuple[str, Path | None]:
    """(clave de Jira o ID, carpeta de spec). Rama: <tipo>/KNOCK-<N>-<english-description>."""
    wanted = os.environ.get("KNOCK_SPEC", "")
    if wanted:
        found = next((d for d in spec_dirs() if d.name.startswith(wanted)), None)
        return wanted, found
    m = re.search(r"KNOCK-(\d+)", git("branch", "--show-current"))
    if not m:
        return "", None
    key = f"KNOCK-{m.group(1)}"
    for d in spec_dirs():
        spec = d / "spec.md"
        if spec.exists() and re.search(rf"\b{key}\b", spec.read_text(encoding="utf-8")):
            return key, d
    return key, None


def spec_state(folder: Path | None) -> str:
    if not folder or not (folder / "spec.md").exists():
        return "inexistente"
    m = re.search(r"\*\*Estado:\*\*\s*([^\n]+)", (folder / "spec.md").read_text(encoding="utf-8"))
    return m.group(1).strip() if m else "Borrador"


# ------------------------------------------------------------------ reglas de edición

def check_edit(rel: str, content: str, allow_edit: list[str]) -> str | None:
    if allow_edit and not matches(rel, allow_edit):
        return f"Este agente solo puede editar {', '.join(allow_edit)}; `{rel}` está fuera de su alcance."
    if matches(rel, PROTECTED) and os.environ.get("KNOCK_GUARD_ALLOW_PROTECTED") != "1":
        return (f"`{rel}` es un archivo protegido (generado, de seguridad o de gobierno). Los agentes no lo editan. "
                "Si es un adaptador de agente, edita la fuente (.agents/, AGENTS.md) y corre `python scripts/sync_agents.py`; "
                "si no, pide el cambio a una persona y justifícalo en el PR.")
    m = re.fullmatch(r"specs/([^/]+)/spec\.md", rel)
    if m and "Firmada" in spec_state(ROOT / "specs" / m.group(1)):
        return f"`{rel}` está Firmada. No se modifica: abre un issue nuevo y su spec."
    if m and re.search(r"\*\*Estado:\*\*\s*(Aprobada|Firmada)", content) and os.environ.get("KNOCK_GUARD_ALLOW_PROTECTED") != "1":
        if "Aprobada" not in spec_state(ROOT / "specs" / m.group(1)) and "Firmada" not in spec_state(ROOT / "specs" / m.group(1)):
            return "Los agentes no aprueban ni firman specs: déjala en Borrador; una persona la aprueba."
    if matches(rel, CODE_DIRS):
        key, folder = active_spec()
        state = spec_state(folder)
        if not re.search(r"Aprobada|Firmada", state):
            return (f"No hay spec aprobada para escribir código (issue: {key or 'rama sin KNOCK-N'}, spec: {folder.name if folder else 'no encontrada'}, estado: {state}). "
                    "Trabaja en la rama `<tipo>/KNOCK-<N>-<english-description>` del issue, con la clave KNOCK-N escrita en `specs/<ID>/spec.md` y la spec Aprobada.")
    if SUPPRESSION.search(content) and not re.search(r"EXC-\d{3}", content) and not rel.endswith(".md"):
        return ("Estás suprimiendo un analizador o regla (nosemgrep, eslint-disable, #pragma, ignore, @ts-ignore…). "
                "Solo se permite con una excepción registrada en security/exceptions.yaml y su ID (EXC-NNN) en el mismo comentario.")
    if rel.endswith(".cs"):
        mod = re.search(r"/Modules/([^/]+)/", "/" + rel)
        if mod:
            for other in re.findall(r"Modules\.(\w+)\.(?:Domain|Data|Services)\b", content):
                if other != mod.group(1):
                    return f"`{rel}` (módulo {mod.group(1)}) no puede usar internos del módulo {other}. Usa `I{other}Api` o un evento."
        if "/Controllers/" in rel and (re.search(r"\bDbContext\b", content) or re.search(r"\btry\s*\{", content)):
            return "Los controllers no usan DbContext ni try/catch: delega en el servicio; el middleware convierte excepciones en ProblemDetails."
        if re.search(r"\[(?:Fact|Theory)\s*\([^)]*Skip\s*=|\[Ignore\b", content):
            return "No desactives pruebas (Skip/Ignore). Arregla la prueba o el código."
        if re.search(r"FromSqlRaw\s*\(\s*\$\"|ExecuteSqlRaw\s*\(\s*\$\"", content):
            return "SQL interpolado en FromSqlRaw/ExecuteSqlRaw: usa FromSqlInterpolated o parámetros (inyección SQL)."
    if rel.endswith(".dart"):
        if "/domain/" in rel and re.search(r"import\s+['\"]package:(flutter|dio|knock_api|flutter_riverpod|riverpod)/", content):
            return "domain/ es Dart puro: no importa flutter, dio, riverpod ni knock_api."
        if "/presentation/" in rel and re.search(r"import\s+['\"]package:(dio|knock_api)/", content):
            return "presentation/ no llama HTTP: usa un caso de uso del dominio."
        if "/test" in rel and re.search(r"\bskip\s*:\s*(true|['\"])", content):
            return "No desactives pruebas con skip. Arregla la prueba o el código."
    if rel.endswith(".ts"):
        if re.search(r"\b(xit|xdescribe|fit|fdescribe)\s*\(|\b(it|describe|test)\.(skip|only)\s*\(", content):
            return "No dejes pruebas desactivadas ni enfocadas (xit, fit, .skip, .only)."
        if "/features/" in rel and rel.endswith(".component.ts") and re.search(r"\bHttpClient\b", content):
            return "Los componentes no usan HttpClient: usa el servicio de la feature, que llama al cliente generado."
        if re.search(r"\[innerHTML\]|bypassSecurityTrust", content):
            return "innerHTML/bypassSecurityTrust abren XSS. Usa interpolación o justifícalo con una excepción EXC-NNN."
    if re.search(r"(?i)(password|secret|api[_-]?key|jwt[_-]?key|connectionstring|client[_-]?secret)\s*[:=]\s*[\"'][^\"'\s]{12,}[\"']", content) and not rel.endswith((".md", "Example.json", ".example")):
        return "Parece un secreto escrito en el código. Usa variables de entorno, user-secrets o Key Vault."
    return None


def check_new_file(rel: str, content: str) -> str | None:
    if (ROOT / rel).exists() or not matches(rel, CODE_DIRS) or re.search(r"(^|/)tests?/|\.spec\.ts$|_test\.dart$", rel):
        return None
    suffix = Path(rel).suffix
    pattern = DECL.get(suffix)
    names = [n for groups in (pattern.findall(content) if pattern else []) for n in (groups if isinstance(groups, tuple) else (groups,)) if n and not n.startswith("_")]
    codemap = ROOT / "docs" / "architecture" / "codemap.json"
    if not names or not codemap.exists():
        return None
    # nosemgrep: python.lang.security.audit.dangerous-subprocess-use-tainted-env-args.dangerous-subprocess-use-tainted-env-args -- EXC-001
    result = subprocess.run([sys.executable, str(ROOT / "scripts" / "codemap.py"), "--root", str(ROOT), "--similar", *names[:8]],
                            capture_output=True, text=True, timeout=60)
    hits = [h for h in result.stdout.splitlines() if "≈" in h]
    if not hits:
        return None
    STATE.mkdir(parents=True, exist_ok=True)
    ack = STATE / ("ack-" + hashlib.sha1((rel + ",".join(sorted(names))).encode()).hexdigest()[:16])
    if ack.exists():
        return None
    ack.write_text("\n".join(hits), encoding="utf-8")
    return ("Antes de crear `" + rel + "`, revisa si ya existe algo equivalente:\n  " + "\n  ".join(hits[:10]) +
            "\nSi puedes reutilizarlo o extenderlo, hazlo. Si de verdad es distinto, repite la misma creación y explica en Memoria.md por qué no se reutilizó.")


# ------------------------------------------------------------------ reglas de comandos

DANGEROUS = [
    (r"\brm\s+-[a-z]*r[a-z]*\s+(/|~|\.|\*)(\s|$)", "Borrado recursivo peligroso."),
    (r"\bgit\s+push\b.*(--force\b|-f\b|--force-with-lease)", "No se hace push forzado."),
    (r"\bgit\s+push\b.*\b(main|release|develop|sprint-\d{2})\b", "No se hace push a ramas fijas (main, release, develop, sprint-XX): todo entra por PR."),
    (r"\bgit\s+reset\s+--hard\b", "git reset --hard puede borrar trabajo; pide confirmación a una persona."),
    (r"\bgit\s+clean\s+-[a-z]*f", "git clean -f borra archivos sin rastro."),
    (r"--no-verify\b", "No se saltan los git hooks (--no-verify)."),
    (r"\bgit\s+commit\b.*\s-n(\s|$)", "No se saltan los git hooks (-n)."),
    (r"\bdotnet\s+ef\s+database\s+drop\b", "No se borra la base de datos desde un agente."),
    (r"\bDROP\s+(DATABASE|SCHEMA|TABLE)\b", "No se ejecutan DROP desde un agente; hazlo con una migración revisada."),
    (r"\b(curl|wget|iwr|Invoke-WebRequest)\b[^|]*\|\s*(ba|z)?sh\b", "No se ejecutan scripts descargados."),
    (r"\baz\s+.*\b(delete|purge)\b", "No se borran recursos de Azure desde un agente."),
    (r"\bgh\s+(pr\s+merge|release\s+create|workflow\s+disable)\b", "Merge, releases y workflows los opera una persona."),
    (r"\bnpm\s+(install|i)\s+[^-]|\bdotnet\s+add\s+.*package|\bflutter\s+pub\s+add\b|\bdart\s+pub\s+add\b", None),
]
BRANCH_CMD = re.compile(r"\bgit\s+(?:checkout\s+-b|switch\s+-c|branch)\s+([^\s;&|]+)")
BRANCH_OK = re.compile(r"^((feature|fix|docs|refactor|test|chore)/KNOCK-\d+-[a-z0-9]+(-[a-z0-9]+)*|hotfix/KNOCK-\d+-[a-z0-9]+(-[a-z0-9]+)*)$")


def check_shell(command: str) -> str | None:
    for pattern, reason in DANGEROUS:
        if re.search(pattern, command, re.I):
            if reason is None:
                return ("Agregar una dependencia requiere evaluar necesidad, licencia, mantenimiento y vulnerabilidades. "
                        "Propón la dependencia en tu respuesta con esa evaluación; una persona la agrega o te autoriza con KNOCK_GUARD_ALLOW_PROTECTED=1.") \
                    if os.environ.get("KNOCK_GUARD_ALLOW_PROTECTED") != "1" else None
            return reason
    b = BRANCH_CMD.search(command)
    if b and not BRANCH_OK.match(b.group(1)) and not b.group(1).startswith("-"):
        return f"Nombre de rama inválido `{b.group(1)}`. Usa `<tipo>/KNOCK-<N>-<english-description>` (tipos: feature, fix, docs, refactor, test, chore) o `hotfix/KNOCK-<N>-…`."
    if os.environ.get("KNOCK_GUARD_ALLOW_PROTECTED") != "1":
        if re.search(r"(>>?|\btee\b|\bsed\s+-i|\bmv\b|\bcp\b|\brm\b|\btruncate\b|Set-Content|Out-File|Remove-Item|Copy-Item|Move-Item)", command):
            for p in PROTECTED:
                base = p.replace("/**", "").replace("**/", "").replace("*", "")
                if base and len(base) > 3 and re.search(r"(^|[\s'\"=/])" + re.escape(base) + r"([\s'\"/]|$)", command):
                    return f"El comando modifica `{base}`, que está protegido."
    return None


# ------------------------------------------------------------------ verificación al terminar

def changed_files() -> list[str]:
    out = git("status", "--porcelain", "--untracked-files=all")
    return [line[3:].split(" -> ")[-1].strip('"') for line in out.splitlines() if line.strip()]


def run(cmd: list[str], cwd: Path = ROOT, timeout: int = 900) -> tuple[bool, str]:
    try:
        p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, timeout=timeout)
        return p.returncode == 0, (p.stdout + p.stderr)[-1500:]
    except (OSError, subprocess.SubprocessError) as exc:
        return False, str(exc)


def stop_checks() -> list[str]:
    mode = os.environ.get("KNOCK_STOP", "standard")
    files = changed_files()
    if not files:
        return []
    problems = []
    code = [f for f in files if matches(f, CODE_DIRS) or re.search(r"\.(cs|dart|ts|sql)$", f)]
    if code and "Memoria.md" not in files:
        problems.append("Cambiaste código pero no actualizaste `Memoria.md` (estado, decisiones con porqué, errores a evitar).")
    py = sys.executable
    for name, cmd in (("sdd_lint", [py, "scripts/sdd_lint.py"]), ("sync de agentes", [py, "scripts/sync_agents.py", "--check"]),
                      ("excepciones de seguridad", [py, "scripts/ci/check_exceptions.py"])):
        ok, out = run(cmd)
        if not ok:
            problems.append(f"`{name}` falla:\n{out}")
    if code:
        ok, out = run([py, "scripts/codemap.py", "--check"])
        if not ok:
            problems.append("El mapa de código encontró violaciones o duplicados:\n" + out)
    if mode == "quick":
        return problems
    for check in CONFIG.get("stop_checks", []):
        if check.get("mode", "standard") == "full" and mode != "full":
            continue
        if not any(re.search(check["when"], f) for f in files):
            continue
        exe = check["cmd"][0]
        if not shutil.which(exe) or ("requires" in check and not (ROOT / check["requires"]).exists()):
            continue
        ok, out = run(check["cmd"], cwd=ROOT / check.get("cwd", "."), timeout=check.get("timeout", 900))
        if not ok:
            problems.append(f"`{check['name']}` falla:\n{out}")
    key, folder = active_spec()
    if mode == "full" and folder:
        ok, out = run([py, "scripts/sdd_lint.py", "--trace", folder.name])
        if not ok:
            problems.append(f"Faltan pruebas para criterios de {folder.name}:\n" + out)
    return problems


MAX_STOP_BLOCKS = 3


def stop_counter(session: str) -> Path:
    STATE.mkdir(parents=True, exist_ok=True)
    return STATE / f"stop-{re.sub(r'[^A-Za-z0-9_-]', '', session or 'default')[:40]}"


# ------------------------------------------------------------------ salida por herramienta

def deny(tool: str, reason: str, event: str) -> int:
    reason = "[knock-guard] " + reason
    if tool == "cursor":
        if event == "stop":
            print(json.dumps({"followup_message": reason + "\nCorrige esto antes de terminar."}))
            return 0
        print(json.dumps({"permission": "deny", "user_message": reason, "agent_message": reason}))
        return 2
    if tool == "copilot":
        if event == "stop":
            print(json.dumps({"decision": "block", "reason": reason}))
            return 0
        print(json.dumps({"permissionDecision": "deny", "permissionDecisionReason": reason}))
        return 2
    print(reason, file=sys.stderr)
    return 2


def session_context() -> str:
    key, folder = active_spec()
    return "\n".join([
        f"Knock ({CONFIG.get('repo', ROOT.name)}) — contexto de trabajo (knock-guard):",
        f"- Issue: {key or 'rama sin KNOCK-N'} · spec: {folder.name if folder else 'no encontrada'} · estado: {spec_state(folder)}",
        "- Sigue el flujo de la skill `story-workflow`: leer → amenazas → plan de pruebas → implementar → verificar → informe honesto.",
        "- Sin spec Aprobada no se escribe código. Lee Memoria.md y la spec antes de cambiar nada.",
        "- Antes de crear una clase o función: `python scripts/codemap.py --query \"<qué necesitas>\"` y docs/architecture/CODEMAP.md.",
        "- No edites archivos protegidos (.github/, scripts/, configuración de analizadores, código generado, constitución).",
        "- Al terminar se verifican Memoria.md, specs, agentes, excepciones, mapa de código, build y análisis.",
    ])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("event", choices=["session-start", "pre-tool", "stop"])
    parser.add_argument("--tool", default="claude", choices=["claude", "codex", "gemini", "cursor", "copilot", "opencode"])
    parser.add_argument("--allow-edit", default="", help="globs separados por coma que el agente puede editar")
    args = parser.parse_args()
    if os.environ.get("KNOCK_GUARD_OFF") == "1":
        return 0
    data = read_payload()

    if args.event == "session-start":
        print(session_context())
        return 0

    if args.event == "stop":
        counter = stop_counter(str(data.get("session_id") or data.get("sessionId") or data.get("conversation_id") or ""))
        problems = stop_checks()
        blocks = int(counter.read_text() or 0) if counter.exists() else 0
        if problems and blocks < MAX_STOP_BLOCKS:
            counter.write_text(str(blocks + 1))
            return deny(args.tool, "Todavía no está terminado:\n- " + "\n- ".join(problems), "stop")
        counter.unlink(missing_ok=True)
        if problems:
            print("[knock-guard] Se agotaron los reintentos; informa a la persona lo pendiente:\n- " + "\n- ".join(problems), file=sys.stderr)
        return 0

    name, targs = tool_info(data)
    allow = [g.strip() for g in args.allow_edit.split(",") if g.strip()]
    if SHELL_TOOLS.search(name) or ("command" in targs and not EDIT_TOOLS.search(name)):
        reason = check_shell(str(targs.get("command") or targs.get("cmd") or targs.get("commandLine") or ""))
        return deny(args.tool, reason, "pre-tool") if reason else 0
    if EDIT_TOOLS.search(name) or any(k in targs for k in ("file_path", "filePath", "path")):
        for rel, content in edit_targets(targs):
            reason = check_edit(rel, content, allow) or check_new_file(rel, content)
            if reason:
                return deny(args.tool, reason, "pre-tool")
    return 0


if __name__ == "__main__":
    sys.exit(main())
