#!/usr/bin/env python3
"""Genera una sola vez las semillas JSON del catálogo para MongoDB (TT-BD-06, decisión (c)).

Entradas (no se modifican):
  db/seeds/colonias_tula.geojson   8 polígonos piloto aproximados
  db/sepomex/tula_colonias.csv     12 colonias del extracto SEPOMEX
  db/seeds/catalog/oficios.sql     oficios de TT-BD-02 (se borra después de esta exportación)

Salidas:
  db/seeds/catalog/colonias.json   12 colonias; las 8 con polígono llevan `geom` y el `centro` de PostGIS
  db/seeds/catalog/oficios.json    oficios con `_id` fijos

El centro se calcula como lo hacía `CatalogSeeder` de TT-BD-02:
`ST_PointOnSurface(ST_Multi(ST_SetSRID(ST_GeomFromGeoJSON(g), 4326)))`. Para eso arranca un contenedor PostGIS
desechable sin red (`--network none`) y le pasa el SQL con `docker exec`: no hay cadena de conexión, puerto ni
contraseña, y el contenedor se borra al terminar.

Los `_id` son fijos: si el JSON ya existe se reutiliza el `_id` de cada clave natural; si no, se deriva de forma
determinista de la clave natural (marca de tiempo fija + SHA-256), así que volver a correrlo da los mismos.

Uso:  python3 db/scripts/generate_catalog_seeds.py
"""

from __future__ import annotations

import csv
import datetime
import hashlib
import json
import re
import secrets
import subprocess
import sys
import time
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GEOJSON = ROOT / "db" / "seeds" / "colonias_tula.geojson"
CSV = ROOT / "db" / "sepomex" / "tula_colonias.csv"
OFICIOS_SQL = ROOT / "db" / "seeds" / "catalog" / "oficios.sql"
COLONIAS_JSON = ROOT / "db" / "seeds" / "catalog" / "colonias.json"
OFICIOS_JSON = ROOT / "db" / "seeds" / "catalog" / "oficios.json"

# Misma imagen que usaban las pruebas de TT-BD-01/02 (PostgisFixture).
POSTGIS_IMAGE = "postgis/postgis:16-3.4"
READY_TIMEOUT_SECONDS = 120
# Línea del entrypoint oficial de postgres al terminar el init; después arranca el servidor definitivo.
INIT_COMPLETE = "PostgreSQL init process complete; ready for start up"
ID_EPOCH = int(datetime.datetime(2026, 10, 27, tzinfo=datetime.timezone.utc).timestamp())
MODALIDADES = {"domicilio", "taller"}
OFICIO_ROW = re.compile(r"\('(?P<clave>[a-z]+)',\s*'(?P<nombre>[^']+)',\s*'(?P<modalidad>[a-z]+)',\s*(?P<activo>TRUE|FALSE)\)")


def normalize(text: str) -> str:
    """Igual que `SearchNameNormalizer`: NFD sin marcas diacríticas, mayúsculas y espacios colapsados."""
    decomposed = unicodedata.normalize("NFD", text)
    stripped = "".join(c for c in decomposed if unicodedata.category(c) != "Mn")
    return " ".join(unicodedata.normalize("NFC", stripped).upper().split())


def object_id(namespace: str, key: str) -> str:
    """ObjectId determinista: 4 bytes de marca de tiempo fija y 8 de SHA-256 de la clave natural."""
    digest = hashlib.sha256(f"{namespace}:{key}".encode("utf-8")).digest()
    return ID_EPOCH.to_bytes(4, "big").hex() + digest[:8].hex()


def existing_ids(path: Path, key) -> dict[str, str]:
    if not path.exists():
        return {}
    return {key(doc): doc["_id"]["$oid"] for doc in json.loads(path.read_text(encoding="utf-8"))}


