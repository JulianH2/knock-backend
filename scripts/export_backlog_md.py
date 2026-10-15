#!/usr/bin/env python3
"""Convierte las fichas del backlog (.docx de cada HU y tarea técnica) en Markdown dentro de docs/backlog/.

Es la fuente de requisitos que leen las personas y los agentes: una ficha por HU-NNN o TT-AREA-NN, más un
INDEX.md. No sustituye a la spec (specs/), que es el contrato aprobado, ni a Jira, donde vive el estado.

Fuentes:
  --docx-dir  carpeta con los .docx (se recorre completa; cada archivo empieza con el ID, p. ej. HU-001_…)
  --jira-csv  exportación de Jira (columnas «Resumen» y «Clave de incidencia»); solo se toma la clave KNOCK-N

Los archivos generados llevan un aviso y no se editan a mano: se corrige la fuente y se vuelve a generar.

Requiere: pip install python-docx
Uso: python scripts/export_backlog_md.py --docx-dir "ruta/HU y TT" --jira-csv ruta/Jira.csv [--check]
"""
from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs" / "backlog"
ID = re.compile(r"^(HU-\d{3}|TT-[A-Z]+-\d{2})(?:[_ ]|$)")
JIRA_SUMMARY = re.compile(r"^(HU-\d{3}|TT-[A-Z]+-\d{2}) ·")
SECTION = re.compile(r"^\d{1,2}\. \S")
SKIP_FIELDS = {"estado"}  # el estado vive en Jira; aquí quedaría desactualizado
GROUPS = {"Sprint_0": "Sprint 0 (12/10 – 16/10/2026)", "Sprint_1": "Sprint 1 (19/10 – 23/10/2026)",
          "Sprint_2": "Sprint 2 (26/10 – 30/10/2026)", "Sprint_3": "Sprint 3 (02/11 – 06/11/2026)",
          "Sprint_4": "Sprint 4 (09/11 – 13/11/2026)", "Sprint_5": "Sprint 5 (16/11 – 20/11/2026)",
          "Sprint_6": "Sprint 6 (23/11 – 27/11/2026)", "Sprint_7": "Sprint 7 (30/11 – 04/12/2026)",
          "Reserva": "Siguiente versión (prioridad media)"}


def jira_keys(path: Path) -> dict[str, str]:
    keys: dict[str, str] = {}
    with path.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            m = JIRA_SUMMARY.match(row.get("Resumen", ""))
            if m:
                keys[m.group(1)] = row["Clave de incidencia"]
    return keys


def group_of(path: Path) -> str:
    for key, label in GROUPS.items():
        if path.parent.name.startswith(key):
            return label
    return path.parent.name


def runs_text(paragraph) -> str:
    """Texto del párrafo con **negritas** donde la ficha las usa."""
    out = []
    for run in paragraph.runs:
        text = run.text
        if run.bold and text.strip():
            lead, trail = text[:len(text) - len(text.lstrip())], text[len(text.rstrip()):]
            out.append(f"{lead}**{text.strip()}**{trail}")
        else:
            out.append(text)
    return re.sub(r"\*\*(\s*)\*\*", r"\1", "".join(out)).strip()


def row_cells(row) -> list[str]:
    cells: list[str] = []
    for cell in row.cells:
        text = cell.text.strip().replace("\n", "<br>").replace("|", "\\|")
        if not cells or cells[-1] != text:  # las celdas combinadas se repiten
            cells.append(text)
    return cells


def render_table(table) -> list[str]:
    rows = [row_cells(r) for r in table.rows]
    width = max(len(r) for r in rows)
    rows = [r + [""] * (width - len(r)) for r in rows]
    lines = ["| " + " | ".join(rows[0]) + " |", "|" + "---|" * width]
    lines += ["| " + " | ".join(r) + " |" for r in rows[1:]]
    return lines


def general_data(table, key: str | None) -> tuple[list[str], dict[str, str]]:
    """«Datos generales» como lista; devuelve también los campos para la cabecera."""
    fields: dict[str, str] = {}
    lines: list[str] = []
    for row in table.rows:
        cells = row_cells(row)
        pairs = [(cells[i], cells[i + 1]) for i in range(0, len(cells) - 1, 2)]
        for name, value in pairs:
            label = name.strip()
            if label.lower() in SKIP_FIELDS:
                continue
            if label.lower() == "clave de jira":
                value = key or "(sin clave en Jira)"
            elif key:
                value = value.replace("KNOCK-<N>", key)
            fields[label.lower()] = value
            lines.append(f"- **{label}:** {value}")
    return lines, fields


