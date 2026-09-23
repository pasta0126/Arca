## Purpose

Definir la estructura de navegación de la aplicación, la cabecera con el estado global y la búsqueda global que localiza un alumno, una taquilla o un grupo desde cualquier pantalla.

## ADDED Requirements

### Requirement: Barra lateral de secciones
El sistema SHALL mostrar una barra lateral fija con las secciones Inicio, Taquillas, Alumnos, Cobros, Llaves e incidencias, Informes, Curso y Ajustes, con icono y texto, colapsable a solo iconos, operable con ratón y teclado.

#### Scenario: Cambiar de sección
- **WHEN** el usuario elige Alumnos en la barra lateral
- **THEN** se abre la sección de alumnos y queda resaltada como la activa

#### Scenario: Colapsar la barra
- **WHEN** el usuario colapsa la barra lateral
- **THEN** se muestran solo los iconos con su texto en una descripción emergente y el estado se recuerda

#### Scenario: Navegación con teclado
- **WHEN** el usuario mueve el foco a la barra y pulsa Intro sobre una sección
- **THEN** se abre esa sección

### Requirement: Reparto de las pantallas por sección
El sistema SHALL ubicar cada pantalla de dominio en una sola sección: Taquillas (taquillas, zonas y su historial), Alumnos (alumnos, importación y asignaciones), Cobros (cargos, morosos y fianzas), Llaves e incidencias (llaves, incidencias y mantenimiento en bloque), Informes (informes y exportación), Curso (curso actual, importes, cierre y conservación de datos) y Ajustes (identidad, tema, licencia, copia de seguridad y restauración, configuración guiada, motivos de incidencia y carpeta de datos).

#### Scenario: Importes del curso
- **WHEN** el usuario busca dónde definir los importes
- **THEN** los encuentra en la sección Curso

#### Scenario: Copia de seguridad
- **WHEN** el usuario busca hacer una copia
- **THEN** la encuentra en Ajustes

#### Scenario: Una pantalla en una sola sección
- **WHEN** se revisa el mapa de secciones
- **THEN** ninguna pantalla aparece en dos secciones distintas

### Requirement: Patrón común de pantalla
El sistema SHALL presentar las pantallas de dominio con una estructura común: título y acciones principales arriba, lista con búsqueda y filtros y detalle del elemento seleccionado, con estados vacíos y de carga guiados, y las mismas acciones en botones, menús y atajos.

#### Scenario: Pantalla de lista y detalle
- **WHEN** el usuario abre una sección con una lista
- **THEN** ve la lista con sus filtros y, al seleccionar un elemento, su detalle y sus acciones

#### Scenario: Sección sin datos
- **WHEN** una sección no tiene ningún elemento
- **THEN** muestra el estado vacío con la acción para crear el primero

### Requirement: Cabecera con el estado global
El sistema SHALL mostrar en una cabecera fija el nombre y el logo del centro, el curso activo o el curso en cierre y el indicador de licencia con los días restantes cuando el estado es prueba o gracia.

#### Scenario: Curso activo
- **WHEN** hay un curso activo
- **THEN** la cabecera muestra su nombre, por ejemplo "2026-2027"

#### Scenario: Curso en cierre sin curso activo
- **WHEN** no hay curso activo y hay uno en cierre
- **THEN** la cabecera indica que no hay curso activo, muestra el curso en cierre y ofrece activar el siguiente

#### Scenario: Licencia en gracia
- **WHEN** la licencia está en gracia con 12 días restantes
- **THEN** la cabecera muestra el indicador con 12 días

#### Scenario: Licencia activa
- **WHEN** la licencia está activa
- **THEN** la cabecera no muestra ningún indicador de licencia

### Requirement: Avisos globales
El sistema SHALL mostrar de forma no bloqueante los avisos de estado de la aplicación con su acción directa: sin curso activo, curso en cierre con pasos pendientes, configuración obligatoria pendiente y licencia en prueba, gracia o solo lectura.

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** se muestra un aviso con una acción para abrir la configuración guiada

#### Scenario: Solo lectura
- **WHEN** la licencia está en solo lectura
- **THEN** se muestra un aviso que explica qué no se puede hacer y cómo activar la licencia

