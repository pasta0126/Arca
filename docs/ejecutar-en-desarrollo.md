# Ejecutar ARCA en desarrollo

## Contraseña y primera ejecución

La aplicación pide la contraseña del centro cada vez que se abre (`acces-i-xifrat`). Si la base de datos no existe, la primera ejecución pide una contraseña de al menos 12 caracteres, muestra la clave de recuperación y pide confirmarla escribiendo dos grupos antes de crear la base en la carpeta de datos. **No hay ninguna llave ni contraseña por defecto**: las variables `ARCA_DEV_DB_KEY` y `ARCA_DEV_CREATE` que existían hasta ahora ya no existen.

Para desarrollar, usa una contraseña de prueba que cumpla la política (por ejemplo `gat ratllat sota pluja`) y guarda la clave de recuperación que se muestra. Las pruebas y la futura herramienta de datos de ejemplo definen sus propias contraseñas en sus proyectos.

## Ejemplo (macOS y Linux)

```bash
dotnet build src/Arca.Desktop
OUT=src/Arca.Desktop/bin/Debug/net10.0
touch $OUT/arca.portable          # modo portable: los datos van en $OUT/data y no se toca tu carpeta de usuario
$OUT/Arca                         # la primera vez pide la contraseña y muestra la clave de recuperación
```

## Dónde van los datos sin modo portable

| Sistema | Carpeta |
|---------|---------|
| Windows | `%LOCALAPPDATA%\ARCA` |
| macOS | `~/Library/Application Support/ARCA` |
| Linux | `$XDG_DATA_HOME/arca` (por defecto `~/.local/share/arca`) |

Contiene `arca.db`, `settings.json` y, mientras la aplicación está abierta, `arca.db.lock`.

## Ver las pantallas sin abrir la aplicación

Las pruebas de vista pueden guardar capturas PNG de las pantallas (arranque, confirmación, información) para revisar el aspecto, sin datos ni ventana:

```bash
ARCA_SCREENSHOT=/tmp/arca-capturas dotnet test --solution Arca.slnx
```

Sin la variable, esas pruebas se omiten. La paleta está en `src/Arca.UI/Theme/ArcaPalette.cs`.
