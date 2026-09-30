## MODIFIED Requirements

### Requirement: Lista de alumnos con búsqueda y filtros
El sistema SHALL mostrar en la sección Alumnos una lista virtualizada de los alumnos activos del curso activo, ordenada por apellidos, con columnas de apellidos y nombre, nivel, grupo, taquilla y estado de pago, con búsqueda por texto sin distinguir mayúsculas ni acentos y filtros por nivel, grupo, estado de asignación y pagos pendientes, con opción de incluir las bajas, y con el recuento de alumnos según los filtros. El estado de pago de cada fila SHALL indicar si el alumno está al corriente o tiene pendientes, sin mostrar importes. La lista nunca SHALL mostrar importes globales, saldos ni totales de deuda.

#### Scenario: Lista por defecto
- **WHEN** el usuario abre Alumnos con un curso activo
- **THEN** ve los alumnos activos por apellidos con el recuento total

#### Scenario: Búsqueda
- **WHEN** el usuario escribe "garcia"
- **THEN** la lista se limita a los alumnos con "García" en el nombre o los apellidos

#### Scenario: Alumnos sin taquilla
- **WHEN** el usuario filtra por sin taquilla
- **THEN** ve solo los alumnos activos del curso activo sin asignación y su recuento

#### Scenario: Alumnos con pendientes de pago
- **WHEN** el usuario filtra por pendientes de pago
- **THEN** ve solo los alumnos con algún cargo pendiente de cualquier curso, con su recuento de alumnos y sin ningún importe total

#### Scenario: Filtros combinados
- **WHEN** el usuario filtra por 2.º de ESO, sin taquilla y con pendientes
- **THEN** la lista cumple los tres criterios a la vez y el recuento coincide

#### Scenario: Búsqueda por taquilla
- **WHEN** el usuario escribe el número de una taquilla ocupada
- **THEN** ve al alumno que la tiene

#### Scenario: Incluir bajas
- **WHEN** el usuario activa incluir bajas
- **THEN** los alumnos de baja aparecen diferenciados con texto y no solo con color

#### Scenario: Sin resultados
- **WHEN** ningún alumno cumple los criterios
- **THEN** la lista lo indica y ofrece limpiar los filtros

#### Scenario: Sin alumnos
- **WHEN** no hay ningún alumno
- **THEN** se muestra un estado vacío que ofrece dar de alta un alumno y avisa de que la importación desde secretaría llegará más adelante

#### Scenario: Privacidad
- **WHEN** se muestra la lista
- **THEN** no aparecen el correo ni el identificador

#### Scenario: Nadie con pendientes
- **WHEN** el usuario filtra por pendientes de pago y ningún alumno los tiene
- **THEN** la lista muestra un mensaje positivo en lugar de una lista vacía sin explicación

## ADDED Requirements

### Requirement: Ficha del alumno en una columna
El sistema SHALL mostrar al seleccionar un alumno su ficha en una sola columna, sin pestañas. La cabecera SHALL mostrar el nombre, el nivel y grupo, la taquilla y el estado de pago, siempre visibles, y las acciones aplicables. Debajo, los cargos pendientes del alumno SHALL mostrarse abiertos, cada uno con su concepto, curso, importe y las acciones de cobro en su misma fila, o un mensaje de que está al corriente. El historial de pagos, los datos del alumno y el historial de actividad SHALL ser bloques colapsables, cerrados por defecto. El historial de pagos usa los mismos cargos que ya se cargaron para los pendientes y muestra su recuento cerrado; el historial de actividad carga su contenido solo al abrirse. La ficha SHALL recordar qué bloques están abiertos al cambiar de alumno y entre sesiones.

#### Scenario: Cabecera de la ficha
- **WHEN** el usuario abre la ficha de un alumno con taquilla y deuda
- **THEN** la cabecera muestra su taquilla y su estado de pago con la deuda

#### Scenario: Alumno con pendientes
- **WHEN** el usuario abre la ficha de un alumno con dos cargos pendientes
- **THEN** ve los dos cargos abiertos, cada uno con su importe y su acción de Marcar como pagado, sin tener que cambiar de pestaña

#### Scenario: Alumno al corriente
- **WHEN** el usuario abre la ficha de un alumno sin cargos pendientes
- **THEN** la ficha indica que está al corriente y no muestra la lista de pendientes

#### Scenario: Alumno sin cargos
- **WHEN** el alumno no tiene ningún cargo
- **THEN** la ficha explica que los cargos se generan al asignarle una taquilla

#### Scenario: Historial de pagos colapsable
- **WHEN** el usuario abre el bloque Historial de pagos
- **THEN** ve todos los cargos del alumno de cualquier curso, por curso descendente, con concepto, curso, importe, estado con texto, fecha y motivo cuando existan, con las mismas acciones que los pendientes cuando correspondan

#### Scenario: Bloques cerrados
- **WHEN** el usuario selecciona un alumno
- **THEN** el historial de pagos, los datos y el historial de actividad aparecen cerrados, el primero con su recuento de cargos, y el historial de actividad no se carga hasta abrirlo

#### Scenario: Datos
- **WHEN** el usuario abre el bloque Datos
- **THEN** ve nombre, apellidos, nivel y grupo del curso activo, y el correo, que solo se muestra aquí

#### Scenario: Historial de actividad
- **WHEN** el usuario abre el bloque Historial de actividad
- **THEN** ve las altas, cambios de datos, de matrícula, bajas, reactivaciones y cambios de asignación con sus valores anterior y nuevo, del más reciente al más antiguo

#### Scenario: Cobrar desde la ficha
- **WHEN** el usuario marca como pagado un cargo pendiente desde la ficha y confirma
- **THEN** el cargo pasa a pagado, la cabecera y la lista actualizan el estado de pago del alumno y aparece una notificación con el cargo y el importe

#### Scenario: Cambiar de alumno
- **WHEN** el usuario selecciona otro alumno con un bloque abierto
- **THEN** la ficha muestra al nuevo alumno con ese bloque abierto y su contenido cargado para él

## REMOVED Requirements

### Requirement: Ficha del alumno
**Reason**: Las pestañas escondían los cargos y obligaban a cambiar de vista para cobrar; la ficha pasa a una sola columna.
**Migration**: `Ficha del alumno en una columna`, con los cargos pendientes abiertos y el historial de pagos, los datos y el historial de actividad en bloques colapsables.

