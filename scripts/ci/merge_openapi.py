#!/usr/bin/env python3
"""Une los contratos OpenAPI de Market e Identidad en un solo documento (KNOCK-137, TT-CI-02).

knock-frontend consume un único contracts/openapi.v1.json y su generador de cliente lee un solo archivo, pero el backend
publica un contrato por servicio. Este script los junta sin inventar nada: rutas, esquemas y etiquetas se copian tal cual.
Falla (código 1) con un mensaje claro si algo choca, en vez de mezclarlo mal:
  - una ruta presente en los dos contratos,
  - un esquema con el mismo nombre pero distinto contenido (ProblemDetails es idéntico en ambos y se une),
  - versiones de OpenAPI o de la API distintas,
  - secciones de components distintas de schemas (hoy no existen; no se descartan en silencio).

Uso:
  python scripts/ci/merge_openapi.py --market docs/openapi.v1.json --identity docs/openapi.identity.v1.json --output ruta.json
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def _problems(market: dict, identity: dict) -> list[str]:
    errors: list[str] = []
    if market.get("openapi") != identity.get("openapi"):
        errors.append(f"Versiones de OpenAPI distintas: {market.get('openapi')} y {identity.get('openapi')}.")
    if market.get("info", {}).get("version") != identity.get("info", {}).get("version"):
        errors.append("La versión de la API (info.version) es distinta entre los contratos.")
    for name, source in (("Market", market), ("Identidad", identity)):
        extra = sorted(set(source.get("components", {})) - {"schemas"})
        if extra:
            errors.append(f"El contrato de {name} tiene components distintos de schemas ({', '.join(extra)}); el script no los une.")
    return errors


def merge(market: dict, identity: dict) -> dict:
    errors = _problems(market, identity)
    paths: dict = {}
    schemas: dict = {}
    tags: dict[str, dict] = {}
    for source in (market, identity):
        for path, item in source.get("paths", {}).items():
            if path in paths:
                errors.append(f"La ruta {path} está en los dos contratos.")
            paths[path] = item
        for name, schema in source.get("components", {}).get("schemas", {}).items():
            if name in schemas and schemas[name] != schema:
                errors.append(f"El esquema {name} es distinto en los dos contratos.")
            schemas.setdefault(name, schema)
        for tag in source.get("tags", []):
            tags.setdefault(tag["name"], tag)
    if errors:
        raise ValueError("\n".join(errors))
    document: dict = {
        "openapi": market["openapi"],
        "info": {"title": "Knock.Api | v1", "version": market["info"]["version"]},
        "paths": paths,
    }
    if tags:
        document["tags"] = [tags[name] for name in sorted(tags)]
    document["components"] = {"schemas": schemas}
    return document


def render(document: dict) -> str:
    return json.dumps(document, indent=2, ensure_ascii=False) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--market", required=True, type=Path)
    parser.add_argument("--identity", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    market = json.loads(args.market.read_text(encoding="utf-8"))
    identity = json.loads(args.identity.read_text(encoding="utf-8"))
    try:
        document = merge(market, identity)
    except ValueError as error:
        for line in str(error).splitlines():
            print(f"::error::{line}")
        return 1
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render(document), encoding="utf-8", newline="\n")
    print(f"Contrato combinado: {len(document['paths'])} rutas, {len(document['components']['schemas'])} esquemas → {args.output}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
