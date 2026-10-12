#!/usr/bin/env python3
"""Registro de excepciones de seguridad y calidad (security/exceptions.yaml).

Falla si:
  - una excepción no tiene id, herramienta, regla, alcance, justificación, responsable, fecha, vence o plan
  - una excepción está vencida (no hay excepciones permanentes)
  - el código suprime un analizador (nosemgrep, eslint-disable, #pragma warning disable, // ignore:,
    [SuppressMessage], @ts-ignore, NOSONAR, gitleaks:allow) sin citar un EXC-NNN registrado y vigente
  - .trivyignore contiene un CVE sin comentario «# EXC-NNN»

Uso: python scripts/ci/check_exceptions.py [--warn-days 7]
"""
from __future__ import annotations

import argparse
import datetime as dt
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REGISTRY = ROOT / "security" / "exceptions.yaml"
REQUIRED = ("id", "herramienta", "regla", "alcance", "justificacion", "responsable", "creada", "vence", "plan")
SUPPRESSION = re.compile(r"nosemgrep|eslint-disable|#pragma\s+warning\s+disable|//\s*ignore(?:_for_file)?:|\[SuppressMessage|@ts-ignore|@ts-expect-error|NOSONAR|gitleaks:allow")
SKIP = {".git", "node_modules", "bin", "obj", ".dart_tool", "build", "dist", ".angular", "coverage", "reports"}
CODE_EXT = {".cs", ".dart", ".ts", ".js", ".yml", ".yaml", ".props", ".editorconfig"}


def parse_registry(text: str) -> list[dict]:
    """YAML mínimo: lista de mapas planos bajo «excepciones:» (sin dependencias externas)."""
    items, current = [], None
    for raw in text.splitlines():
        line = raw.split(" #")[0].rstrip() if not raw.strip().startswith("#") else ""
        if not line.strip() or line.strip() == "excepciones:" or line.strip() == "excepciones: []":
            continue
        m = re.match(r"^\s*-\s+(\w+):\s*(.*)$", line)
        if m:
            current = {m.group(1): m.group(2).strip().strip("\"'")}
            items.append(current)
            continue
        m = re.match(r"^\s+(\w+):\s*(.*)$", line)
        if m and current is not None:
            current[m.group(1)] = m.group(2).strip().strip("\"'")
    return items


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--warn-days", type=int, default=7)
    args = parser.parse_args()
    today = dt.date.today()
    errors, warnings = [], []
    items = parse_registry(REGISTRY.read_text(encoding="utf-8")) if REGISTRY.exists() else []
    valid = set()
    for item in items:
        missing = [k for k in REQUIRED if not item.get(k)]
        if missing:
            errors.append(f"{item.get('id', '¿sin id?')}: faltan campos {missing}")
            continue
        try:
            expires = dt.date.fromisoformat(item["vence"])
        except ValueError:
            errors.append(f"{item['id']}: fecha «vence» inválida ({item['vence']})")
            continue
        if expires < today:
            errors.append(f"{item['id']}: venció el {expires}. Corrige el hallazgo o renueva la excepción con nueva justificación y aprobación.")
        elif (expires - today).days <= args.warn_days:
            warnings.append(f"{item['id']}: vence en {(expires - today).days} días")
            valid.add(item["id"])
        else:
            valid.add(item["id"])
        if (expires - dt.date.fromisoformat(item["creada"])).days > 90:
            errors.append(f"{item['id']}: una excepción dura como máximo 90 días.")

    for path in ROOT.rglob("*"):
        if not path.is_file() or set(path.relative_to(ROOT).parts) & SKIP or path.suffix not in CODE_EXT:
            continue
        rel = path.relative_to(ROOT).as_posix()
        if rel.startswith(("scripts/", "security/", ".github/", ".semgrep/", ".claude/", ".opencode/", ".codex/", ".gemini/", ".cursor/")):
            continue
        for n, line in enumerate(path.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
            if SUPPRESSION.search(line):
                ids = re.findall(r"EXC-\d{3}", line)
                if not ids:
                    errors.append(f"{rel}:{n}: supresión sin excepción registrada (agrega «EXC-NNN» en el comentario)")
                for i in ids:
                    if i not in valid:
                        errors.append(f"{rel}:{n}: {i} no existe o no está vigente en security/exceptions.yaml")

    trivy = ROOT / ".trivyignore"
    if trivy.exists():
        for n, line in enumerate(trivy.read_text(encoding="utf-8").splitlines(), 1):
            if line.strip() and not line.startswith("#") and not re.search(r"#\s*EXC-\d{3}", line):
                errors.append(f".trivyignore:{n}: «{line.strip()}» sin «# EXC-NNN»")

    for w in warnings:
        print(f"AVISO: {w}")
    for e in errors:
        print(f"ERROR: {e}")
    if not errors:
        print(f"OK — {len(items)} excepciones registradas, {len(valid)} vigentes")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
