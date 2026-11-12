#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
echo 'Checking .NET 10 backend build...'
dotnet build Blazor/Blazor.csproj
BASE_URL="${1:-http://localhost:5097}"
echo "Checking site at $BASE_URL (start it separately first)..."
for path in / /all-tables /create-table /customers/create /reservations; do
  code="$(curl --silent --output /dev/null --write-out '%{http_code}' "$BASE_URL$path" || true)"
  echo "$path => $code"
  if [[ "$code" != 200 ]]; then echo "FAILED: $path returned $code" >&2; exit 1; fi
done
echo 'HTTP smoke checks passed. Manually test forms and data persistence.'