def run(args: list[str], stdin: str | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(args, input=stdin, capture_output=True, text=True, check=False)


def fail_with_logs(name: str, message: str) -> None:
    """Termina mostrando las últimas líneas del log del contenedor (con autenticación trust no hay contraseña que mostrar)."""
    logs = run(["docker", "logs", "--tail", "60", name])
    print(f"--- logs de {name} ({POSTGIS_IMAGE}) ---", file=sys.stderr)
    print((logs.stdout + logs.stderr).strip() or "(sin logs)", file=sys.stderr)
    print("--- fin de los logs ---", file=sys.stderr)
    sys.exit(message)


def wait_until_ready(name: str) -> None:
    """
    Espera al servidor definitivo, no al temporal del init: primero la línea de fin del init en los logs, luego
    `pg_isready` y `SELECT postgis_version()` (la extensión ya creada por la imagen). Si el contenedor se detiene o se
    agota el tiempo, termina mostrando sus logs.
    """
    deadline = time.monotonic() + READY_TIMEOUT_SECONDS
    while True:
        state = run(["docker", "inspect", "--format", "{{.State.Status}} (código {{.State.ExitCode}})", name]).stdout.strip()
        if not state.startswith("running"):
            fail_with_logs(name, f"El contenedor PostGIS se detuvo: {state}.")
        logs = run(["docker", "logs", name])
        if INIT_COMPLETE in logs.stdout + logs.stderr \
                and run(["docker", "exec", name, "pg_isready", "-U", "postgres", "-q"]).returncode == 0 \
                and run(["docker", "exec", name, "psql", "-U", "postgres", "-At", "-q",
                         "-c", "SELECT postgis_version()"]).returncode == 0:
            return
        if time.monotonic() > deadline:
            fail_with_logs(name, f"PostGIS no quedó listo en {READY_TIMEOUT_SECONDS} s.")
        time.sleep(2)


def postgis_centers(geometries: list[str]) -> tuple[list[tuple[dict, bool, bool]], str, str]:
    """Centro (GeoJSON), ¿válido? y ¿el centro cae dentro? por geometría, más versión de PostGIS y digest de la imagen."""
    values = ",\n".join(f"({i}, $g${g}$g$)" for i, g in enumerate(geometries))
    # Sin CREATE EXTENSION: la imagen la crea en su init (docker-entrypoint-initdb.d/10_postgis.sh).
    sql = f"""
WITH source(i, g) AS (VALUES {values}),
shapes AS (SELECT i, ST_Multi(ST_SetSRID(ST_GeomFromGeoJSON(g), 4326)) AS shape FROM source)
SELECT i, ST_AsGeoJSON(ST_PointOnSurface(shape)), ST_IsValid(shape), ST_Contains(shape, ST_PointOnSurface(shape))
FROM shapes ORDER BY i;
SELECT 'version', postgis_lib_version();
"""
    name = "knock-seedgen-" + secrets.token_hex(4)
    # Sin --rm: si el contenedor se detiene al arrancar, sigue existiendo y se pueden leer sus logs; se borra en el finally.
    started = run(["docker", "run", "-d", "--name", name, "--network", "none",
                   "-e", "POSTGRES_HOST_AUTH_METHOD=trust", POSTGIS_IMAGE])
    if started.returncode != 0:
        sys.exit(f"No se pudo arrancar {POSTGIS_IMAGE}: {started.stderr.strip()}")
    try:
        wait_until_ready(name)
        result = run(["docker", "exec", "-i", name, "psql", "-U", "postgres", "-At", "-F", "|",
                      "-v", "ON_ERROR_STOP=1", "-q"], stdin=sql)
        if result.returncode != 0:
            fail_with_logs(name, f"La consulta de centros falló: {result.stderr.strip()[-300:]}")
        digest = run(["docker", "image", "inspect", "--format", "{{index .RepoDigests 0}}", POSTGIS_IMAGE]).stdout.strip()
    finally:
        run(["docker", "rm", "-f", name])

    rows, version = {}, "?"
    for line in result.stdout.splitlines():
        parts = line.split("|")
        if parts[0] == "version":
            version = parts[1]
        elif len(parts) == 4:
            rows[int(parts[0])] = (json.loads(parts[1]), parts[2] == "t", parts[3] == "t")
    if sorted(rows) != list(range(len(geometries))):
        sys.exit("PostGIS no devolvió un centro por geometría.")
    return [rows[i] for i in range(len(geometries))], version, digest


def as_multipolygon(geometry: dict) -> dict:
    if geometry["type"] == "MultiPolygon":
        return {"type": "MultiPolygon", "coordinates": geometry["coordinates"]}
    if geometry["type"] == "Polygon":
        return {"type": "MultiPolygon", "coordinates": [geometry["coordinates"]]}
    sys.exit(f"Geometría no soportada: {geometry['type']}")


def build_colonias() -> tuple[list[dict], list[str]]:
    features = json.loads(GEOJSON.read_text(encoding="utf-8"))["features"]
    by_key = {(f["properties"]["municipio"], f["properties"]["cp"], f["properties"]["nombre"]): f for f in features}
    with CSV.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle))
    keys = [(r["municipio"], r["codigo_postal"], r["asentamiento"]) for r in rows]
    missing = set(by_key) - set(keys)
    if missing:
        sys.exit(f"Polígonos sin colonia en el extracto: {sorted(missing)}")
    if len(set(keys)) != len(keys):
        sys.exit("El extracto tiene colonias repetidas.")

    with_polygon = [k for k in keys if k in by_key]
    centers, version, digest = postgis_centers([json.dumps(by_key[k]["geometry"]) for k in with_polygon])
    center_by_key = dict(zip(with_polygon, centers))
    previous = existing_ids(COLONIAS_JSON, lambda d: (d["municipio"], d["cp"], d["nombre"]))

    report = [f"PostGIS {version} · imagen {digest or POSTGIS_IMAGE}"]
    documents = []
    for municipio, cp, nombre in keys:
        key = (municipio, cp, nombre)
        doc = {
            "_id": {"$oid": previous.get(key) or object_id("colonias", "|".join(key))},
            "nombre": nombre,
            "nombreBusqueda": normalize(nombre),
            "cp": cp,
            "municipio": municipio,
        }
        if key in by_key:
            center, valid, contains = center_by_key[key]
            if not valid or not contains:
                sys.exit(f"{nombre}: polígono inválido o centro fuera del polígono.")
            doc |= {"piloto": True, "aproximado": True, "geom": as_multipolygon(by_key[key]["geometry"]),
                    "centro": {"type": "Point", "coordinates": center["coordinates"]}}
            report.append(f"  {nombre} ({cp}) centro {center['coordinates']} · válido · centro dentro")
        else:
            doc |= {"piloto": False, "aproximado": False}
            report.append(f"  {nombre} ({cp}) sin polígono: sin geom ni centro")
        documents.append(doc)
    return documents, report


