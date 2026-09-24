## Purpose

Definir la interfaz de la sección Taquillas en el hito 1: gestión de zonas, listado de taquillas con filtros y contadores, altas, edición, reserva, avería, baja e historial, sobre las reglas de `taquilles-i-zones`.

## ADDED Requirements

### Requirement: Organización de la sección Taquillas
El sistema SHALL organizar la sección Taquillas en dos vistas, Taquillas y Zonas, accesibles por pestañas, con Taquillas como vista inicial y ambas con el patrón común de lista, filtros y detalle.

#### Scenario: Abrir la sección
- **WHEN** el usuario abre Taquillas
- **THEN** ve la vista de taquillas y puede cambiar a Zonas con el ratón o el teclado

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
