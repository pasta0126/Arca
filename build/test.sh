#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Single definition of "the build is green": restore, compile with warnings as errors,
# and run every test project (docs/stack.md, "Verificación multiplataforma").
# The licence check (build/CheckLicenses.cs) also verifies THIRD-PARTY-NOTICES.md is current.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet restore Arca.slnx
dotnet build Arca.slnx --no-restore -c Release -warnaserror
dotnet test --solution Arca.slnx --no-build -c Release
dotnet run build/CheckLicenses.cs
echo "OK: build and tests passed"
