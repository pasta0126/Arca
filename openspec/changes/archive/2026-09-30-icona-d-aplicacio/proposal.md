## Why

ARCA todavía no tiene icono: la ventana, la barra de tareas, el dock y el paquete de cada sistema muestran el genérico del sistema o de Avalonia, y eso da una imagen de programa sin acabar. Hace falta **un icono sencillo y sin pretensiones ya**, pensado para cambiarlo más adelante por una imagen definitiva sin tocar código.

## What Changes

- **Icono provisional propio y discreto**: un símbolo neutro de taquilla (un cuadrado redondeado con la puerta de una taquilla) en los tonos pastel del tema, dibujado para este proyecto.
- **Un solo fichero maestro** (vectorial) del que salen, con un script, todos los formatos: varios tamaños PNG, `.ico` de Windows, `.icns` de macOS. Los derivados se guardan en el repositorio para que compilar no necesite herramientas de imágenes.
- **Se muestra** en la ventana principal y en las ventanas de diálogo, en la barra de tareas o el dock y en el selector de aplicaciones, en Windows, Linux y macOS.
- **Los paquetes lo incluyen**: icono del ejecutable en Windows, `ARCA.app` con su `.icns` en macOS y PNG con un acceso de escritorio de ejemplo en el paquete de Linux.
- **Sustituirlo después** es cambiar el fichero maestro y ejecutar el script; nada en el código ni en las specs depende del dibujo concreto.
- El icono de la aplicación es independiente del **logotipo del centro** (Ajustes, identidad), que sigue siendo del centro y se muestra en la cabecera.

## Capabilities

### New Capabilities

- `icona-d-aplicacio`: el icono de ARCA, dónde se ve, cómo se empaqueta, cómo se sustituye y con qué licencia.

### Modified Capabilities

Ninguna.

## Impact

- Código: recurso del icono en `Arca.Desktop`, asignación a las ventanas (`Window.Icon`), `ApplicationIcon` en el proyecto, scripts de empaquetado (`build/package.sh`) y un script nuevo para generar los derivados.
- Ficheros nuevos: `assets/icon/` con el maestro y los derivados.
- Sin dependencias nuevas en tiempo de ejecución; las herramientas de generación son gratuitas y de código abierto (coste cero).
- **RGPD**: no afecta.
- Marca: el icono provisional es un dibujo original con la licencia del repositorio; el icono o logotipo definitivo de la marca ARCA (`TRADEMARK.md`) se decidirá aparte.

## Fuera de alcance

- El logotipo definitivo de la marca, el diseño gráfico del instalador, su pantalla de bienvenida y cualquier rotulación.
- Icono distinto por estado (aviso, versión nueva) o icono en la bandeja del sistema.
- Firma de los paquetes y del instalador (documentada en `docs/firma-de-codigo.md`).
