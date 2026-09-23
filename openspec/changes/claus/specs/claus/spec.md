## Purpose

Controlar la llave única de cada taquilla desde su entrega al alumno hasta su devolución, incluidas las pérdidas con reposición y copia, y garantizar que ninguna taquilla se asigna a otro alumno mientras su llave anterior no esté resuelta.

## ADDED Requirements

### Requirement: Estado de la llave por asignación
El sistema SHALL registrar para cada asignación el estado de su llave: pendiente de entrega, entregada, perdida, devuelta o repuesta.

#### Scenario: Asignación nueva
- **WHEN** se abre una asignación
- **THEN** su llave queda en estado pendiente de entrega, salvo que se entregue en ese mismo acto

#### Scenario: Consulta del estado
- **WHEN** el usuario consulta un alumno o una taquilla ocupada
- **THEN** ve el estado de la llave de su asignación vigente

### Requirement: Entrega al asignar
El sistema SHALL permitir entregar la llave en el mismo acto de la asignación, y SHALL ofrecerlo como opción por defecto.

#### Scenario: Entrega inmediata
- **WHEN** el usuario asigna una taquilla y mantiene la opción de entregar la llave
- **THEN** la asignación queda creada con la llave entregada en la fecha de hoy

#### Scenario: Entrega diferida
- **WHEN** el usuario asigna una taquilla y desmarca la entrega de la llave
- **THEN** la asignación queda creada con la llave pendiente de entrega

### Requirement: Entrega posterior
El sistema SHALL permitir marcar como entregada una llave pendiente de entrega o devuelta, con la fecha de entrega, que por defecto es hoy y no puede ser futura.

#### Scenario: Entrega posterior correcta
- **WHEN** el usuario entrega la llave de una asignación con la llave pendiente
- **THEN** la llave queda entregada con su fecha y se registra el evento

#### Scenario: Entregar de nuevo una llave devuelta
- **WHEN** el usuario entrega la llave de una asignación vigente cuya llave había sido devuelta
- **THEN** la llave queda entregada de nuevo y se registra el evento

#### Scenario: Llave ya entregada
- **WHEN** el usuario intenta entregar una llave que ya está entregada
- **THEN** el sistema lo rechaza con un error de estado no válido

#### Scenario: Fecha futura
- **WHEN** la fecha de entrega es posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

### Requirement: Devolución de una llave
El sistema SHALL permitir marcar como devuelta una llave entregada, con la fecha de devolución, que por defecto es hoy y no puede ser futura, incluso cuando la asignación ya está cerrada o el alumno está de baja.

#### Scenario: Devolución correcta
- **WHEN** el usuario marca como devuelta una llave entregada
- **THEN** la llave queda devuelta con su fecha y se registra el evento

#### Scenario: Devolución tras la liberación
- **WHEN** el usuario marca como devuelta la llave de una asignación ya cerrada
- **THEN** la llave queda devuelta y la taquilla pasa a tener su llave disponible

#### Scenario: Llave no entregada
- **WHEN** el usuario intenta devolver una llave que no está entregada
- **THEN** el sistema lo rechaza con un error de estado no válido

#### Scenario: Devolver una llave perdida que aparece
- **WHEN** el usuario marca como devuelta una llave que estaba perdida porque el alumno la ha encontrado
- **THEN** la llave queda devuelta y se registra el evento

#### Scenario: La devolución no toca la fianza
- **WHEN** el usuario devuelve una llave de un alumno con la fianza pagada
- **THEN** la fianza no cambia

### Requirement: Devolución masiva de llaves
El sistema SHALL permitir devolver varias llaves a la vez a partir de una selección que el usuario puede editar, en una operación indivisible, con una fecha común y con confirmación explícita.

#### Scenario: Devolución masiva al cerrar el curso
- **WHEN** el usuario abre la devolución masiva
- **THEN** el sistema propone todas las llaves entregadas seleccionadas y permite quitar las que no han vuelto

#### Scenario: Llaves excluidas
- **WHEN** el usuario excluye 5 llaves y confirma la devolución de las demás
- **THEN** las demás quedan devueltas y las 5 excluidas siguen entregadas

#### Scenario: Confirmación con recuentos
- **WHEN** el usuario inicia la devolución masiva
- **THEN** el sistema muestra cuántas llaves se marcarán como devueltas y no actúa hasta la confirmación

#### Scenario: Llave que cambió de estado
- **WHEN** entre la selección y la confirmación una de las llaves deja de estar entregada
- **THEN** el sistema no devuelve ninguna y muestra la selección actualizada

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error durante la aplicación
- **THEN** ninguna llave cambia de estado

