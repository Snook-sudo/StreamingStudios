#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet publish Jellyfin.Plugin.StreamingStudios/Jellyfin.Plugin.StreamingStudios.csproj -c Release -o out
rm -rf dist && mkdir -p dist
cp out/Jellyfin.Plugin.StreamingStudios.dll meta.json dist/
(cd dist && zip -j StreamingStudios_1.0.0.0.zip Jellyfin.Plugin.StreamingStudios.dll meta.json)
echo "Pronto: dist/StreamingStudios_1.0.0.0.zip"
