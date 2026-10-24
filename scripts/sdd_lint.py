#!/usr/bin/env python3
"""Lint de specs SDD de Knock.

Revisa cada carpeta specs/<ID>-<slug>/ (ID = HU-NNN, TT-ÁREA-NN o BUG-NNN):
  - spec.md con las secciones 1 a 8 y «Aprobación», y un estado válido (Borrador, Aprobada, Firmada, Reemplazada)
  - plan.md, task.md y checklists/gates.md presentes
  - casos de prueba TC-<ID>-KK únicos, consecutivos y del mismo ID que la carpeta
  - una spec Aprobada, Firmada o Reemplazada tiene su clave de Jira (KNOCK-N) y revisor
  - constitution.md (raíz) idéntico a .specify/memory/constitution.md

Con --trace <ID> busca en el código de pruebas un test por cada caso:
  C#: TC_HU_001_01 en el nombre · Dart/TS: 'TC-HU-001-01' en la descripción.

Uso:
  python scripts/sdd_lint.py
  python scripts/sdd_lint.py --trace HU-001
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SPECS = ROOT / "specs"
CONFIG = json.loads((ROOT / ".agents" / "knock.json").read_text(encoding="utf-8")) if (ROOT / ".agents" / "knock.json").exists() else {}
REQUIRED_FILES = ("spec.md", "plan.md", "task.md", "checklists/gates.md")
SECTIONS = [rf"^## {n}\. " for n in range(1, 9)] + [r"^## Aprobación"]
FOLDER = re.compile(r"^(HU-\d{3}|TT-[A-Z]+-\d{2}|BUG-\d{3})(-[a-z0-9-]+)?$")
CASE_ROW = re.compile(r"^\|\s*(TC-(HU-\d{3}|TT-[A-Z]+-\d{2}|BUG-\d{3})-(\d{2}))\s*\|", re.M)
STATES = ("Borrador", "Aprobada", "Firmada", "Reemplazada")
TEST_GLOBS = CONFIG.get("test_globs", ["tests/**/*.cs", "**/test/**/*.dart", "**/integration_test/**/*.dart", "**/*.spec.ts", "**/e2e/**/*.ts"])


def spec_dirs() -> list[Path]:
    return sorted(p for p in SPECS.glob("*") if p.is_dir() and not p.name.startswith(("_", ".")))


def cases_of(text: str) -> list[str]:
    return [m.group(1) for m in CASE_ROW.finditer(text)]


def lint_spec(folder: Path) -> list[str]:
    errors = []
    m = FOLDER.match(folder.name)
    if not m:
        return [f"{folder.name}: nombre inválido (usa HU-NNN-slug, TT-AREA-NN-slug o BUG-NNN-slug)"]
    sid = m.group(1)
    for name in REQUIRED_FILES:
        if not (folder / name).exists():
            errors.append(f"{folder.name}: falta {name}")
    spec = folder / "spec.md"
    if not spec.exists():
        return errors
    text = spec.read_text(encoding="utf-8")
    for pattern in SECTIONS:
        if not re.search(pattern, text, re.M):
            errors.append(f"{folder.name}/spec.md: falta la sección «## {pattern[4:].replace(chr(92), '').strip()}»")
    state = re.search(r"\*\*Estado:\*\*\s*([^\n]+)", text)
    if not state or not any(state.group(1).strip().startswith(s) for s in STATES):
        errors.append(f"{folder.name}/spec.md: estado inválido (usa {', '.join(STATES)})")
    cases = cases_of(text)
    if not cases:
        errors.append(f"{folder.name}/spec.md: sin casos de prueba TC-{sid}-KK en la tabla de §2")
    dup = {c for c in cases if cases.count(c) > 1}
    if dup:
        errors.append(f"{folder.name}/spec.md: casos duplicados {sorted(dup)}")
    wrong = [c for c in cases if not c.startswith(f"TC-{sid}-")]
    if wrong:
        errors.append(f"{folder.name}/spec.md: casos de otro ID {wrong}")
    nums = [int(c[-2:]) for c in cases if c.startswith(f"TC-{sid}-")]
    if nums and sorted(nums) != list(range(1, len(nums) + 1)):
        errors.append(f"{folder.name}/spec.md: casos no consecutivos {sorted(nums)}")
    if state and state.group(1).strip().startswith(("Aprobada", "Firmada", "Reemplazada")):
        if not re.search(r"\bKNOCK-\d+\b", text):
            errors.append(f"{folder.name}/spec.md: está {state.group(1).strip()} pero no tiene clave de Jira (KNOCK-N)")
        if re.search(r"- \[ \] Revisor 1", text):
            errors.append(f"{folder.name}/spec.md: está {state.group(1).strip()} sin revisor marcado")
    return errors


def lint_constitution() -> list[str]:
    mirror, source = ROOT / "constitution.md", ROOT / ".specify/memory/constitution.md"
    if not mirror.exists():
        return ["constitution.md espejo no existe (corre python scripts/sync_agents.py)"]
    if mirror.read_text(encoding="utf-8") != source.read_text(encoding="utf-8"):
        return ["constitution.md difiere de .specify/memory/constitution.md (corre sync_agents.py)"]
    return []


def marked(spec_text: str, case: str, tag: str) -> bool:
    """El caso tiene el marcador «(tag)» en su fila de la tabla de §2."""
    return bool(re.search(rf"\|\s*{re.escape(case)}\s*\|[^\n]*\({re.escape(tag)}\)", spec_text))


def trace(spec_id: str) -> int:
    folder = next((d for d in spec_dirs() if d.name.startswith(spec_id)), None)
    if not folder:
        print(f"ERROR: no existe specs/{spec_id}*")
        return 1
    spec_text = (folder / "spec.md").read_text(encoding="utf-8")
    if re.search(r"\*\*Estado:\*\*\s*Reemplazada", spec_text):
        print(f"AVISO: {folder.name} está Reemplazada por otra spec; sus casos ya no se verifican")
        return 0
    cases = cases_of(spec_text)
    files = {f for g in TEST_GLOBS for f in ROOT.glob(g) if f.is_file() and "node_modules" not in f.parts}
    if not files:
        print(f"AVISO: aún no hay pruebas; {len(cases)} casos de {folder.name} sin verificar")
        return 0
    corpus = "\n".join(f.read_text(encoding="utf-8", errors="ignore") for f in files).replace("-", "_")
    missing = []
    print("| Caso | Prueba |\n|---|---|")
    for case in cases:
        found = case.replace("-", "_") in corpus
        by_command = not found and marked(spec_text, case, "comando")
        print(f"| {case} | {'OK' if found else 'COMANDO (evidencia en el PR)' if by_command else 'FALTA'} |")
        if not found and not by_command:
            missing.append(case)
    if missing:
        print(f"\nDRIFT: {len(missing)} caso(s) sin prueba. Si un caso no aplica a este repositorio, márcalo «(otro repo)»; si se verifica solo con un comando (build, formato, auditoría), márcalo «(comando)» y pega su salida en el PR; ambos en la columna Verificación.")
        other = [c for c in missing if marked(spec_text, c, "otro repo")]
        return 0 if len(other) == len(missing) else 1
    print("\nOK — todos los casos tienen prueba")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--trace", metavar="ID", help="verifica casos ↔ nombres de pruebas")
    args = parser.parse_args()
    if args.trace:
        return trace(args.trace)
    errors = lint_constitution()
    folders = spec_dirs()
    for folder in folders:
        errors += lint_spec(folder)
    if errors:
        print("ERRORES:")
        print("\n".join(f"  - {e}" for e in errors))
        return 1
    print(f"OK — {len(folders)} specs válidas")
    return 0


if __name__ == "__main__":
    sys.exit(main())
