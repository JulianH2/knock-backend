#!/usr/bin/env python3
"""Mapa de código de Knock: índice de símbolos, grafo de dependencias,
duplicados y violaciones de arquitectura. Sin dependencias externas.

Lee C# (backend/) y Dart (app/, packages/) y genera en docs/architecture/:
  codemap.json   índice completo (lo consultan agentes y scripts)
  CODEMAP.md     índice legible: módulo → capa → archivo → símbolos
  modules.mmd    grafo Mermaid de dependencias entre módulos
  graph.html     grafo interactivo de clases y módulos (abrir en el navegador)

Uso:
  python scripts/codemap.py                      # regenera lo que cambió (no reescribe si solo cambian fecha o ruta)
  python scripts/codemap.py --query "enmascarar telefono"   # ¿ya existe algo así?
  python scripts/codemap.py --similar PhoneMasker MaskPhone # nombres parecidos
  python scripts/codemap.py --check              # CI: NO escribe; falla por violaciones, clones nuevos o mapa desactualizado
  python scripts/codemap.py --root ../otro-repo --out /tmp/mapa   # otro proyecto
"""
from __future__ import annotations

import argparse
import difflib
import hashlib
import json
import re
import shutil
import sys
import tempfile
from collections import defaultdict
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SKIP_DIRS = {"bin", "obj", ".git", ".dart_tool", "build", "node_modules", "Migrations", "TestResults", "StrykerOutput", ".idea", ".vs", "dist", ".angular", "coverage", "reports"}
SKIP_SUFFIXES = (".g.dart", ".freezed.dart", ".mocks.dart", ".Designer.cs", ".AssemblyInfo.cs", ".GlobalUsings.g.cs", ".d.ts", ".config.ts")
EXTENSIONS = {".cs": "csharp", ".dart": "dart", ".ts": "typescript"}


def generated_prefixes(root: Path) -> tuple[str, ...]:
    cfg = root / ".agents" / "knock.json"
    if cfg.exists():
        return tuple(p.rstrip("*").rstrip("/") + "/" for p in json.loads(cfg.read_text(encoding="utf-8")).get("generated", []))
    return ()
CS_KEYWORDS = {"if", "for", "foreach", "while", "switch", "catch", "using", "return", "new", "typeof", "nameof", "lock", "base", "this", "when", "default", "sizeof", "checked", "fixed", "await"}
DART_KEYWORDS = {"if", "for", "while", "switch", "catch", "return", "super", "this", "assert", "await", "print", "throw"}
LAYERS = {"controllers", "services", "domain", "data", "dtos", "validators", "events", "hubs", "presentation", "usecases", "repositories", "middleware", "hosting", "common", "consumers", "interfaces", "models", "context", "components", "pages", "guards", "interceptors", "store", "state", "shared", "core", "api"}
TS_KEYWORDS = {"if", "for", "while", "switch", "catch", "return", "constructor", "super", "function", "new", "typeof", "await"}
MIN_CLONE_TOKENS = 40


@dataclass
class Symbol:
    kind: str
    name: str
    line: int
    visibility: str = "public"
    container: str = ""
    body_hash: str = ""
    tokens: int = 0


@dataclass
class SourceFile:
    path: str
    lang: str
    module: str
    layer: str
    is_test: bool
    namespace: str = ""
    imports: list[str] = field(default_factory=list)
    symbols: list[Symbol] = field(default_factory=list)


# ---------------------------------------------------------------- utilidades

def strip_comments_and_strings(code: str) -> str:
    """Reemplaza comentarios y literales por espacios conservando saltos de línea."""
    pattern = re.compile(r'//[^\n]*|/\*.*?\*/|@"(?:""|[^"])*"|\$?"(?:\\.|[^"\\\n])*"|\'(?:\\.|[^\'\\\n])*\'|r\'[^\'\n]*\'|"""[\s\S]*?"""', re.S)
    return pattern.sub(lambda m: re.sub(r"[^\n]", " ", m.group(0)), code)


def line_of(text: str, index: int) -> int:
    return text.count("\n", 0, index) + 1


def block_end(text: str, open_index: int) -> int:
    depth = 0
    for i in range(open_index, len(text)):
        ch = text[i]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return i
    return len(text) - 1


