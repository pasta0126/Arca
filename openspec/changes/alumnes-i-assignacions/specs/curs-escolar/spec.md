## Purpose

Definir el curso escolar como un año académico con fechas, del que solo uno está activo, y conservar los anteriores como histórico de consulta.

## ADDED Requirements

### Requirement: Crear un curso escolar
El sistema SHALL permitir crear un curso escolar con un año de inicio y fechas de inicio y de fin, y SHALL derivar su nombre en la forma "2026-2027".

#### Scenario: Curso nuevo
- **WHEN** el usuario crea un curso con inicio el 1 de septiembre de 2026 y fin el 30 de junio de 2027
- **THEN** el curso queda creado con el nombre "2026-2027"

#### Scenario: Fechas incoherentes
- **WHEN** la fecha de fin es igual o anterior a la de inicio
- **THEN** el sistema lo rechaza con un error de fechas no válidas

#### Scenario: Solapamiento con otro curso
- **WHEN** el intervalo del nuevo curso se solapa con el de otro existente
- **THEN** el sistema lo rechaza con un error de solapamiento

#### Scenario: Nombre duplicado
- **WHEN** ya existe un curso con el mismo año de inicio
- **THEN** el sistema lo rechaza con un error de curso ya existente

### Requirement: Un solo curso activo
El sistema SHALL mantener como máximo un curso activo a la vez, y SHALL exigir que las altas de alumnos, matrículas y asignaciones se hagan en el curso activo.

#### Scenario: Primer curso
- **WHEN** el usuario crea el primer curso del sistema
- **THEN** el curso queda activo

#### Scenario: Segundo curso mientras hay uno activo
- **WHEN** el usuario crea un curso y ya hay otro activo
- **THEN** el nuevo curso queda creado sin activar y el activo no cambia

#### Scenario: Sin curso activo
- **WHEN** no hay ningún curso activo y el usuario intenta dar de alta un alumno, importar o asignar una taquilla
- **THEN** el sistema lo rechaza con un error que indica que hay que crear o activar un curso

### Requirement: Activación de un curso
El sistema SHALL permitir activar un curso solo cuando no hay otro activo. El cierre del curso activo se define en otro cambio.

#### Scenario: Activar con otro activo
- **WHEN** el usuario intenta activar un curso mientras otro está activo
- **THEN** el sistema lo rechaza con un error que indica que primero hay que cerrar el curso activo

#### Scenario: Activar sin curso activo
- **WHEN** no hay ningún curso activo y el usuario activa uno existente
- **THEN** el curso pasa a ser el activo

### Requirement: Histórico de solo lectura
El sistema SHALL permitir consultar los cursos no activos y SHALL impedir modificar sus matrículas y asignaciones.

#### Scenario: Consulta de un curso anterior
- **WHEN** el usuario consulta un curso no activo
- **THEN** ve sus matrículas y asignaciones tal como quedaron

#### Scenario: Modificación en un curso anterior
- **WHEN** el usuario intenta modificar una matrícula o asignación de un curso no activo
- **THEN** el sistema lo rechaza con un error de curso no activo

### Requirement: Eliminación de cursos
El sistema SHALL permitir eliminar únicamente un curso que no tiene matrículas ni asignaciones.

#### Scenario: Curso vacío
- **WHEN** el usuario elimina un curso sin matrículas ni asignaciones
- **THEN** el curso desaparece

#### Scenario: Curso con datos
- **WHEN** el usuario intenta eliminar un curso que tiene matrículas o asignaciones
- **THEN** el sistema lo rechaza con un error que indica que el curso tiene datos
