## Purpose

Mantener actualizados los alumnos a partir del fichero CSV de secretaría, tratado como fuente de verdad: concilia el fichero con los alumnos existentes, muestra una revisión previa completa y solo guarda tras la confirmación del usuario.

## ADDED Requirements

### Requirement: Conciliación con el fichero de secretaría
El sistema SHALL concluir de cada fila del fichero si el alumno ya existe, es nuevo o reaparece, y SHALL considerar de baja a los alumnos activos que no constan en el fichero.

#### Scenario: Alumno existente
- **WHEN** una fila corresponde a un alumno activo ya registrado
- **THEN** se crea su matrícula del curso activo si aún no la tiene, o se actualizan su nivel y su grupo si han cambiado, conservando su ficha y sus datos

#### Scenario: Alumno sin cambios
- **WHEN** una fila corresponde a un alumno activo cuyo nivel y grupo ya coinciden
- **THEN** no se modifica nada y se cuenta como sin cambios

#### Scenario: Alumno nuevo
- **WHEN** una fila no corresponde a ningún alumno registrado
- **THEN** se crea un alumno nuevo en el nivel y grupo indicados, sin necesidad de que sea el inicio de curso

#### Scenario: Alumno que no consta
- **WHEN** un alumno activo no corresponde a ninguna fila del fichero
- **THEN** se propone su baja, incluidos los alumnos de fin de etapa

#### Scenario: Alumno de baja que reaparece
- **WHEN** una fila corresponde a un alumno que está de baja
- **THEN** se propone reactivarlo con el nivel y grupo del fichero

### Requirement: Reconocimiento del alumno por clave configurable
El sistema SHALL reconocer a cada alumno aplicando, por orden, el identificador, el correo y el nombre con apellidos, y SHALL tratar como dudoso todo caso ambiguo o contradictorio.

#### Scenario: Reconocimiento por identificador
- **WHEN** el fichero aporta un identificador que coincide con el de un alumno registrado
- **THEN** la fila se reconoce como ese alumno aunque el nombre difiera

#### Scenario: Reconocimiento por correo
- **WHEN** no hay identificador y el correo coincide, sin distinguir mayúsculas, con el de un alumno registrado
- **THEN** la fila se reconoce como ese alumno

#### Scenario: Reconocimiento por nombre y apellidos
- **WHEN** no hay identificador ni correo coincidente y el nombre y apellidos coinciden, sin distinguir mayúsculas, acentos ni espacios repetidos, con un único alumno
- **THEN** la fila se reconoce como ese alumno

#### Scenario: Homónimos distinguidos por nivel y grupo
- **WHEN** el nombre y apellidos coinciden con varios alumnos y solo uno tiene el mismo nivel y grupo que la fila
- **THEN** la fila se reconoce como ese alumno

#### Scenario: Homónimos sin desempate
- **WHEN** el nombre y apellidos coinciden con varios alumnos y el nivel y grupo no permiten distinguirlos
- **THEN** la fila se marca como dudosa para que el usuario decida

#### Scenario: Claves contradictorias
- **WHEN** el identificador de una fila coincide con un alumno y su nombre coincide con otro alumno distinto
- **THEN** la fila se marca como dudosa

#### Scenario: Desactivar una clave
- **WHEN** el centro ha configurado no usar el identificador como clave
- **THEN** el sistema no lo utiliza para reconocer y aplica solo las claves restantes

### Requirement: Resolución de los casos dudosos
El sistema SHALL exigir que el usuario resuelva cada caso dudoso antes de confirmar, eligiendo a qué alumno corresponde o indicando que es un alumno nuevo.

#### Scenario: Elegir un alumno existente
- **WHEN** el usuario resuelve una fila dudosa indicando un alumno existente
- **THEN** la fila se trata como una actualización de ese alumno

#### Scenario: Indicar alumno nuevo
- **WHEN** el usuario indica que la fila dudosa es una persona distinta
- **THEN** la fila se trata como un alumno nuevo

#### Scenario: Confirmar con dudosos sin resolver
- **WHEN** quedan casos dudosos sin resolver
- **THEN** el sistema no permite confirmar

#### Scenario: Homónimos dentro del mismo fichero
- **WHEN** dos filas del fichero tienen el mismo nombre, apellidos, nivel y grupo
- **THEN** ambas se marcan dudosas para que el usuario indique si son personas distintas o una fila repetida

### Requirement: Validación de cada fila
El sistema SHALL marcar como errónea cada fila que incumpla las reglas de datos, indicando su línea y el motivo, y SHALL no importarla.

#### Scenario: Datos obligatorios
- **WHEN** una fila carece de nombre, de apellidos o de nivel
- **THEN** se marca errónea por dato obligatorio

#### Scenario: Longitud excesiva
- **WHEN** un valor supera la longitud máxima permitida
- **THEN** se marca errónea por longitud máxima

#### Scenario: Mismo alumno en varias filas
- **WHEN** dos filas se reconocen como el mismo alumno existente
- **THEN** ambas se marcan erróneas por alumno repetido en el fichero

#### Scenario: Grupo ausente
- **WHEN** una fila tiene nivel y no tiene grupo
- **THEN** la fila es válida

### Requirement: Correspondencia de columnas guiada
El sistema SHALL ofrecer un asistente que permita indicar qué columna del fichero corresponde a cada dato, SHALL proponer una correspondencia por similitud de cabeceras y SHALL recordarla para siguientes importaciones.

#### Scenario: Primera importación
- **WHEN** el usuario carga un fichero por primera vez
- **THEN** el asistente propone una correspondencia según las cabeceras y permite corregirla antes de continuar

#### Scenario: Cabeceras conocidas
- **WHEN** el fichero tiene las mismas cabeceras que en una importación anterior
- **THEN** el sistema reutiliza la correspondencia guardada sin pedirla de nuevo