def body_fingerprint(body: str) -> tuple[str, int]:
    """Huella tipo-2: identificadores y literales normalizados."""
    tokens = re.findall(r"[A-Za-z_]\w*|\d+(?:\.\d+)?|\S", body)
    keywords = CS_KEYWORDS | DART_KEYWORDS | {"var", "final", "const", "else", "try", "finally", "async", "true", "false", "null", "in", "is", "as", "out", "ref"}
    norm = ["ID" if re.match(r"[A-Za-z_]", t) and t not in keywords else ("N" if t[0].isdigit() else t) for t in tokens]
    return hashlib.sha1(" ".join(norm).encode()).hexdigest()[:16], len(tokens)


def stem(word: str) -> str:
    for suffix in ("ing", "ers", "er", "or", "ed", "es", "s", "ar", "cion", "dor"):
        if len(word) > len(suffix) + 3 and word.endswith(suffix):
            return word[: -len(suffix)]
    return word


def split_words(name: str) -> list[str]:
    parts = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", name.replace("_", " ").replace("-", " "))
    return [stem(w.lower()) for w in parts.split() if len(w) > 1]


def classify(rel: str) -> tuple[str, str, bool]:
    parts = rel.split("/")
    lower = [p.lower() for p in parts]
    is_test = any(re.search(r"test|e2e", p) for p in lower[:-1]) or lower[-1].endswith(("_test.dart", "tests.cs", "test.cs", ".spec.ts", ".e2e.ts"))
    module, layer = "", ""
    for key in ("modules", "features"):
        if key in lower:
            i = lower.index(key)
            if i + 1 < len(parts) - 1:
                module = parts[i + 1]
                for nxt in lower[i + 2:-1]:
                    if nxt in LAYERS:
                        layer = nxt
                        break
            break
    if not module:
        dirs = parts[:-1]
        for p in reversed(dirs):
            if p.lower() in LAYERS:
                layer = p.lower()
                break
        candidates = [p for p in dirs if p.lower() not in LAYERS and p not in ("lib", "src", "backend", "app", "packages", "test", "integration_test", "panel", "tests")]
        module = candidates[0] if candidates else (dirs[0] if dirs else "root")
    return module, layer or "-", is_test


# ---------------------------------------------------------------- parsers

CS_TYPE = re.compile(r"\b(?P<mods>(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|file|ref|unsafe|new)\s+)*)(?P<kind>record\s+struct|record\s+class|record|class|interface|enum|struct)\s+(?P<name>[A-Z]\w*)")
CS_METHOD = re.compile(r"^[ \t]*(?P<mods>(?:(?:public|internal|private|protected|static|virtual|override|async|sealed|abstract|extern|new|partial|unsafe)\s+)+)(?P<ret>[\w<>\[\],.?() ]+?)\s+(?P<name>[A-Z]\w*)\s*(?:<[^>()]*>)?\s*\((?P<args>[^)]*)\)\s*(?:where[^{;=]*)?(?P<open>\{|=>)", re.M)


def parse_cs(text: str, sf: SourceFile) -> None:
    clean = strip_comments_and_strings(text)
    ns = re.search(r"^\s*namespace\s+([\w.]+)", clean, re.M)
    sf.namespace = ns.group(1) if ns else ""
    sf.imports = re.findall(r"^\s*using\s+(?:static\s+)?([\w.]+)\s*;", clean, re.M)
    types: list[tuple[int, int, str]] = []
    for m in CS_TYPE.finditer(clean):
        mods = m.group("mods")
        vis = next((v for v in ("public", "internal", "protected", "private") if v in mods.split()), "internal")
        kind = re.sub(r"\s+", " ", m.group("kind"))
        sf.symbols.append(Symbol(kind, m.group("name"), line_of(clean, m.start("name")), vis))
        brace = clean.find("{", m.end())
        semi = clean.find(";", m.end())
        if brace != -1 and (semi == -1 or brace < semi):
            types.append((brace, block_end(clean, brace), m.group("name")))
    for m in CS_METHOD.finditer(clean):
        name = m.group("name")
        if name in CS_KEYWORDS:
            continue
        mods = m.group("mods").split()
        vis = next((v for v in ("public", "internal", "protected", "private") if v in mods), "private")
        container = next((t[2] for t in reversed(types) if t[0] < m.start() < t[1]), "")
        if m.group("open") == "{":
            start = m.end() - 1
            body = clean[start:block_end(clean, start) + 1]
        else:
            body = clean[m.end():clean.find(";", m.end()) + 1]
        fp, n = body_fingerprint(body)
        kind = "constructor" if name == container else "method"
        sf.symbols.append(Symbol(kind, name, line_of(clean, m.start("name")), vis, container, fp, n))


