## Why

Toda la información del centro vive en un único fichero en un solo PC: si el equipo se estropea o se borra el fichero, se pierde el trabajo de años. Los conserjes necesitan hacer una copia y recuperarla sin conocimientos técnicos, con la seguridad de que restaurar no puede empeorar las cosas y sin depender de la red.

## What Changes

- Copia manual: un fichero único que es una copia consistente y verificada de la base cifrada, con nombre que incluye la fecha, guardado en la carpeta que elija el usuario.
- Restauración guiada: elegir un fichero, verificarlo (integridad, clave, versión), mostrar qué contiene y confirmar con la consecuencia. Antes de sustituir se guarda automáticamente una copia de los datos actuales.
- Una copia de una versión anterior se migra al restaurar con el migrador existente; una de una versión más nueva se rechaza.
- Si la restauración falla a mitad, los datos actuales se recuperan automáticamente.
- Copia y restauración funcionan sin conexión, y la restauración se ofrece también cuando la base actual está dañada.
- Sin copias automáticas, programadas ni recordatorios, y sin mostrar la fecha de la última copia.

## Capabilities

### New Capabilities
- `copia-de-seguretat`: hacer una copia manual consistente, verificada y atómica.
- `restauracio`: restaurar desde una copia con verificación, vista previa, copia previa de los datos actuales y recuperación ante fallos.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Reutiliza el migrador y el rechazo de versiones nuevas de `arquitectura-base`. -->

## Fuera de alcance

- Copias automáticas, programadas o recordatorios, y registro de la última copia.
- Copia en la nube o envío por red: los datos de alumnos no salen del equipo.
- Una contraseña distinta para la copia: la copia usa las mismas llaves que la base (`acces-i-xifrat`).
- Restauración parcial (solo alumnos, solo un curso) y fusión de copias.
- Copiar los ajustes locales (ruta de la base de datos, modo portable).
- Exportar datos en CSV (`informes-csv`) y pantallas (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: servicios de copia y restauración en Application, con la implementación de la copia consistente y del intercambio de ficheros en Infrastructure; reutiliza el migrador y la comprobación de integridad.
- **Datos personales (RGPD)**: la copia contiene todos los datos de menores fuera de la instalación. Va cifrada con la misma llave que la base, protegida por la contraseña del centro y la clave de recuperación (`acces-i-xifrat`); se avisa al hacerla y la custodia del fichero es del centro. Los recuentos de la vista previa no incluyen datos personales y el registro técnico nunca recibe datos de alumnos.
- **Depende de**: `arquitectura-base`.