#### Scenario: Cabeceras distintas
- **WHEN** las cabeceras del fichero no coinciden con la correspondencia guardada
- **THEN** el asistente vuelve a pedir la correspondencia

#### Scenario: Datos imprescindibles sin columna
- **WHEN** el usuario no asigna columna a nombre, apellidos o nivel
- **THEN** el sistema no permite continuar y señala el dato que falta

#### Scenario: Datos opcionales
- **WHEN** el usuario no asigna columna a correo, identificador o grupo
- **THEN** el sistema continúa sin esos datos

### Requirement: Formato del fichero
El sistema SHALL aceptar ficheros CSV en UTF-8 con o sin marca BOM, con separador detectado automáticamente, con una fila de cabecera y un máximo de 5000 filas de datos.

#### Scenario: Codificación no válida
- **WHEN** el fichero no está en UTF-8 válido
- **THEN** el sistema lo rechaza con un error de codificación sin procesar ninguna fila

#### Scenario: Fichero vacío
- **WHEN** el fichero no contiene filas de datos
- **THEN** el sistema lo rechaza con un error de fichero vacío

#### Scenario: Fichero demasiado grande
- **WHEN** el fichero supera las 5000 filas de datos
- **THEN** el sistema lo rechaza con un error de tamaño excesivo

### Requirement: Revisión previa sin efectos
El sistema SHALL mostrar antes de guardar un resumen con los recuentos de alumnos nuevos, actualizados, sin cambios, dados de baja, reactivados, dudosos y erróneos, con el detalle de cada grupo, sin modificar ningún dato hasta la confirmación.

#### Scenario: Resumen de la revisión
- **WHEN** el usuario carga y correlaciona un fichero
- **THEN** el sistema muestra los recuentos y permite consultar el detalle de cada categoría

#### Scenario: Valores nuevos de nivel y grupo
- **WHEN** el fichero contiene niveles o grupos que no existen en el catálogo
- **THEN** la revisión los lista como valores nuevos y solo se crean tras la confirmación

#### Scenario: Cancelar tras la revisión
- **WHEN** el usuario cancela después de la revisión
- **THEN** no se modifica ningún dato

#### Scenario: Excluir una baja propuesta
- **WHEN** el usuario excluye a un alumno de la lista de bajas propuestas
- **THEN** ese alumno permanece activo y sin cambios

### Requirement: Salvaguarda ante bajas masivas
El sistema SHALL avisar de forma destacada y exigir una confirmación adicional cuando las bajas propuestas superen el 30 % de los alumnos activos.

#### Scenario: Fichero incorrecto
- **WHEN** el fichero solo contiene una parte del centro y provoca la baja propuesta del 60 % de los alumnos activos
- **THEN** la revisión avisa de que el fichero podría no ser el correcto y exige una confirmación adicional explícita

#### Scenario: Bajas dentro de lo normal
- **WHEN** las bajas propuestas no superan el 30 % de los alumnos activos
- **THEN** no se exige confirmación adicional

#### Scenario: Sin alumnos previos
- **WHEN** no hay alumnos activos registrados
- **THEN** el aviso de bajas masivas no se aplica

### Requirement: Confirmación y aplicación indivisible
El sistema SHALL, tras la confirmación explícita del usuario con los recuentos indicados, aplicar todos los cambios de la revisión en una única operación indivisible, revalidando contra el estado actual, y SHALL informar del resultado.

#### Scenario: Aplicación correcta
- **WHEN** el usuario confirma una revisión válida
- **THEN** se crean, actualizan, reactivan y dan de baja los alumnos indicados, se crean los niveles y grupos nuevos, y se informa de los recuentos

#### Scenario: Fallo al guardar
- **WHEN** ocurre un error a mitad de la aplicación
- **THEN** no se aplica ningún cambio

#### Scenario: Datos cambiados desde la revisión
- **WHEN** entre la revisión y la confirmación cambian los datos de forma que la revisión deja de ser válida
- **THEN** el sistema no guarda nada y muestra la revisión actualizada

#### Scenario: Filas erróneas
- **WHEN** el usuario confirma una revisión con filas erróneas
- **THEN** se aplican las filas válidas y las erróneas se informan como omitidas

#### Scenario: Importar el mismo fichero dos veces
- **WHEN** el usuario importa un fichero ya aplicado
- **THEN** todas las filas resultan sin cambios y no se crea ni se da de baja a nadie

### Requirement: Efecto de las bajas por importación
El sistema SHALL liberar la taquilla de cada alumno dado de baja por una importación y SHALL registrar el motivo en su historial.

#### Scenario: Baja con taquilla
- **WHEN** se aplica la baja de un alumno con taquilla asignada
- **THEN** su taquilla queda libre y el historial del alumno y el de la taquilla registran que fue por importación

### Requirement: Progreso, cancelación y resultado
El sistema SHALL mostrar el progreso del análisis y de la aplicación con recuentos, SHALL permitir cancelar antes del guardado y SHALL informar del resultado final.

#### Scenario: Cancelar durante el análisis
- **WHEN** el usuario cancela mientras se analiza el fichero
- **THEN** no se crea ni modifica ningún dato

#### Scenario: Resultado final
- **WHEN** la aplicación termina
- **THEN** el sistema informa de cuántos alumnos se han creado, actualizado, reactivado y dado de baja, cuántos valores nuevos de catálogo se han creado y cuántas filas se han omitido

### Requirement: Curso activo obligatorio
El sistema SHALL exigir un curso activo para importar alumnos.

#### Scenario: Sin curso activo
- **WHEN** el usuario intenta importar y no hay curso activo
- **THEN** el sistema lo rechaza con un error que indica que hay que crear o activar un curso