DART_TYPE = re.compile(r"^\s*(?:abstract\s+|sealed\s+|base\s+|final\s+|interface\s+|mixin\s+)*(?P<kind>class|mixin|enum|extension(?:\s+type)?|typedef)\s+(?P<name>[A-Za-z_]\w*)", re.M)
DART_FUNC = re.compile(r"^(?P<indent>[ \t]*)(?:@\w+\s+)*(?:static\s+|external\s+|factory\s+)?(?:(?:Future|FutureOr|Stream|Iterable|List|Map|Set)<[^\n{;=]*?>\??|[A-Za-z_][\w.]*\??)\s+(?:get\s+)?(?P<name>[A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\((?P<args>[^)]*)\)\s*(?:async\*?|sync\*)?\s*(?P<open>\{|=>)", re.M)


def parse_dart(text: str, sf: SourceFile) -> None:
    clean = strip_comments_and_strings(text)
    sf.imports = re.findall(r"^\s*(?:import|export)\s+['\"]([^'\"]+)['\"]", text, re.M)
    types: list[tuple[int, int, str]] = []
    for m in DART_TYPE.finditer(clean):
        name = m.group("name")
        vis = "private" if name.startswith("_") else "public"
        sf.symbols.append(Symbol(m.group("kind").split()[0], name, line_of(clean, m.start("name")), vis))
        brace = clean.find("{", m.end())
        semi = clean.find(";", m.end())
        if brace != -1 and (semi == -1 or brace < semi):
            types.append((brace, block_end(clean, brace), name))
    for m in DART_FUNC.finditer(clean):
        name = m.group("name")
        if name in DART_KEYWORDS:
            continue
        container = next((t[2] for t in reversed(types) if t[0] < m.start() < t[1]), "")
        if m.group("open") == "{":
            start = m.end() - 1
            body = clean[start:block_end(clean, start) + 1]
        else:
            body = clean[m.end():clean.find(";", m.end()) + 1]
        fp, n = body_fingerprint(body)
        vis = "private" if name.startswith("_") else "public"
        sf.symbols.append(Symbol("method" if container else "function", name, line_of(clean, m.start("name")), vis, container, fp, n))


TS_TYPE = re.compile(r"^\s*(?:export\s+)?(?:default\s+)?(?:abstract\s+)?(?P<kind>class|interface|enum|type)\s+(?P<name>[A-Za-z_]\w*)", re.M)
TS_FUNC = re.compile(r"^\s*(?:export\s+)?(?:async\s+)?function\s+(?P<name>[A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\((?P<args>[^)]*)\)[^{;]*(?P<open>\{)", re.M)
TS_ARROW = re.compile(r"^\s*export\s+const\s+(?P<name>[A-Za-z_]\w*)\s*(?::[^=]+)?=\s*(?:async\s*)?\([^)]*\)\s*(?::[^=]+)?=>\s*(?P<open>\{)?", re.M)
TS_METHOD = re.compile(r"^[ \t]+(?P<mods>(?:(?:public|private|protected|static|async|readonly|override)\s+)*)(?P<name>[A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\((?P<args>[^)]*)\)\s*(?::\s*[^{;=]+)?(?P<open>\{)", re.M)


def parse_ts(text: str, sf: SourceFile) -> None:
    clean = strip_comments_and_strings(text)
    sf.imports = re.findall(r"^\s*import\s+(?:[^'\"]*\s+from\s+)?['\"]([^'\"]+)['\"]", text, re.M)
    types: list[tuple[int, int, str]] = []
    for m in TS_TYPE.finditer(clean):
        sf.symbols.append(Symbol(m.group("kind"), m.group("name"), line_of(clean, m.start("name")), "public"))
        brace = clean.find("{", m.end())
        if m.group("kind") in ("class", "interface", "enum") and brace != -1:
            types.append((brace, block_end(clean, brace), m.group("name")))
    for regex, kind in ((TS_FUNC, "function"), (TS_ARROW, "function"), (TS_METHOD, "method")):
        for m in regex.finditer(clean):
            name = m.group("name")
            if name in TS_KEYWORDS:
                continue
            container = next((t[2] for t in reversed(types) if t[0] < m.start() < t[1]), "")
            if kind == "method" and not container:
                continue
            if kind == "function" and container:
                continue
            body = ""
            if m.groupdict().get("open"):
                start = m.end() - 1
                body = clean[start:block_end(clean, start) + 1]
            fp, n = body_fingerprint(body) if body else ("", 0)
            vis = "private" if "private" in (m.groupdict().get("mods") or "") or name.startswith("_") else "public"
            sf.symbols.append(Symbol(kind, name, line_of(clean, m.start("name")), vis, container, fp, n))


