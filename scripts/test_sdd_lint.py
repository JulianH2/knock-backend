#!/usr/bin/env python3
"""Pruebas mínimas del estado «Reemplazada» de sdd_lint.py. Uso: python scripts/test_sdd_lint.py (sin dependencias; no corre en el CI)."""
from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sdd_lint  # noqa: E402


class ReplacedStateTests(unittest.TestCase):
    def test_header_line_replaced_is_accepted(self) -> None:
        text = "> **Estado:** Reemplazada (por TT-BD-06)\n\n## 8\nTexto.\n"
        self.assertTrue(sdd_lint.is_replaced(sdd_lint.spec_state(text)))

    def test_body_that_quotes_the_state_does_not_replace_the_header(self) -> None:
        text = "> **Estado:** Aprobada\n\n## 8\nPasará a **Estado:** Reemplazada (por TT-BD-06) cuando se fusione.\n"
        self.assertEqual("Aprobada", sdd_lint.spec_state(text))
        self.assertFalse(sdd_lint.is_replaced(sdd_lint.spec_state(text)))

    def test_state_with_extra_characters_is_not_replaced(self) -> None:
        for state in ("Reemplazadaxyz", "Reemplazada por TT-BD-06", "Reemplazada (por algo)", "reemplazada"):
            self.assertFalse(sdd_lint.is_replaced(state), state)

    def test_plain_replaced_and_missing_state(self) -> None:
        self.assertTrue(sdd_lint.is_replaced("Reemplazada"))
        self.assertFalse(sdd_lint.is_replaced(None))


if __name__ == "__main__":
    unittest.main()
