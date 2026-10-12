# Knock — selector de Python para los git hooks (se carga con `.`; no es un hook).
# Define PY con el primer intérprete >= 3.11 que de verdad ejecute: $KNOCK_PYTHON, python3, python, "py -3".
# Descarta el alias roto de la Microsoft Store. Uso: knock_select_python <nombre-del-hook> || exit 1; $PY script.py
knock_select_python() {
  for candidate in "${KNOCK_PYTHON:-}" python3 python "py -3"; do
    [ -n "$candidate" ] || continue
    # shellcheck disable=SC2086  # "py -3" necesita separarse en palabras
    if $candidate -c "import sys; sys.exit(0 if sys.version_info >= (3, 11) else 1)" </dev/null >/dev/null 2>&1; then
      PY=$candidate
      return 0
    fi
  done
  echo "[$1] No encontré Python 3.11+ (probé KNOCK_PYTHON, python3, python, py -3). Define KNOCK_PYTHON con la ruta de tu intérprete." >&2
  return 1
}
