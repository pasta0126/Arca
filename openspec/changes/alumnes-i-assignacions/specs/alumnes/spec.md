## Purpose

Gestionar a los alumnos como personas que persisten entre cursos, con una matrícula por curso (nivel y grupo), un estado activo o de baja reversible y un catálogo de niveles y grupos que se alimenta de los ficheros importados.

## ADDED Requirements

### Requirement: Ficha de alumno persistente
El sistema SHALL representar al alumno como una única ficha con identidad interna que persiste entre cursos, con nombre, apellidos y correo, que es su identificador único mientras pertenezca al centro.

#### Scenario: Continuidad entre cursos
- **WHEN** un alumno pasa a otro nivel en el curso siguiente
- **THEN** conserva su ficha, su historial y sus datos, y solo cambia su matrícula del nuevo curso

#### Scenario: Nombre y apellidos obligatorios
- **WHEN** el usuario intenta crear un alumno sin nombre o sin apellidos
- **THEN** el sistema lo rechaza con un error de dato obligatorio

#### Scenario: Longitud máxima
- **WHEN** el nombre o los apellidos superan los 100 caracteres
- **THEN** el sistema lo rechaza con un error de longitud máxima

#### Scenario: Correo obligatorio
- **WHEN** el usuario intenta crear un alumno sin correo o con un correo de formato inválido
- **THEN** el sistema lo rechaza con un error de correo inválido

#### Scenario: Correo único
- **WHEN** el usuario intenta crear un alumno con un correo que ya tiene otro alumno, activo o de baja
- **THEN** el sistema lo rechaza con un error de correo ya registrado, sin distinguir mayúsculas

#### Scenario: Homónimos
- **WHEN** dos alumnos tienen el mismo nombre y apellidos y correos distintos
- **THEN** el sistema los acepta como personas distintas

### Requirement: Privacidad del correo
El sistema SHALL usar el correo únicamente para identificar al alumno, y SHALL no mostrarlo en listados ni incluirlo en ninguna exportación.

#### Scenario: Listados y exportaciones
- **WHEN** el usuario consulta un listado o exporta un fichero que incluye alumnos
- **THEN** no aparece el correo

#### Scenario: Consulta de la ficha
- **WHEN** el usuario abre la ficha de un alumno para corregir sus datos, o revisa una importación
- **THEN** el sistema puede mostrar el correo solo en esa ficha o en esa revisión

### Requirement: Matrícula por curso
El sistema SHALL registrar para cada alumno y curso una matrícula con su nivel y su grupo, y SHALL admitir como máximo una matrícula por alumno y curso.

#### Scenario: Alta con matrícula
- **WHEN** el usuario da de alta un alumno indicando nivel y grupo en el curso activo
- **THEN** el alumno queda creado, activo y con su matrícula del curso activo

#### Scenario: Cambio de nivel o de grupo
- **WHEN** el usuario cambia el nivel o el grupo de un alumno en el curso activo
- **THEN** la matrícula del curso activo se actualiza y se registra el cambio en el historial del alumno

#### Scenario: Alumno sin grupo
- **WHEN** el usuario da de alta un alumno con nivel y sin grupo
- **THEN** el sistema lo acepta

#### Scenario: Alumno sin nivel
- **WHEN** el usuario intenta dar de alta un alumno sin nivel
- **THEN** el sistema lo rechaza con un error de nivel obligatorio

### Requirement: Catálogo de niveles y grupos
El sistema SHALL mantener un catálogo de niveles y de grupos que se alimenta de los ficheros importados y de las altas manuales, sin valores fijos en el código.

#### Scenario: Valor nuevo en un alta manual
- **WHEN** el usuario da de alta un alumno con un nivel o grupo que no existe en el catálogo
- **THEN** el sistema pide confirmar la creación del valor nuevo antes de guardar

#### Scenario: Equivalencia de grafías
- **WHEN** el usuario indica el grupo "a" y existe el grupo "A" en el nivel
- **THEN** el sistema lo reconoce como el mismo, sin distinguir mayúsculas ni acentos

