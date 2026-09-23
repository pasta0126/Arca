## Purpose

Asignar una taquilla a cada alumno dentro del curso activo, con reglas claras y el mismo resultado se inicie la acción desde el alumno, desde la taquilla o arrastrando; e integrar la ocupación real, la reserva con alumno y las decisiones de avería.

## ADDED Requirements

### Requirement: Asignación uno a uno en el curso activo
El sistema SHALL permitir asignar una taquilla a un alumno activo del curso activo, con como máximo una asignación vigente por alumno y una por taquilla.

#### Scenario: Asignación correcta
- **WHEN** el usuario asigna una taquilla libre a un alumno activo sin taquilla
- **THEN** la asignación queda vigente, la taquilla pasa a estar ocupada y se registra el evento en el historial del alumno y en el de la taquilla

#### Scenario: Alumno que ya tiene taquilla
- **WHEN** el usuario intenta asignar otra taquilla a un alumno que ya tiene una vigente
- **THEN** el sistema lo rechaza con un error que indica que ya tiene taquilla y sugiere cambiarla

#### Scenario: Taquilla ya ocupada
- **WHEN** el usuario intenta asignar una taquilla que ya tiene una asignación vigente
- **THEN** el sistema lo rechaza con un error de taquilla ocupada

#### Scenario: Alumno de baja
- **WHEN** el usuario intenta asignar una taquilla a un alumno de baja
- **THEN** el sistema lo rechaza con un error de alumno de baja

#### Scenario: Alumno sin matrícula del curso activo
- **WHEN** el usuario intenta asignar una taquilla a un alumno que no tiene matrícula en el curso activo
- **THEN** el sistema lo rechaza con un error de alumno no matriculado

### Requirement: Taquillas asignables
El sistema SHALL permitir asignar solo taquillas libres, o reservadas para ese mismo alumno.

#### Scenario: Taquilla averiada
- **WHEN** el usuario intenta asignar una taquilla averiada
- **THEN** el sistema lo rechaza con un error de taquilla no disponible

#### Scenario: Taquilla de baja
- **WHEN** el usuario intenta asignar una taquilla de baja
- **THEN** el sistema lo rechaza con un error de taquilla no disponible

#### Scenario: Taquilla reservada sin alumno
- **WHEN** el usuario intenta asignar una taquilla reservada sin alumno asociado
- **THEN** el sistema lo rechaza con un error que indica que hay que quitar la reserva antes

#### Scenario: Taquilla reservada para otro alumno
- **WHEN** el usuario intenta asignar una taquilla reservada para un alumno distinto
- **THEN** el sistema lo rechaza con un error de taquilla reservada para otro alumno

#### Scenario: Taquilla reservada para el mismo alumno
- **WHEN** el usuario asigna una taquilla reservada para ese alumno
- **THEN** la asignación queda vigente y la reserva se consume

### Requirement: Iniciar la asignación desde el alumno, la taquilla o arrastrando
El sistema SHALL permitir iniciar una asignación desde el alumno, desde la taquilla o arrastrando un alumno sobre una taquilla libre, y SHALL aplicar en los tres casos las mismas validaciones, confirmaciones y resultado.

#### Scenario: Desde el alumno
- **WHEN** el usuario elige un alumno y luego una taquilla libre
- **THEN** se crea la asignación con las validaciones habituales

#### Scenario: Desde la taquilla
- **WHEN** el usuario elige una taquilla libre y luego un alumno
- **THEN** se crea la misma asignación con las mismas validaciones

#### Scenario: Arrastrando
- **WHEN** el usuario arrastra un alumno sobre una taquilla libre
- **THEN** se crea la misma asignación con las mismas validaciones y confirmaciones

#### Scenario: Destino no válido al arrastrar
- **WHEN** el usuario suelta un alumno sobre una taquilla que no es asignable
- **THEN** el sistema no asigna nada y explica el motivo

#### Scenario: Alternativa sin arrastrar
- **WHEN** el usuario no puede o no quiere arrastrar
- **THEN** dispone de una acción equivalente accesible por menú y teclado

