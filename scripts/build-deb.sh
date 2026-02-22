#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR/.."
WEB="$ROOT/ElgatoControl.Web"
API="$ROOT/ElgatoControl.Api"

echo "==> Building React frontend..."
cd "$WEB"
npm install
npm run build

echo "==> Copying frontend to API wwwroot..."
rm -rf "$API/wwwroot"
cp -r "$WEB/dist" "$API/wwwroot"

echo "==> Building Electron .deb package..."
cd "$API"
DOTNET_ROLL_FORWARD=LatestMajor electronize build /target linux /electron-arch x64

echo ""
echo "==> Done! Output in: $ROOT/bin/Desktop"
ls "$ROOT/bin/Desktop"/*.deb 2>/dev/null || echo "(no .deb found — check electron-builder output above)"
