#!/usr/bin/env python3
"""Validate the future Tula GeoJSON without changing or generating geometry."""

from __future__ import annotations

import argparse
import copy
import json
import math
import sys
from datetime import date
from pathlib import Path
from typing import Any


DEFAULT_GEOJSON = Path(__file__).resolve().parents[1] / "seeds" / "colonias_tula.geojson"
TULA_LONGITUDE_BOUNDS = (-99.40, -99.28)
TULA_LATITUDE_BOUNDS = (20.00, 20.12)
REQUIRED_PROPERTIES = (
    "nombre",
    "cp",
    "tipo_asentamiento",
    "municipio",
    "estado",
    "piloto",
    "aproximado",
    "autor",
    "fecha_elaboracion",
)


class GeoJsonValidationError(ValueError):
    """Raised when a GeoJSON document does not follow the Tula data contract."""


def _require_nonempty_text(properties: dict[str, Any], key: str, feature_number: int) -> str:
    value = properties.get(key)
    if not isinstance(value, str) or not value.strip():
        raise GeoJsonValidationError(
            f"Feature {feature_number}: properties.{key} must be non-empty text"
        )
    return value


def _validate_properties(properties: Any, feature_number: int) -> None:
    if not isinstance(properties, dict):
        raise GeoJsonValidationError(f"Feature {feature_number}: properties must be an object")

    missing = [key for key in REQUIRED_PROPERTIES if key not in properties]
    if missing:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: missing properties: {', '.join(missing)}"
        )

    for key in ("nombre", "tipo_asentamiento", "municipio", "estado", "autor"):
        _require_nonempty_text(properties, key, feature_number)

    postal_code = properties["cp"]
    if not isinstance(postal_code, str) or len(postal_code) != 5 or not postal_code.isdigit():
        raise GeoJsonValidationError(
            f"Feature {feature_number}: properties.cp must be a five-digit string"
        )

    if properties["tipo_asentamiento"] != "Colonia":
        raise GeoJsonValidationError(
            f"Feature {feature_number}: properties.tipo_asentamiento must be 'Colonia'"
        )
    if properties["municipio"] != "Tula de Allende" or properties["estado"] != "Hidalgo":
        raise GeoJsonValidationError(
            f"Feature {feature_number}: municipio/estado must identify Tula de Allende, Hidalgo"
        )
    if properties["piloto"] is not True or properties["aproximado"] is not True:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: piloto and aproximado must both be true"
        )

    elaboration_date = properties["fecha_elaboracion"]
    if not isinstance(elaboration_date, str):
        raise GeoJsonValidationError(
            f"Feature {feature_number}: properties.fecha_elaboracion must be YYYY-MM-DD text"
        )
    try:
        if date.fromisoformat(elaboration_date).isoformat() != elaboration_date:
            raise ValueError
    except ValueError as error:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: properties.fecha_elaboracion must be a valid YYYY-MM-DD date"
        ) from error


def _validate_position(position: Any, feature_number: int) -> None:
    if not isinstance(position, list) or len(position) < 2:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: each coordinate must be [longitude, latitude, ...]"
        )

    longitude, latitude = position[:2]
    if (
        isinstance(longitude, bool)
        or isinstance(latitude, bool)
        or not isinstance(longitude, (int, float))
        or not isinstance(latitude, (int, float))
        or not math.isfinite(longitude)
        or not math.isfinite(latitude)
    ):
        raise GeoJsonValidationError(
            f"Feature {feature_number}: longitude and latitude must be finite numbers"
        )
    if not -180 <= longitude <= 180 or not -90 <= latitude <= 90:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: coordinate order/range invalid; expected "
            "[longitude in -180..180, latitude in -90..90]"
        )
    if longitude >= 0 or latitude <= 0:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: Tula coordinates must be west longitude (<0) "
            "and north latitude (>0), in [longitude, latitude] order"
        )
    if not TULA_LONGITUDE_BOUNDS[0] <= longitude <= TULA_LONGITUDE_BOUNDS[1]:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: longitude {longitude} is outside the Tula pilot "
            f"bounds {TULA_LONGITUDE_BOUNDS[0]}..{TULA_LONGITUDE_BOUNDS[1]}"
        )
    if not TULA_LATITUDE_BOUNDS[0] <= latitude <= TULA_LATITUDE_BOUNDS[1]:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: latitude {latitude} is outside the Tula pilot "
            f"bounds {TULA_LATITUDE_BOUNDS[0]}..{TULA_LATITUDE_BOUNDS[1]}"
        )


