# Ejecutar ARCA en desarrollo

Mientras no exista el flujo de contraseña de `acces-i-xifrat`, la aplicación no puede pedir la llave de la base de datos. Para desarrollar hay dos variables de entorno que **solo existen para desarrollo** y desaparecerán cuando llegue ese cambio. Ninguna llave vive en el código.

| Variable | Qué hace |
|----------|----------|
| `ARCA_DEV_DB_KEY` | Llave de 64 caracteres hexadecimales. Sin ella la aplicación muestra «Aquesta versió d'ARCA encara no té l'accés amb contrasenya» y no abre nada. |
| `ARCA_DEV_CREATE` | Con el valor `1` crea la base de datos si el fichero no existe (hasta que exista la pantalla de primera ejecución de `configuracio-inicial`). |

## Ejemplo (macOS y Linux)

```bash
dotnet build src/Arca.Desktop
OUT=src/Arca.Desktop/bin/Debug/net10.0
touch $OUT/arca.portable          # modo portable: los datos van en $OUT/data y no se toca tu carpeta de usuario
export ARCA_DEV_DB_KEY=$(python3 -c "import secrets;print(secrets.token_hex(32))")
ARCA_DEV_CREATE=1 $OUT/Arca
```

Guarda la llave que has generado (la misma variable) para volver a abrir esa base de datos: una base creada con una llave no se abre con otra.

## Dónde van los datos sin modo portable

| Sistema | Carpeta |
|---------|---------|
| Windows | `%LOCALAPPDATA%\ARCA` |
| macOS | `~/Library/Application Support/ARCA` |
| Linux | `$XDG_DATA_HOME/arca` (por defecto `~/.local/share/arca`) |

Contiene `arca.db`, `settings.json` y, mientras la aplicación está abierta, `arca.db.lock`.
