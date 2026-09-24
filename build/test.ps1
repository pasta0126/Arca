# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Same checks as build/test.sh, for Windows. Run only before going to production (docs/stack.md).
# The licence check (build/CheckLicenses.cs) also verifies THIRD-PARTY-NOTICES.md is current.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

# ARCA sends nothing off the computer (AGENTS.md), and that includes the tools that build it: switch off the
# anonymous telemetry of the .NET CLI, the test platform and Avalonia's build services for every script here.
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'

dotnet restore Arca.slnx;                                   if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet build Arca.slnx --no-restore -c Release -warnaserror; if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet test --solution Arca.slnx --no-build -c Release;     if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet run build/CheckLicenses.cs;                       if ($LASTEXITCODE) { exit $LASTEXITCODE }
Write-Host 'OK: build and tests passed'