### Requirement: Sugerencia de taquilla libre
El sistema SHALL sugerir la taquilla libre de número más bajo de la zona elegida, y de la siguiente zona con taquillas libres si la elegida no tiene ninguna.

#### Scenario: Zona con taquillas libres
- **WHEN** el usuario elige la zona "Planta 1" que tiene libres las taquillas 12, 15 y 20
- **THEN** el sistema sugiere la 12

#### Scenario: Zona sin taquillas libres
- **WHEN** la zona elegida no tiene taquillas libres y otra sí
- **THEN** el sistema sugiere la libre de menor número de la siguiente zona con disponibilidad e indica la zona

#### Scenario: Sin taquillas libres
- **WHEN** no queda ninguna taquilla libre en todo el centro
- **THEN** el sistema informa de que no hay taquillas disponibles

### Requirement: Avisos que exigen confirmación
El sistema SHALL permitir que otras capacidades añadan avisos a una asignación, y SHALL exigir confirmación explícita del usuario antes de asignar cuando exista alguno.

#### Scenario: Asignación con aviso
- **WHEN** una asignación produce un aviso, como una deuda de cursos anteriores
- **THEN** el sistema muestra el aviso y no asigna hasta que el usuario lo confirme

#### Scenario: Aviso rechazado
- **WHEN** el usuario rechaza el aviso
- **THEN** no se crea la asignación

#### Scenario: Sin avisos
- **WHEN** la asignación no produce avisos
- **THEN** se realiza sin confirmación adicional

### Requirement: Impedimentos de otras capacidades
El sistema SHALL permitir que otras capacidades declaren impedimentos para una asignación, y SHALL rechazar la asignación cuando exista alguno, indicando el motivo.

#### Scenario: Asignación con impedimento
- **WHEN** una asignación produce un impedimento, como una taquilla sin llave disponible
- **THEN** el sistema no asigna, no ofrece confirmar y explica el motivo y cómo resolverlo

#### Scenario: Impedimento y aviso a la vez
- **WHEN** una asignación produce un impedimento y además un aviso
- **THEN** el sistema rechaza la asignación por el impedimento sin pedir confirmar el aviso

### Requirement: Cambio de taquilla
El sistema SHALL permitir cambiar a un alumno de taquilla en una única operación indivisible, liberando la anterior.

#### Scenario: Cambio correcto
- **WHEN** el usuario cambia a un alumno a otra taquilla asignable
- **THEN** la taquilla anterior queda libre, la nueva queda ocupada y se registran los eventos en el historial de ambas taquillas y del alumno

#### Scenario: Destino no asignable
- **WHEN** la taquilla de destino no es asignable
- **THEN** no cambia nada y la asignación original se mantiene

#### Scenario: Mismo destino
- **WHEN** el usuario elige como destino la taquilla que el alumno ya tiene
- **THEN** el sistema lo rechaza con un error de taquilla actual

#### Scenario: Cambio de curso activo
- **WHEN** el usuario cambia de taquilla a un alumno cuya asignación pertenece a un curso no activo
- **THEN** el sistema lo rechaza con un error de curso no activo

### Requirement: Liberación de una taquilla
El sistema SHALL permitir liberar la taquilla de un alumno, con un motivo opcional, dejando la taquilla libre y conservando la asignación en el historial.

#### Scenario: Liberación manual
- **WHEN** el usuario libera la taquilla de un alumno
- **THEN** la taquilla queda libre, la asignación queda cerrada con su fecha y motivo y el alumno queda sin taquilla

#### Scenario: Liberación de una taquilla averiada con alumno
- **WHEN** el usuario libera una taquilla averiada que conservaba a su alumno
- **THEN** la taquilla sigue averiada y el alumno queda sin taquilla

#### Scenario: Confirmación
- **WHEN** el usuario inicia la liberación
- **THEN** el sistema pide confirmación indicando que la taquilla quedará libre

