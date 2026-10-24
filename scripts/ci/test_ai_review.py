#!/usr/bin/env python3
"""Pruebas de scripts/ci/ai_review.py con servidores falsos en 127.0.0.1 (sin red externa ni llaves reales).

Uso: python scripts/ci/test_ai_review.py (sin dependencias; no corre en el CI todavía).
"""
from __future__ import annotations

import io
import json
import os
import subprocess
import sys
import tempfile
import threading
import unittest
from contextlib import redirect_stdout
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ai_review  # noqa: E402


class FakeServices(BaseHTTPRequestHandler):
    """Hace de proveedor del modelo y de la API de GitHub."""

    comments: list[dict] = []
    model_status = 200
    model_content = "**Resumen** cambio pequeño.\n**Veredicto**: Sin bloqueos"
    model_raw: object = None
    model_requests: list[dict] = []

    def log_message(self, *args: object) -> None:
        pass

    def _send(self, status: int, payload: object) -> None:
        data = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def _body(self) -> dict:
        return json.loads(self.rfile.read(int(self.headers.get("Content-Length", "0"))) or b"{}")

    def do_GET(self) -> None:  # noqa: N802
        self._send(200, FakeServices.comments)

    def do_POST(self) -> None:  # noqa: N802
        body = self._body()
        if self.path.endswith("/chat/completions"):
            FakeServices.model_requests.append({"auth": self.headers.get("Authorization"), "body": body})
            if FakeServices.model_status != 200:
                self._send(FakeServices.model_status, {"error": "falla simulada"})
                return
            if FakeServices.model_raw is not None:
                self._send(200, FakeServices.model_raw)
                return
            self._send(200, {"choices": [{"message": {"content": FakeServices.model_content}}],
                             "usage": {"prompt_tokens": 1200, "completion_tokens": 80}})
            return
        comment = {"id": len(FakeServices.comments) + 1, "body": body["body"]}
        FakeServices.comments.append(comment)
        self._send(201, comment)

    def do_PATCH(self) -> None:  # noqa: N802
        body = self._body()
        comment_id = int(self.path.rsplit("/", 1)[1])
        for comment in FakeServices.comments:
            if comment["id"] == comment_id:
                comment["body"] = body["body"]
        self._send(200, {"id": comment_id})


def git(cwd: Path, *args: str) -> str:
    return subprocess.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", "-c", "core.hooksPath=/dev/null", *args],
                          cwd=cwd, capture_output=True, text=True, encoding="utf-8", check=True).stdout.strip()


class AiReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.server = HTTPServer(("127.0.0.1", 0), FakeServices)
        threading.Thread(target=cls.server.serve_forever, daemon=True).start()
        cls.url = f"http://127.0.0.1:{cls.server.server_address[1]}"
        cls.repo = Path(tempfile.mkdtemp(prefix="ai_review_test_"))
        git(cls.repo, "init", "-q", "-b", "main")
        (cls.repo / "src").mkdir()
        (cls.repo / "src" / "a.cs").write_text("class A {}\n", encoding="utf-8")
        (cls.repo / "package-lock.json").write_text("{}\n", encoding="utf-8")
        git(cls.repo, "add", "-A")
        git(cls.repo, "commit", "-q", "-m", "base")
        cls.base = git(cls.repo, "rev-parse", "HEAD")
        (cls.repo / "src" / "a.cs").write_text("class A { string key = \"sk-abcdefghijklmnopqrstuvwxyz123456\"; }\n", encoding="utf-8")
        (cls.repo / "package-lock.json").write_text("{\"x\": 1}\n", encoding="utf-8")
        git(cls.repo, "commit", "-q", "-am", "cambio")
        cls.head = git(cls.repo, "rev-parse", "HEAD")

    @classmethod
    def tearDownClass(cls) -> None:
        cls.server.shutdown()

    def setUp(self) -> None:
        FakeServices.comments = []
        FakeServices.model_status = 200
        FakeServices.model_content = "**Resumen** cambio pequeño.\n**Veredicto**: Sin bloqueos"
        FakeServices.model_requests = []
        self.env = {"AI_REVIEW_API_KEY": "llave-falsa-de-prueba", "AI_REVIEW_BASE_URL": self.url, "AI_REVIEW_MODEL": "modelo-falso",
                    "AI_REVIEW_ALLOW_LOCAL": "1", "GITHUB_API_URL": self.url, "GITHUB_TOKEN": "token-falso", "GITHUB_REPOSITORY": "o/r", "PR_NUMBER": "7",
                    "BASE_SHA": self.base, "HEAD_SHA": self.head, "PR_TITLE": "KNOCK-1 · prueba"}

    def run_main(self, **extra: str) -> tuple[int, str]:
        buffer = io.StringIO()
        with mock.patch.object(ai_review, "ROOT", self.repo), mock.patch.dict(os.environ, {**self.env, **extra}, clear=False), redirect_stdout(buffer):
            code = ai_review.main()
        return code, buffer.getvalue()

    def test_secrets_are_masked_and_lockfiles_are_not_sent(self) -> None:
        code, _ = self.run_main()
        self.assertEqual(0, code)
        sent = json.dumps(FakeServices.model_requests[0]["body"]["messages"])
        self.assertNotIn("sk-abcdefghijklmnopqrstuvwxyz123456", sent)
        self.assertIn("[REDACTED]", sent)
        self.assertNotIn('{\\"x\\": 1}', sent)
        self.assertIn("package-lock.json", sent)  # aparece solo como archivo omitido

    def test_publishes_one_comment_and_updates_it_until_the_limit(self) -> None:
        for expected in (1, 2, 3):
            self.run_main(AI_REVIEW_MAX_RUNS="3")
            self.assertEqual(1, len(FakeServices.comments))
            self.assertIn(f"Revisión automática n.º {expected}", FakeServices.comments[0]["body"])
        before = FakeServices.comments[0]["body"]
        _, output = self.run_main(AI_REVIEW_MAX_RUNS="3")
        self.assertEqual(before, FakeServices.comments[0]["body"])
        self.assertIn("máximo", output)
        self.assertIn("No sustituye la revisión de pares", before)

    def test_model_failure_never_fails_the_job_and_posts_nothing(self) -> None:
        FakeServices.model_status = 500
        code, output = self.run_main()
        self.assertEqual(0, code)
        self.assertEqual([], FakeServices.comments)
        self.assertIn("::warning::", output)

    def test_empty_model_answer_posts_nothing(self) -> None:
        FakeServices.model_content = ""
        code, output = self.run_main()
        self.assertEqual(0, code)
        self.assertEqual([], FakeServices.comments)
        self.assertIn("no devolvió texto", output)

    def test_deepseek_gets_thinking_disabled_and_other_providers_do_not(self) -> None:
        self.run_main(AI_REVIEW_BASE_URL=self.url + "/deepseek")
        self.run_main(AI_REVIEW_BASE_URL=self.url)
        self.assertEqual({"type": "disabled"}, FakeServices.model_requests[0]["body"].get("thinking"))
        self.assertNotIn("thinking", FakeServices.model_requests[1]["body"])

    def test_extract_text_tolerates_other_response_shapes(self) -> None:
        shapes = {
            "texto": ({"choices": [{"message": {"content": " hola "}}]}, "hola"),
            "partes": ({"choices": [{"message": {"content": [{"type": "text", "text": "uno "}, {"type": "text", "text": "dos"}]}}]}, "uno dos"),
            "sin choices": ({"error": {"message": "x"}}, ""),
            "choices vacío": ({"choices": []}, ""),
            "sin message": ({"choices": [{"finish_reason": "content_filter"}]}, ""),
            "content nulo": ({"choices": [{"message": {"content": None}}]}, ""),
            "no es un objeto": (["x"], ""),
        }
        for name, (result, expected) in shapes.items():
            self.assertEqual(expected, ai_review.extract_text(result), name)

    def test_unexpected_provider_shape_posts_nothing_and_does_not_fail(self) -> None:
        FakeServices.model_raw = {"error": {"message": "filtro"}}
        code, output = self.run_main()
        FakeServices.model_raw = None
        self.assertEqual(0, code)
        self.assertEqual([], FakeServices.comments)
        self.assertIn("no devolvió texto", output)

    def test_key_is_never_sent_to_an_untrusted_or_insecure_address(self) -> None:
        for bad in ("http://api.deepseek.com", "https://evil.example.com", "https://api.deepseek.com.evil.example.com", "ftp://api.deepseek.com"):
            self.assertIsNotNone(ai_review.validate_base_url(bad, "", False), bad)
        self.assertIsNone(ai_review.validate_base_url("https://api.deepseek.com", "", False))
        self.assertIsNone(ai_review.validate_base_url("https://generativelanguage.googleapis.com/v1beta/openai", "", False))
        self.assertIsNotNone(ai_review.validate_base_url("http://127.0.0.1:1", "", False))  # el servidor local solo se admite en pruebas
        code, output = self.run_main(AI_REVIEW_BASE_URL="https://evil.example.com", AI_REVIEW_ALLOW_LOCAL="0")
        self.assertEqual(0, code)
        self.assertEqual([], FakeServices.model_requests)
        self.assertIn("La llave no se envía", output)

    def test_key_is_sent_only_as_bearer_and_never_printed(self) -> None:
        _, output = self.run_main()
        self.assertEqual("Bearer llave-falsa-de-prueba", FakeServices.model_requests[0]["auth"])
        self.assertNotIn("llave-falsa-de-prueba", output)

    def test_missing_key_skips_without_calling_anything(self) -> None:
        code, _ = self.run_main(AI_REVIEW_API_KEY="")
        self.assertEqual(0, code)
        self.assertEqual([], FakeServices.model_requests)

    def test_diff_is_cut_at_the_limit(self) -> None:
        with mock.patch.object(ai_review, "ROOT", self.repo):
            diff, skipped, cut = ai_review.collect_diff(self.base, self.head, 20)
        self.assertEqual(["package-lock.json"], skipped)
        self.assertEqual(["src/a.cs"], cut)
        self.assertNotIn("sk-abcdefghijklmnopqrstuvwxyz123456", diff)

    def test_exclusions(self) -> None:
        for name in ("package-lock.json", "app/pubspec.lock", "app/lib/l10n/generado/app_localizations.dart", ".env", "docs/architecture/codemap.json"):
            self.assertTrue(ai_review.is_excluded(name), name)
        for name in ("src/Knock.Api/Program.cs", "specs/HU-001/spec.md", "Memoria.md"):
            self.assertFalse(ai_review.is_excluded(name), name)


if __name__ == "__main__":
    unittest.main()
