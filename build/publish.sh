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

# ARCA sends nothing off the computer (AGENTS.md), and that includes the tools that build it: switch off the
# anonymous telemetry of the .NET CLI, the test platform and Avalonia's build services for every script here.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export TESTINGPLATFORM_TELEMETRY_OPTOUT=1
export AVALONIA_TELEMETRY_OPTOUT=1

rid="${1:?usage: build/publish.sh <runtime-identifier>}"
dotnet publish src/Arca.Desktop/Arca.Desktop.csproj \
  -c Release -r "$rid" --self-contained true \
  -p:PublishSingleFile=false -o "artifacts/$rid"
echo "OK: artifacts/$rid"