# ---------------------------------------------------------------- análisis

def scan(root: Path) -> list[SourceFile]:
    files = []
    generated = generated_prefixes(root)
    parsers = {"csharp": parse_cs, "dart": parse_dart, "typescript": parse_ts}
    # Orden por ruta en texto (sensible a mayúsculas): sorted(Path) es distinto en Windows (sin distinguir) y Linux,
    # y el mapa generado en una máquina no coincidiría con el que calcula el CI.
    for path in sorted(root.rglob("*"), key=lambda q: q.relative_to(root).as_posix()):
        if path.suffix not in EXTENSIONS or not path.is_file():
            continue
        rel = path.relative_to(root).as_posix()
        if set(path.relative_to(root).parts) & SKIP_DIRS or rel.endswith(SKIP_SUFFIXES) or rel.startswith(generated) or rel.startswith(("scripts/", "docs/")):
            continue
        module, layer, is_test = classify(rel)
        sf = SourceFile(rel, EXTENSIONS[path.suffix], module, layer, is_test)
        parsers[sf.lang](path.read_text(encoding="utf-8", errors="ignore"), sf)
        files.append(sf)
    return files


def type_edges(root: Path, files: list[SourceFile]) -> list[tuple[str, str]]:
    owners = {}
    for sf in files:
        for s in sf.symbols:
            if s.kind not in ("method", "constructor", "function") and not sf.is_test and len(s.name) > 2:
                owners.setdefault(s.name, sf)
    if not owners:
        return []
    names = sorted(owners, key=len, reverse=True)
    pattern = re.compile(r"\b(" + "|".join(map(re.escape, names)) + r")\b")
    edges = set()
    for sf in files:
        if sf.is_test:
            continue
        declared = {s.name for s in sf.symbols if s.name in owners and owners[s.name] is sf}
        if not declared:
            continue
        clean = strip_comments_and_strings((root / sf.path).read_text(encoding="utf-8", errors="ignore"))
        used = set(pattern.findall(clean)) - declared
        source = sorted(declared)[0]
        for target in used:
            edges.add((source, target))
    return sorted(edges)


def module_of_import(imp: str, modules: set[str]) -> str | None:
    lowered = imp.replace("\\", "/")
    for key in (".Modules.", "/features/", "features/"):
        if key in lowered:
            rest = lowered.split(key, 1)[1]
            candidate = re.split(r"[./]", rest)[0]
            if candidate in modules:
                return candidate
    return None


def module_edges(files: list[SourceFile]) -> dict[tuple[str, str], list[str]]:
    modules = {f.module for f in files}
    edges: dict[tuple[str, str], list[str]] = defaultdict(list)
    for sf in files:
        if sf.is_test:
            continue
        for imp in sf.imports:
            target = module_of_import(imp, modules)
            if target and target != sf.module:
                edges[(sf.module, target)].append(f"{sf.path} → {imp}")
    return edges


def violations(files: list[SourceFile]) -> list[dict]:
    found = []
    modules = {f.module for f in files}
    for sf in files:
        if sf.is_test:
            continue
        for imp in sf.imports:
            if sf.lang == "csharp" and ".Modules." in imp:
                target = module_of_import(imp, modules)
                tail = imp.split(".Modules.", 1)[1]
                if target and target != sf.module and re.search(r"\.(Domain|Data)(\.|$)", tail):
                    found.append({"rule": "modulo-cruzado", "file": sf.path, "detail": f"{sf.module} usa {imp}; usa I{target}Api o un evento"})
            if sf.lang == "dart" and sf.layer == "domain" and re.match(r"package:(flutter|dio|knock_api|riverpod|flutter_riverpod)/", imp):
                found.append({"rule": "dominio-impuro", "file": sf.path, "detail": f"domain/ importa {imp}"})
            if sf.lang == "dart" and sf.layer == "presentation" and re.match(r"package:(dio|knock_api)/", imp):
                found.append({"rule": "presentacion-http", "file": sf.path, "detail": f"presentation/ importa {imp}; usa un caso de uso"})
        if sf.lang in ("dart", "typescript"):
            for imp in sf.imports:
                target = module_of_import(imp, modules) if "features/" in imp else None
                if target and target != sf.module and re.search(r"/(data|domain|services|store|state)/", imp):
                    found.append({"rule": "feature-cruzada", "file": sf.path, "detail": f"{sf.module} usa internos de {target} ({imp}); muévelo a core/shared"})
        if sf.lang == "typescript" and sf.path.endswith(".component.ts") and any(i.startswith("@angular/common/http") for i in sf.imports):
            found.append({"rule": "componente-http", "file": sf.path, "detail": "el componente usa HttpClient; delega en el servicio de la feature"})
        if sf.lang == "csharp" and sf.layer == "controllers" and any(re.search(r"\.Data$|EntityFrameworkCore", i) for i in sf.imports):
            found.append({"rule": "controller-con-datos", "file": sf.path, "detail": "el controller usa DbContext/EF; delega en un servicio"})
    return found


