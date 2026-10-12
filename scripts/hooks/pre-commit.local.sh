#!/usr/bin/env sh
# Comprobaciones rápidas propias de knock-backend (las llama .githooks/pre-commit con la lista de archivos).
STAGED="$1"
if echo "$STAGED" | grep -qE '\.cs$' && command -v dotnet >/dev/null 2>&1 && [ -f Knock.slnx ]; then
  echo "[pre-commit] formato C#"
  dotnet format Knock.slnx --verify-no-changes --no-restore -v q || exit 1
fi
if echo "$STAGED" | grep -qE '^db/.*\.sql$' && command -v sqlfluff >/dev/null 2>&1; then
  echo "[pre-commit] SQL"
  sqlfluff lint $(echo "$STAGED" | grep -E '^db/.*\.sql$') --dialect postgres || exit 1
fi
