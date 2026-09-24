## Purpose

Definir la interfaz de la sección Alumnos en el hito 1: lista con búsqueda y filtros, alta manual, ficha del alumno, baja y reactivación, y el flujo de asignar, cambiar y liberar taquilla, sobre las reglas de `alumnes-i-assignacions`.

## ADDED Requirements

### Requirement: Lista de alumnos con búsqueda y filtros
El sistema SHALL mostrar en la sección Alumnos una lista virtualizada de los alumnos activos del curso activo, ordenada por apellidos, con columnas de apellidos y nombre, nivel, grupo, taquilla y estado de pago, con búsqueda por texto sin distinguir mayúsculas ni acentos y filtros por nivel, grupo y estado de asignación, y con opción de incluir las bajas.

#### Scenario: Lista por defecto
- **WHEN** el usuario abre Alumnos con un curso activo
- **THEN** ve los alumnos activos por apellidos con el recuento total

#### Scenario: Búsqueda
- **WHEN** el usuario escribe "garcia"
- **THEN** la lista se limita a los alumnos con "García" en el nombre o los apellidos

#### Scenario: Alumnos sin taquilla
- **WHEN** el usuario filtra por sin taquilla
- **THEN** ve solo los alumnos activos del curso activo sin asignación y su recuento

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

### Requirement: Alumnos sin curso activo
El sistema SHALL indicar en la sección Alumnos, cuando no hay curso activo, que hay que activar un curso, ofrecer ir a la sección Curso y deshabilitar el alta y la asignación con ese motivo, sin ocultar la consulta de los alumnos existentes.

#### Scenario: Sin curso activo
- **WHEN** el usuario abre Alumnos y no hay curso activo
- **THEN** ve el aviso con la acción de ir a Curso y las altas y asignaciones deshabilitadas con su motivo

### Requirement: Alta manual de un alumno
El sistema SHALL ofrecer la acción Nuevo alumno con un formulario de nombre y apellidos obligatorios, nivel y grupo tomados del catálogo con la posibilidad de escribir un valor nuevo, y correo e identificador opcionales, que crea la ficha y la matrícula del curso activo y valida al guardar.

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

#### Scenario: Datos de reconocimiento opcionales
- **WHEN** el usuario deja vacíos el correo y el identificador
- **THEN** el alta se completa sin ellos y el formulario indica para qué sirven y que nunca se muestran en listados

### Requirement: Ficha del alumno
El sistema SHALL mostrar al seleccionar un alumno su ficha con pestañas Datos, Taquilla, Cobros e Historial, con el estado de asignación y de pago siempre visibles en la cabecera de la ficha.

#### Scenario: Cabecera de la ficha
- **WHEN** el usuario abre la ficha de un alumno con taquilla y deuda
- **THEN** la cabecera muestra su taquilla y su estado de pago con la deuda

#### Scenario: Pestaña Datos
- **WHEN** el usuario abre Datos
- **THEN** ve nombre, apellidos, nivel y grupo del curso activo, y el correo y el identificador solo aquí y solo si existen

#### Scenario: Pestaña Historial
- **WHEN** el usuario abre Historial
- **THEN** ve las altas, cambios de datos, de matrícula, bajas, reactivaciones y cambios de asignación con sus valores anterior y nuevo, del más reciente al más antiguo

#### Scenario: Pestaña Cobros
- **WHEN** el usuario abre Cobros
- **THEN** ve los cargos del alumno con las mismas acciones que en la sección Cobros

### Requirement: Editar los datos del alumno
El sistema SHALL permitir editar el nombre, los apellidos, el correo, el identificador, el nivel y el grupo desde la pestaña Datos, con validación al guardar y sin cerrar el formulario si hay errores.

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
