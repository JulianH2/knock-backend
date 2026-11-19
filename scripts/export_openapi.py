#!/usr/bin/env python3
"""Exporta el contrato OpenAPI de un servicio de Knock (Market o Identity) a docs/.

Arranca la API en Development en un puerto libre de 127.0.0.1, lee /openapi/v1.json, quita lo que cambia entre ejecuciones
(la lista «servers», que lleva el puerto) y guarda el JSON con formato estable y salto de línea LF. No agrega paquetes
ni toca la base de datos: solo lee el documento que la API ya publica en Development. La API no arranca sin
ConnectionStrings:KnockMongo (TT-BD-06), así que se le da una cadena sintética hacia un destino que no responde, igual que
KnockApiFactory en las pruebas: la exportación no se conecta a MongoDB y nunca usa una cadena real del entorno.
Identity además valida al arrancar Redis, la clave del JWT, Firebase y las llaves del celular: se le dan valores sintéticos
generados al azar en cada ejecución (nada se escribe en disco ni se lee del entorno real; ver identity_environment).

Uso:
  python scripts/export_openapi.py                         Market -> docs/openapi.v1.json (compila en Release)
  python scripts/export_openapi.py --service identity      Identity -> docs/openapi.identity.v1.json
  python scripts/export_openapi.py --no-build              usa la compilación existente (así lo llama el CI)
  python scripts/export_openapi.py --output ruta.json

CI (ci-pr.yml): después de exportar los dos servicios, `git diff --exit-code` sobre ambos archivos falla si el contrato
cambió sin regenerarse. Nunca se edita ningún docs/openapi*.json a mano.
"""
from __future__ import annotations

import argparse
import base64
import json
import os
import secrets
import signal
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SERVICE = "market"
# Servicio -> (carpeta del proyecto en src/, archivo del contrato en docs/). El de Market conserva su nombre histórico
# (docs/openapi.v1.json) porque el frontend y la publicación del contrato dependen de esa ruta.
SERVICES: dict[str, tuple[str, str]] = {
    "market": ("Knock.Market.Api", "openapi.v1.json"),
    "identity": ("Knock.Identity.Api", "openapi.identity.v1.json"),
}
# Cadena sintética: puerto 9 (descarte) en loopback, sin credenciales. La API solo la valida al arrancar; el cliente no conecta.
UNREACHABLE_MONGO = "mongodb://127.0.0.1:9/?directConnection=true"


def project_path(service: str) -> Path:
    return ROOT / "src" / SERVICES[service][0] / f"{SERVICES[service][0]}.csproj"


def default_output(service: str) -> Path:
    return ROOT / "docs" / SERVICES[service][1]


def random_base64(size: int = 32) -> str:
    return base64.b64encode(secrets.token_bytes(size)).decode("ascii")


def identity_environment() -> dict[str, str]:
    """Configuración mínima y sintética para que Identity arranque en Development (mismas claves que IdentityApiFactory).

    Todo secreto se genera al azar en cada ejecución y solo vive en el entorno del proceso hijo: no hay ninguno literal.
    Mongo y Redis apuntan a destinos que no responden; Firebase usa un proyecto de demostración."""
    return {
        "ConnectionStrings__KnockRedis": "127.0.0.1:6379,password=" + secrets.token_hex(16),
        "Jwt__SigningKey": random_base64(),
        "Firebase__ProjectId": "demo-knock",
        "Identity__PhoneProtection__EncryptionKeys__1": random_base64(),
        "Identity__PhoneProtection__HmacKeys__1": random_base64(),
        "Identity__PhoneProtection__EncryptionKeyVersion": "1",
        "Identity__PhoneProtection__HmacKeyVersion": "1",
    }


def service_environment(service: str) -> dict[str, str]:
    return identity_environment() if service == "identity" else {"Busqueda__Cursor__Key": random_base64()}


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--service", choices=sorted(SERVICES), default=DEFAULT_SERVICE, help="servicio a exportar (por defecto market)")
    parser.add_argument("--no-build", action="store_true", help="no compila: usa la compilación Release existente")
    parser.add_argument("--output", type=Path, default=None, help="archivo de salida (por defecto docs/openapi.v1.json o docs/openapi.identity.v1.json)")
    parser.add_argument("--timeout", type=int, default=90, help="segundos de espera a que la API responda")
    return parser.parse_args(argv)


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


def unify_newlines(value: object) -> object:
    """Los comentarios XML de los .cs traen CRLF cuando el código se descargó en Windows y LF en Linux (CI): se unifican a LF
    para que el contrato salga igual en cualquier sistema."""
    if isinstance(value, str):
        return value.replace("\r\n", "\n")
    if isinstance(value, list):
        return [unify_newlines(item) for item in value]
    if isinstance(value, dict):
        return {key: unify_newlines(item) for key, item in value.items()}
    return value


def normalize(raw: bytes) -> str:
    document = unify_newlines(json.loads(raw))
    assert isinstance(document, dict)
    document.pop("servers", None)
    return json.dumps(document, indent=2, ensure_ascii=False) + "\n"


def main() -> int:
    args = parse_args()
    output = args.output if args.output is not None else default_output(args.service)

    port = free_port()
    command = ["dotnet", "run", "--project", str(project_path(args.service)), "-c", "Release", "--no-launch-profile"]
    if args.no_build:
        command.append("--no-build")
    environment = {
        **os.environ,
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": f"http://127.0.0.1:{port}",
        "ConnectionStrings__KnockMongo": UNREACHABLE_MONGO,
        **service_environment(args.service),
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
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(text, encoding="utf-8", newline="\n")
    print(f"OK — contrato exportado a {output.relative_to(ROOT) if output.is_relative_to(ROOT) else output} ({len(text.splitlines())} líneas)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