### Requirement: Ocupación real de las taquillas
El sistema SHALL determinar que una taquilla está ocupada cuando tiene una asignación vigente, y SHALL proporcionar esa información al cálculo del estado visible de las taquillas.

#### Scenario: Estado ocupada
- **WHEN** una taquilla no averiada tiene una asignación vigente
- **THEN** su estado visible es ocupada

#### Scenario: Estado tras liberar
- **WHEN** se libera la única asignación vigente de una taquilla no averiada ni reservada
- **THEN** su estado visible es libre

### Requirement: Reserva para un alumno
El sistema SHALL permitir asociar a una reserva un alumno concreto, con una nota opcional, y convertirla en asignación cuando se formaliza.

#### Scenario: Reservar para un alumno
- **WHEN** el usuario reserva una taquilla libre indicando un alumno
- **THEN** la taquilla queda reservada para ese alumno y se registra el evento en ambos historiales

#### Scenario: Alumno con taquilla ya asignada
- **WHEN** el usuario intenta reservar una taquilla para un alumno que ya tiene una taquilla vigente
- **THEN** el sistema lo rechaza con un error que indica que ya tiene taquilla

#### Scenario: Alumno con otra reserva
- **WHEN** el usuario intenta reservar una taquilla para un alumno que ya tiene otra reserva
- **THEN** el sistema lo rechaza con un error que indica que ya tiene una reserva

#### Scenario: Baja del alumno reservado
- **WHEN** se da de baja a un alumno que tiene una taquilla reservada a su nombre
- **THEN** la reserva se quita y la taquilla queda libre

### Requirement: Decisión de reasignar o liberar al poner fuera de servicio una taquilla ocupada
El sistema SHALL completar las decisiones exigidas al marcar como averiada o en mantenimiento una taquilla ocupada: reasignar al alumno a otra taquilla libre, o liberarlo.

#### Scenario: Reasignar
- **WHEN** el usuario marca como averiada o en mantenimiento una taquilla ocupada decidiendo reasignar y elige una taquilla libre
- **THEN** en una sola operación la taquilla fuera de servicio queda sin alumno, el alumno pasa a la nueva taquilla y se registran los eventos con la decisión tomada

#### Scenario: Reasignar sin destino válido
- **WHEN** el usuario decide reasignar y la taquilla de destino no es asignable
- **THEN** no cambia nada, la taquilla no queda averiada y se informa del motivo

#### Scenario: Liberar
- **WHEN** el usuario marca como averiada o en mantenimiento una taquilla ocupada decidiendo liberar al alumno
- **THEN** la taquilla queda fuera de servicio sin asignación y el alumno queda sin taquilla

### Requirement: Historial de asignaciones
El sistema SHALL conservar todas las asignaciones, vigentes y cerradas, con sus fechas y motivo de cierre, y SHALL permitir consultarlas por alumno y por taquilla.

#### Scenario: Historial por alumno
- **WHEN** el usuario consulta las asignaciones de un alumno
- **THEN** ve todas las taquillas que ha tenido, con fechas y curso

#### Scenario: Historial por taquilla
- **WHEN** el usuario consulta las asignaciones de una taquilla
- **THEN** ve todos los alumnos que la han tenido, con fechas y curso

#### Scenario: Taquilla de baja con número reutilizado
- **WHEN** existen una taquilla de baja y otra activa con el mismo número
- **THEN** cada una muestra solo sus propias asignaciones

### Requirement: Feedback y guía en las asignaciones
El sistema SHALL informar del resultado de cada asignación, cambio y liberación, y SHALL guiar al usuario cuando no hay alumnos, taquillas libres o curso activo.

#### Scenario: Resultado
- **WHEN** se completa una asignación
- **THEN** el sistema confirma qué alumno tiene ahora qué taquilla y en qué zona

#### Scenario: Sin alumnos sin taquilla
- **WHEN** todos los alumnos activos ya tienen taquilla
- **THEN** el sistema lo indica en lugar de mostrar una lista vacía sin explicación

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** el sistema indica que hay que crear o activar un curso antes de asignar
