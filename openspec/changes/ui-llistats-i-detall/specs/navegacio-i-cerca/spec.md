## MODIFIED Requirements

### Requirement: Barra lateral de secciones
El sistema SHALL mostrar una barra lateral fija con las secciones Inicio, Taquillas, Alumnos, Llaves e incidencias, Informes, Curso y Ajustes, con icono y texto, colapsable a solo iconos, operable con ratón y teclado.

#### Scenario: Cambiar de sección
- **WHEN** el usuario elige Alumnos en la barra lateral
- **THEN** se abre la sección de alumnos y queda resaltada como la activa

#### Scenario: Colapsar la barra
- **WHEN** el usuario colapsa la barra lateral
- **THEN** se muestran solo los iconos con su texto en una descripción emergente y el estado se recuerda

#### Scenario: Navegación con teclado
- **WHEN** el usuario mueve el foco a la barra y pulsa Intro sobre una sección
- **THEN** se abre esa sección

#### Scenario: Sin sección de cobros
- **WHEN** el usuario mira la barra lateral
- **THEN** no hay ninguna sección Cobros, porque los cobros se hacen desde el alumno

### Requirement: Reparto de las pantallas por sección
El sistema SHALL ubicar cada pantalla de dominio en una sola sección: Taquillas (mapa, lista, zonas y su historial), Alumnos (alumnos, sus cargos y fianza, importación y asignaciones), Llaves e incidencias (llaves, incidencias y mantenimiento en bloque), Informes (informes y exportación), Curso (curso actual, importes, cierre y conservación de datos) y Ajustes (identidad, tema, registro y avisos de versión, copia de seguridad y restauración, configuración guiada, motivos de incidencia y carpeta de datos).

#### Scenario: Importes del curso
- **WHEN** el usuario busca dónde definir los importes
- **THEN** los encuentra en la sección Curso

#### Scenario: Copia de seguridad
- **WHEN** el usuario busca hacer una copia
- **THEN** la encuentra en Ajustes

#### Scenario: Cobrar a un alumno
- **WHEN** el usuario busca dónde cobrar
- **THEN** lo encuentra en la ficha del alumno, en Alumnos

#### Scenario: Una pantalla en una sola sección
- **WHEN** se revisa el mapa de secciones
- **THEN** ninguna pantalla aparece en dos secciones distintas

### Requirement: Patrón común de pantalla
El sistema SHALL presentar las pantallas de dominio con una estructura común: título y acciones principales arriba, lista con búsqueda y filtros y detalle del elemento seleccionado, con estados vacíos y de carga guiados, y las mismas acciones en botones, menús y atajos. La lista SHALL mostrar el recuento de elementos según los filtros, un botón de reiniciar que vacía el cuadro de búsqueda y quita todos los filtros para devolver la lista a su estado inicial, y una etiqueta visible por cada filtro activo que se puede quitar por separado. Pulsar `Esc` con un elemento seleccionado SHALL quitar la selección y el foco, dejando la pantalla sin ningún elemento activo.

#### Scenario: Pantalla de lista y detalle
- **WHEN** el usuario abre una sección con una lista
- **THEN** ve la lista con sus filtros y, al seleccionar un elemento, su detalle y sus acciones

#### Scenario: Sección sin datos
- **WHEN** una sección no tiene ningún elemento
- **THEN** muestra el estado vacío con la acción para crear el primero

#### Scenario: Reiniciar la búsqueda
- **WHEN** el usuario tiene texto en la búsqueda y dos filtros activos y pulsa Reiniciar
- **THEN** el cuadro queda vacío, los filtros se quitan, la lista vuelve a su estado inicial con su recuento y la selección se conserva solo si el elemento sigue en la lista

#### Scenario: Nada que reiniciar
- **WHEN** la búsqueda está vacía y no hay filtros activos
- **THEN** el botón Reiniciar aparece deshabilitado

#### Scenario: Quitar un filtro
- **WHEN** el usuario quita la etiqueta de un filtro activo
- **THEN** solo se quita ese filtro y el resto se mantiene

#### Scenario: Quitar la selección con Esc
- **WHEN** hay un elemento seleccionado y el usuario pulsa `Esc`
- **THEN** la selección y el foco desaparecen, el detalle vuelve a su estado de «elige un elemento» y ningún elemento queda activo

#### Scenario: Esc dentro de un formulario
- **WHEN** hay un formulario o un diálogo abierto y el usuario pulsa `Esc`
- **THEN** se cierra primero el diálogo y la selección de la lista no cambia

#### Scenario: Esc con la búsqueda enfocada
- **WHEN** el foco está en el cuadro de búsqueda con texto y el usuario pulsa `Esc`
- **THEN** se vacía el texto y la selección no cambia

### Requirement: Indicadores de sección
El sistema SHALL mostrar en la barra lateral un indicador con el recuento cuando una sección tiene elementos que atender: alumnos con pagos pendientes en Alumnos, llaves sin resolver e incidencias abiertas en Llaves e incidencias y pasos pendientes en Curso. Los indicadores cuentan elementos y nunca muestran importes.

#### Scenario: Incidencias abiertas
- **WHEN** hay 3 incidencias abiertas
- **THEN** la sección Llaves e incidencias muestra un indicador con 3

#### Scenario: Alumnos con pendientes
- **WHEN** hay 60 alumnos con pagos pendientes
- **THEN** la sección Alumnos muestra un indicador con 60 y no un importe

#### Scenario: Nada que atender
- **WHEN** una sección no tiene elementos pendientes
- **THEN** no muestra ningún indicador

## ADDED Requirements

### Requirement: Scroll en pantallas con contenido largo
El sistema SHALL permitir desplazar con ratón, rueda y teclado cualquier pantalla o panel cuyo contenido sea más alto que la ventana, de modo que ningún bloque quede inaccesible, en cualquier tamaño de ventana permitido y con cualquier escala de DPI.

#### Scenario: Ajustes con ventana pequeña
- **WHEN** el usuario abre Ajustes y sus bloques no caben en la ventana
- **THEN** la pantalla se desplaza y puede llegar al último bloque

#### Scenario: Bloque que crece
- **WHEN** el usuario despliega un bloque colapsable que hace la pantalla más alta que la ventana
- **THEN** el contenido adicional es accesible desplazando

#### Scenario: Teclado
- **WHEN** el foco recorre con Tab los controles de una pantalla larga
- **THEN** el control enfocado se desplaza a la vista
