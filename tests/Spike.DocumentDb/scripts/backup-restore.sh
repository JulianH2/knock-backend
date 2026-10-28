#!/usr/bin/env bash
# P8 (TC-TT-BD-05-08) del spike TT-BD-05 (KNOCK-147): mongodump de una base de spike, mongorestore a otra base
# y comparación de conteos por colección y definiciones de índices (2dsphere, texto, TTL).
#
# Uso:  SPIKE_BACKUP_URI=... tests/Spike.DocumentDb/scripts/backup-restore.sh
#       (si SPIKE_BACKUP_URI no existe se usa KNOCK_SPIKE_MONGO).
# La URI solo se lee de la variable de entorno: nunca va como argumento de un proceso (se escribe en un archivo
# --config temporal con permisos 600) y la salida de las herramientas pasa por un filtro que enmascara credenciales.
# Requiere MongoDB Database Tools (mongodump, mongorestore) y mongosh.
set -euo pipefail

SPIKE_BACKUP_URI="${SPIKE_BACKUP_URI:-${KNOCK_SPIKE_MONGO:-}}"
if [[ -z "$SPIKE_BACKUP_URI" ]]; then
  echo "Define SPIKE_BACKUP_URI (o KNOCK_SPIKE_MONGO). La URI no se acepta como argumento." >&2
  exit 2
fi
export SPIKE_BACKUP_URI

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../../.." && pwd)"
suffix="$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')"
export SPIKE_P8_SRC="knock_spike_p8_${suffix}"
export SPIKE_P8_DST="knock_spike_p8_${suffix}_restored"
export SPIKE_P8_GEOJSON="$repo/db/seeds/colonias_tula.geojson"
export SPIKE_P8_OFICIOS="$repo/db/seeds/catalog/oficios.json"

# El dump va a un directorio temporal fuera del repositorio y se borra al terminar (spec §6).
umask 077
work="$(mktemp -d "${TMPDIR:-/tmp}/knock-spike-p8.XXXXXX")"
config="$work/tools.yaml"
uri_yaml="${SPIKE_BACKUP_URI//\'/\'\'}"
printf "uri: '%s'\n" "$uri_yaml" > "$config"

mask() { sed -E 's#(mongodb(\+srv)?://)[^@/[:space:]]+@#\1***:***@#g'; }
js() { SPIKE_P8_MODE="$1" mongosh --nodb --quiet --file "$here/backup-restore.js" 2>&1 | mask; }

cleanup() {
  local status=$?
  # Borra la base original y la _restored también si algo falló antes; conserva el código de salida.
  if js drop >/dev/null; then
    echo "== limpieza: bases $SPIKE_P8_SRC y $SPIKE_P8_DST borradas"
  else
    echo "Aviso: no se pudieron borrar las bases $SPIKE_P8_SRC y $SPIKE_P8_DST." >&2
  fi
  rm -rf "$work"
  exit "$status"
}
trap cleanup EXIT

echo "== P8: base origen $SPIKE_P8_SRC → destino $SPIKE_P8_DST (dump en directorio temporal)"
js seed

echo "== mongodump"
mongodump --config="$config" --db="$SPIKE_P8_SRC" --out="$work/dump" 2>&1 | mask

echo "== mongorestore"
# Restauración serial: Azure DocumentDB responde «(WriteConflict) Could not acquire lock for operation due to
# deadlock» si crea colecciones en paralelo (hallazgo de P8).
restore_status=0
mongorestore --config="$config" --nsInclude="$SPIKE_P8_SRC.*" \
  --nsFrom="$SPIKE_P8_SRC.*" --nsTo="$SPIKE_P8_DST.*" \
  --numParallelCollections=1 --numInsertionWorkersPerCollection=1 \
  "$work/dump" 2>&1 | mask | tee "$work/restore.log" || restore_status=$?
if (( restore_status != 0 )) || grep -q "Failed:" "$work/restore.log" \
  || grep -Eq "[1-9][0-9]* document\(s\) failed to restore" "$work/restore.log"; then
  echo "P8: FALLA (mongorestore reportó errores; código $restore_status)" >&2
  exit 1
fi

echo "== comparación"
js compare
