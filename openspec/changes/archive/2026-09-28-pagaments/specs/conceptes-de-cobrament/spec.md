## Purpose

Definir los importes de la cuota anual, la fianza y la reposición de llave para cada curso escolar, como valores fijos para todos los alumnos que no alteran lo ya generado al cambiar.

## ADDED Requirements

### Requirement: Conceptos de cobro de la versión 1
El sistema SHALL ofrecer exactamente tres conceptos de cobro: cuota anual, fianza y reposición de llave.

#### Scenario: Lista de conceptos
- **WHEN** el usuario consulta los conceptos
- **THEN** ve la cuota anual, la fianza y la reposición de llave, y no puede crear otros

### Requirement: Importe por curso
El sistema SHALL guardar para cada curso escolar un importe de cada concepto, en euros con dos decimales, mayor que cero y no superior a 9999,99.

#### Scenario: Definir importes de un curso
- **WHEN** el usuario define para el curso activo una cuota de 50,00, una fianza de 20,00 y una reposición de 10,00
- **THEN** los importes quedan guardados para ese curso

#### Scenario: Importe no válido
- **WHEN** el usuario indica un importe cero, negativo, con más de dos decimales o superior a 9999,99
- **THEN** el sistema lo rechaza con un error de importe no válido

#### Scenario: Curso sin importes
- **WHEN** se intenta generar un cargo de un concepto cuyo importe no está definido en el curso
- **THEN** el sistema no genera el cargo y avisa de que hay que definir los importes del curso

### Requirement: Importes iniciales heredados
El sistema SHALL proponer al crear los importes de un curso los del curso anterior, para que el usuario los confirme o cambie.

#### Scenario: Curso con antecesor
- **WHEN** el usuario define los importes de un curso y existe un curso anterior con importes
- **THEN** el sistema propone esos valores y solo se guardan al confirmarlos

#### Scenario: Primer curso
- **WHEN** no existe ningún curso anterior con importes
- **THEN** los campos se presentan vacíos

### Requirement: Cambio de importe sin efecto retroactivo
El sistema SHALL aplicar el nuevo importe únicamente a los cargos que se generen después, sin modificar los cargos ya existentes.

#### Scenario: Subida de la cuota
- **WHEN** el usuario cambia la cuota del curso activo de 50,00 a 55,00 habiendo cargos ya generados
- **THEN** los cargos ya generados conservan 50,00 y los nuevos se generan con 55,00

#### Scenario: Cambio registrado
- **WHEN** el usuario cambia un importe
- **THEN** el sistema conserva en el historial el valor anterior, el nuevo y el instante del cambio

### Requirement: Importes de cursos finalizados
El sistema SHALL permitir consultar los importes de cualquier curso y SHALL permitir modificar solo los de cursos cuya fecha de fin no ha pasado.

#### Scenario: Consultar un curso finalizado
- **WHEN** el usuario consulta los importes de un curso cuya fecha de fin ya pasó
- **THEN** los ve sin posibilidad de cambiarlos

#### Scenario: Modificar un curso finalizado
- **WHEN** el usuario intenta modificar los importes de un curso cuya fecha de fin ya pasó
- **THEN** el sistema lo rechaza con un error de curso no modificable