def build_oficios() -> tuple[list[dict], list[str]]:
    matches = list(OFICIO_ROW.finditer(OFICIOS_SQL.read_text(encoding="utf-8")))
    if not matches:
        sys.exit("No se encontraron oficios en oficios.sql.")
    previous = existing_ids(OFICIOS_JSON, lambda d: d["clave"])
    documents, report = [], []
    for m in sorted(matches, key=lambda m: m["clave"]):
        if m["modalidad"] not in MODALIDADES:
            sys.exit(f"Modalidad inválida en {m['clave']}.")
        documents.append({
            "_id": {"$oid": previous.get(m["clave"]) or object_id("oficios", m["clave"])},
            "clave": m["clave"],
            "nombre": m["nombre"],
            "nombreBusqueda": normalize(m["nombre"]),
            "modalidad": m["modalidad"],
            "activo": m["activo"] == "TRUE",
        })
        report.append(f"  {m['clave']} {m['nombre']} ({m['modalidad']})")
    return documents, report


def write(path: Path, documents: list[dict]) -> str:
    ids = [d["_id"]["$oid"] for d in documents]
    if len(set(ids)) != len(ids):
        sys.exit(f"{path.name}: _id repetidos.")
    text = json.dumps(documents, ensure_ascii=False, indent=2) + "\n"
    path.write_text(text, encoding="utf-8", newline="\n")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def main() -> None:
    colonias, colonias_report = build_colonias()
    oficios, oficios_report = build_oficios()
    colonias_hash = write(COLONIAS_JSON, colonias)
    oficios_hash = write(OFICIOS_JSON, oficios)
    print("\n".join(colonias_report))
    print(f"colonias.json: {len(colonias)} colonias, sha256 {colonias_hash}")
    print("\n".join(oficios_report))
    print(f"oficios.json: {len(oficios)} oficios, sha256 {oficios_hash}")


if __name__ == "__main__":
    main()
