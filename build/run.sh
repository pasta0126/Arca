#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Compila y abre ARCA en modo portable para desarrollar (macOS y Linux). Desde la raíz del repositorio:
#
#   build/run.sh            compila y abre la aplicación
#   build/run.sh --reset    borra antes los datos de desarrollo (pide la contraseña y la clave de recuperación de nuevo)
#   build/run.sh --demo     crea antes una base con datos ficticios (600 taquillas, 900 alumnos, cobros y bajas) y la abre;
#                           tarda unos 20 segundos y la contraseña es la de prueba (ver abajo)
#   build/run.sh --new-year crea antes un centro que empieza el curso: curso nuevo activo con sus importes, 6 zonas y 600 taquillas,
#                           900 alumnos matriculados sin taquilla y 60 con deuda del curso anterior (contraseña: demo)
#
# Modo portable: los datos van en src/Arca.Desktop/bin/Debug/net10.0/data y no se toca la carpeta de usuario.
# Contraseña de prueba que cumple la política: demo
set -euo pipefail

cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1

OUT=src/Arca.Desktop/bin/Debug/net10.0

dotnet build src/Arca.Desktop

if [[ "${1:-}" == "--reset" ]]; then
    rm -rf "$OUT/data"
    echo "Datos de desarrollo borrados."
fi

if [[ "${1:-}" == "--demo" ]]; then
    dotnet run --project tools/Arca.DemoData -c Release -- --folder "$OUT/data" --force
fi

if [[ "${1:-}" == "--new-year" ]]; then
    dotnet run --project tools/Arca.DemoData -c Release -- --folder "$OUT/data" --force --profile new-year
fi

touch "$OUT/arca.portable"
exec "$OUT/Arca"