#### Scenario: Progreso y cancelación
- **WHEN** la devolución masiva procesa muchas llaves
- **THEN** el sistema muestra el avance con recuentos y permite cancelar antes del guardado

#### Scenario: Alcance de la selección
- **WHEN** el usuario filtra por zona, nivel o grupo antes de devolver
- **THEN** la selección propuesta contiene solo las llaves que cumplen el filtro

### Requirement: Pérdida de una llave con copia
El sistema SHALL permitir registrar la pérdida de una llave entregada, decidiendo en el mismo acto si se cobra la reposición y si se entrega una copia, sin cambiar la taquilla ni la asignación.

#### Scenario: Pérdida con cobro y copia
- **WHEN** el usuario registra la pérdida decidiendo cobrar y entregar copia
- **THEN** se genera un cargo de reposición pendiente, la llave queda entregada de nuevo con la copia y se registran los eventos, y la taquilla sigue asignada al mismo alumno

#### Scenario: Pérdida sin cobro
- **WHEN** el usuario registra la pérdida decidiendo no cobrar
- **THEN** no se genera ningún cargo y el resto del proceso continúa

#### Scenario: Pérdida con la copia pendiente
- **WHEN** el usuario registra la pérdida sin entregar la copia todavía
- **THEN** la llave queda perdida y el alumno conserva su taquilla, y la copia puede entregarse después

#### Scenario: Llave no entregada
- **WHEN** el usuario intenta registrar la pérdida de una llave que no está entregada
- **THEN** el sistema lo rechaza con un error de estado no válido

#### Scenario: Importes sin definir
- **WHEN** el usuario decide cobrar la reposición y el curso no tiene importes definidos
- **THEN** el sistema no registra nada y avisa de que hay que definirlos

#### Scenario: Varias pérdidas
- **WHEN** el mismo alumno pierde la llave más de una vez en la misma asignación
- **THEN** cada pérdida y cada copia queda registrada y se puede cobrar cada reposición

### Requirement: Entrega de la copia
El sistema SHALL permitir entregar una copia cuando la llave está perdida, dejándola entregada y contabilizando las copias entregadas de la asignación.

#### Scenario: Copia entregada
- **WHEN** el usuario entrega la copia de una llave perdida
- **THEN** la llave queda entregada, se registra el evento y el recuento de copias de la asignación aumenta en uno

#### Scenario: Llave no perdida
- **WHEN** el usuario intenta entregar una copia de una llave que no está perdida
- **THEN** el sistema lo rechaza con un error de estado no válido

### Requirement: Decisión sobre la llave al liberar o cambiar de taquilla
El sistema SHALL exigir que se indique qué ocurre con la llave al liberar o cambiar de taquilla en una operación manual, con las opciones devuelta, pendiente de devolución o perdida, y SHALL dejarla pendiente de devolución en los procesos automáticos.

#### Scenario: Liberación con llave devuelta
- **WHEN** el usuario libera una taquilla e indica que la llave se devuelve
- **THEN** la asignación se cierra con la llave devuelta

#### Scenario: Liberación con llave pendiente
- **WHEN** el usuario libera una taquilla e indica que la llave queda pendiente de devolución
- **THEN** la asignación se cierra con la llave entregada y la taquilla queda sin llave disponible

#### Scenario: Liberación con llave perdida
- **WHEN** el usuario libera una taquilla e indica que la llave está perdida
- **THEN** la asignación se cierra con la llave perdida y se ofrece decidir el cobro de la reposición

#### Scenario: Decisión ausente
- **WHEN** el usuario intenta liberar o cambiar una taquilla con la llave entregada sin indicar qué ocurre con ella
- **THEN** el sistema no actúa y pide la decisión

#### Scenario: Cambio de taquilla
- **WHEN** el usuario cambia a un alumno de taquilla e indica que devuelve la llave anterior
- **THEN** la llave anterior queda devuelta y la nueva asignación queda con la llave pendiente o entregada según se indique

#### Scenario: Procesos automáticos
- **WHEN** una baja por importación o una decisión de avería libera la taquilla de un alumno con la llave entregada
- **THEN** la llave queda entregada sin marcarse como devuelta y figura en la lista de llaves pendientes de devolución

#### Scenario: Llave sin entregar
- **WHEN** se cierra una asignación cuya llave estaba pendiente de entrega
- **THEN** no se pide decisión y la llave no consta como entregada

