## Why

Con las capacidades de dominio y los componentes comunes definidos, falta el marco que los une: cómo se navega entre áreas, cómo se busca un alumno o una taquilla desde cualquier sitio, cómo se identifica el centro y qué ve el conserje al abrir la aplicación. La pantalla principal ideal aún no se ha validado con los conserjes, así que este cambio propone una y hace que sea sustituible sin tocar el resto.

## What Changes

- Navegación con una barra lateral fija de secciones (Inicio, Taquillas, Alumnos, Cobros, Llaves e incidencias, Informes, Curso y Ajustes), colapsable a solo iconos, con indicadores de aviso.
- Cabecera con el nombre y el logo del centro, el curso activo (o en cierre).
- Búsqueda global siempre visible que encuentra alumnos, taquillas y grupos con su estado de pago y de taquilla, navegable con teclado y con atajo.
- Identidad del centro configurable (nombre, logo y color de acento) y tema claro, oscuro o del sistema, con contraste garantizado sobre el color elegido.
- Pantalla principal provisional: mapa de taquillas por zona con su estado, filtros, panel de detalle y lista de alumnos sin taquilla para asignar arrastrando. Se registra como pantalla de inicio sustituible.
- Mapa de secciones que reparte las pantallas de cada capacidad y define el patrón común de lista, detalle y acciones.
- Avisos globales del estado de la aplicación: sin curso activo, curso en cierre, versión nueva disponible y asistente de configuración pendiente.

## Capabilities

### New Capabilities
- `navegacio-i-cerca`: secciones, cabecera, avisos globales y búsqueda global.
- `identitat-i-tema`: nombre, logo y color del centro, y tema claro, oscuro o del sistema.
- `pantalla-principal`: mapa de taquillas por zona como inicio provisional y su mecanismo de sustitución.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Compone las capacidades y componentes de los cambios anteriores. -->

## Fuera de alcance

- Validar la pantalla principal con los conserjes: el mapa es una propuesta y puede reemplazarse.
- Diseño detallado y contenido de cada pantalla de dominio más allá del patrón común y del reparto en secciones.
- Componentes genéricos, atajos y adaptabilidad (`ux-fonaments`), ya definidos.
- Idiomas distintos del catalán, diseño táctil y accesibilidad más allá de la de Avalonia.
- Identidad en la pantalla de arranque: muestra la de ARCA, porque el arranque ocurre antes de abrir la base de datos donde vive la del centro.

## Impacto

- **Código**: proyecto de UI con ventana principal, navegación, modelos de vista de secciones, servicio de búsqueda global en Application y de identidad en Application e Infrastructure; almacenamiento del nombre, el logo y el acento en la base de datos y de la preferencia de tema en los ajustes locales.
- **Datos personales (RGPD)**: la búsqueda y el mapa muestran nombres de menores en pantalla y su estado de pago; no se exportan ni se registran. El logo y el nombre del centro no son datos personales. Las notificaciones y el registro técnico no incluyen nombres.
- **Depende de**: todos los cambios anteriores; en especial `ux-fonaments`, `alumnes-i-assignacions`, `taquilles-i-zones`, `pagaments`, `registre-i-actualitzacions` y `configuracio-inicial`.
