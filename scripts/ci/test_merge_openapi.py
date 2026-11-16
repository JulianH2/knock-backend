#!/usr/bin/env python3
"""Pruebas de merge_openapi.py (KNOCK-137). Uso: python scripts/ci/test_merge_openapi.py"""
from __future__ import annotations

import copy
import json
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from merge_openapi import merge, render  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
PROBLEM = {"type": "object", "properties": {"title": {"type": "string"}}}


def contract(paths: dict, schemas: dict | None = None, tags: list[str] | None = None, version: str = "1.0.0") -> dict:
    return {
        "openapi": "3.2.0",
        "info": {"title": "x", "version": version},
        "paths": paths,
        "tags": [{"name": t} for t in tags or []],
        "components": {"schemas": {"ProblemDetails": PROBLEM, **(schemas or {})}},
    }


class MergeOpenApiTests(unittest.TestCase):
    def test_joins_paths_schemas_and_tags(self) -> None:
        market = contract({"/a": {"get": {}}}, {"A": {}}, ["Zeta"])
        identity = contract({"/b": {"get": {}}}, {"B": {}}, ["Alfa"])
        merged = merge(market, identity)
        self.assertEqual(list(merged["paths"]), ["/a", "/b"])
        self.assertEqual(sorted(merged["components"]["schemas"]), ["A", "B", "ProblemDetails"])
        self.assertEqual([t["name"] for t in merged["tags"]], ["Alfa", "Zeta"])

    def test_same_path_in_both_fails(self) -> None:
        with self.assertRaisesRegex(ValueError, "/a está en los dos"):
            merge(contract({"/a": {}}), contract({"/a": {}}))

    def test_same_schema_name_with_different_content_fails(self) -> None:
        with self.assertRaisesRegex(ValueError, "esquema A es distinto"):
            merge(contract({}, {"A": {"type": "string"}}), contract({}, {"A": {"type": "integer"}}))

    def test_identical_shared_schema_is_joined_once(self) -> None:
        merged = merge(contract({"/a": {}}), contract({"/b": {}}))
        self.assertEqual(list(merged["components"]["schemas"]), ["ProblemDetails"])

    def test_different_api_version_fails(self) -> None:
        with self.assertRaisesRegex(ValueError, "info.version"):
            merge(contract({}), contract({}, version="2.0.0"))

    def test_other_components_sections_are_not_dropped_silently(self) -> None:
        identity = contract({})
        identity["components"]["securitySchemes"] = {"bearer": {}}
        with self.assertRaisesRegex(ValueError, "securitySchemes"):
            merge(contract({}), identity)

    def test_does_not_mutate_the_inputs(self) -> None:
        market, identity = contract({"/a": {}}), contract({"/b": {}})
        before = copy.deepcopy((market, identity))
        merge(market, identity)
        self.assertEqual((market, identity), before)

    def test_output_is_lf_with_final_newline_and_valid_json(self) -> None:
        text = render(merge(contract({"/a": {}}), contract({"/b": {}})))
        self.assertTrue(text.endswith("\n"))
        self.assertNotIn("\r", text)
        self.assertEqual(json.loads(text)["openapi"], "3.2.0")

    def test_real_contracts_of_the_repository_merge_without_conflicts(self) -> None:
        market = json.loads((ROOT / "docs" / "openapi.v1.json").read_text(encoding="utf-8"))
        identity = json.loads((ROOT / "docs" / "openapi.identity.v1.json").read_text(encoding="utf-8"))
        merged = merge(market, identity)
        self.assertEqual(len(merged["paths"]), len(market["paths"]) + len(identity["paths"]))


if __name__ == "__main__":
    unittest.main()