### Requirement: Llave disponible en la taquilla
El sistema SHALL considerar que la llave de una taquilla no está disponible cuando alguna asignación anterior suya tiene la llave entregada o perdida sin resolver, y SHALL impedir asignarla a otro alumno hasta que se resuelva.

#### Scenario: Taquilla sin llave disponible
- **WHEN** el usuario intenta asignar una taquilla cuya llave anterior no ha sido devuelta
- **THEN** el sistema rechaza la asignación con un error de llave no disponible que indica cómo resolverlo

#### Scenario: Llave devuelta
- **WHEN** se marca como devuelta la llave pendiente de una taquilla
- **THEN** la taquilla vuelve a poder asignarse

#### Scenario: Estado visible
- **WHEN** una taquilla libre no tiene la llave disponible
- **THEN** su estado visible sigue siendo libre y se muestra además un indicador de llave no disponible

#### Scenario: Asignación de un curso anterior
- **WHEN** la llave sin devolver pertenece a una asignación de un curso anterior
- **THEN** la taquilla sigue sin llave disponible

### Requirement: Regularización de una llave no devuelta
El sistema SHALL permitir marcar como repuesta una llave entregada o perdida de una asignación cerrada, con fecha y nota opcional de hasta 500 caracteres, cuando el centro dispone de una llave nueva para la taquilla.

#### Scenario: Llave repuesta
- **WHEN** el usuario marca como repuesta la llave perdida de una asignación cerrada
- **THEN** la llave queda repuesta, la taquilla vuelve a poder asignarse y se registra el evento con su nota

#### Scenario: Asignación vigente
- **WHEN** el usuario intenta marcar como repuesta la llave de una asignación vigente
- **THEN** el sistema lo rechaza y le indica que use la pérdida con copia

#### Scenario: Nota demasiado larga
- **WHEN** la nota supera los 500 caracteres
- **THEN** el sistema lo rechaza con un error de longitud máxima

### Requirement: Listas de llaves pendientes
El sistema SHALL ofrecer una lista de llaves entregadas de asignaciones cerradas o de alumnos de baja y otra de llaves perdidas sin resolver, con filtros y totales.

#### Scenario: Llaves pendientes de devolución
- **WHEN** el usuario abre la lista de llaves pendientes de devolución
- **THEN** ve por taquilla y alumno las llaves entregadas de asignaciones cerradas, con su curso, ordenadas por fecha de cierre, y el total

#### Scenario: Llaves perdidas sin resolver
- **WHEN** el usuario abre la lista de llaves perdidas
- **THEN** ve las que no tienen copia ni reposición, con la asignación y el alumno

#### Scenario: Filtros
- **WHEN** el usuario filtra por curso, zona, nivel o grupo
- **THEN** la lista muestra solo las llaves que cumplen el filtro

#### Scenario: Sin pendientes
- **WHEN** no hay llaves pendientes
- **THEN** el sistema lo indica con un mensaje explicativo

#### Scenario: Privacidad
- **WHEN** el usuario consulta o exporta las listas
- **THEN** no aparecen el correo ni el identificador de los alumnos

### Requirement: Historial de la llave
El sistema SHALL registrar cada entrega, devolución, pérdida, copia y reposición con su instante, fecha y estados anterior y nuevo en un historial de solo añadir, visible en la ficha del alumno y en la de la taquilla.

#### Scenario: Consulta del historial
- **WHEN** el usuario consulta el historial de una taquilla o de un alumno
- **THEN** ve los eventos de la llave junto a los demás, con el texto en el idioma activo

#### Scenario: Historial inmutable
- **WHEN** se intenta modificar o borrar un evento de la llave
- **THEN** el sistema no ofrece esa operación

### Requirement: Independencia de la fianza
El sistema SHALL no modificar la fianza como consecuencia de ninguna operación con la llave.

#### Scenario: Pérdida de llave
- **WHEN** se registra la pérdida de una llave de un alumno con la fianza pagada
- **THEN** la fianza no cambia

### Requirement: Feedback y guía en las operaciones con llaves
El sistema SHALL informar del resultado de cada operación con llaves, con recuentos en las masivas, y SHALL guiar al usuario cuando no hay llaves que gestionar.

#### Scenario: Resultado
- **WHEN** el usuario completa una entrega, devolución, pérdida o regularización
- **THEN** el sistema confirma qué se ha hecho y sobre qué taquilla y alumno

#### Scenario: Sin llaves entregadas
- **WHEN** no hay ninguna llave entregada y el usuario abre la devolución masiva
- **THEN** el sistema lo indica en lugar de mostrar una lista vacía sin explicación
