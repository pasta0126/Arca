## Purpose

Definir la interfaz de la sección Cobros en el hito 1: los cargos de un alumno con sus operaciones y la consulta de morosos, sobre las reglas de `pagaments`.

## ADDED Requirements

### Requirement: Organización de la sección Cobros
El sistema SHALL organizar la sección Cobros con la consulta de morosos como vista inicial y una búsqueda de alumno que abre sus cargos, y SHALL mostrar los cargos de un alumno también desde su ficha en Alumnos.

#### Scenario: Abrir la sección
- **WHEN** el usuario abre Cobros
- **THEN** ve los morosos con sus totales y un cuadro para buscar un alumno

#### Scenario: Desde la ficha
- **WHEN** el usuario abre la pestaña Cobros de la ficha de un alumno
- **THEN** ve los mismos cargos y acciones que en la sección Cobros

### Requirement: Consulta de morosos
El sistema SHALL mostrar los alumnos con cargos pendientes en una lista ordenada por apellidos con nombre, nivel, grupo, taquilla, importe pendiente y marca de baja, con filtros por curso, concepto, nivel, grupo y zona, y con el número de alumnos y el importe total según los filtros.

#### Scenario: Lista por defecto
- **WHEN** el usuario abre la consulta
- **THEN** ve los alumnos con deuda, sus importes y los totales

#### Scenario: Desglose
- **WHEN** el usuario selecciona un alumno
- **THEN** ve el desglose de su deuda por concepto, curso e importe

#### Scenario: Sin morosos
- **WHEN** no hay ningún cargo pendiente
- **THEN** se muestra un mensaje positivo en lugar de una lista vacía

#### Scenario: Alumno de baja con deuda
- **WHEN** un alumno de baja tiene cargos pendientes
- **THEN** aparece marcado como de baja

#### Scenario: Privacidad
- **WHEN** se muestra la lista o el desglose
- **THEN** no aparecen el correo ni el identificador

### Requirement: Nomenclatura visible de la deuda
El sistema SHALL usar en la interfaz «Pendents de pagament» y nunca «morosos», siguiendo `docs/glosario.md`.

#### Scenario: Título de la vista
- **WHEN** el usuario abre la consulta
- **THEN** el título y las etiquetas usan la expresión del glosario

### Requirement: Cargos de un alumno
El sistema SHALL mostrar los cargos de un alumno de cualquier curso en una lista con concepto, curso, importe, estado con texto, fecha y motivo cuando existan, ordenados por curso descendente, y con un resumen que indique si el alumno está al corriente o cuánto debe.

#### Scenario: Alumno al corriente
- **WHEN** ningún cargo del alumno está pendiente
- **THEN** el resumen indica que está al corriente

#### Scenario: Alumno sin cargos
- **WHEN** el alumno no tiene cargos
- **THEN** la vista explica que se generan al asignarle una taquilla

#### Scenario: Cargos de cursos anteriores
- **WHEN** el alumno tiene cargos de cursos anteriores
- **THEN** se muestran con su curso y se pueden gestionar igual que los del activo

### Requirement: Operaciones sobre un cargo
El sistema SHALL ofrecer sobre un cargo pendiente las acciones Marcar como pagado (con fecha por defecto hoy y no futura), Marcar exento, Condonar y Anular con motivo obligatorio cuando corresponda, y Cambiar importe con motivo; y sobre un cargo pagado, exento o condonado la acción Revertir a pendiente con motivo, con las no disponibles deshabilitadas y su motivo.

#### Scenario: Pagar
- **WHEN** el usuario elige Marcar como pagado en un cargo pendiente
- **THEN** un formulario pide la fecha, precargada con hoy, y al confirmar el cargo pasa a pagado

#### Scenario: Fecha futura
- **WHEN** el usuario indica una fecha futura
- **THEN** el campo se marca y no se puede confirmar

#### Scenario: Motivo obligatorio
- **WHEN** el usuario elige Exento o Condonar sin escribir motivo
- **THEN** el formulario no permite confirmar e indica que el motivo es obligatorio, con un máximo de 500 caracteres

#### Scenario: Revertir
- **WHEN** el usuario revierte un cargo pagado y escribe el motivo
- **THEN** el sistema pide confirmación indicando el efecto y, al confirmar, el cargo vuelve a pendiente

#### Scenario: Cargo anulado
- **WHEN** un cargo está anulado
- **THEN** no ofrece ninguna acción porque el estado es final

#### Scenario: Los pagos no se editan ni se borran
- **WHEN** el usuario mira las acciones de un cargo
- **THEN** no existe ninguna acción para editar ni borrar el cargo, solo para anularlo con motivo

### Requirement: Historial del cargo
El sistema SHALL mostrar en el detalle de un cargo su historial de solo lectura con cada cambio, su instante, el estado anterior y el nuevo, el motivo y los importes.

#### Scenario: Historial visible
- **WHEN** el usuario abre el detalle de un cargo con varios cambios
- **THEN** ve todos en orden cronológico inverso y sin poder modificarlos

### Requirement: Reposición de llave bajo demanda
El sistema SHALL ofrecer en los cargos de un alumno la acción Cobrar reposición de llave, con el importe del curso activo, que crea el cargo solo tras la confirmación del usuario.

#### Scenario: Reposición
- **WHEN** el usuario elige Cobrar reposición y confirma
- **THEN** se crea un cargo pendiente con el importe de la reposición del curso activo

#### Scenario: Importe sin definir
- **WHEN** el curso activo no tiene importe de reposición
- **THEN** el sistema avisa de que hay que definir los importes y ofrece ir a la sección Curso

### Requirement: Aviso de deuda al asignar
El sistema SHALL presentar, al asignar una taquilla a un alumno con deuda de cursos anteriores, el aviso con el desglose de la deuda y exigir una confirmación explícita antes de asignar.

#### Scenario: Confirmar la asignación con deuda
- **WHEN** el usuario asigna una taquilla a un alumno con deuda anterior
- **THEN** el diálogo muestra la deuda por concepto y curso, y solo asigna si el usuario confirma

#### Scenario: Rechazar el aviso
- **WHEN** el usuario cancela el diálogo
- **THEN** la asignación no se realiza

### Requirement: Feedback y doble ejecución en Cobros
El sistema SHALL confirmar cada operación con una notificación que indique el cargo y el importe afectados, mostrar los errores de forma comprensible y persistente y proteger de la doble ejecución todas las operaciones sobre cargos.

#### Scenario: Doble clic en confirmar
- **WHEN** el usuario hace doble clic en Confirmar al marcar un pago
- **THEN** se registra un solo pago y una sola notificación
