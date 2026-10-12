#!/usr/bin/env python3
"""Reglas de ramas, commits y PR de Knock (job branch-name del CI y git hooks).

Esquema (Sprint Planning · Nomenclatura):
  Ramas fijas: main · release · develop · sprint-XX
  Personal:    <tipo>/KNOCK-<N>-<english-kebab-description>   tipos: feature, fix, docs, refactor, test, chore
  Urgente:     hotfix/KNOCK-<N>-<english-kebab-description>
  Combinaciones (destino ← origen):
    sprint-XX ← <tipo>/KNOCK-N
    develop   ← sprint-XX | hotfix/* | dependabot/* | contract/api-<sha> (automático)
    release   ← develop | main
    main      ← release | hotfix/* | release-please--*
  Commit: type(scope): description in english, imperative, lowercase (KNOCK-N)
          types: feat, fix, docs, refactor, test, chore, ci, build, perf
          La clave es obligatoria y debe coincidir con la de la rama. Prohibida la atribución a IA (Co-Authored-By de agentes,
          líneas «Generated with…»); un Co-Authored-By de una persona del equipo sí se permite.
          Excepción para bots (sin clave KNOCK-N): chore(main|release|deps) de release-please, Dependabot y contract-publish,
          y build(deps) de Dependabot; ver EXEMPT_COMMIT.
  PR:     KNOCK-N · <ID> <título>   con la sección «Uso de IA» completa

Uso:
  python scripts/ci/branch_rules.py branch --head feature/KNOCK-45-request-quote --base sprint-02
  python scripts/ci/branch_rules.py commits --range origin/sprint-02..HEAD [--head <rama>]  # --head por defecto: $GITHUB_HEAD_REF
  python scripts/ci/branch_rules.py msg --file .git/COMMIT_EDITMSG [--branch <rama>]
  python scripts/ci/branch_rules.py pr --head feature/KNOCK-45-request-quote --title "KNOCK-45 · HU-024 Pedir una cotización" --body-file body.md
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = json.loads((ROOT / ".agents" / "knock.json").read_text(encoding="utf-8")) if (ROOT / ".agents" / "knock.json").exists() else {}
SCOPES = CONFIG.get("scopes", ["identity", "catalog", "requests", "messaging", "reputation", "trust", "notifications", "assistant", "app", "panel", "ci", "infra", "docs"])
SLUG = r"[a-z0-9]+(?:-[a-z0-9]+)*"
PERSONAL = re.compile(rf"^(feature|fix|docs|refactor|test|chore)/KNOCK-(\d+)-{SLUG}$")
HOTFIX = re.compile(rf"^hotfix/KNOCK-(\d+)-{SLUG}$")
SPRINT = re.compile(r"^sprint-\d{2}$")
DEPENDABOT = re.compile(r"^dependabot/")
RELEASE_PLEASE = re.compile(r"^release-please--")
CONTRACT = re.compile(r"^contract/api-[0-9a-f]{7,40}$")  # PR automático de contract-publish (solo knock-frontend)
TYPES = "feat|fix|docs|refactor|test|chore|ci|build|perf"
COMMIT = re.compile(rf"^(?P<type>{TYPES})\((?P<scope>[a-z0-9-]+)\)!?: (?P<desc>[a-z0-9].{{2,}}?) \((?P<key>KNOCK-\d+)\)$")
MAX_SUBJECT = 100
AI_ATTRIBUTION = re.compile(
    r"^\s*co-authored-by\s*:.*(\b(claude|anthropic|copilot|codex|gemini|cursor|opencode|chatgpt|openai)\b|noreply@(anthropic|openai)\.com)"
    r"|^\s*generated with\b|\U0001F916",
    re.I | re.M)
# Bots y generados que no llevan clave KNOCK-N: release-please (chore(main|release)), Dependabot (build(deps)) y el commit
# automático de contract-publish.yml (chore(deps)). Los trailers prohibidos se revisan igual en todos los commits.
EXEMPT_COMMIT = re.compile(r"^(Merge |Revert \"|fixup! |squash! |chore\((main|release|deps)\)|build\(deps(-dev)?\))")
PR_TITLE = re.compile(r"^(KNOCK-\d+) · (HU-\d{3}|TT-[A-Z]+-\d{2}|BUG-\d{3}) .{3,}$")


def allowed(base: str, head: str) -> bool:
    if SPRINT.match(base):
        return bool(PERSONAL.match(head))
    if base == "develop":
        return bool(SPRINT.match(head) or HOTFIX.match(head) or DEPENDABOT.match(head) or CONTRACT.match(head))
    if base == "release":
        return head in ("develop", "main")
    if base == "main":
        return head == "release" or bool(HOTFIX.match(head) or RELEASE_PLEASE.match(head))
    return False


def check_branch(head: str, base: str | None) -> list[str]:
    errors = []
    known = PERSONAL.match(head) or HOTFIX.match(head) or SPRINT.match(head) or DEPENDABOT.match(head) or RELEASE_PLEASE.match(head) or CONTRACT.match(head) \
        or head in ("main", "release", "develop")
    if not known:
        errors.append(f"Nombre de rama inválido: `{head}`. Usa `<tipo>/KNOCK-<N>-<english-description>` (feature, fix, docs, refactor, test, chore) "
                      "o `hotfix/KNOCK-<N>-<english-description>`, en inglés, minúsculas y kebab-case.")
    if base and not allowed(base, head):
        errors.append(f"Combinación no permitida: `{base}` ← `{head}`. Permitidas: sprint-XX ← <tipo>/KNOCK-N · develop ← sprint-XX | hotfix | dependabot · "
                      "release ← develop | main · main ← release | hotfix | release-please--*.")
    return errors


def check_message(subject: str, branch: str | None = None) -> list[str]:
    subject = subject.strip()
    if not subject or EXEMPT_COMMIT.match(subject):
        return []
    example = "feat(requests): upload up to 5 photos with the request (KNOCK-45)"
    m = COMMIT.match(subject)
    if not m:
        return [f"Commit inválido: «{subject}». Formato (en inglés): type(scope): description, imperative, lowercase (KNOCK-N), "
                f"p. ej. «{example}». Types: {TYPES.replace('|', ', ')}. Scopes: {', '.join(SCOPES)}. La descripción empieza en minúscula; la clave (KNOCK-N) es obligatoria y va al final."]
    errors = []
    if m.group("scope") not in SCOPES:
        errors.append(f"Scope desconocido «{m.group('scope')}» en «{subject}». Permitidos: {', '.join(SCOPES)}.")
    if len(subject) > MAX_SUBJECT:
        errors.append(f"El encabezado tiene {len(subject)} caracteres; el máximo es {MAX_SUBJECT}.")
    if m.group("desc").rstrip().endswith("."):
        errors.append("La descripción no termina en punto.")
    branch_key = re.search(r"KNOCK-(\d+)", branch or "")
    if branch_key and m.group("key") != f"KNOCK-{branch_key.group(1)}":
        errors.append(f"La clave del commit ({m.group('key')}) no coincide con la de la rama (KNOCK-{branch_key.group(1)}).")
    return errors


def check_trailers(message: str) -> list[str]:
    if AI_ATTRIBUTION.search(message):
        return ["Prohibida la atribución a IA en commits: trailers Co-Authored-By de agentes (Claude, Copilot, Codex, Gemini, Cursor, opencode, noreply@anthropic.com) "
                "y líneas «Generated with…». Un Co-Authored-By de una persona del equipo sí se permite. "
                "El uso de IA se declara solo en la sección «Uso de IA» de la plantilla del PR."]
    return []


def check_full_message(message: str, branch: str | None = None) -> list[str]:
    lines = [ln for ln in message.splitlines() if not ln.startswith("#")]
    subject = next((ln for ln in lines if ln.strip()), "")
    return check_message(subject, branch) + check_trailers("\n".join(lines))


def check_commits(rev_range: str, branch: str | None = None) -> list[str]:
    out = subprocess.run(["git", "log", "--format=%B%x1e", rev_range], cwd=ROOT, capture_output=True, text=True, check=False).stdout
    return [e for message in out.split("\x1e") if message.strip() for e in check_full_message(message, branch)]


def check_pr(head: str, title: str, body: str) -> list[str]:
    if not (PERSONAL.match(head) or HOTFIX.match(head)):
        return []
    errors = []
    m = PR_TITLE.match(title.strip())
    if not m:
        errors.append(f"Título de PR inválido: «{title}». Formato: «KNOCK-45 · HU-024 Pedir una cotización».")
    branch_key = (PERSONAL.match(head) or HOTFIX.match(head)).group(2 if PERSONAL.match(head) else 1)
    if m and m.group(1) != f"KNOCK-{branch_key}":
        errors.append(f"El título dice {m.group(1)} pero la rama es KNOCK-{branch_key}.")
    section = re.search(r"##\s*Uso de IA\s*\n(.*?)(?=\n## |\Z)", body, re.S)
    filled = False
    if section:
        text = re.sub(r"<!--.*?-->", "", section.group(1), flags=re.S)
        for line in text.splitlines():
            line = line.strip().lstrip("-*").strip()
            if line and not line.endswith(":"):
                filled = True
    if not filled:
        errors.append("La sección «## Uso de IA» falta o está vacía: indica herramienta, qué generó y cómo se validó (o «No se usó IA»).")
    for heading in ("## Criterios", "## Pruebas"):
        if heading not in body:
            errors.append(f"Falta la sección «{heading}» de la plantilla de PR.")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="cmd", required=True)
    b = sub.add_parser("branch"); b.add_argument("--head", required=True); b.add_argument("--base")
    c = sub.add_parser("commits"); c.add_argument("--range", required=True); c.add_argument("--head", default=os.environ.get("GITHUB_HEAD_REF"))
    m = sub.add_parser("msg"); m.add_argument("--file", required=True); m.add_argument("--branch")
    p = sub.add_parser("pr"); p.add_argument("--head", required=True); p.add_argument("--title", required=True); p.add_argument("--body-file", required=True)
    args = parser.parse_args()
    if args.cmd == "branch":
        errors = check_branch(args.head, args.base)
    elif args.cmd == "commits":
        errors = check_commits(args.range, args.head)
    elif args.cmd == "msg":
        errors = check_full_message(Path(args.file).read_text(encoding="utf-8"), args.branch)
    else:
        errors = check_pr(args.head, args.title, Path(args.body_file).read_text(encoding="utf-8"))
    for e in errors:
        print(f"::error::{e}" if __import__("os").environ.get("GITHUB_ACTIONS") else f"✗ {e}")
    if not errors:
        print("OK")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
