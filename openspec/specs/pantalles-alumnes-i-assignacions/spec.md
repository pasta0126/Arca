# pantalles-alumnes-i-assignacions Specification

## Purpose
Definir la interfaz de la sección Alumnos en el hito 1: lista con búsqueda y filtros, alta manual, ficha del alumno, baja y reactivación, y el flujo de asignar, cambiar y liberar taquilla, sobre las reglas de `alumnes-i-assignacions`.

## Requirements

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

### Requirement: Alumnos sin curso activo
El sistema SHALL indicar en la sección Alumnos, cuando no hay curso activo, que hay que activar un curso, ofrecer ir a la sección Curso y deshabilitar el alta y la asignación con ese motivo, sin ocultar la consulta de los alumnos existentes.

#### Scenario: Sin curso activo
- **WHEN** el usuario abre Alumnos y no hay curso activo
- **THEN** ve el aviso con la acción de ir a Curso y las altas y asignaciones deshabilitadas con su motivo

### Requirement: Alta manual de un alumno
El sistema SHALL ofrecer la acción Nuevo alumno con un formulario de nombre y apellidos obligatorios, nivel y grupo tomados del catálogo con la posibilidad de escribir un valor nuevo, y correo obligatorio, que crea la ficha y la matrícula del curso activo y valida al guardar.

#### Scenario: Alta correcta
- **WHEN** el usuario guarda un alumno con nombre, apellidos, nivel y grupo
- **THEN** se crea la ficha con su matrícula del curso activo, aparece en la lista y se ofrece asignarle una taquilla

#### Scenario: Datos obligatorios
- **WHEN** falta el nombre o los apellidos, o superan los 100 caracteres
- **THEN** el campo se marca con el error correspondiente y no se pierde lo escrito

#### Scenario: Valor nuevo de nivel o grupo
- **WHEN** el usuario escribe un grupo que no está en el catálogo
- **THEN** el formulario lo acepta y lo señala como nuevo, y al guardar pasa a formar parte del catálogo

#### Scenario: Posible duplicado
- **WHEN** ya existe un alumno con el mismo nombre y apellidos
- **THEN** el formulario avisa, muestra al alumno existente y pide confirmar que se trata de otra persona

#### Scenario: Correo obligatorio y único
- **WHEN** el usuario deja vacío el correo, lo escribe mal formado o ya pertenece a otro alumno
- **THEN** el campo se marca con el error correspondiente sin perder lo escrito, y el formulario indica que el correo identifica al alumno y que nunca se muestra en listados

### Requirement: Editar los datos del alumno
El sistema SHALL permitir editar el nombre, los apellidos, el correo, el nivel y el grupo desde la pestaña Datos, con validación al guardar y sin cerrar el formulario si hay errores.

#### Scenario: Corregir un apellido
- **WHEN** el usuario corrige un apellido y guarda
- **THEN** el cambio se aplica, se anota en el historial y se notifica

#### Scenario: Cambio de grupo
- **WHEN** el usuario cambia el grupo de la matrícula del curso activo
- **THEN** la ficha y la lista muestran el nuevo grupo

### Requirement: Dar de baja y reactivar
El sistema SHALL ofrecer dar de baja a un alumno con motivo obligatorio y una confirmación que indique que su taquilla quedará libre, y SHALL ofrecer reactivar a un alumno de baja en el curso activo.

#### Scenario: Baja con taquilla
- **WHEN** el usuario da de baja a un alumno con taquilla
- **THEN** el diálogo indica qué taquilla se liberará, pide el motivo y, al confirmar, la taquilla queda libre y el alumno aparece de baja

#### Scenario: Baja sin motivo
- **WHEN** el usuario intenta confirmar sin motivo
- **THEN** el diálogo no lo permite y lo indica

#### Scenario: Reactivar
- **WHEN** el usuario reactiva a un alumno de baja
- **THEN** vuelve a la lista de activos con su misma ficha y se notifica

### Requirement: Asignar una taquilla desde el alumno
El sistema SHALL ofrecer en la ficha de un alumno activo sin taquilla la acción Asignar taquilla, que abre un selector de zona con la taquilla libre de número más bajo sugerida y la lista de las libres, o reservadas para ese alumno, de la zona elegida.

#### Scenario: Sugerencia
- **WHEN** el usuario elige la zona Planta 1 que tiene libres las taquillas 5 y 9
- **THEN** el selector propone la 5 y permite elegir otra libre

#### Scenario: Zona sin libres
- **WHEN** la zona elegida no tiene taquillas libres
- **THEN** el selector lo indica y propone la siguiente zona con libres

#### Scenario: Sin taquillas libres
- **WHEN** no queda ninguna taquilla libre
- **THEN** el selector lo explica y no ofrece confirmar

#### Scenario: Confirmar
- **WHEN** el usuario confirma la taquilla elegida
- **THEN** la asignación se realiza, la ficha muestra la taquilla y se notifica

### Requirement: Asignar desde la taquilla y arrastrando
El sistema SHALL ofrecer asignar desde el detalle de una taquilla libre, con un selector de alumnos activos sin taquilla con búsqueda, y arrastrando un alumno sobre una taquilla libre en Inicio, con las mismas validaciones, avisos y resultado que desde el alumno.

#### Scenario: Desde la taquilla
- **WHEN** el usuario elige Asignar en una taquilla libre y busca a un alumno sin taquilla
- **THEN** puede elegirlo y confirmar con el mismo resultado que desde el alumno

#### Scenario: Arrastrando
- **WHEN** el usuario arrastra un alumno sobre una taquilla libre
- **THEN** se aplican las mismas validaciones y avisos y se notifica el resultado

### Requirement: Avisos e impedimentos al asignar
El sistema SHALL mostrar antes de asignar los avisos que exigen confirmación, con su texto y desglose, y SHALL, cuando exista un impedimento, explicar el motivo y no permitir confirmar.

#### Scenario: Aviso de deuda
- **WHEN** el alumno tiene deuda de cursos anteriores
- **THEN** el diálogo muestra el aviso con el desglose y solo asigna si el usuario lo confirma explícitamente

#### Scenario: Impedimento
- **WHEN** existe un impedimento
- **THEN** el diálogo muestra el motivo y no ofrece confirmar

### Requirement: Cambiar de taquilla y liberar
El sistema SHALL ofrecer en la ficha de un alumno con taquilla las acciones Cambiar de taquilla, con el mismo selector de asignar, y Liberar taquilla, con motivo opcional y una confirmación que indique que el alumno quedará sin taquilla, conservando el historial.

#### Scenario: Cambiar
- **WHEN** el usuario elige otra taquilla libre y confirma
- **THEN** el alumno pasa a la nueva, la anterior queda libre y el pago le sigue sin generar cargos nuevos

#### Scenario: Liberar
- **WHEN** el usuario libera la taquilla de un alumno y confirma
- **THEN** la taquilla queda libre, el alumno aparece sin taquilla y se notifica

### Requirement: Feedback y doble ejecución en Alumnos
El sistema SHALL confirmar cada operación con una notificación que indique alumno y taquilla, mostrar los errores de forma comprensible y persistente y proteger de la doble ejecución todas las altas, ediciones, bajas y asignaciones.

#### Scenario: Doble clic en asignar
- **WHEN** el usuario hace doble clic en Confirmar la asignación
- **THEN** se realiza una sola asignación y una sola notificación

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
