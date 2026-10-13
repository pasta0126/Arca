# Ejecutar ARCA en desarrollo

## Contraseña y primera ejecución

La aplicación pide la contraseña del centro cada vez que se abre (`acces-i-xifrat`). Si la base de datos no existe, la primera ejecución pide una contraseña de al menos 4 caracteres (avisa si es débil, sobre todo por debajo de 12), muestra la clave de recuperación y pide confirmarla escribiendo dos grupos antes de crear la base en la carpeta de datos. **No hay ninguna llave ni contraseña por defecto**: las variables `ARCA_DEV_DB_KEY` y `ARCA_DEV_CREATE` que existían hasta ahora ya no existen.

Para desarrollar, usa una contraseña de prueba corta (por ejemplo `demo`; la política solo pide 4 caracteres) y guarda la clave de recuperación que se muestra. Las pruebas y la futura herramienta de datos de ejemplo definen sus propias contraseñas en sus proyectos.

## Un solo comando (macOS y Linux)

Desde la raíz del repositorio, en la terminal de VS Code:

```bash
build/run.sh            # compila y abre ARCA en modo portable
build/run.sh --reset    # igual, pero borra antes los datos de desarrollo (vuelve a pedir contraseña y clave)
build/run.sh --demo     # crea antes una base con datos ficticios y la abre (contraseña: demo)
```

La opción `--demo` usa `tools/Arca.DemoData`, que rellena la base llamando a los casos de uso reales: 6 zonas y 600 taquillas (50 libres, 520 ocupadas, 5 reservadas, 15 avariadas y 10 en mantenimiento), 900 alumnos en 20 grupos, cobros pagados, exentos y pendientes, deuda del curso anterior y 20 bajas con la fianza por devolver. Los nombres son inventados y los correos usan `test.cat`. El centro se llama «Ins. Monturiol» con acento `#A9C4D3` y Inicio ya trae las siete tarjetas de serie. `--profile montu` (solo con `tools/Arca.DemoData`) crea un centro pequeño de 2 zonas, 200 alumnos y 190 taquillas. `--new-year` crea en su lugar un centro que empieza el curso (curso nuevo activo, 912 alumnos matriculados sin taquilla y 60 con deuda del curso anterior). La misma semilla da siempre los mismos datos (`--seed`). También se puede lanzar suelta: `dotnet run --project tools/Arca.DemoData -- --folder <carpeta> [--password <texto>] [--force]`. **Cierra ARCA antes**: necesita la base para ella sola.

## Editor de bases de datos (`tools/Arca.DbEditor`)

Herramienta de desarrollo, fuera del producto, para ver y corregir a mano cualquier fichero SQLite sin depender de los modelos de ARCA: sirve para la base de ARCA y para cualquier otra. No se incluye en ningún paquete ni instalador.

```bash
dotnet run --project tools/Arca.DbEditor                       # abre la ventana y eliges el fichero
dotnet run --project tools/Arca.DbEditor -- <ruta>/arca.db     # con el fichero ya puesto
```

- **Abrir**: sin nada en el segundo campo, SQLite normal; con la contraseña del centro, una base de ARCA (necesita `arca.arcakeys` al lado); con 64 caracteres hexadecimales, SQLCipher 4 con esa llave.
- **Ver y editar**: lista de tablas y vistas a la izquierda, rejilla paginada (200 filas) con filtro por cualquier columna. Doble clic en una celda para editarla; `NULL` escrito tal cual es NULL y `x'AB12'` es un blob. Añadir fila (rellena solo lo que la tabla exige) y eliminar las seleccionadas.
- **SQL**: pestaña aparte para sentencias libres. Las consultas (`SELECT`, `EXPLAIN`) no tocan nada.
- **Seguridad**: lo que se escribe queda pendiente en una transacción hasta pulsar «Desa els canvis» («Descarta» lo deshace). Antes de la primera escritura de la sesión se hace una copia `<fichero>.<fecha>.bak` junto al original, con el mismo cifrado. Las claves foráneas se comprueban. **Cierra ARCA antes**: edita el fichero directamente, sin pasar por las reglas de negocio, así que un cambio puede dejar datos que ARCA no espera.

Los datos quedan en `src/Arca.Desktop/bin/Debug/net10.0/data`. Es lo mismo que el ejemplo de abajo, en un script.

## Ejemplo paso a paso (macOS y Linux)

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