#### Scenario: Aviso descartable
- **WHEN** el usuario cierra un aviso informativo
- **THEN** no vuelve a aparecer hasta el siguiente arranque, salvo los que bloquean acciones

### Requirement: Indicadores de sección
El sistema SHALL mostrar en la barra lateral un indicador con el recuento cuando una sección tiene elementos que atender: cargos pendientes en Cobros, llaves sin resolver e incidencias abiertas en Llaves e incidencias y pasos pendientes en Curso.

#### Scenario: Incidencias abiertas
- **WHEN** hay 3 incidencias abiertas
- **THEN** la sección Llaves e incidencias muestra un indicador con 3

#### Scenario: Nada que atender
- **WHEN** una sección no tiene elementos pendientes
- **THEN** no muestra ningún indicador

### Requirement: Búsqueda global siempre visible
El sistema SHALL mostrar en todas las pantallas un cuadro de búsqueda global que encuentre alumnos, taquillas y grupos sin distinguir mayúsculas ni acentos, y SHALL enfocarlo con el atajo de buscar.

#### Scenario: Buscar un alumno
- **WHEN** el usuario escribe "garcia"
- **THEN** ve los alumnos con "García" en sus apellidos agrupados bajo Alumnos

#### Scenario: Buscar una taquilla
- **WHEN** el usuario escribe "15"
- **THEN** ve la taquilla 15 bajo Taquillas y, si hay alumnos cuya taquilla es la 15, también bajo Alumnos

#### Scenario: Buscar un grupo
- **WHEN** el usuario escribe el nombre de un grupo
- **THEN** ve el grupo con su nivel y el número de alumnos

#### Scenario: Atajo
- **WHEN** el usuario pulsa el atajo de buscar en cualquier pantalla
- **THEN** el foco pasa al cuadro de búsqueda global

### Requirement: Resultados con estado visible
El sistema SHALL mostrar en cada resultado de alumno su nivel, grupo, taquilla y estado de pago, y en cada resultado de taquilla su zona, estado y alumno asignado, sin mostrar correo ni identificador.

#### Scenario: Alumno moroso
- **WHEN** un alumno con la cuota pendiente aparece en los resultados
- **THEN** su fila muestra su taquilla y la marca de deuda

#### Scenario: Sin correo ni identificador
- **WHEN** aparece un alumno en los resultados
- **THEN** la fila no muestra su correo ni su identificador

### Requirement: Navegación de resultados
El sistema SHALL permitir recorrer los resultados con el teclado, abrir la ficha del elemento con Intro y cerrar la búsqueda con Escape, mostrando un máximo razonable de resultados por tipo con acceso a la lista completa.

#### Scenario: Abrir la ficha
- **WHEN** el usuario elige un resultado con las flechas y pulsa Intro
- **THEN** se abre la ficha del alumno o de la taquilla en su sección

#### Scenario: Muchos resultados
- **WHEN** hay más resultados de un tipo que el máximo mostrado
- **THEN** se indica cuántos hay en total y se ofrece ver la lista completa filtrada

#### Scenario: Sin resultados
- **WHEN** nada coincide con lo escrito
- **THEN** se indica y se ofrece revisar la escritura o buscar en alumnos de baja

### Requirement: Búsqueda sin bloquear
El sistema SHALL buscar mientras el usuario escribe con una pequeña espera, sin bloquear la interfaz y descartando las búsquedas anteriores.

#### Scenario: Escritura rápida
- **WHEN** el usuario escribe varias letras seguidas
- **THEN** solo se muestra el resultado de la última búsqueda

### Requirement: Ámbito de la búsqueda
El sistema SHALL buscar por defecto entre los alumnos activos del curso activo y las taquillas activas, con una opción para incluir bajas, y SHALL buscar en el curso en cierre cuando no hay curso activo.

#### Scenario: Alumno de baja
- **WHEN** el usuario busca un alumno de baja sin incluir bajas
- **THEN** no aparece y la búsqueda ofrece incluirlas

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo y hay uno en cierre
- **THEN** la búsqueda usa los datos del curso en cierre
