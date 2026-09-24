## Purpose

Guiar la puesta en marcha de ARCA, y el arranque de cada curso nuevo, con una lista de pasos que se pueden hacer por partes, retomar y reabrir, sin duplicar la lógica de negocio.

## ADDED Requirements

### Requirement: Pasos del asistente
El sistema SHALL presentar la configuración como una lista guiada de pasos en este orden: curso escolar, importes del curso, zonas, taquillas y alumnos, cada uno con su explicación y el estado en que está.

#### Scenario: Lista de pasos
- **WHEN** el usuario abre el asistente
- **THEN** ve los cinco pasos en orden con su estado y una breve explicación de para qué sirve cada uno

### Requirement: Estado de los pasos derivado de los datos
El sistema SHALL deducir el estado de cada paso de los datos existentes y SHALL guardar únicamente las omisiones y el descarte del asistente.

#### Scenario: Estados derivados
- **WHEN** hay un curso activo con importes, dos zonas y ninguna taquilla
- **THEN** los pasos de curso, importes y zonas figuran como hechos y los de taquillas y alumnos como pendientes

#### Scenario: Cambio fuera del asistente
- **WHEN** el usuario crea una zona desde su pantalla habitual
- **THEN** el paso de zonas figura como hecho al abrir el asistente

#### Scenario: Omitir un paso opcional
- **WHEN** el usuario omite el paso de alumnos
- **THEN** figura como omitido, se puede volver a abrir y no impide terminar

### Requirement: Pasos obligatorios
El sistema SHALL considerar obligatorios solo el curso escolar activo y los importes del curso activo, y SHALL no permitir omitirlos ni descartar el asistente mientras estén pendientes.

#### Scenario: Omitir un paso obligatorio
- **WHEN** el usuario intenta omitir el paso de curso escolar
- **THEN** el sistema lo impide y explica que sin curso activo no se puede asignar ni cobrar

#### Scenario: Descartar sin lo obligatorio
- **WHEN** el usuario intenta descartar el asistente con el curso o los importes pendientes
- **THEN** el sistema lo impide y explica qué falta

#### Scenario: Descartar con lo obligatorio hecho
- **WHEN** curso e importes están hechos y hay pasos opcionales pendientes
- **THEN** el usuario puede descartar el asistente y los pasos pendientes quedan disponibles desde ajustes

### Requirement: Cada paso guarda lo suyo de inmediato
El sistema SHALL confirmar y guardar cada paso en el momento de completarlo, de modo que abandonar el asistente en cualquier punto no pierda ni deje datos a medias.

#### Scenario: Salir a mitad
- **WHEN** el usuario completa el paso de zonas y cierra la aplicación
- **THEN** las zonas están guardadas y al volver el asistente continúa en el paso siguiente

#### Scenario: Paso interrumpido
- **WHEN** el usuario cancela un paso a mitad
- **THEN** ese paso no guarda nada y sigue pendiente

### Requirement: Reutilización de las operaciones existentes
El sistema SHALL ejecutar cada paso mediante los casos de uso y validaciones de las capacidades correspondientes, sin reglas propias, y SHALL mostrar los mismos errores y confirmaciones que sus pantallas habituales.

#### Scenario: Importes inválidos
- **WHEN** el usuario introduce un importe cero en el paso de importes
- **THEN** el sistema lo rechaza con el mismo error que la pantalla de importes

#### Scenario: Importación de alumnos
- **WHEN** el usuario importa alumnos en el asistente
- **THEN** pasa por la revisión previa de la importación habitual antes de confirmar

#### Scenario: Importación de taquillas
- **WHEN** el usuario elige importar taquillas en su paso
- **THEN** se usa la misma importación con revisión previa que fuera del asistente

### Requirement: Paso de curso escolar
El sistema SHALL proponer en el paso de curso escolar el año académico actual con fechas de inicio y fin editables, crearlo y activarlo, y SHALL indicarlo como hecho cuando exista un curso activo.

#### Scenario: Primer curso
- **WHEN** el usuario acepta la propuesta en septiembre de 2026
- **THEN** se crea el curso "2026-2027" y queda activo

#### Scenario: Curso siguiente
- **WHEN** no hay curso activo porque el anterior está en cierre y el usuario abre el paso
- **THEN** puede crear y activar el curso siguiente

