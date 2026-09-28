## Purpose

Registrar por alumno el estado de cada concepto de cobro (pendiente, pagado, exento, condonado o anulado), generarlo automáticamente al asignar una taquilla y determinar quién está al corriente de pago, incluida la deuda arrastrada de cursos anteriores.

## ADDED Requirements

### Requirement: Cargo por alumno y concepto
El sistema SHALL representar cada obligación de cobro como un cargo de un alumno, con su concepto, el curso en que se generó, un importe fijado en el momento de crearlo y un estado.

#### Scenario: Importe fijado al crear
- **WHEN** se genera un cargo de cuota con el importe de 50,00 del curso
- **THEN** el cargo conserva 50,00 aunque el importe del curso cambie después

### Requirement: Estados de un cargo
El sistema SHALL asignar a cada cargo uno de estos estados: pendiente, pagado, exento, condonado o anulado, y SHALL crearlo en estado pendiente.

#### Scenario: Cargo nuevo
- **WHEN** se genera un cargo
- **THEN** su estado es pendiente

### Requirement: Marcar un cargo como pagado
El sistema SHALL permitir marcar como pagado un cargo pendiente indicando la fecha de pago, que por defecto es la de hoy y no puede ser futura.

#### Scenario: Pago correcto
- **WHEN** el usuario marca como pagado un cargo pendiente sin indicar fecha
- **THEN** el cargo queda pagado con la fecha de hoy y se registra el cambio en su historial

#### Scenario: Fecha futura
- **WHEN** el usuario indica una fecha de pago posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

#### Scenario: Fecha pasada
- **WHEN** el usuario indica una fecha de pago anterior a hoy
- **THEN** el sistema la acepta

#### Scenario: Cargo que no está pendiente
- **WHEN** el usuario intenta marcar como pagado un cargo que no está pendiente
- **THEN** el sistema lo rechaza con un error de estado no válido

### Requirement: Exento y condonado con motivo obligatorio
El sistema SHALL permitir marcar un cargo pendiente como exento o como condonado, exigiendo en ambos casos un motivo de entre 1 y 500 caracteres.

#### Scenario: Exención por beca
- **WHEN** el usuario marca como exento un cargo pendiente indicando el motivo "Beca"
- **THEN** el cargo queda exento con ese motivo y deja de contar como deuda

#### Scenario: Condonación
- **WHEN** el usuario condona un cargo pendiente indicando un motivo
- **THEN** el cargo queda condonado con ese motivo y deja de contar como deuda

#### Scenario: Motivo vacío
- **WHEN** el usuario intenta marcar como exento o condonado sin motivo o con solo espacios
- **THEN** el sistema lo rechaza con un error de motivo obligatorio

#### Scenario: Motivo demasiado largo
- **WHEN** el motivo supera los 500 caracteres
- **THEN** el sistema lo rechaza con un error de longitud máxima

### Requirement: Revertir a pendiente
El sistema SHALL permitir revertir un cargo pagado, exento o condonado al estado pendiente, exigiendo un motivo y conservando todo lo ocurrido en el historial.

#### Scenario: Corregir un pago erróneo
- **WHEN** el usuario revierte un cargo pagado indicando un motivo
- **THEN** el cargo vuelve a pendiente y el historial conserva el pago, la reversión y el motivo

#### Scenario: Revertir sin motivo
- **WHEN** el usuario intenta revertir sin motivo
- **THEN** el sistema lo rechaza con un error de motivo obligatorio

#### Scenario: Cargo anulado
- **WHEN** el usuario intenta revertir un cargo anulado
- **THEN** el sistema lo rechaza con un error de estado no válido

#### Scenario: Confirmación
- **WHEN** el usuario inicia la reversión de un cargo pagado
- **THEN** el sistema pide confirmación indicando que el cargo volverá a contar como deuda

### Requirement: Anular un cargo
El sistema SHALL permitir anular un cargo pendiente con un motivo, dejándolo como estado final que no cuenta como deuda ni se puede revertir.

#### Scenario: Cargo generado por error
- **WHEN** el usuario anula un cargo pendiente indicando un motivo
- **THEN** el cargo queda anulado y no cuenta como deuda

#### Scenario: Anular un cargo pagado
- **WHEN** el usuario intenta anular un cargo pagado
- **THEN** el sistema lo rechaza y le indica que primero debe revertirlo a pendiente