def clones(files: list[SourceFile]) -> list[dict]:
    groups: dict[str, list[str]] = defaultdict(list)
    for sf in files:
        if sf.is_test:
            continue
        for s in sf.symbols:
            if s.body_hash and s.tokens >= MIN_CLONE_TOKENS:
                groups[s.body_hash].append(f"{sf.path}:{s.line} {s.container + '.' if s.container else ''}{s.name}")
    return [{"hash": h, "tokens": None, "places": sorted(p)} for h, p in groups.items() if len(p) > 1]


def same_names(files: list[SourceFile]) -> list[dict]:
    seen: dict[str, list[str]] = defaultdict(list)
    for sf in files:
        if sf.is_test:
            continue
        for s in sf.symbols:
            if s.kind not in ("method", "constructor", "function") and s.visibility != "private":
                seen[s.name].append(f"{sf.path}:{s.line}")
    return [{"name": n, "places": p} for n, p in sorted(seen.items()) if len(p) > 1]


# ---------------------------------------------------------------- salidas

def build(root: Path) -> dict:
    files = scan(root)
    medges = module_edges(files)
    return {
        "generatedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "root": str(root),
        "files": [asdict(f) for f in files],
        "moduleEdges": [{"from": a, "to": b, "count": len(v), "examples": v[:3]} for (a, b), v in sorted(medges.items())],
        "typeEdges": type_edges(root, files),
        "violations": violations(files),
        "clones": clones(files),
        "sameNameTypes": same_names(files),
    }


