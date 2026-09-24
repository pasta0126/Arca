#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Builds a self-contained portable package for one runtime, e.g.:
#   build/publish.sh win-x64      (can be generated from macOS)
#   build/publish.sh osx-arm64
#   build/publish.sh linux-x64
# Output: artifacts/<runtime>/
set -euo pipefail
cd "$(dirname "$0")/.."

rid="${1:?usage: build/publish.sh <runtime-identifier>}"
dotnet publish src/Arca.Desktop/Arca.Desktop.csproj \
  -c Release -r "$rid" --self-contained true \
  -p:PublishSingleFile=false -o "artifacts/$rid"
echo "OK: artifacts/$rid"
