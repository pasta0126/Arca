#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Regenerates every icon file of the application from the single master, assets/icon/arca.svg (icona-d-aplicacio).
# To change the icon: replace the master (or the drawing in it) and run this script from the repository root:
#
#   build/icons.sh
#
# Step 1 renders the master to the PNG sizes with whichever free tool the computer has (sips on macOS; rsvg-convert, Inkscape or
# ImageMagick on Linux and macOS). Step 2 (build/MakeIcons.cs, pure .NET) builds arca.ico, arca.icns and the summary of the master.
# The results are committed, so building the application needs none of these tools.
set -euo pipefail
cd "$(dirname "$0")/.."

export DOTNET_CLI_TELEMETRY_OPTOUT=1

folder="assets/icon"
master="$folder/arca.svg"
sizes=(16 32 48 64 128 256 512 1024)

render() { # render <size> <out>
  local size="$1" out="$2"
  if command -v rsvg-convert >/dev/null 2>&1; then
    rsvg-convert -w "$size" -h "$size" "$master" -o "$out"
  elif command -v inkscape >/dev/null 2>&1; then
    inkscape "$master" --export-type=png --export-width="$size" --export-height="$size" --export-filename="$out" >/dev/null
  elif command -v magick >/dev/null 2>&1; then
    magick -background none -density 384 "$master" -resize "${size}x${size}" "$out"
  elif command -v sips >/dev/null 2>&1; then
    sips -s format png -z "$size" "$size" "$master" --out "$out" >/dev/null
  else
    echo "No free image tool found to render the SVG. Install one of: rsvg-convert (librsvg), inkscape, imagemagick; macOS already has sips." >&2
    exit 1
  fi
}

for size in "${sizes[@]}"; do
  render "$size" "$folder/arca-$size.png"
done
dotnet run build/MakeIcons.cs
