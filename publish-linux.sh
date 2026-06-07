#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_DIR="$ROOT_DIR/src/SportsMonitor.Web"
BFF_PROJECT="$ROOT_DIR/src/SportsMonitor.Bff/SportsMonitor.Bff.csproj"
OUT_DIR="$ROOT_DIR/publish-linux"

echo "==> Building Angular..."
cd "$WEB_DIR"
npm ci
npm run build -- --configuration production

echo "==> Publishing BFF for linux-x64..."
rm -rf "$OUT_DIR"
dotnet publish "$BFF_PROJECT" \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  --output "$OUT_DIR"

mkdir -p "$OUT_DIR/data"

echo ""
echo "==> Build complete: $OUT_DIR"
echo "    Configure production secrets with appsettings.Production.json or environment variables."
