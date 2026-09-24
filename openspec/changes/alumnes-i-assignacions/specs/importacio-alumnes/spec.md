## Purpose

Mantener actualizados los alumnos a partir de un fichero ODS de secretaría, tratado como fuente de verdad: concilia el fichero con los alumnos existentes usando el correo como identificador único, muestra una revisión previa completa y solo guarda tras la confirmación del usuario.

## ADDED Requirements

### Requirement: Formato del fichero
El sistema SHALL aceptar ficheros ODS con una hoja por grupo, en las que la fila 1 es la cabecera con las columnas `Nom complet` y `Correu` (sin distinguir mayúsculas ni acentos, en cualquier orden, ignorando otras columnas) y el resto de filas son alumnos, con un máximo de 5000 filas de datos entre todas las hojas.

#### Scenario: Fichero válido
- **WHEN** el usuario carga un ODS con hojas cuya primera fila contiene `Nom complet` y `Correu`
- **THEN** el sistema lee las filas de datos de cada hoja

#### Scenario: Fichero que no es ODS
- **WHEN** el fichero no es un ODS válido (otro formato, dañado o cifrado)
- **THEN** el sistema lo rechaza con un error de formato de fichero sin procesar ninguna fila

#### Scenario: Hoja sin cabecera esperada
- **WHEN** una hoja con filas no tiene las columnas `Nom complet` y `Correu` en su primera fila
- **THEN** el sistema rechaza el fichero e indica qué hoja y qué columna falta

#### Scenario: Hojas vacías y filas vacías
- **WHEN** el fichero tiene hojas sin filas de datos o filas totalmente vacías
- **THEN** el sistema las ignora sin error

#### Scenario: Fichero vacío
- **WHEN** ninguna hoja contiene filas de datos
- **THEN** el sistema lo rechaza con un error de fichero vacío

#### Scenario: Fichero demasiado grande
- **WHEN** el total de filas de datos supera las 5000
- **THEN** el sistema lo rechaza con un error de tamaño excesivo

### Requirement: Nivel y grupo a partir del nombre de la hoja
El sistema SHALL obtener el nivel y el grupo de cada alumno del nombre de su hoja, con el formato `<nivel> <grupo>`, donde el grupo es la última palabra y el nivel el resto.

#### Scenario: Hoja con nivel y grupo
- **WHEN** una hoja se llama `1r ESO A`
- **THEN** sus alumnos quedan en el nivel `1r ESO` y el grupo `A`

#### Scenario: Nivel con varias palabras
- **WHEN** una hoja se llama `2n BATX B`
- **THEN** sus alumnos quedan en el nivel `2n BATX` y el grupo `B`

#### Scenario: Nombre de hoja no interpretable
- **WHEN** el nombre de una hoja con filas tiene una sola palabra
- **THEN** el sistema rechaza el fichero e indica esa hoja

### Requirement: Conciliación con el fichero de secretaría
El sistema SHALL reconocer a cada alumno por su correo, sin distinguir mayúsculas ni espacios extremos, y SHALL considerar de baja a los alumnos activos cuyo correo no consta en el fichero.

#### Scenario: Alumno existente
- **WHEN** el correo de una fila corresponde a un alumno activo ya registrado
- **THEN** se crea su matrícula del curso activo si aún no la tiene, o se actualizan su nivel y su grupo si han cambiado, conservando su ficha y sus datos

#### Scenario: Nombre distinto con el mismo correo
- **WHEN** el correo corresponde a un alumno registrado y el nombre del fichero difiere del guardado
- **THEN** se actualizan el nombre y los apellidos del alumno, porque el correo es su identidad, y el cambio consta en su historial

#### Scenario: Alumno sin cambios
- **WHEN** una fila corresponde a un alumno activo cuyo nombre, nivel y grupo ya coinciden
- **THEN** no se modifica nada y se cuenta como sin cambios

#### Scenario: Alumno nuevo
- **WHEN** el correo de una fila no corresponde a ningún alumno registrado
- **THEN** se crea un alumno nuevo en el nivel y grupo de su hoja, sin necesidad de que sea el inicio de curso

#### Scenario: Alumno que no consta
- **WHEN** un alumno activo no corresponde a ninguna fila del fichero
- **THEN** se propone su baja, incluidos los alumnos de fin de etapa

#### Scenario: Alumno de baja que reaparece
- **WHEN** el correo de una fila corresponde a un alumno que está de baja
- **THEN** se propone reactivarlo con el nivel y grupo del fichero

### Requirement: Validación de cada fila
El sistema SHALL marcar como errónea cada fila que incumpla las reglas de datos, indicando su hoja, su línea y el motivo, y SHALL no importarla.

#### Scenario: Correo ausente o mal formado
- **WHEN** una fila carece de correo o su correo no tiene un formato válido
- **THEN** se marca errónea por correo inválido

#### Scenario: Nombre ausente o sin separador
- **WHEN** una fila carece de nombre completo o este no tiene el formato `Apellidos, Nombre` (una coma con apellidos y nombre no vacíos)
- **THEN** se marca errónea por nombre inválido

#### Scenario: Longitud excesiva
- **WHEN** un valor supera la longitud máxima permitida
- **THEN** se marca errónea por longitud máxima

#### Scenario: Correo repetido en el fichero
- **WHEN** dos o más filas, de la misma hoja o de hojas distintas, tienen el mismo correo
- **THEN** todas ellas se marcan erróneas por correo repetido en el fichero

#### Scenario: Separación del nombre
- **WHEN** el nombre completo es `Bosch Camps, Aina`
- **THEN** los apellidos son `Bosch Camps` y el nombre es `Aina`, recortando espacios sobrantes

### Requirement: Revisión previa sin efectos
El sistema SHALL mostrar antes de guardar un resumen con los recuentos de alumnos nuevos, actualizados, sin cambios, dados de baja, reactivados y erróneos, con el detalle de cada grupo, sin modificar ningún dato hasta la confirmación.

#### Scenario: Resumen de la revisión
- **WHEN** el usuario carga un fichero válido
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
