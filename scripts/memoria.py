#!/usr/bin/env python3
"""Memoria por tarea de Knock (KNOCK-189).

Desde 2026-11-21 cada tarea registra su entrada en un archivo propio, docs/memoria/AAAA-MM-DD-KNOCK-N[-slug].md,
para que dos PR que agregan memoria al mismo tiempo no choquen en Memoria.md. Memoria.md conserva el historial anterior.

Cada entrada lleva las secciones «Qué cambió», «Por qué» y «A evitar» (y, opcional, «Decidido»), como las de Memoria.md.

Uso:
  python scripts/memoria.py --check   # valida nombre y secciones de docs/memoria/*.md (CI)
  python scripts/memoria.py --index   # escribe docs/memoria/INDEX.md (generado: no se versiona)
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MEMORIA_DIR = ROOT / "docs" / "memoria"
HISTORY = ROOT / "Memoria.md"
NAME = re.compile(r"^(\d{4})-(\d{2})-(\d{2})-(KNOCK-\d+)(?:-([a-z0-9]+(?:-[a-z0-9]+)*))?\.md$")
EXCLUDED = ("README.md", "INDEX.md")
# Cada sección acepta el encabezado como «## Qué cambió», «- **Qué cambió:**» o «**Qué cambió**» (el formato de Memoria.md).
REQUIRED = (("Qué cambió", ("Qué cambió",)), ("Por qué", ("Por qué", "Porqué")), ("A evitar", ("A evitar",)))
TITLE = re.compile(r"^#{1,2}\s+(.+?)\s*$")


def entry_files(directory: Path = MEMORIA_DIR) -> list[Path]:
    """Archivos de entrada (todo `*.md` menos README.md e INDEX.md), ordenados por nombre."""
    if not directory.is_dir():
        return []
    return sorted(p for p in directory.glob("*.md") if p.name not in EXCLUDED)


def has_section(text: str, labels: tuple[str, ...]) -> bool:
    for label in labels:
        escaped = re.escape(label)
        if re.search(rf"^(?:#{{2,4}}\s+{escaped}\b|\s*[-*]?\s*\*\*{escaped}\s*:?\*\*)", text, re.M):
            return True
    return False


def title_of(text: str, fallback: str) -> str:
    for line in text.splitlines():
        match = TITLE.match(line)
        if match:
            return match.group(1)
    return fallback


def check_directory(directory: Path = MEMORIA_DIR) -> list[str]:
    errors: list[str] = []
    for path in entry_files(directory):
        match = NAME.match(path.name)
        if not match:
            errors.append(f"{path.name}: el nombre debe ser AAAA-MM-DD-KNOCK-N[-slug].md (ej. 2026-11-21-KNOCK-189.md)")
            continue
        year, month, day, _key, _slug = match.groups()
        if not (1 <= int(month) <= 12 and 1 <= int(day) <= 31):
            errors.append(f"{path.name}: la fecha {year}-{month}-{day} no es válida")
        text = path.read_text(encoding="utf-8")
        for name, labels in REQUIRED:
            if not has_section(text, labels):
                errors.append(f"{path.name}: falta la sección «{name}»")
    return errors


def history_count(history: Path = HISTORY) -> int:
    if not history.exists():
        return 0
    return sum(1 for line in history.read_text(encoding="utf-8").splitlines() if line.startswith("## "))


def build_index(directory: Path = MEMORIA_DIR, history: Path = HISTORY) -> str:
    entries = []
    for path in entry_files(directory):
        match = NAME.match(path.name)
        date = f"{match.group(1)}-{match.group(2)}-{match.group(3)}" if match else "sin fecha"
        entries.append((date, path.name, title_of(path.read_text(encoding="utf-8"), path.stem)))
    entries.sort(key=lambda e: (e[0], e[1]), reverse=True)
    lines = [
        "# Índice de memoria por tarea",
        "",
        "> Generado por `python scripts/memoria.py --index`. No se versiona ni se edita a mano.",
        "",
        f"- Entradas históricas en `Memoria.md` (hasta 2026-11-20): {history_count(history)}",
        f"- Entradas por tarea en `docs/memoria/`: {len(entries)}",
        "",
        "## Entradas por tarea (más recientes primero)",
        "",
    ]
    lines += [f"- {date} · [{name}]({name}) — {title}" for date, name, title in entries] or ["_Aún no hay entradas por tarea._"]
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--check", action="store_true", help="valida los archivos de docs/memoria")
    group.add_argument("--index", action="store_true", help="escribe docs/memoria/INDEX.md")
    args = parser.parse_args()
    if args.index:
        MEMORIA_DIR.mkdir(parents=True, exist_ok=True)
        (MEMORIA_DIR / "INDEX.md").write_bytes(build_index().encode("utf-8"))
        print(f"OK — índice escrito en {MEMORIA_DIR / 'INDEX.md'} ({len(entry_files())} entradas por tarea)")
        return 0
    errors = check_directory()
    if errors:
        print("ERRORES:")
        print("\n".join(f"  - {e}" for e in errors))
        return 1
    print(f"OK — {len(entry_files())} entradas de memoria por tarea válidas")
    return 0


if __name__ == "__main__":
    sys.exit(main())
