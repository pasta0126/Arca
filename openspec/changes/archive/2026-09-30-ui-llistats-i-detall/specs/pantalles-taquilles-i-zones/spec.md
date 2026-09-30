## MODIFIED Requirements

### Requirement: Organización de la sección Taquillas
El sistema SHALL organizar la sección Taquillas en tres vistas, Mapa, Lista y Zonas, accesibles por pestañas, con Mapa como vista inicial y todas con el patrón común de lista, filtros y detalle. El Mapa y la Lista SHALL ser dos presentaciones de las mismas taquillas: comparten filtros, búsqueda, selección y detalle, y cambiar de una a otra mantiene todo eso.

#### Scenario: Abrir la sección
- **WHEN** el usuario abre Taquillas
- **THEN** ve el mapa y puede cambiar a Lista o a Zonas con el ratón o el teclado

#### Scenario: Cambiar de vista con filtros
- **WHEN** el usuario filtra por averiada en el mapa y cambia a Lista
- **THEN** la lista muestra las averiadas, con el mismo filtro activo y la misma taquilla seleccionada

## ADDED Requirements

### Requirement: Vista de mapa de taquillas
El sistema SHALL mostrar en la vista Mapa las taquillas activas agrupadas por zona, cada una con su número y su estado visible, ordenadas por número, con las zonas como secciones colapsables separadas entre sí por un espacio visible, con filtros por estado y zona, con la marca de deuda en las ocupadas con un alumno con pendientes, con contadores por estado y con las taquillas que coinciden con la búsqueda resaltadas. El mapa SHALL cargarse con una consulta agregada por lotes y mantenerse receptivo con más de 300 taquillas.

#### Scenario: Mapa por zonas
- **WHEN** el usuario abre la vista Mapa
- **THEN** ve cada zona con sus taquillas y el número de taquillas por estado, y cada zona se distingue claramente de la siguiente por un espacio entre ellas

#### Scenario: Estado visible
- **WHEN** se muestra una taquilla
- **THEN** su estado (libre, ocupada, reservada, averiada o en mantenimiento) se distingue por color y por icono o texto

#### Scenario: Taquilla ocupada con deuda
- **WHEN** una taquilla ocupada tiene un alumno con cargos pendientes
- **THEN** muestra además la marca de deuda

#### Scenario: Filtrar por estado
- **WHEN** el usuario filtra por libre
- **THEN** el mapa muestra solo las taquillas libres de cada zona, con las zonas sin ninguna ocultas o indicadas como vacías, y los contadores se mantienen

#### Scenario: Zona desactivada
- **WHEN** una zona está desactivada
- **THEN** no aparece en el mapa

#### Scenario: Resaltar una búsqueda
- **WHEN** el usuario elige una taquilla en los resultados de la búsqueda global
- **THEN** se abre Taquillas con el mapa, la taquilla resaltada y su detalle abierto

#### Scenario: Esc en el mapa
- **WHEN** hay una taquilla seleccionada y el usuario pulsa `Esc`
- **THEN** la selección y el foco desaparecen y el detalle vuelve a su estado de «elige una taquilla»

#### Scenario: Sin taquillas
- **WHEN** no hay taquillas activas
- **THEN** se muestra un estado vacío que guía a darlas de alta o a la configuración guiada

#### Scenario: 300 taquillas
- **WHEN** el usuario abre el mapa con 300 taquillas
- **THEN** aparece con un indicador de carga que no bloquea la interfaz

#### Scenario: Actualización tras un cambio
- **WHEN** se asigna o libera una taquilla
- **THEN** solo se actualiza esa taquilla y los contadores, sin recargar todo el mapa

### Requirement: Panel de alumnos sin taquilla en el mapa
El sistema SHALL ofrecer en la vista Mapa un panel con los alumnos activos del curso activo sin taquilla, con búsqueda, recuento y botón de reiniciar, que sirve de origen para asignar arrastrando y también permite asignar desde el menú y el teclado.

#### Scenario: Lista de alumnos
- **WHEN** hay 40 alumnos sin taquilla
- **THEN** el panel los lista con el recuento 40

#### Scenario: Asignar arrastrando
- **WHEN** el usuario arrastra un alumno del panel sobre una taquilla libre
- **THEN** se asigna con las validaciones y avisos habituales y el mapa y el panel se actualizan

#### Scenario: Todos con taquilla
- **WHEN** no queda ningún alumno sin taquilla
- **THEN** el panel lo indica y ofrece ver los alumnos

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** el panel indica que hay que activar un curso y no permite asignar