### Requirement: Ajuste del importe de un cargo pendiente
El sistema SHALL permitir cambiar el importe de un cargo pendiente con un motivo, respetando los límites de importe, y SHALL no permitirlo en cargos que no estén pendientes.

#### Scenario: Ajuste correcto
- **WHEN** el usuario cambia el importe de un cargo pendiente indicando un motivo
- **THEN** el cargo tiene el nuevo importe y el historial conserva el anterior

#### Scenario: Cargo pagado
- **WHEN** el usuario intenta cambiar el importe de un cargo pagado
- **THEN** el sistema lo rechaza con un error de estado no válido

### Requirement: Historial del cargo de solo añadir
El sistema SHALL registrar cada cambio de un cargo con su instante, el estado anterior y el nuevo, el motivo y los importes cuando cambien, y SHALL no permitir modificar ni borrar el historial.

#### Scenario: Consulta del historial
- **WHEN** el usuario consulta el historial de un cargo
- **THEN** ve todos sus cambios ordenados del más reciente al más antiguo, con el texto en el idioma activo

#### Scenario: Historial inmutable
- **WHEN** se intenta modificar o borrar un evento del historial
- **THEN** el sistema no ofrece esa operación

### Requirement: Generación de la cuota al abrir la primera asignación del curso
El sistema SHALL generar la cuota del curso de un alumno cuando se abre su primera asignación en ese curso, y SHALL no generarla dos veces para el mismo alumno y curso.

#### Scenario: Primera asignación del curso
- **WHEN** se asigna una taquilla a un alumno que no tiene cuota en el curso activo
- **THEN** se genera un cargo de cuota pendiente con el importe del curso

#### Scenario: Cambio de taquilla
- **WHEN** el alumno cambia de taquilla dentro del mismo curso
- **THEN** no se genera una cuota nueva

#### Scenario: Liberar y volver a asignar
- **WHEN** se libera la taquilla de un alumno y se le asigna otra en el mismo curso
- **THEN** no se genera una cuota nueva y se mantiene la existente

#### Scenario: Alumno que llega a mitad de curso
- **WHEN** se asigna una taquilla a un alumno a mitad de curso
- **THEN** se genera la cuota completa, sin prorrateo

#### Scenario: Alumno que se va antes de terminar
- **WHEN** se libera la taquilla de un alumno a mitad de curso con la cuota pendiente
- **THEN** la cuota sigue pendiente, salvo que el usuario la marque exenta o condonada

#### Scenario: Importes sin definir
- **WHEN** no están definidos los importes del curso
- **THEN** la asignación no se realiza y se avisa de que hay que definirlos primero

### Requirement: Reposición de llave a demanda
El sistema SHALL generar un cargo de reposición de llave solo cuando el usuario decide cobrarla, con el importe del curso activo.

#### Scenario: Llave perdida con cobro
- **WHEN** el usuario decide cobrar la reposición de la llave de un alumno
- **THEN** se genera un cargo de reposición pendiente con el importe del curso activo

#### Scenario: Llave perdida sin cobro
- **WHEN** el usuario decide no cobrar la reposición
- **THEN** no se genera ningún cargo

#### Scenario: Varias reposiciones
- **WHEN** el usuario cobra una segunda reposición al mismo alumno en el mismo curso
- **THEN** se genera un cargo distinto

### Requirement: Gestión de cargos de cursos anteriores
El sistema SHALL permitir gestionar los cargos de cualquier curso, incluidos los de cursos que ya no están activos.

#### Scenario: Pagar una deuda antigua
- **WHEN** el usuario marca como pagado un cargo pendiente de un curso anterior
- **THEN** el cargo queda pagado con su fecha

### Requirement: Deuda arrastrada y aviso al asignar
El sistema SHALL mantener como pendiente la deuda de cursos anteriores hasta que se pague, se exima, se condone o se anule, y SHALL avisar al asignar una taquilla a un alumno con deuda de cursos anteriores, exigiendo confirmación explícita.

#### Scenario: Aviso al asignar
- **WHEN** se asigna una taquilla a un alumno con cargos pendientes de cursos anteriores
- **THEN** el sistema avisa indicando los conceptos, los cursos y el importe total, y no asigna hasta que el usuario lo confirme

#### Scenario: Aviso rechazado
- **WHEN** el usuario rechaza el aviso
- **THEN** no se crea la asignación

#### Scenario: Sin deuda anterior
- **WHEN** el alumno solo tiene cargos pendientes del curso activo o ninguno
- **THEN** no aparece ningún aviso

