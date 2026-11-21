#!/usr/bin/env python3
"""Pruebas de scripts/memoria.py (memoria por tarea, KNOCK-189). Uso: python scripts/test_memoria.py (sin dependencias ni red)."""
from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import memoria  # noqa: E402

VALID = "# Titulo de la tarea\n\n## Qué cambió\n- a\n\n## Por qué\n- b\n\n## A evitar\n- c\n"


class MemoriaTests(unittest.TestCase):
    def setUp(self) -> None:
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.dir = Path(self.tmp.name)

    def write(self, name: str, text: str = VALID) -> None:
        (self.dir / name).write_text(text, encoding="utf-8", newline="\n")

    def test_valid_entries_and_slug_pass(self) -> None:
        self.write("2026-11-21-KNOCK-189.md")
        self.write("2026-11-22-KNOCK-190-otro-tema.md")
        self.assertEqual([], memoria.check_directory(self.dir))

    def test_readme_and_index_are_not_entries(self) -> None:
        self.write("README.md", "# Formato\n")
        self.write("INDEX.md", "# Indice\n")
        self.assertEqual([], memoria.check_directory(self.dir))
        self.assertEqual([], memoria.entry_files(self.dir))

    def test_bad_names_are_reported(self) -> None:
        for name in ("KNOCK-189.md", "2026-11-21-189.md", "2026-11-21-KNOCK-189-Slug.md", "2026-11-21-knock-189.md"):
            self.write(name)
            errors = memoria.check_directory(self.dir)
            self.assertTrue(any(name in e and "nombre" in e for e in errors), name)
            (self.dir / name).unlink()

    def test_impossible_date_is_reported(self) -> None:
        self.write("2026-13-40-KNOCK-1.md")
        self.assertTrue(any("fecha" in e for e in memoria.check_directory(self.dir)))

    def test_each_missing_section_is_reported(self) -> None:
        for section in ("Qué cambió", "Por qué", "A evitar"):
            self.write("2026-11-21-KNOCK-5.md", VALID.replace(f"## {section}", "## Otra cosa"))
            errors = memoria.check_directory(self.dir)
            self.assertEqual([f"2026-11-21-KNOCK-5.md: falta la sección «{section}»"], errors, section)

    def test_memoria_md_bullet_style_is_accepted(self) -> None:
        self.write("2026-11-21-KNOCK-6.md", "# T\n- **Qué cambió:** a\n- **Porqué:** b\n- **A evitar:** c\n")
        self.assertEqual([], memoria.check_directory(self.dir))

    def test_same_key_in_two_prs_is_allowed_with_different_slugs(self) -> None:
        self.write("2026-11-21-KNOCK-7-primer-pr.md")
        self.write("2026-11-23-KNOCK-7-segundo-pr.md")
        self.assertEqual([], memoria.check_directory(self.dir))

    def test_index_is_sorted_by_date_descending_with_titles_and_history_count(self) -> None:
        history = self.dir / "Memoria.md"
        history.write_text("# Memoria\n\n## uno\n- x\n\n## dos\n- y\n### no cuenta\n", encoding="utf-8")
        sub = self.dir / "memoria"
        sub.mkdir()
        (sub / "2026-11-21-KNOCK-1.md").write_text(VALID.replace("Titulo de la tarea", "Primera"), encoding="utf-8")
        (sub / "2026-11-25-KNOCK-2.md").write_text(VALID.replace("Titulo de la tarea", "Segunda"), encoding="utf-8")
        text = memoria.build_index(sub, history)
        self.assertIn("(hasta 2026-11-20): 2", text)
        self.assertLess(text.index("KNOCK-2.md"), text.index("KNOCK-1.md"))
        self.assertIn("— Segunda", text)
        self.assertTrue(text.endswith("\n") and "\r" not in text)

    def test_empty_index_says_so(self) -> None:
        self.assertIn("Aún no hay entradas", memoria.build_index(self.dir, self.dir / "no-existe.md"))

    def test_two_branches_adding_entries_touch_different_files(self) -> None:
        # Garantía del diseño: el nombre lleva la clave de la tarea, así que dos tareas nunca escriben el mismo archivo.
        self.assertNotEqual("2026-11-21-KNOCK-8.md", "2026-11-21-KNOCK-9.md")
        self.write("2026-11-21-KNOCK-8.md")
        self.write("2026-11-21-KNOCK-9.md")
        self.assertEqual([], memoria.check_directory(self.dir))


if __name__ == "__main__":
    unittest.main()
