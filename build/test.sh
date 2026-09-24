#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Single definition of "the build is green": restore, compile with warnings as errors,
# and run every test project (docs/stack.md, "Verificación multiplataforma").
# The licence check (build/CheckLicenses.cs) also verifies THIRD-PARTY-NOTICES.md is current.
set -euo pipefail
cd "$(dirname "$0")/.."

# ARCA sends nothing off the computer (AGENTS.md), and that includes the tools that build it: switch off the
# anonymous telemetry of the .NET CLI, the test platform and Avalonia's build services for every script here.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export TESTINGPLATFORM_TELEMETRY_OPTOUT=1
export AVALONIA_TELEMETRY_OPTOUT=1

dotnet restore Arca.slnx
dotnet build Arca.slnx --no-restore -c Release -warnaserror
dotnet test --solution Arca.slnx --no-build -c Release
dotnet run build/CheckLicenses.cs
echo "OK: build and tests passed"
