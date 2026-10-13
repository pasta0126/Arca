# Arca.DbEditor

Editor genérico de ficheros SQLite (planos, SQLCipher 4 o la base de ARCA). Herramienta de desarrollo: **no forma parte del producto** y no se incluye en ningún paquete ni instalador.

## Levantarla

Desde la raíz del repositorio (`/Users/pasta0126/arca`):

```bash
dotnet run --project tools/Arca.DbEditor                      # abre la ventana y eliges el fichero
dotnet run --project tools/Arca.DbEditor -- <ruta>/arca.db    # con el fichero ya puesto
```

Requisitos: el SDK de .NET fijado en `global.json` (.NET 10). No hace falta nada más.

El binario compilado queda en `tools/Arca.DbEditor/bin/Debug/net10.0/Arca.DbEditor`. En macOS no es una `.app`: se lanza desde la terminal.

## Qué fichero abrir

| Fichero | Segundo campo |
|---------|---------------|
| SQLite normal | vacío |
| Base de ARCA (`arca.db`) | contraseña del centro (necesita `arca.arcakeys` al lado) |
| SQLCipher 4 | llave de 64 caracteres hexadecimales |

## Base de demo lista para probar

Genera (o regenera) la base pequeña `montu` en `artifacts/demo-db` (carpeta ignorada por git; contraseña `demo`):

```bash
dotnet run --project tools/Arca.DemoData -c Release -- --folder artifacts/demo-db --force --profile montu
```

Contenido: 2 zonas (Planta 0 y Planta 1), 200 alumnos (20 por nivel de ESO, 30 en cada 1r de Batxillerat, 40 en 2n A y 20 en 2n B) y 190 taquillas. Sin `--profile montu` sale el centro grande de siempre (`demo`: 900 alumnos y 600 taquillas); también existe `--profile new-year`.

Y ábrela con el editor, escribiendo `demo` en el segundo campo:

```bash
dotnet run --project tools/Arca.DbEditor -- artifacts/demo-db/arca.db
```

La base de desarrollo de ARCA está en `src/Arca.Desktop/bin/Debug/net10.0/data/arca.db`. Con datos ficticios (`build/run.sh --demo`) la contraseña es `demo`.

## Uso

- **Tablas**: lista a la izquierda (vistas marcadas). Rejilla paginada de 200 filas con filtro por cualquier columna.
- **Editar**: doble clic en una celda. `NULL` escrito tal cual es NULL; `x'AB12'` es un blob.
- **Filas**: «Afegeix fila» (rellena solo lo que la tabla exige) y «Elimina seleccionades».
- **SQL**: pestaña aparte para sentencias libres. `SELECT` y `EXPLAIN` no tocan nada.
- **Guardar**: los cambios quedan pendientes hasta «Desa els canvis»; «Descarta» los deshace. Si cierras con cambios pendientes avisa primero.

## Seguridad

- Antes de la primera escritura de la sesión se hace una copia `<fichero>.<fecha>.bak` junto al original, con el mismo cifrado.
- Las claves foráneas se comprueban.
- Edita el fichero directamente, sin pasar por las reglas de negocio de ARCA: un cambio puede dejar datos que la aplicación no espera. **Cierra ARCA antes de usarla.**
- No envía nada fuera del equipo.

## Llevarla a otro equipo

```bash
dotnet publish tools/Arca.DbEditor -c Release -r win-x64 --self-contained   # o osx-arm64, linux-x64…
```

Solo está comprobada en macOS.
