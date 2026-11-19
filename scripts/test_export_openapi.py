#!/usr/bin/env python3
"""Pruebas de export_openapi.py: ruta de salida por servicio, --service inválido y configuración sintética de Identity.
Uso: python scripts/test_export_openapi.py (sin dependencias; no arranca ninguna API)."""
from __future__ import annotations

import base64
import contextlib
import io
import json
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import export_openapi  # noqa: E402


class OutputPathTests(unittest.TestCase):
    def test_market_keeps_its_historical_path(self) -> None:
        self.assertEqual(export_openapi.ROOT / "docs" / "openapi.v1.json", export_openapi.default_output("market"))

    def test_identity_has_its_own_file(self) -> None:
        self.assertEqual(export_openapi.ROOT / "docs" / "openapi.identity.v1.json", export_openapi.default_output("identity"))

    def test_project_path_per_service(self) -> None:
        self.assertEqual("Knock.Market.Api.csproj", export_openapi.project_path("market").name)
        self.assertEqual("Knock.Identity.Api.csproj", export_openapi.project_path("identity").name)

    def test_outputs_are_distinct(self) -> None:
        outputs = {export_openapi.default_output(service) for service in export_openapi.SERVICES}
        self.assertEqual(len(export_openapi.SERVICES), len(outputs))


class ArgumentTests(unittest.TestCase):
    def test_default_service_is_market(self) -> None:
        args = export_openapi.parse_args([])
        self.assertEqual("market", args.service)
        self.assertIsNone(args.output)

    def test_identity_is_accepted(self) -> None:
        self.assertEqual("identity", export_openapi.parse_args(["--service", "identity"]).service)

    def test_invalid_service_fails(self) -> None:
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit) as raised:
            export_openapi.parse_args(["--service", "gateway"])
        self.assertEqual(2, raised.exception.code)


class NormalizeTests(unittest.TestCase):
    def test_crlf_is_unified_and_servers_removed(self) -> None:
        raw = json.dumps({"servers": [{"url": "http://127.0.0.1:1"}], "info": {"summary": "a\r\nb"}, "list": ["x\r\ny"]}).encode()
        text = export_openapi.normalize(raw)
        self.assertNotIn("servers", text)
        self.assertNotIn("\\r", text)
        self.assertIn("a\\nb", text)
        self.assertTrue(text.endswith("\n"))


class SyntheticEnvironmentTests(unittest.TestCase):
    def test_market_cursor_key_is_random_and_valid(self) -> None:
        first = export_openapi.service_environment("market")["Busqueda__Cursor__Key"]
        second = export_openapi.service_environment("market")["Busqueda__Cursor__Key"]
        self.assertEqual(32, len(base64.b64decode(first, validate=True)))
        self.assertNotEqual(first, second)

    def test_identity_secrets_are_random_and_valid(self) -> None:
        first = export_openapi.service_environment("identity")
        second = export_openapi.service_environment("identity")
        for key in ("Jwt__SigningKey", "Identity__PhoneProtection__EncryptionKeys__1", "Identity__PhoneProtection__HmacKeys__1"):
            self.assertGreaterEqual(len(base64.b64decode(first[key], validate=True)), 32, key)
            self.assertNotEqual(first[key], second[key], key)
        self.assertNotEqual(first["ConnectionStrings__KnockRedis"], second["ConnectionStrings__KnockRedis"])
        self.assertTrue(first["ConnectionStrings__KnockRedis"].startswith("127.0.0.1:6379,password="))
        self.assertEqual("demo-knock", first["Firebase__ProjectId"])


if __name__ == "__main__":
    unittest.main()
