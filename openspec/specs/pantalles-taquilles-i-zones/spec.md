# pantalles-taquilles-i-zones Specification

## Purpose
Definir la interfaz de la sección Taquillas en el hito 1: gestión de zonas, listado de taquillas con filtros y contadores, altas, edición, reserva, avería, baja e historial, sobre las reglas de `taquilles-i-zones`.

## Requirements

### Requirement: Organización de la sección Taquillas
El sistema SHALL organizar la sección Taquillas en tres vistas, Mapa, Lista y Zonas, accesibles por pestañas, con Mapa como vista inicial y todas con el patrón común de lista, filtros y detalle. El Mapa y la Lista SHALL ser dos presentaciones de las mismas taquillas: comparten filtros, búsqueda, selección y detalle, y cambiar de una a otra mantiene todo eso.

#### Scenario: Abrir la sección
- **WHEN** el usuario abre Taquillas
- **THEN** ve el mapa y puede cambiar a Lista o a Zonas con el ratón o el teclado

#### Scenario: Cambiar de vista con filtros
- **WHEN** el usuario filtra por averiada en el mapa y cambia a Lista
- **THEN** la lista muestra las averiadas, con el mismo filtro activo y la misma taquilla seleccionada

### Requirement: Lista de taquillas con filtros y contadores
El sistema SHALL mostrar las taquillas activas en una lista virtualizada ordenada por número, con columnas de número, zona, estado visible, alumno asignado y marca de deuda, filtros por zona, estado y número, opción de incluir las de baja y contadores por estado.

#### Scenario: Lista por defecto
- **WHEN** el usuario abre la vista de taquillas
- **THEN** ve las activas por número con los contadores por estado y sin las de baja

#### Scenario: Incluir bajas
- **WHEN** el usuario activa incluir bajas
- **THEN** las de baja aparecen diferenciadas con texto y no solo con color

#### Scenario: Filtro sin resultados
- **WHEN** ninguna taquilla cumple los filtros
- **THEN** la lista muestra que no hay coincidencias y ofrece limpiar los filtros

#### Scenario: Sin taquillas
- **WHEN** no hay taquillas
- **THEN** se muestra un estado vacío que ofrece crear zonas y dar de alta taquillas

#### Scenario: Privacidad
- **WHEN** la lista muestra el alumno de una taquilla ocupada
- **THEN** solo muestra su nombre y apellidos, sin correo ni identificador

### Requirement: Detalle de la taquilla
El sistema SHALL mostrar al seleccionar una taquilla su detalle con el estado, la zona, la nota, el alumno con su estado de pago, la reserva o incidencia si las hay, sus acciones y su historial de eventos, con las acciones no disponibles deshabilitadas y su motivo.

#### Scenario: Taquilla libre
- **WHEN** el usuario selecciona una taquilla libre
- **THEN** el detalle ofrece Asignar, Reservar, Marcar como averiada, En mantenimiento, Editar y Dar de baja

#### Scenario: Taquilla de baja
- **WHEN** el usuario selecciona una taquilla de baja
- **THEN** el detalle muestra su historial y ninguna acción de modificación

#### Scenario: Acción no disponible
- **WHEN** una acción no procede, por ejemplo dar de baja una taquilla ocupada
- **THEN** aparece deshabilitada con su motivo

#### Scenario: Historial
- **WHEN** el usuario abre el historial de una taquilla
- **THEN** ve cada evento con su instante y los valores anterior y nuevo, del más reciente al más antiguo

### Requirement: Alta individual desde un formulario
El sistema SHALL ofrecer la acción Nueva taquilla con un formulario de número, zona activa y nota opcional, que valida al guardar y, al crearla, mantiene el formulario abierto con el número siguiente propuesto para dar de alta más taquillas seguidas.

#### Scenario: Alta correcta
- **WHEN** el usuario guarda la taquilla 15 en la zona Planta 1
- **THEN** se crea libre, se notifica y el formulario propone el 16 con la misma zona

#### Scenario: Número repetido
- **WHEN** el número ya existe entre las taquillas activas
- **THEN** el campo se marca y explica que el número está en uso, sin perder lo escrito

#### Scenario: Sin zonas activas
- **WHEN** no existe ninguna zona activa
- **THEN** el formulario explica que hace falta una zona y ofrece crearla

### Requirement: Alta por rangos con vista previa
El sistema SHALL ofrecer la acción Alta por rangos, con primer número, último número y zona, que muestra una vista previa con el recuento de taquillas a crear y los números en conflicto antes de confirmar.

