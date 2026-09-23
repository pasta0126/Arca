## Purpose

Gestionar la fianza como un depósito único durante toda la estancia del alumno en el centro, que se mantiene aunque cada curso se entregue y devuelva la llave y que solo se devuelve cuando el alumno causa baja.

## ADDED Requirements

### Requirement: Una fianza vigente por alumno
El sistema SHALL mantener como máximo una fianza vigente por alumno, entendiendo por vigente la que está pendiente, pagada, exenta o condonada y no devuelta ni anulada.

#### Scenario: Primera asignación sin fianza
- **WHEN** se abre la primera asignación de un alumno que no tiene fianza vigente
- **THEN** se genera un cargo de fianza pendiente con el importe del curso

#### Scenario: Alumno con fianza vigente
- **WHEN** se abre una asignación de un alumno que ya tiene una fianza vigente, en cualquier curso
- **THEN** no se genera una fianza nueva

#### Scenario: Fianza exenta
- **WHEN** el alumno tiene la fianza exenta y se abre una asignación en un curso posterior
- **THEN** no se genera una fianza nueva porque la exenta sigue vigente

### Requirement: La llave anual no afecta a la fianza
El sistema SHALL mantener la fianza sin cambios cuando se libera una taquilla, se cierra un curso o se devuelve la llave.

#### Scenario: Liberación de la taquilla
- **WHEN** se libera la taquilla de un alumno con la fianza pagada
- **THEN** la fianza sigue pagada y no pasa a devolución

#### Scenario: Nuevo curso
- **WHEN** un alumno con la fianza pagada recibe taquilla en el curso siguiente
- **THEN** conserva su fianza pagada y no se le cobra otra

### Requirement: Baja del alumno y fianza
El sistema SHALL tratar la fianza al causar baja el alumno según su estado: la fianza pagada pasa a por devolver, la pendiente se anula automáticamente y la exenta o condonada no requiere ninguna acción.

#### Scenario: Fianza pagada
- **WHEN** un alumno con la fianza pagada causa baja
- **THEN** la fianza queda por devolver

#### Scenario: Fianza pendiente
- **WHEN** un alumno con la fianza pendiente causa baja
- **THEN** la fianza queda anulada automáticamente con un motivo de baja del alumno

#### Scenario: Fianza exenta o condonada
- **WHEN** un alumno con la fianza exenta o condonada causa baja
- **THEN** no hay nada que devolver y la fianza queda sin cambios

#### Scenario: Cuota pendiente
- **WHEN** un alumno con la cuota pendiente causa baja
- **THEN** la cuota sigue pendiente

#### Scenario: Bajas masivas por importación
- **WHEN** una importación da de baja a muchos alumnos con la fianza pagada
- **THEN** todas sus fianzas quedan por devolver, sin marcarse como devueltas

### Requirement: Devolución de la fianza
El sistema SHALL permitir marcar como devuelta una fianza por devolver, con la fecha de devolución, que no puede ser futura, y una nota opcional de hasta 500 caracteres.

#### Scenario: Devolución correcta
- **WHEN** el usuario marca como devuelta una fianza por devolver indicando una nota
- **THEN** la fianza queda devuelta con su fecha y su nota y se registra en el historial

#### Scenario: Fecha futura
- **WHEN** la fecha de devolución es posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

#### Scenario: Alumno que sigue en el centro
- **WHEN** el usuario intenta devolver la fianza de un alumno activo
- **THEN** el sistema lo rechaza con un error que indica que solo se devuelve al causar baja

#### Scenario: Fianza no pagada
- **WHEN** el usuario intenta marcar como devuelta una fianza que no está por devolver
- **THEN** el sistema lo rechaza con un error de estado no válido

### Requirement: Devolución en bloque
El sistema SHALL permitir marcar como devueltas varias fianzas a la vez con una fecha y una nota comunes, en una operación indivisible y con confirmación explícita.

#### Scenario: Devolución de varias fianzas
- **WHEN** el usuario selecciona 40 fianzas por devolver, indica la fecha y confirma
- **THEN** las 40 quedan devueltas con esa fecha

#### Scenario: Confirmación con recuentos
- **WHEN** el usuario inicia la devolución en bloque
- **THEN** el sistema muestra cuántas fianzas y qué importe total se marcarán como devueltas y no actúa hasta la confirmación

#### Scenario: Fianza que dejó de estar por devolver
- **WHEN** entre la selección y la confirmación una de las fianzas deja de estar por devolver
- **THEN** el sistema no marca ninguna y muestra la selección actualizada

### Requirement: Lista de fianzas por devolver
El sistema SHALL ofrecer una lista de las fianzas por devolver, con el alumno, el importe y la fecha de baja, con filtros y totales.

#### Scenario: Listado por defecto
- **WHEN** el usuario abre la lista
- **THEN** ve las fianzas por devolver ordenadas por fecha de baja, con el importe total pendiente de devolución

#### Scenario: Exentas fuera de la lista
- **WHEN** un alumno con la fianza exenta causa baja
- **THEN** no aparece en la lista

#### Scenario: Sin fianzas por devolver
- **WHEN** no hay ninguna fianza por devolver
- **THEN** el sistema lo indica con un mensaje explicativo

### Requirement: Corrección de una devolución
El sistema SHALL permitir revertir una fianza devuelta a por devolver con un motivo, y SHALL impedir revertir a pendiente una fianza devuelta sin hacerlo antes.

#### Scenario: Devolución marcada por error
- **WHEN** el usuario revierte una fianza devuelta indicando un motivo
- **THEN** la fianza vuelve a por devolver y el historial conserva ambos eventos

#### Scenario: Revertir el pago de una devuelta
- **WHEN** el usuario intenta revertir a pendiente una fianza que está devuelta
- **THEN** el sistema lo rechaza y le indica que primero debe revertir la devolución

### Requirement: Reactivación del alumno y fianza
El sistema SHALL, al reactivar a un alumno, restablecer como pagada una fianza por devolver y permitir que se genere una nueva si la anterior estaba devuelta o anulada.

#### Scenario: Reactivado con la fianza aún por devolver
- **WHEN** se reactiva a un alumno cuya fianza estaba por devolver
- **THEN** la fianza vuelve a estar pagada y vigente

#### Scenario: Reactivado con la fianza devuelta
- **WHEN** se reactiva a un alumno cuya fianza fue devuelta y se le asigna una taquilla
- **THEN** se genera una fianza nueva

#### Scenario: Reactivado con la fianza anulada
- **WHEN** se reactiva a un alumno cuya fianza pendiente se anuló al causar baja y se le asigna una taquilla
- **THEN** se genera una fianza nueva

### Requirement: Alumno sin depósito real
El sistema SHALL representar un alumno sin depósito real mediante la fianza exenta con motivo obligatorio, y SHALL no ofrecer para ella devolución.

#### Scenario: Marcar la fianza exenta
- **WHEN** el usuario marca como exenta la fianza de un alumno con el motivo "Beca de comedor"
- **THEN** la fianza queda exenta y los informes la distinguen de una fianza cobrada

#### Scenario: Sin devolución
- **WHEN** el usuario consulta las acciones disponibles de una fianza exenta
- **THEN** no se ofrece marcarla como devuelta
