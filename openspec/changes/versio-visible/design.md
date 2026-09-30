## Context

Ver `proposal.md` (Why). Hoy `AppInfo.ApplicationVersion` (que sale de `ApplicationVersion()` en `AppStartup`, a partir de la versión del ensamblado) se muestra solo en el bloque de información de Ajustes (`InfoView`). La cabecera (`HeaderView`) muestra el nombre del centro, o el de la aplicación mientras no tiene identidad, y su logotipo; a la derecha, el curso activo y el indicador de trabajo. `Directory.Build.props` define `<Version>0.1.0-dev</Version>`, de donde salen también los nombres de los paquetes.

## Goals / Non-Goals

**Goals:**
- Que la versión se vea siempre, sin ruido, y que sea la misma en todos los sitios.

**Non-Goals:**
- Un mecanismo nuevo de versionado o de actualización.

## Decisions

**D1. Se reutiliza `AppInfo.ApplicationVersion`.** La cabecera recibe el texto ya leído por `AppInfo` (el mismo objeto que usa Ajustes), de modo que ambos sitios muestran por construcción el mismo valor y la fuente sigue siendo el ensamblado construido con `<Version>`. *Alternativa*: leer la versión otra vez en la cabecera; se descarta por abrir la puerta a dos valores distintos.

**D2. Junto al título, en pequeño.** Un `TextBlock` con el tamaño pequeño del tema y el color secundario, alineado a la línea base del título, dentro del grupo izquierdo de la cabecera (tras el nombre), de modo que acompaña tanto al nombre de la aplicación como al del centro. Con descripción emergente «Versió de l'aplicació». No se añade a la cabecera nada más.

**D3. Ventana estrecha.** El grupo izquierdo no se recorta: la versión va en una línea y el nombre del centro es el que se acorta con puntos suspensivos cuando falta sitio, nunca la versión.

**D4. Formato tal cual.** Se muestra la cadena de la versión informativa sin sufijo de compilación del control de versiones (`+hash`), que se quita al leerla en `AppInfo`; el sufijo `-dev` se conserva.

**D5. Fallo de lectura.** Si no se puede leer, `AppInfo` devuelve `?` y el registro técnico anota el fallo; nunca se detiene el arranque.

**D6. Copiar en Ajustes.** El valor de la fila de Ajustes es un texto seleccionable (`SelectableTextBlock`) con la acción de copiar habitual; no se añade botón.

**UX transversal:** el texto y la descripción salen de claves i18n; el número se distingue por el tamaño y el color secundario, no solo por color; legible con el tema claro, oscuro y del sistema.

## Risks / Trade-offs

- [El espacio de la cabecera es limitado] → la versión es corta y fija y lo que se acorta es el nombre (D3); prueba a tamaño mínimo de ventana.
- [Una versión de desarrollo con `+hash` ensucia la cabecera] → se recorta al leerla (D4).

## Migration Plan

1. Mostrar la versión en la cabecera con el valor de `AppInfo`.
2. Hacer seleccionable la de Ajustes.
3. Retroceso: quitar el texto de la cabecera; Ajustes sigue como estaba.