def convert(docx_path: Path, key: str | None) -> tuple[str, dict[str, str]]:
    from docx import Document
    from docx.oxml.ns import qn
    from docx.table import Table
    from docx.text.paragraph import Paragraph

    doc = Document(str(docx_path))
    body: list[str] = []
    fields: dict[str, str] = {}
    title = ""
    in_general = False
    for child in doc.element.body.iterchildren():
        if child.tag == qn("w:p"):
            p = Paragraph(child, doc)
            text = runs_text(p)
            plain = p.text.strip()
            if not plain:
                continue
            if plain in ("HISTORIA DE USUARIO", "TAREA TÉCNICA"):
                continue
            if not title:
                title = plain
                body.append(f"# {plain}")
                continue
            if SECTION.match(plain) and "List" not in p.style.name:
                in_general = plain.split(". ", 1)[1].lower().startswith("datos generales")
                body += ["", f"## {plain}", ""]
            elif p.style.name == "List Bullet":
                body.append(f"- {text}")
            elif p.style.name == "List Number":
                body.append(f"1. {text}")
            else:
                body += ["", text, ""]
        elif child.tag == qn("w:tbl"):
            table = Table(child, doc)
            if in_general:
                lines, fields = general_data(table, key)
            else:
                lines = render_table(table)
            body += [""] + lines + [""]
    text = "\n".join(body)
    if key:
        text = text.replace("KNOCK-<N>", key).replace("KNOCK-____", key)
    return re.sub(r"\n{3,}", "\n\n", text).strip() + "\n", {**fields, "título": fields.get("título", title)}


def front_matter(item_id: str, kind: str, key: str | None, fields: dict[str, str], source: str) -> str:
    def q(value: str) -> str:
        return json.dumps(value, ensure_ascii=False)

    pairs = [("id", item_id), ("tipo", kind), ("jira", key or ""), ("titulo", fields.get("título", "")),
             ("epica", fields.get("épica", "")), ("sprint", fields.get("sprint", "")),
             ("responsable", fields.get("responsable", "")),
             ("puntos", fields.get("puntos") or fields.get("story points") or ""), ("fuente", source)]
    lines = ["---"] + [f"{name}: {q(value)}" for name, value in pairs] + ["---"]
    return "\n".join(lines)


NOTICE = ("> Documento **generado** desde la ficha del backlog (`{src}`) con `scripts/export_backlog_md.py`. "
          "No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira "
          "({jira}); el contrato aprobado de la tarea es su spec en `specs/`.\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--docx-dir", required=True, type=Path)
    parser.add_argument("--jira-csv", required=True, type=Path)
    parser.add_argument("--check", action="store_true", help="no escribe; falla si docs/backlog no coincide")
    args = parser.parse_args()

    keys = jira_keys(args.jira_csv)
    files = sorted(p for p in args.docx_dir.rglob("*.docx") if ID.match(p.name))
    if not files:
        print(f"ERROR: no hay fichas HU-NNN / TT-AREA-NN en {args.docx_dir}")
        return 1
    generated: dict[str, str] = {}
    rows: list[tuple[str, str, str, str, str, str, str]] = []
    warnings: list[str] = []
    for path in files:
        item_id = ID.match(path.name).group(1)
        key = keys.get(item_id)
        if not key:
            warnings.append(f"{item_id}: sin clave en Jira")
        md, fields = convert(path, key)
        kind = "historia" if item_id.startswith("HU-") else "tarea técnica"
        head = front_matter(item_id, kind, key, fields, path.name)
        generated[f"{item_id}.md"] = f"{head}\n\n{NOTICE.format(src=path.name, jira=key or 'sin clave')}\n{md}"
        rows.append((group_of(path), item_id, fields.get("título", ""), key or "—", fields.get("responsable", ""),
                     fields.get("puntos") or fields.get("story points") or "", fields.get("épica", "")))
    for missing in sorted(set(keys) - {r[1] for r in rows}):
        warnings.append(f"{missing}: está en Jira pero no hay ficha")

    order = list(GROUPS.values())
    rows.sort(key=lambda r: (order.index(r[0]) if r[0] in order else 99, r[1]))
    index = ["# Backlog del proyecto", "",
             "Fichas de cada historia de usuario (HU) y tarea técnica (TT), generadas desde el backlog con "
             "`scripts/export_backlog_md.py`. **El estado vive en Jira**; el contrato aprobado de cada una es su "
             "spec en `specs/`. Las fichas no se editan a mano.", ""]
    current = ""
    for group, item_id, title, key, owner, points, epic in rows:
        if group != current:
            current = group
            index += ["", f"## {group}", "", "| ID | Título | Jira | Responsable | Pts | Épica |", "|---|---|---|---|---|---|"]
        index.append(f"| [{item_id}]({item_id}.md) | {title} | {key} | {owner} | {points} | {epic} |")
    generated["INDEX.md"] = "\n".join(index).replace("| |", "| — |") + "\n"

    if args.check:
        stale = [n for n, t in generated.items()
                 if not (OUT / n).exists() or (OUT / n).read_text(encoding="utf-8") != t]
        print("OK — docs/backlog al día" if not stale else f"DESACTUALIZADO: {len(stale)} archivo(s): {', '.join(stale[:8])}")
        return 1 if stale else 0
    OUT.mkdir(parents=True, exist_ok=True)
    for name, text in generated.items():
        (OUT / name).write_text(text, encoding="utf-8", newline="\n")
    print(f"OK — {len(rows)} fichas escritas en {OUT.relative_to(ROOT)} ({date.today()})")
    for w in warnings:
        print(f"AVISO: {w}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
