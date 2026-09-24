#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Single definition of "the build is green": restore, compile with warnings as errors,
# and run every test project (docs/stack.md, "Verificación multiplataforma").
# TODO(arquitectura-base 9.1b): add the dependency licence check here.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet restore Arca.slnx
dotnet build Arca.slnx --no-restore -c Release -warnaserror
dotnet test --solution Arca.slnx --no-build -c Release
echo "OK: build and tests passed"
