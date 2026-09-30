## Why

Durante el desarrollo y las pruebas con los conserjes hay que saber **en qué versión se está** de un vistazo, sin abrir nada: para comparar lo que se ve con lo que se ha cambiado, para ubicar un fallo y para dar soporte. Hoy la versión solo aparece dentro de Ajustes.

## What Changes

- **Número de versión `x.y.z`, en pequeño, junto al título de ARCA en la cabecera**, visible desde cualquier pantalla, y el mismo número en Ajustes.
- **Una única fuente**: la versión del proyecto (`Directory.Build.props`), de donde sale también el nombre de los paquetes; no se escribe a mano en ningún otro sitio.
- **Las compilaciones de desarrollo se distinguen**: la versión lleva el sufijo de la compilación (por ejemplo `0.1.0-dev`) y se ve tal cual, para no confundir una prueba con una versión publicada.
- En Ajustes el número se puede seleccionar y copiar para pegarlo en un aviso de soporte.

## Capabilities

### New Capabilities

- `versio-de-l-aplicacio`: dónde se ve la versión, de dónde sale, su formato y su coherencia con el paquete.

### Modified Capabilities

Ninguna.

## Impact

- Código: la cabecera (`HeaderView`) y el bloque de información de Ajustes, que ya leen la versión de `AppInfo`; claves i18n en catalán.
- Sin cambios de dominio, de base de datos ni dependencias.
- **RGPD**: no afecta; la versión no identifica a nadie y no se envía a ningún sitio por este cambio (el registro de instalaciones es de `registre-i-actualitzacions`).

## Fuera de alcance

- Avisar de versiones nuevas o actualizar (es de `registre-i-actualitzacions`).
- Mostrar la versión del esquema de datos junto al título (sigue solo en Ajustes).
- Numeración y calendario de versiones publicadas.
