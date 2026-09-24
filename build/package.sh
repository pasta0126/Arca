#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Builds the distributable package of one system, from macOS or Linux:
#   build/package.sh win-x64       ARCA-<version>-win-x64.zip     portable (arca.portable marker)
#   build/package.sh linux-x64     ARCA-<version>-linux-x64.tar.gz portable (arca.portable marker)
#   build/package.sh osx-arm64     ARCA-<version>-osx-arm64.zip   ARCA.app, not signed (data in the user's folder)
# Also osx-x64 and linux-arm64. Output: artifacts/. The Windows installer needs Windows (Inno Setup) and comes later.
set -euo pipefail
cd "$(dirname "$0")/.."

rid="${1:?usage: build/package.sh <win-x64|linux-x64|linux-arm64|osx-arm64|osx-x64>}"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -1)"
name="ARCA-${version}-${rid}"
stage="artifacts/stage/${name}"
out="$(pwd)/artifacts"

build/publish.sh "$rid"
rm -rf "$stage"
mkdir -p "$stage" "$out"

# Licence, third-party notices and where to get the source: required by the GPL and by the MIT/Apache licences.
notes() {
  cp LICENSE THIRD-PARTY-NOTICES.md "$1/"
  cat > "$1/LEEME.txt" <<TXT
ARCA - Administracio de Recursos, Claus i Armariets (versio ${version})

Programari lliure i gratuit, sota la llicencia GNU GPL v3.0 o posterior (fitxer LICENSE).
El nom ARCA i el seu logotip son marca del titular. Vegeu TRADEMARK.md al repositori.
Codi font: https://github.com/pasta0126/Arca
Programari de tercers: THIRD-PARTY-NOTICES.md
TXT
}

case "$rid" in
  win-*|linux-*)
    cp -R "artifacts/${rid}/." "$stage/"
    : > "$stage/arca.portable"   # portable mode: data and settings go in the "data" folder next to the program
    notes "$stage"
    if [[ "$rid" == win-* ]]; then
      (cd artifacts/stage && rm -f "$out/${name}.zip" && zip -qry "$out/${name}.zip" "$name")
      echo "OK: artifacts/${name}.zip"
    else
      tar -C artifacts/stage -czf "$out/${name}.tar.gz" "$name"
      echo "OK: artifacts/${name}.tar.gz"
    fi
    ;;
  osx-*)
    app="$stage/ARCA.app"
    mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
    cp -R "artifacts/${rid}/." "$app/Contents/MacOS/"
    chmod +x "$app/Contents/MacOS/Arca"
    cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>ARCA</string>
  <key>CFBundleDisplayName</key><string>ARCA</string>
  <key>CFBundleIdentifier</key><string>io.github.pasta0126.arca</string>
  <key>CFBundleExecutable</key><string>Arca</string>
  <key>CFBundleVersion</key><string>${version}</string>
  <key>CFBundleShortVersionString</key><string>${version}</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST
    notes "$stage"
    (cd artifacts/stage && rm -f "$out/${name}.zip" && zip -qry "$out/${name}.zip" "$name")
    echo "OK: artifacts/${name}.zip (not signed: macOS asks the user to allow it the first time)"
    ;;
  *)
    echo "unsupported runtime: $rid" >&2
    exit 2
    ;;
esac
