#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Builds the Windows demo package: the portable package with the `demo` data already in data/, signed with the
# self-signed certificate (build/sign-local.sh). Output: artifacts/demo/ARCA-<version>-win-x64-demo.zip (and the folder).
# Password: demo. Not for real centres: the database is public and the data are invented.
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -1)"
name="ARCA-${version}-win-x64"
out="artifacts/demo"

build/package.sh win-x64
rm -rf "$out" && mkdir -p "$out"
unzip -q "artifacts/${name}.zip" -d "$out"
dotnet run --project tools/Arca.DemoData -c Release -- --folder "$out/$name/data" --force | tee "$out/demo-data.log" | tail -4
build/sign-local.sh "$out/$name"
cp docs/demo-win-leeme.txt "$out/$name/LEEME-DEMO.txt"
(cd "$out" && zip -qry "${name}-demo.zip" "$name")
echo "OK: $out/${name}-demo.zip"
