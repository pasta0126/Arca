## Purpose

Permitir asignar una taquilla arrastrando un alumno sobre una taquilla libre, sin que sea la única vía y sin saltarse ninguna validación.

## ADDED Requirements

### Requirement: Asignar arrastrando un alumno
El sistema SHALL permitir arrastrar un alumno sobre una taquilla libre para asignársela, y SHALL invocar el mismo caso de uso de asignación que el resto de caminos.

#### Scenario: Asignación arrastrando
- **WHEN** el usuario arrastra un alumno sin taquilla sobre una taquilla libre y suelta
- **THEN** se ejecuta la asignación con las mismas validaciones y avisos que desde el alumno o la taquilla

#### Scenario: Aviso con confirmación
- **WHEN** el alumno tiene deuda de cursos anteriores
- **THEN** se pide la misma confirmación explícita antes de asignar

### Requirement: Respuesta visual del destino
El sistema SHALL indicar durante el arrastre si cada taquilla es un destino válido, no válido o no disponible, y SHALL mostrar la razón cuando no lo es.

#### Scenario: Destino válido
- **WHEN** el usuario arrastra un alumno sobre una taquilla libre y operativa
- **THEN** la taquilla se resalta como destino válido

#### Scenario: Destino no válido
- **WHEN** el usuario arrastra un alumno sobre una taquilla averiada u ocupada
- **THEN** se marca como no válida, explica el motivo y no se puede soltar

#### Scenario: Soltar fuera
- **WHEN** el usuario suelta el alumno fuera de una taquilla
- **THEN** no ocurre nada

### Requirement: Cancelar el arrastre
El sistema SHALL permitir cancelar un arrastre en curso con Escape sin cambiar ningún dato.

#### Scenario: Escape durante el arrastre
- **WHEN** el usuario pulsa Escape mientras arrastra
- **THEN** el arrastre se cancela y no cambia nada

### Requirement: Alternativas de teclado y de menú
El sistema SHALL ofrecer para la misma asignación una acción de teclado y una entrada de menú contextual, con el mismo resultado y los mismos avisos, y SHALL no depender del arrastre para ninguna función.

#### Scenario: Asignar desde el menú
- **WHEN** el usuario elige asignar en el menú contextual de un alumno y elige una taquilla libre
- **THEN** obtiene el mismo resultado que arrastrando

#### Scenario: Solo teclado
- **WHEN** el usuario asigna sin ratón
- **THEN** puede completar la asignación con el teclado

### Requirement: Feedback tras soltar
El sistema SHALL informar del resultado de la asignación con una notificación y actualizar la vista sin recargar, y SHALL evitar asignaciones duplicadas por soltar dos veces.

#### Scenario: Asignación realizada
- **WHEN** se completa la asignación
- **THEN** una notificación indica el alumno y la taquilla y la vista refleja el cambio

#### Scenario: Soltar dos veces
- **WHEN** el usuario suelta repetidamente sobre la misma taquilla
- **THEN** se ejecuta una sola asignación

### Requirement: Solo este caso en v1
El sistema SHALL limitar el arrastrar y soltar en v1 a la asignación de un alumno a una taquilla libre.

#### Scenario: Otras acciones
- **WHEN** el usuario intenta arrastrar una zona o una taquilla
- **THEN** no se inicia ningún arrastre
