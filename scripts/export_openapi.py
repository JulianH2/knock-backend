#!/usr/bin/env python3
"""Exporta el contrato OpenAPI de Knock.Api a docs/openapi.v1.json.

Arranca la API en Development en un puerto libre de 127.0.0.1, lee /openapi/v1.json, quita lo que cambia entre ejecuciones
(la lista «servers», que lleva el puerto) y guarda el JSON con formato estable y salto de línea LF. No agrega paquetes
ni toca la base de datos: solo lee el documento que la API ya publica en Development. La API no arranca sin
ConnectionStrings:KnockMongo (TT-BD-06), así que se le da una cadena sintética hacia un destino que no responde, igual que
KnockApiFactory en las pruebas: la exportación no se conecta a MongoDB y nunca usa una cadena real del entorno.

Uso:
  python scripts/export_openapi.py              compila (Release) y exporta
  python scripts/export_openapi.py --no-build   usa la compilación existente (así lo llama el CI)
  python scripts/export_openapi.py --output ruta.json

CI (ci-pr.yml): después de exportar, `git diff --exit-code -- docs/openapi.v1.json` falla si el contrato cambió sin
regenerarse. Nunca se edita docs/openapi.v1.json a mano.
"""
from __future__ import annotations

import argparse
import json
import os
import signal
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src" / "Knock.Api" / "Knock.Api.csproj"
DEFAULT_OUTPUT = ROOT / "docs" / "openapi.v1.json"
# Cadena sintética: puerto 9 (descarte) en loopback, sin credenciales. La API solo la valida al arrancar; el cliente no conecta.
UNREACHABLE_MONGO = "mongodb://127.0.0.1:9/?directConnection=true"


def free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def stop(process: subprocess.Popen[bytes]) -> None:
    """Detiene la API y los procesos que `dotnet run` haya creado."""
    if process.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True, check=False)
    else:
        os.killpg(os.getpgid(process.pid), signal.SIGTERM)
    try:
        process.wait(timeout=15)
    except subprocess.TimeoutExpired:
        process.kill()


def fetch(url: str, process: subprocess.Popen[bytes], timeout: int) -> bytes:
    deadline = time.monotonic() + timeout
    last_error = "sin respuesta"
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError(f"la API terminó antes de responder (código {process.returncode})")
        try:
            with urllib.request.urlopen(url, timeout=5) as response:
                return response.read()
        except (urllib.error.URLError, ConnectionError, TimeoutError) as error:
            last_error = type(error).__name__
            time.sleep(1)
    raise RuntimeError(f"la API no respondió en {timeout} s ({last_error})")


def normalize(raw: bytes) -> str:
    document = json.loads(raw)
    document.pop("servers", None)
    return json.dumps(document, indent=2, ensure_ascii=False) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--no-build", action="store_true", help="no compila: usa la compilación Release existente")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT, help="archivo de salida (por defecto docs/openapi.v1.json)")
    parser.add_argument("--timeout", type=int, default=90, help="segundos de espera a que la API responda")
    args = parser.parse_args()

    port = free_port()
    command = ["dotnet", "run", "--project", str(PROJECT), "-c", "Release", "--no-launch-profile"]
    if args.no_build:
        command.append("--no-build")
    environment = {
        **os.environ,
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": f"http://127.0.0.1:{port}",
        "ConnectionStrings__KnockMongo": UNREACHABLE_MONGO,
    }
    popen_options: dict[str, object] = {"start_new_session": True} if os.name != "nt" else {}
    process = subprocess.Popen(command, cwd=ROOT, env=environment, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, **popen_options)
    try:
        raw = fetch(f"http://127.0.0.1:{port}/openapi/v1.json", process, args.timeout)
    except RuntimeError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    finally:
        stop(process)

    try:
        text = normalize(raw)
    except json.JSONDecodeError as error:
        print(f"ERROR: la respuesta no es JSON válido ({error.msg})", file=sys.stderr)
        return 1
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(text, encoding="utf-8", newline="\n")
    print(f"OK — contrato exportado a {args.output.relative_to(ROOT) if args.output.is_relative_to(ROOT) else args.output} ({len(text.splitlines())} líneas)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