#### Scenario: Grupo por nivel
- **WHEN** existe el grupo "A" en "1r ESO"
- **THEN** el grupo "A" de "2n ESO" es un valor distinto y se gestiona por separado

### Requirement: Baja de un alumno
El sistema SHALL permitir dar de baja a un alumno, indicando un motivo, y SHALL liberar su taquilla al hacerlo, conservando su ficha y su historial.

#### Scenario: Baja con taquilla
- **WHEN** el usuario da de baja a un alumno que tiene una taquilla asignada
- **THEN** el alumno queda de baja, la taquilla queda libre y se registran los eventos en el historial de ambos

#### Scenario: Baja sin taquilla
- **WHEN** el usuario da de baja a un alumno sin taquilla
- **THEN** el alumno queda de baja

#### Scenario: Baja ya registrada
- **WHEN** el usuario intenta dar de baja a un alumno que ya está de baja
- **THEN** el sistema lo rechaza con un error de alumno ya de baja

#### Scenario: Baja con confirmación
- **WHEN** el usuario inicia la baja de un alumno
- **THEN** el sistema pide confirmación indicando que la taquilla se liberará

### Requirement: Reactivación de un alumno
El sistema SHALL permitir reactivar a un alumno de baja, con su misma ficha, en el curso activo.

#### Scenario: Reactivación
- **WHEN** el usuario reactiva a un alumno de baja indicando nivel y grupo
- **THEN** el alumno vuelve a estar activo con una matrícula en el curso activo y se conserva su historial

#### Scenario: Alumno que no está de baja
- **WHEN** el usuario intenta reactivar a un alumno activo
- **THEN** el sistema lo rechaza con un error de alumno ya activo

### Requirement: Edición de datos del alumno
El sistema SHALL permitir corregir el nombre, los apellidos y el correo de un alumno, registrando el cambio en su historial.

#### Scenario: Corrección de un apellido
- **WHEN** el usuario corrige un apellido
- **THEN** el dato se actualiza y el historial conserva el valor anterior y el nuevo

#### Scenario: Corrección del correo
- **WHEN** el usuario corrige el correo de un alumno a otro no usado por nadie
- **THEN** el dato se actualiza y el historial conserva el valor anterior y el nuevo

#### Scenario: Correo ya registrado
- **WHEN** el usuario corrige el correo de un alumno a uno que ya tiene otro alumno
- **THEN** el sistema lo rechaza con un error de correo ya registrado

### Requirement: Búsqueda y consulta de alumnos
El sistema SHALL permitir buscar alumnos por nombre, apellidos, nivel, grupo, número de taquilla y estado de asignación, sin distinguir mayúsculas ni acentos, y SHALL mostrar por defecto solo los alumnos activos del curso activo.

#### Scenario: Búsqueda por apellido
- **WHEN** el usuario busca "garcia"
- **THEN** los resultados incluyen a los alumnos con "García" en sus apellidos

#### Scenario: Alumnos sin taquilla
- **WHEN** el usuario filtra por alumnos sin taquilla
- **THEN** ve los alumnos activos del curso activo que no tienen asignación

#### Scenario: Búsqueda por taquilla
- **WHEN** el usuario busca por el número de una taquilla ocupada
- **THEN** ve al alumno que la tiene asignada

#### Scenario: Incluir bajas
- **WHEN** el usuario pide incluir los alumnos de baja
- **THEN** el listado los muestra diferenciados

#### Scenario: Sin resultados
- **WHEN** ningún alumno cumple los criterios
- **THEN** el sistema devuelve una lista vacía y ofrece limpiar los filtros

### Requirement: Historial del alumno
El sistema SHALL registrar en un historial de solo añadir cada alta, cambio de datos, cambio de matrícula, baja, reactivación y cambio de asignación, con su instante y los valores anterior y nuevo.

#### Scenario: Consulta del historial
- **WHEN** el usuario consulta el historial de un alumno
- **THEN** ve sus eventos ordenados del más reciente al más antiguo, con el texto en el idioma activo

#### Scenario: Historial inmutable
- **WHEN** se intenta modificar o borrar un evento registrado
- **THEN** el sistema no ofrece esa operación
