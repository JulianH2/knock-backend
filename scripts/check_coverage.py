#!/usr/bin/env python3
"""Umbral de cobertura para Cobertura XML (.NET), lcov (Flutter) o json-summary (Angular/Istanbul).

Uso:
  python scripts/check_coverage.py --cobertura reports/coverage/Cobertura.xml --min 60 [--min-branch 50]
  python scripts/check_coverage.py --lcov app/coverage/lcov.info --min 50 --exclude .g.dart --exclude packages/knock_api
  python scripts/check_coverage.py --istanbul panel/coverage/coverage-summary.json --min 60
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


def cobertura(path: Path) -> tuple[float, float | None]:
    """Lee `line-rate` y `branch-rate` del elemento raíz <coverage>, sin analizar el XML completo
    (así no se procesan entidades ni DTD externos; no hace falta defusedxml)."""
    head = path.read_text(encoding="utf-8")[:8192]
    tag = re.search(r"<coverage\b[^>]*>", head)
    if not tag:
        sys.exit(f"ERROR: {path} no parece un informe Cobertura (no se encontró <coverage ...>)")
    attrs = dict(re.findall(r'([\w-]+)="([^"]*)"', tag.group(0)))
    return float(attrs.get("line-rate", 0)) * 100, float(attrs.get("branch-rate", 0)) * 100


def lcov(path: Path, excludes: list[str]) -> tuple[float, float | None]:
    lf = lh = bf = bh = 0
    current = ""
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("SF:"):
            current = line[3:]
        skip = any(e in current for e in excludes)
        if skip:
            continue
        key, _, value = line.partition(":")
        if key in ("LF", "LH", "BRF", "BRH"):
            n = int(value or 0)
            lf += n if key == "LF" else 0
            lh += n if key == "LH" else 0
            bf += n if key == "BRF" else 0
            bh += n if key == "BRH" else 0
    return (lh / lf * 100 if lf else 0.0), (bh / bf * 100 if bf else None)


def istanbul(path: Path) -> tuple[float, float | None]:
    total = json.loads(path.read_text(encoding="utf-8"))["total"]
    return float(total["lines"]["pct"]), float(total["branches"]["pct"])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    src = parser.add_mutually_exclusive_group(required=True)
    src.add_argument("--cobertura", type=Path)
    src.add_argument("--lcov", type=Path)
    src.add_argument("--istanbul", type=Path)
    parser.add_argument("--min", type=float, required=True, help="mínimo de líneas (%%)")
    parser.add_argument("--min-branch", type=float, default=None, help="mínimo de ramas (%%)")
    parser.add_argument("--exclude", action="append", default=[])
    args = parser.parse_args()
    path = args.cobertura or args.lcov or args.istanbul
    if not path.exists():
        print(f"ERROR: no existe {path} (¿se ejecutaron las pruebas con cobertura?)")
        return 1
    lines, branches = cobertura(path) if args.cobertura else lcov(path, args.exclude) if args.lcov else istanbul(path)
    print(f"Cobertura de líneas: {lines:.1f} % (mínimo {args.min} %)" + (f" · ramas: {branches:.1f} %" if branches is not None else ""))
    failed = lines < args.min or (args.min_branch is not None and branches is not None and branches < args.min_branch)
    if failed:
        print("ERROR: la cobertura está por debajo del mínimo. No bajes el umbral: agrega pruebas que validen comportamiento.")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