#### Scenario: Vista previa
- **WHEN** el usuario indica del 1 al 40 en una zona
- **THEN** ve que se crearán 40 taquillas y pide confirmación con ese recuento

#### Scenario: Conflicto en el rango
- **WHEN** algún número del rango ya existe
- **THEN** la vista previa lo indica, no permite confirmar y no se crea ninguna

#### Scenario: Progreso
- **WHEN** el alta tarda
- **THEN** se muestra el progreso con recuentos y no se puede lanzar dos veces

### Requirement: Editar número y zona
El sistema SHALL permitir editar el número y la zona de una taquilla que no esté de baja desde su detalle, con validación al guardar.

#### Scenario: Cambio de zona
- **WHEN** el usuario cambia la zona de una taquilla ocupada y guarda
- **THEN** el cambio se aplica, la asignación se conserva y se anota en el historial

### Requirement: Reservar y quitar la reserva
El sistema SHALL permitir reservar una taquilla libre con una nota opcional y quitar la reserva desde su detalle, mostrando la nota en la lista y en el detalle.

#### Scenario: Reservar
- **WHEN** el usuario reserva una taquilla libre con la nota "Profesorado"
- **THEN** la taquilla pasa a reservada y muestra la nota

### Requirement: Fuera de servicio con decisión
El sistema SHALL permitir marcar una taquilla como averiada o en mantenimiento y marcarla como reparada desde su detalle, y SHALL, si la taquilla está ocupada, presentar antes de aplicar el cambio un diálogo de decisión con reasignar, mantener o liberar, sin cambiar nada hasta que se decida.

#### Scenario: Taquilla libre averiada
- **WHEN** el usuario marca como averiada una taquilla libre
- **THEN** pasa a averiada y se notifica

#### Scenario: Taquilla ocupada
- **WHEN** el usuario marca como averiada una taquilla ocupada
- **THEN** aparece el diálogo con las tres opciones y el alumno afectado, y no se cambia nada hasta elegir una

#### Scenario: Cancelar la decisión
- **WHEN** el usuario cierra el diálogo sin elegir
- **THEN** la taquilla y su asignación quedan como estaban

#### Scenario: Reparada
- **WHEN** el usuario marca como reparada una taquilla fuera de servicio
- **THEN** vuelve a operativa y su estado visible se recalcula

### Requirement: Baja de una taquilla con confirmación
El sistema SHALL ofrecer dar de baja una taquilla sin asignación ni reserva con una confirmación que indique que es irreversible y que su historial se conserva.

#### Scenario: Confirmar la baja
- **WHEN** el usuario confirma la baja de una taquilla libre
- **THEN** la taquilla desaparece de la lista por defecto y se notifica

#### Scenario: Taquilla ocupada
- **WHEN** la taquilla tiene asignación o reserva
- **THEN** Dar de baja aparece deshabilitada con el motivo

### Requirement: Vista de zonas
El sistema SHALL mostrar en la vista Zonas la lista de zonas ordenada según el catalán con su estado y su número de taquillas activas, y SHALL ofrecer crear, renombrar, desactivar, reactivar y eliminar zonas con las restricciones de `taquilles-i-zones`.

#### Scenario: Crear zona
- **WHEN** el usuario crea la zona "Planta 1"
- **THEN** aparece en la lista activa y queda disponible en los formularios de taquillas

#### Scenario: Nombre repetido
- **WHEN** el nombre coincide con otra zona sin distinguir mayúsculas ni acentos
- **THEN** el formulario lo indica y no la crea

#### Scenario: Desactivar con taquillas
- **WHEN** la zona tiene taquillas activas
- **THEN** Desactivar aparece deshabilitada con el motivo

#### Scenario: Eliminar zona con historial
- **WHEN** la zona ha tenido alguna taquilla
- **THEN** Eliminar aparece deshabilitada con el motivo y se ofrece desactivarla si procede

#### Scenario: Sin zonas
- **WHEN** no hay zonas
- **THEN** la vista explica que las taquillas se organizan por zonas y ofrece crear la primera

### Requirement: Feedback y doble ejecución en Taquillas
El sistema SHALL confirmar el resultado de cada operación con una notificación, usar el indicador de trabajo y proteger de la doble ejecución todos los guardados, altas, bajas y cambios de estado.

#### Scenario: Error de una operación
- **WHEN** una operación falla por una regla de negocio
- **THEN** se muestra un mensaje comprensible y persistente que indica qué hacer, sin datos técnicos

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