#### Scenario: Deuda saldada
- **WHEN** todos los cargos de cursos anteriores están pagados, exentos, condonados o anulados
- **THEN** no aparece ningún aviso

### Requirement: Condonación en bloque
El sistema SHALL permitir condonar varios cargos pendientes a la vez con un motivo común, en una operación indivisible y con confirmación explícita.

#### Scenario: Condonación de varios cargos
- **WHEN** el usuario selecciona 20 cargos pendientes, indica un motivo y confirma
- **THEN** los 20 cargos quedan condonados con ese motivo y cada uno conserva su historial

#### Scenario: Confirmación con recuentos
- **WHEN** el usuario inicia una condonación en bloque
- **THEN** el sistema muestra cuántos cargos y qué importe total se condonarán y no actúa hasta la confirmación

#### Scenario: Cargo que dejó de ser pendiente
- **WHEN** entre la selección y la confirmación uno de los cargos deja de estar pendiente
- **THEN** el sistema no condona ninguno y muestra la selección actualizada

#### Scenario: Motivo obligatorio
- **WHEN** el usuario intenta confirmar la condonación en bloque sin motivo
- **THEN** el sistema lo rechaza con un error de motivo obligatorio

### Requirement: Estado al corriente de pago de un alumno
El sistema SHALL considerar que un alumno está al corriente cuando ninguno de sus cargos, de cualquier curso, está pendiente, y SHALL desglosar la deuda cuando no lo está.

#### Scenario: Al corriente
- **WHEN** todos los cargos de un alumno están pagados, exentos, condonados o anulados
- **THEN** el alumno está al corriente

#### Scenario: Con deuda
- **WHEN** un alumno tiene cargos pendientes
- **THEN** su estado indica que tiene pendiente, con el importe total y el desglose por concepto y curso, distinguiendo el curso activo de los anteriores

#### Scenario: Alumno sin cargos
- **WHEN** un alumno no tiene ningún cargo
- **THEN** está al corriente

#### Scenario: Exento de todo
- **WHEN** todos los cargos de un alumno están exentos
- **THEN** el alumno está al corriente y su estado indica que es por exención

### Requirement: Estado de pago de una taquilla
El sistema SHALL mostrar el estado de pago de una taquilla ocupada como el de su alumno asignado.

#### Scenario: Taquilla con alumno moroso
- **WHEN** una taquilla está ocupada por un alumno con cargos pendientes
- **THEN** la taquilla muestra que no está al corriente

#### Scenario: Taquilla libre
- **WHEN** una taquilla está libre
- **THEN** no muestra estado de pago

### Requirement: Consulta de morosos
El sistema SHALL permitir consultar los alumnos con cargos pendientes con su desglose por concepto, curso y importe, con filtros por curso, concepto, nivel, grupo y zona.

#### Scenario: Listado por defecto
- **WHEN** el usuario abre la consulta de morosos
- **THEN** ve los alumnos con cargos pendientes ordenados por apellidos, con el importe pendiente total y el desglose

#### Scenario: Alumno de baja con deuda
- **WHEN** un alumno de baja tiene una cuota pendiente
- **THEN** aparece en la consulta marcado como de baja

#### Scenario: Filtro por concepto
- **WHEN** el usuario filtra por el concepto fianza
- **THEN** ve solo los alumnos con una fianza pendiente

#### Scenario: Totales
- **WHEN** el usuario consulta los morosos
- **THEN** ve el número de alumnos y el importe pendiente total, en general y según los filtros

#### Scenario: Sin morosos
- **WHEN** no hay ningún cargo pendiente
- **THEN** el sistema lo indica con un mensaje positivo en lugar de una lista vacía sin explicación

#### Scenario: Privacidad
- **WHEN** el usuario consulta o exporta los morosos
- **THEN** no aparecen el correo ni el identificador de los alumnos

### Requirement: Feedback y confirmaciones en los cobros
El sistema SHALL informar del resultado de cada operación con sus recuentos e importes y SHALL pedir confirmación en las operaciones de reversión y en bloque.

#### Scenario: Resultado
- **WHEN** el usuario completa una operación de cobro
- **THEN** el sistema confirma qué se ha hecho y sobre qué cargo o cuántos

#### Scenario: Sin cargos que mostrar
- **WHEN** un alumno no tiene cargos
- **THEN** la ficha lo indica y explica que se generan al asignarle una taquilla