def write_markdown(data: dict, out: Path) -> None:
    files = [f for f in data["files"] if not f["is_test"]]
    by_module: dict[str, dict[str, list[dict]]] = defaultdict(lambda: defaultdict(list))
    for f in files:
        by_module[f["module"]][f["layer"]].append(f)
    lines = ["# Mapa de código", "",
             f"> Generado por `scripts/codemap.py` el {data['generatedAt']}. No editar a mano.",
             "> **Antes de crear una clase, método o archivo, busca aquí o corre `python scripts/codemap.py --query \"<qué necesitas>\"`.**", "",
             "## Resumen", "", "| Módulo | Capas | Archivos | Tipos | Métodos públicos |", "|---|---|---|---|---|"]
    for module in sorted(by_module):
        layers = by_module[module]
        fl = [f for group in layers.values() for f in group]
        types = sum(1 for f in fl for s in f["symbols"] if s["kind"] not in ("method", "constructor", "function"))
        methods = sum(1 for f in fl for s in f["symbols"] if s["kind"] in ("method", "function") and s["visibility"] != "private")
        lines.append(f"| `{module}` | {', '.join(sorted(layers))} | {len(fl)} | {types} | {methods} |")
    if data["violations"]:
        lines += ["", "## ⚠️ Violaciones de arquitectura", ""] + [f"- **{v['rule']}** — `{v['file']}`: {v['detail']}" for v in data["violations"]]
    if data["clones"]:
        lines += ["", "## ⚠️ Código duplicado (misma estructura)", ""] + [f"- {' · '.join(f'`{p}`' for p in c['places'])}" for c in data["clones"]]
    if data["sameNameTypes"]:
        lines += ["", "## Tipos con el mismo nombre en varios lugares", ""] + [f"- `{d['name']}`: {', '.join(f'`{p}`' for p in d['places'])}" for d in data["sameNameTypes"]]
    lines += ["", "## Índice por módulo"]
    for module in sorted(by_module):
        lines += ["", f"### {module}"]
        for layer in sorted(by_module[module]):
            lines.append(f"\n**{layer}**\n")
            for f in sorted(by_module[module][layer], key=lambda x: x["path"]):
                types = [s for s in f["symbols"] if s["kind"] not in ("method", "constructor", "function")]
                methods = [s for s in f["symbols"] if s["kind"] in ("method", "function") and s["visibility"] != "private"]
                head = ", ".join(f"{s['kind']} `{s['name']}`" for s in types) or "—"
                lines.append(f"- `{f['path']}` — {head}")
                if methods:
                    lines.append("  - " + ", ".join(f"`{m['name']}()`:{m['line']}" for m in methods[:25]) + (" …" if len(methods) > 25 else ""))
    (out / "CODEMAP.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_mermaid(data: dict, out: Path) -> None:
    modules = sorted({f["module"] for f in data["files"] if not f["is_test"]})
    safe = {m: re.sub(r"\W", "_", m) or "root" for m in modules}
    lines = ["graph LR"] + [f"  {safe[m]}[\"{m}\"]" for m in modules]
    for e in data["moduleEdges"]:
        if e["from"] in safe and e["to"] in safe:
            lines.append(f"  {safe[e['from']]} -->|{e['count']}| {safe[e['to']]}")
    (out / "modules.mmd").write_text("\n".join(lines) + "\n", encoding="utf-8")


HTML = """<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Mapa de código</title>
<script src="https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.30.2/cytoscape.min.js"></script>
<style>
:root{--bg:#FBF5E9;--ink:#231A15;--accent:#E9682D;--muted:#1C3A4C;--card:#fff}
@media (prefers-color-scheme:dark){:root{--bg:#1a1512;--ink:#f3ece2;--card:#241d19;--muted:#8fb3c7}}
body{margin:0;font:14px/1.4 system-ui,sans-serif;background:var(--bg);color:var(--ink);display:grid;grid-template-columns:320px 1fr;height:100vh}
aside{padding:16px;overflow:auto;border-right:1px solid #0002}#cy{width:100%;height:100vh}
input{width:100%;padding:8px;border-radius:8px;border:1px solid #0003;box-sizing:border-box;background:var(--card);color:var(--ink)}
h1{font-size:18px;margin:0 0 8px}h2{font-size:14px;margin:16px 0 6px;color:var(--muted)}
.item{padding:6px 8px;border-radius:6px;cursor:pointer}.item:hover{background:#0001}.warn{color:var(--accent)}
code{font-size:12px}#info{white-space:pre-wrap;font-size:12px;background:var(--card);padding:8px;border-radius:8px;min-height:40px}
@media (max-width:700px){body{grid-template-columns:1fr;grid-template-rows:45vh 55vh}aside{border-right:0}}
</style></head><body>
<aside><h1>Mapa de código</h1><input id="q" placeholder="Buscar clase o módulo…">
<h2>Seleccionado</h2><div id="info">Toca un nodo.</div>
<h2>Resultados</h2><div id="res"></div><h2 class="warn">Alertas</h2><div id="alerts"></div></aside>
<div id="cy"></div>
<script>
const DATA = __DATA__;
const els=[], typeFile={}, palette=["#E9682D","#1C3A4C","#3c8d5a","#8e5ea2","#c4a000","#2a7ab0","#b5443b","#5d6d7e"];
const modules=[...new Set(DATA.files.filter(f=>!f.is_test).map(f=>f.module))].sort();
modules.forEach((m,i)=>els.push({data:{id:"m:"+m,label:m,color:palette[i%palette.length]}}));
DATA.files.filter(f=>!f.is_test).forEach(f=>f.symbols.filter(s=>!["method","constructor","function"].includes(s.kind)).forEach(s=>{
  if(typeFile[s.name])return; typeFile[s.name]=f;
  const methods=f.symbols.filter(x=>x.container===s.name&&x.visibility!=="private").map(x=>x.name);
  els.push({data:{id:"t:"+s.name,label:s.name,parent:"m:"+f.module,kind:s.kind,path:f.path+":"+s.line,layer:f.layer,methods}});
}));
DATA.typeEdges.forEach(([a,b],i)=>{if(typeFile[a]&&typeFile[b])els.push({data:{id:"e"+i,source:"t:"+a,target:"t:"+b}})});
const cy=cytoscape({container:document.getElementById("cy"),elements:els,layout:{name:"cose",animate:false,nodeRepulsion:9000},
 style:[{selector:"node",style:{label:"data(label)","font-size":9,color:"#888","background-color":"#bbb",width:12,height:12}},
 {selector:"node:parent",style:{"background-opacity":0.08,"background-color":"data(color)","border-color":"data(color)","border-width":2,label:"data(label)","font-size":14,color:"data(color)","text-valign":"top"}},
 {selector:"edge",style:{width:1,"line-color":"#9995","target-arrow-shape":"triangle","target-arrow-color":"#9995","curve-style":"bezier","arrow-scale":0.6}},
 {selector:".hl",style:{"background-color":"#E9682D","line-color":"#E9682D","target-arrow-color":"#E9682D",width:3}}]});
const info=document.getElementById("info");
cy.on("tap","node",e=>{const d=e.target.data();cy.elements().removeClass("hl");e.target.addClass("hl");e.target.connectedEdges().addClass("hl");
 info.textContent=d.path?`${d.kind} ${d.label}\\n${d.path}\\ncapa: ${d.layer}\\nmétodos: ${(d.methods||[]).join(", ")||"—"}\\nusa: ${e.target.outgoers("node").map(n=>n.data("label")).join(", ")||"—"}\\nlo usan: ${e.target.incomers("node").map(n=>n.data("label")).join(", ")||"—"}`:`módulo ${d.label}`});
document.getElementById("q").addEventListener("input",ev=>{const q=ev.target.value.toLowerCase(),res=document.getElementById("res");res.innerHTML="";if(!q)return;
 cy.nodes().filter(n=>n.data("label").toLowerCase().includes(q)).slice(0,30).forEach(n=>{const div=document.createElement("div");div.className="item";div.innerHTML=`<code>${n.data("label")}</code> <small>${n.data("path")||""}</small>`;div.onclick=()=>{cy.animate({center:{eles:n},zoom:2});n.emit("tap")};res.appendChild(div)})});
const al=document.getElementById("alerts");
DATA.violations.forEach(v=>al.insertAdjacentHTML("beforeend",`<div class="item warn">${v.rule}: <code>${v.file}</code></div>`));
DATA.clones.forEach(c=>al.insertAdjacentHTML("beforeend",`<div class="item warn">duplicado: <code>${c.places.join(" · ")}</code></div>`));
if(!al.innerHTML)al.textContent="Sin alertas.";
</script></body></html>"""


def write_html(data: dict, out: Path) -> None:
    slim = {"files": [{k: f[k] for k in ("path", "module", "layer", "is_test", "symbols")} for f in data["files"]],
            "typeEdges": data["typeEdges"], "violations": data["violations"], "clones": data["clones"]}
    (out / "graph.html").write_text(HTML.replace("__DATA__", json.dumps(slim, ensure_ascii=False)), encoding="utf-8")


# ---------------------------------------------------------------- consultas

def load_or_build(root: Path, out: Path) -> dict:
    cached = out / "codemap.json"
    return json.loads(cached.read_text(encoding="utf-8")) if cached.exists() else build(root)


def all_symbols(data: dict):
    for f in data["files"]:
        if f["is_test"]:
            continue
        for s in f["symbols"]:
            yield f, s


def query(data: dict, text: str, limit: int = 15) -> list[tuple[float, str]]:
    words = set(split_words(text)) | {w.lower() for w in re.findall(r"\w{3,}", text)}
    scored = []
    for f, s in all_symbols(data):
        target = set(split_words(s["name"])) | set(split_words(Path(f["path"]).stem)) | {f["module"].lower(), f["layer"]}
        overlap = len(words & target) / max(len(words), 1)
        ratio = difflib.SequenceMatcher(None, text.lower().replace(" ", ""), s["name"].lower()).ratio()
        partial = max((1.0 for w in words for t in target if len(w) >= 4 and (w in t or t in w)), default=0)
        score = overlap * 0.6 + ratio * 0.25 + partial * 0.15
        if score >= 0.3:
            where = f"{f['path']}:{s['line']}"
            owner = f"{s['container']}." if s["container"] else ""
            scored.append((round(score, 2), f"{s['kind']:<11} {owner}{s['name']:<40} {where}"))
    return sorted(scored, reverse=True)[:limit]


def similar(data: dict, names: list[str], threshold: float = 0.78) -> list[str]:
    hits = []
    for name in names:
        nw = set(split_words(name))
        for f, s in all_symbols(data):
            if s["kind"] == "constructor":
                continue
            ratio = difflib.SequenceMatcher(None, name.lower(), s["name"].lower()).ratio()
            sw = set(split_words(s["name"]))
            jac = len(nw & sw) / len(nw | sw) if nw | sw else 0
            if ratio >= threshold or jac >= 0.67 or name.lower() == s["name"].lower():
                hits.append(f"{name} ≈ {s['kind']} {s['name']} en {f['path']}:{s['line']}")
    return sorted(set(hits))


# ---------------------------------------------------------------- escritura y comparación

OUTPUT_FILES = ("codemap.json", "CODEMAP.md", "modules.mmd", "graph.html")
# Valores que cambian en cada ejecución y no cuentan al comparar: fecha de generación y ruta absoluta del repositorio.
VOLATILE = (
    re.compile(r'"generatedAt":"[^"]*"'),
    re.compile(r'"root":"(?:[^"\\]|\\.)*"'),
    re.compile(r"el \d{4}-\d{2}-\d{2}T[\d:]+Z\. No editar"),
)


def normalized(text: str) -> str:
    text = text.replace("\r\n", "\n")
    for pattern in VOLATILE:
        text = pattern.sub("", text)
    return text


def write_outputs(data: dict, out: Path) -> None:
    out.mkdir(parents=True, exist_ok=True)
    (out / "codemap.json").write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    write_markdown(data, out)
    write_mermaid(data, out)
    write_html(data, out)


def stale_outputs(fresh: Path, out: Path) -> list[str]:
    """Archivos de `out` ausentes o distintos de los recién generados (sin contar fecha ni ruta)."""
    stale = []
    for name in OUTPUT_FILES:
        current = out / name
        if not current.exists() or normalized(current.read_text(encoding="utf-8")) != normalized((fresh / name).read_text(encoding="utf-8")):
            stale.append(name)
    return stale


# ---------------------------------------------------------------- main

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", type=Path, default=REPO)
    parser.add_argument("--out", type=Path, default=None)
    parser.add_argument("--query", metavar="TEXTO")
    parser.add_argument("--similar", nargs="+", metavar="NOMBRE")
    parser.add_argument("--check", action="store_true", help="no escribe; falla si hay violaciones, clones fuera de la línea base o el mapa está desactualizado")
    parser.add_argument("--update-baseline", action="store_true")
    args = parser.parse_args()
    root = args.root.resolve()
    out = (args.out or (root / "docs" / "architecture")).resolve()

    if args.query:
        results = query(load_or_build(root, out), args.query)
        print("\n".join(f"{score:.2f}  {line}" for score, line in results) or "Sin coincidencias: probablemente no existe; créalo siguiendo el molde _Template.")
        return 0
    if args.similar:
        hits = similar(load_or_build(root, out), args.similar)
        print("\n".join(hits) or "Sin nombres parecidos.")
        return 1 if hits else 0

    data = build(root)
    with tempfile.TemporaryDirectory() as tmp:
        fresh = Path(tmp)
        write_outputs(data, fresh)
        stale = stale_outputs(fresh, out)
        if not args.check and stale:
            out.mkdir(parents=True, exist_ok=True)
            for name in stale:  # solo se reescribe lo que de verdad cambió; siempre con saltos de línea LF
                (out / name).write_bytes((fresh / name).read_bytes().replace(b"\r\n", b"\n"))
    n_types = sum(1 for _, s in all_symbols(data) if s["kind"] not in ("method", "constructor", "function"))
    print(f"codemap: {len(data['files'])} archivos, {n_types} tipos, {len(data['moduleEdges'])} dependencias entre módulos, "
          f"{len(data['violations'])} violaciones, {len(data['clones'])} grupos de código duplicado → {out}"
          + (" (sin escribir)" if args.check else f" ({len(stale)} archivo(s) actualizado(s))"))

    baseline_path = out / "clones-baseline.json"
    if args.update_baseline:
        baseline_path.write_text(json.dumps(sorted(c["hash"] for c in data["clones"]), indent=1), encoding="utf-8")
        print(f"línea base actualizada: {baseline_path}")
    if args.check:
        baseline = set(json.loads(baseline_path.read_text())) if baseline_path.exists() else set()
        new_clones = [c for c in data["clones"] if c["hash"] not in baseline]
        for v in data["violations"]:
            print(f"VIOLACIÓN {v['rule']}: {v['file']} — {v['detail']}")
        for c in new_clones:
            print("DUPLICADO: " + " · ".join(c["places"]))
        for name in stale:
            print(f"DESACTUALIZADO: {out.name}/{name}")
        if data["violations"] or new_clones or stale:
            if stale:
                print("\nEl mapa de docs/architecture no coincide con el código: corre `python scripts/codemap.py` y sube el resultado.")
            if data["violations"] or new_clones:
                print("\nReutiliza lo existente o extrae una función común; si el duplicado es intencional, corre --update-baseline en un PR revisado.")
            return 1
        print("OK — sin violaciones ni duplicados nuevos y mapa al día")
    return 0


if __name__ == "__main__":
    sys.exit(main())