def _validate_polygon_coordinates(coordinates: Any, feature_number: int) -> None:
    if not isinstance(coordinates, list) or not coordinates:
        raise GeoJsonValidationError(f"Feature {feature_number}: polygon coordinates are empty")

    for polygon_index, polygon in enumerate(coordinates, start=1):
        if not isinstance(polygon, list) or not polygon:
            raise GeoJsonValidationError(
                f"Feature {feature_number}: polygon {polygon_index} must contain rings"
            )
        for ring_index, ring in enumerate(polygon, start=1):
            if not isinstance(ring, list) or len(ring) < 4:
                raise GeoJsonValidationError(
                    f"Feature {feature_number}: polygon {polygon_index}, ring {ring_index} "
                    "must contain at least four positions"
                )
            for position in ring:
                _validate_position(position, feature_number)
            if ring[0] != ring[-1]:
                raise GeoJsonValidationError(
                    f"Feature {feature_number}: polygon {polygon_index}, ring {ring_index} "
                    "is not closed"
                )


def _normalize_geometry(geometry: Any, feature_number: int) -> tuple[dict[str, Any], bool]:
    if not isinstance(geometry, dict):
        raise GeoJsonValidationError(f"Feature {feature_number}: geometry must be an object")

    geometry_type = geometry.get("type")
    coordinates = geometry.get("coordinates")
    if geometry_type == "Polygon":
        normalized = {"type": "MultiPolygon", "coordinates": [coordinates]}
        was_polygon = True
    elif geometry_type == "MultiPolygon":
        normalized = {"type": geometry_type, "coordinates": coordinates}
        was_polygon = False
    else:
        raise GeoJsonValidationError(
            f"Feature {feature_number}: geometry.type must be Polygon or MultiPolygon"
        )

    _validate_polygon_coordinates(normalized["coordinates"], feature_number)
    return normalized, was_polygon


def validate_geojson(document: Any) -> tuple[int, int]:
    """Validate features and return (feature count, Polygon count normalized in memory)."""
    if not isinstance(document, dict) or document.get("type") != "FeatureCollection":
        raise GeoJsonValidationError("Root must be a GeoJSON FeatureCollection")

    features = document.get("features")
    if not isinstance(features, list) or not features:
        raise GeoJsonValidationError("FeatureCollection.features must be a non-empty array")

    polygon_count = 0
    seen_names: set[str] = set()
    for feature_number, feature in enumerate(features, start=1):
        if not isinstance(feature, dict) or feature.get("type") != "Feature":
            raise GeoJsonValidationError(f"Feature {feature_number}: expected a GeoJSON Feature")

        properties = feature.get("properties")
        _validate_properties(properties, feature_number)
        name = properties["nombre"].strip().casefold()
        if name in seen_names:
            raise GeoJsonValidationError(f"Feature {feature_number}: duplicate colonia name")
        seen_names.add(name)

        _, was_polygon = _normalize_geometry(feature.get("geometry"), feature_number)
        polygon_count += int(was_polygon)

    return len(features), polygon_count


def normalize_geojson(document: Any) -> tuple[dict[str, Any], int]:
    """Return a validated copy with Polygon geometries represented as MultiPolygon."""
    _, polygon_count = validate_geojson(document)
    normalized_document = copy.deepcopy(document)
    for feature_number, feature in enumerate(normalized_document["features"], start=1):
        geometry, _ = _normalize_geometry(feature["geometry"], feature_number)
        feature["geometry"] = geometry
    return normalized_document, polygon_count


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "geojson",
        nargs="?",
        type=Path,
        default=DEFAULT_GEOJSON,
        help=f"GeoJSON path (default: {DEFAULT_GEOJSON})",
    )
    parser.add_argument(
        "--normalize-output",
        type=Path,
        help="write a validated normalized copy here; the input is never modified",
    )
    args = parser.parse_args()

    try:
        with args.geojson.open(encoding="utf-8") as source:
            document = json.load(source)
        if args.normalize_output and args.geojson.resolve() == args.normalize_output.resolve():
            raise GeoJsonValidationError("--normalize-output must not overwrite the input file")
        normalized_document, polygon_count = normalize_geojson(document)
    except (OSError, json.JSONDecodeError, GeoJsonValidationError) as error:
        print(f"Invalid Tula GeoJSON: {error}", file=sys.stderr)
        return 1

    if args.normalize_output:
        try:
            with args.normalize_output.open("w", encoding="utf-8") as destination:
                json.dump(normalized_document, destination, ensure_ascii=False, indent=2)
                destination.write("\n")
        except OSError as error:
            print(f"Could not write normalized Tula GeoJSON: {error}", file=sys.stderr)
            return 1

    print(
        f"Valid Tula GeoJSON: {len(normalized_document['features'])} feature(s); "
        f"{polygon_count} Polygon feature(s) normalized in memory to MultiPolygon."
    )
    if args.normalize_output:
        print(f"Normalized copy written to: {args.normalize_output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
