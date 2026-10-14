#!/usr/bin/env python3
"""Fija las acciones de GitHub Actions a SHA completos (cadena de suministro).

Convierte `uses: owner/repo@v4` en `uses: owner/repo@<sha40> # v4`. Dependabot (ecosistema
github-actions) sigue actualizando los SHA y el comentario de versión.

Uso:
  python scripts/ci/pin_actions.py          # reescribe .github/workflows/*.yml
  python scripts/ci/pin_actions.py --check  # CI: falla si hay acciones sin fijar
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
USES = re.compile(r"^(?P<pre>\s*-?\s*uses:\s*)(?P<action>[\w.-]+/[\w.-]+)(?P<path>/[\w./-]+)?@(?P<ref>[\w.-]+)(?P<post>\s*(#.*)?)$", re.M)
CACHE: dict[str, str | None] = {}


def resolve(action: str, ref: str) -> str | None:
    key = f"{action}@{ref}"
    if key in CACHE:
        return CACHE[key]
    out = subprocess.run(["git", "ls-remote", f"https://github.com/{action}", f"refs/tags/{ref}", f"refs/tags/{ref}^{{}}", f"refs/heads/{ref}"],
                         capture_output=True, text=True, timeout=60).stdout
    refs = dict(reversed(line.split("\t")) for line in out.splitlines() if "\t" in line)
    sha = refs.get(f"refs/tags/{ref}^{{}}") or refs.get(f"refs/tags/{ref}") or refs.get(f"refs/heads/{ref}")
    CACHE[key] = sha
    return sha


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    unpinned, failed = [], []
    for wf in sorted((ROOT / ".github" / "workflows").glob("*.y*ml")):
        text = wf.read_text(encoding="utf-8")

        def repl(m: re.Match) -> str:
            ref = m.group("ref")
            if re.fullmatch(r"[0-9a-f]{40}", ref):
                return m.group(0)
            unpinned.append(f"{wf.name}: {m.group('action')}@{ref}")
            if args.check:
                return m.group(0)
            sha = resolve(m.group("action"), ref)
            if not sha:
                failed.append(f"{m.group('action')}@{ref}")
                return m.group(0)
            return f"{m.group('pre')}{m.group('action')}{m.group('path') or ''}@{sha} # {ref}"

        new = USES.sub(repl, text)
        if new != text and not args.check:
            wf.write_text(new, encoding="utf-8")
    if args.check:
        if unpinned:
            print("Acciones sin fijar a SHA:\n  " + "\n  ".join(unpinned) + "\nCorre: python scripts/ci/pin_actions.py")
            return 1
        print("OK — todas las acciones están fijadas a SHA")
        return 0
    print(f"Fijadas {len(unpinned) - len(failed)} referencias" + (f"; no se pudieron resolver: {failed}" if failed else ""))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