### Requirement: Paso de importes
El sistema SHALL permitir en el paso de importes definir la cuota, la fianza y la reposición de llave del curso activo, proponiendo los del curso anterior si existen, y SHALL considerarlo hecho cuando el curso activo tiene los tres importes.

#### Scenario: Curso con importes anteriores
- **WHEN** existe un curso anterior con importes
- **THEN** el paso los propone como valores iniciales editables

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** el paso de importes indica que primero hay que completar el de curso escolar

### Requirement: Pasos de zonas y taquillas
El sistema SHALL permitir en el paso de zonas crear las zonas, y en el de taquillas dar de alta taquillas por rangos o importarlas, y SHALL requerir al menos una zona para el paso de taquillas.

#### Scenario: Taquillas sin zonas
- **WHEN** no hay ninguna zona y el usuario abre el paso de taquillas
- **THEN** el sistema indica que primero hay que crear una zona y enlaza con ese paso

#### Scenario: Alta por rangos
- **WHEN** el usuario da de alta las taquillas 1 a 300 en una zona
- **THEN** el paso figura como hecho

### Requirement: Paso de alumnos
El sistema SHALL permitir en el paso de alumnos importar alumnos desde el fichero de secretaría, y SHALL permitir omitirlo cuando todavía no se dispone del fichero, indicando dónde hacerlo después.

#### Scenario: Sin fichero todavía
- **WHEN** el usuario no tiene el fichero de alumnos y omite el paso
- **THEN** el paso queda omitido y el sistema indica que se puede importar más tarde desde alumnos

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** el paso de alumnos indica que primero hay que completar el de curso escolar

### Requirement: Reanudación
El sistema SHALL abrir el asistente en el primer paso pendiente en cada arranque mientras haya pasos obligatorios pendientes, y SHALL no reabrirlo automáticamente cuando lo obligatorio esté hecho y el usuario lo haya descartado o terminado.

#### Scenario: Retomar tras cerrar
- **WHEN** el usuario cierra la aplicación con los importes pendientes y la abre otro día
- **THEN** el asistente se abre en el paso de importes

#### Scenario: Descartado
- **WHEN** el usuario descartó el asistente con lo obligatorio hecho
- **THEN** no vuelve a abrirse solo

#### Scenario: Aparece un paso pendiente nuevo
- **WHEN** el curso activo pasa a cierre y no hay curso activo
- **THEN** el paso de curso escolar vuelve a figurar como pendiente y el asistente se ofrece en el siguiente arranque

### Requirement: Reapertura desde ajustes
El sistema SHALL permitir abrir la configuración guiada en cualquier momento desde ajustes, mostrando el estado de cada paso y permitiendo abrir cualquiera de ellos.

#### Scenario: Volver a abrir
- **WHEN** el usuario abre la configuración guiada desde ajustes
- **THEN** ve el estado actual de los cinco pasos y puede abrir cualquiera

#### Scenario: Volver a abrir un paso omitido
- **WHEN** el usuario abre un paso omitido
- **THEN** deja de estar omitido al completarlo

### Requirement: Navegación y progreso
El sistema SHALL permitir avanzar, retroceder y saltar entre pasos, mostrar el progreso como recuento de pasos hechos sobre el total y sugerir siempre el siguiente paso.

#### Scenario: Progreso
- **WHEN** hay tres pasos hechos de cinco
- **THEN** el asistente muestra "3 de 5" y sugiere el siguiente pendiente

#### Scenario: Terminado
- **WHEN** todos los pasos están hechos
- **THEN** el asistente lo indica y guía hacia la pantalla de trabajo

### Requirement: Feedback y guía
El sistema SHALL informar del resultado de cada paso con recuentos, explicar el motivo de cada bloqueo y no bloquear la interfaz en operaciones largas, sin incluir datos de alumnos en el registro técnico.

#### Scenario: Resultado de un paso
- **WHEN** el usuario completa el alta de 300 taquillas
- **THEN** el sistema indica que se han creado 300 taquillas y sugiere el siguiente paso

#### Scenario: Error comprensible
- **WHEN** falla un paso
- **THEN** el sistema muestra un mensaje claro y el paso sigue pendiente sin datos a medias
