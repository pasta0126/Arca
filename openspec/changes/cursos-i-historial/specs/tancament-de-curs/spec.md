## Purpose

Guiar el cierre de un curso escolar como un proceso que se puede hacer por partes y en varias sesiones: liberar las taquillas, recuperar las llaves, revisar la deuda y dar el curso por cerrado, sin obligar a completarlo todo a la vez.

## ADDED Requirements

### Requirement: Estados de un curso
El sistema SHALL asignar a cada curso uno de estos estados: sin activar, activo, en cierre o cerrado, y SHALL permitir como máximo un curso activo y cualquier número de cursos en cierre o cerrados.

#### Scenario: Curso recién creado
- **WHEN** el usuario crea un curso y ya hay otro activo
- **THEN** el curso queda sin activar

#### Scenario: Varios cursos en cierre
- **WHEN** un curso en cierre no se ha cerrado del todo y el usuario inicia el cierre de otro curso posterior
- **THEN** ambos cursos están en cierre y ninguno es el activo

### Requirement: Iniciar el cierre
El sistema SHALL permitir iniciar el cierre del curso activo con confirmación explícita que indica su consecuencia, y SHALL dejar el curso en estado en cierre, sin curso activo hasta que se active otro.

#### Scenario: Inicio del cierre
- **WHEN** el usuario confirma el inicio del cierre del curso activo
- **THEN** el curso pasa a en cierre, deja de ser el activo y se muestra el asistente de cierre

#### Scenario: Sin curso activo
- **WHEN** el usuario intenta iniciar un cierre y no hay curso activo
- **THEN** el sistema lo rechaza con un error que indica que no hay curso activo

#### Scenario: Confirmación pendiente
- **WHEN** el usuario abre la acción de iniciar el cierre
- **THEN** el sistema muestra qué ocurrirá y no actúa hasta la confirmación

### Requirement: Siguiente curso durante el cierre
El sistema SHALL permitir activar y trabajar en el curso siguiente mientras el anterior está en cierre.

#### Scenario: Activar el siguiente
- **WHEN** hay un curso en cierre y ningún curso activo y el usuario activa un curso sin activar
- **THEN** ese curso pasa a ser el activo y el curso en cierre sigue en cierre

#### Scenario: Alta de alumnos en el nuevo curso
- **WHEN** el curso siguiente está activo y el anterior está en cierre
- **THEN** las altas, matrículas y asignaciones se hacen en el curso activo

### Requirement: Operaciones permitidas en un curso en cierre
El sistema SHALL permitir en un curso en cierre únicamente las operaciones de cierre: liberar sus taquillas, resolver sus llaves y gestionar sus cobros pendientes, y SHALL rechazar cualquier otra modificación de sus matrículas y asignaciones.

#### Scenario: Liberar en un curso en cierre
- **WHEN** el usuario libera una taquilla de una asignación vigente de un curso en cierre
- **THEN** la asignación se cierra

#### Scenario: Nueva asignación en un curso en cierre
- **WHEN** el usuario intenta asignar una taquilla en un curso en cierre
- **THEN** el sistema lo rechaza con un error de curso no activo

#### Scenario: Cambiar el nivel en un curso en cierre
- **WHEN** el usuario intenta modificar la matrícula de un curso en cierre
- **THEN** el sistema lo rechaza con un error de curso no activo

### Requirement: Volver a activar un curso en cierre
El sistema SHALL permitir volver a activar un curso en cierre cuando no hay otro curso activo, sin perder lo ya hecho en el cierre.

#### Scenario: Reactivar
- **WHEN** no hay curso activo y el usuario reactiva un curso en cierre
- **THEN** el curso pasa a activo y conserva las taquillas ya liberadas y las llaves ya devueltas

#### Scenario: Reactivar con otro curso activo
- **WHEN** hay otro curso activo y el usuario intenta reactivar un curso en cierre
- **THEN** el sistema lo rechaza con un error que indica que hay otro curso activo

### Requirement: Pasos del asistente opcionales y reanudables
El sistema SHALL presentar el cierre como una lista guiada de pasos independientes, opcionales y en cualquier orden: liberar taquillas, devolver llaves, revisar deuda pendiente y cerrar el curso, mostrando el estado de cada uno, y SHALL permitir abandonar el asistente y retomarlo más tarde sin perder el avance.

#### Scenario: Estado de los pasos
- **WHEN** el usuario abre el asistente de un curso en cierre
- **THEN** ve para cada paso si está pendiente, hecho u omitido, con el recuento de lo que queda

#### Scenario: Omitir un paso
- **WHEN** el usuario omite un paso
- **THEN** el paso queda como omitido, se puede volver a abrir y no impide cerrar el curso

#### Scenario: Retomar en otra sesión
- **WHEN** el usuario cierra la aplicación con el cierre a medias y la abre otro día
- **THEN** el asistente muestra el estado real actual de cada paso

#### Scenario: Paso hecho sin trabajo
- **WHEN** no queda ninguna asignación vigente en el curso
- **THEN** el paso de liberar taquillas figura como hecho

#### Scenario: Pasos en otro orden
- **WHEN** el usuario devuelve llaves antes de liberar las taquillas
- **THEN** el sistema lo permite

### Requirement: Liberación masiva de taquillas
El sistema SHALL permitir liberar de una vez las taquillas de las asignaciones vigentes de un curso en cierre a partir de una selección que el usuario puede editar, con confirmación explícita, en una operación indivisible y en dos fases: análisis y aplicación tras revalidar.

#### Scenario: Propuesta
- **WHEN** el usuario abre la liberación masiva
- **THEN** el sistema propone todas las asignaciones vigentes del curso seleccionadas, con el recuento, y permite excluir algunas o filtrar por zona, nivel y grupo

#### Scenario: Exclusiones
- **WHEN** el usuario excluye 5 asignaciones y confirma
- **THEN** las demás se cierran con el motivo de fin de curso y las 5 excluidas siguen vigentes

#### Scenario: Historial y llaves
- **WHEN** se libera una asignación con la llave entregada
- **THEN** la asignación queda cerrada con el motivo de fin de curso, la llave queda pendiente de devolución y se registra en el historial de la taquilla y del alumno

#### Scenario: Fianza y alumnos sin cambios
- **WHEN** se libera masivamente
- **THEN** ninguna fianza cambia y ningún alumno causa baja

#### Scenario: Asignación que cambió
- **WHEN** entre el análisis y la confirmación una asignación seleccionada deja de estar vigente
- **THEN** el sistema no libera ninguna y muestra la selección actualizada

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error durante la aplicación
- **THEN** ninguna asignación cambia

#### Scenario: Progreso y cancelación
- **WHEN** la liberación procesa muchas asignaciones
- **THEN** el sistema muestra el avance con recuentos y permite cancelar antes del guardado

#### Scenario: Sin asignaciones vigentes
- **WHEN** el usuario abre la liberación masiva y no hay asignaciones vigentes
- **THEN** el sistema lo indica y no ofrece confirmar

### Requirement: Llaves y deuda desde el asistente
El sistema SHALL ofrecer desde el asistente el acceso a la devolución masiva de llaves y a la gestión de la deuda pendiente del curso, mostrando los recuentos de llaves sin resolver y de cargos pendientes, sin modificar por sí mismo llaves ni cobros.

#### Scenario: Llaves sin resolver
- **WHEN** el curso tiene 12 llaves entregadas o perdidas sin resolver
- **THEN** el asistente muestra 12 y permite abrir la devolución masiva

#### Scenario: Deuda pendiente
- **WHEN** el curso tiene cargos pendientes
- **THEN** el asistente muestra el recuento y el importe total y permite abrir la consulta de morosos, y advierte de que la deuda se arrastra si no se condona

#### Scenario: Cierre sin tocar la deuda
- **WHEN** el usuario cierra el curso con cargos pendientes
- **THEN** los cargos siguen pendientes y siguen avisando al asignar en cursos posteriores

### Requirement: Cierre definitivo con pendientes
El sistema SHALL permitir dar por cerrado un curso en cierre en cualquier momento, mostrando antes lo que queda pendiente (asignaciones vigentes, llaves sin resolver y cargos pendientes) y exigiendo confirmación explícita cuando queda algo.

#### Scenario: Todo resuelto
- **WHEN** no queda ninguna asignación vigente ni llave sin resolver y el usuario cierra el curso
- **THEN** el sistema pide una confirmación simple y el curso pasa a cerrado

#### Scenario: Con pendientes
- **WHEN** quedan 3 asignaciones vigentes y 12 llaves sin resolver y el usuario cierra el curso
- **THEN** el sistema muestra esos recuentos, exige confirmarlos de forma explícita y, al confirmar, cierra el curso

#### Scenario: Asignaciones que quedan al cerrar
- **WHEN** se cierra el curso con asignaciones vigentes
- **THEN** se cierran con el motivo de fin de curso, sus llaves quedan pendientes de devolución y se registra en el historial

#### Scenario: Cierre repetido
- **WHEN** el usuario intenta cerrar un curso que ya está cerrado
- **THEN** el sistema lo rechaza con un error de estado no válido

### Requirement: Registro del cierre
El sistema SHALL guardar al cerrar un curso un registro con la fecha y los recuentos de lo que quedó pendiente en ese momento, sin datos personales, y SHALL mantenerlo aunque se anonimicen o borren los datos del curso.

#### Scenario: Registro al cerrar
- **WHEN** se cierra un curso con 12 llaves sin resolver y 4 cargos pendientes
- **THEN** el registro guarda la fecha, 12 llaves y 4 cargos pendientes

#### Scenario: Consulta del registro
- **WHEN** el usuario consulta un curso cerrado
- **THEN** ve la fecha de cierre y los recuentos pendientes del momento

### Requirement: Curso cerrado de solo lectura
El sistema SHALL impedir modificar las matrículas y asignaciones de un curso cerrado, SHALL permitir seguir resolviendo sus llaves y sus cobros pendientes y SHALL mantener su consulta.

#### Scenario: Modificación en un curso cerrado
- **WHEN** el usuario intenta modificar una matrícula o una asignación de un curso cerrado
- **THEN** el sistema lo rechaza con un error de curso no activo

#### Scenario: Devolver una llave tardía
- **WHEN** el usuario devuelve una llave de una asignación de un curso cerrado
- **THEN** la llave queda devuelta

#### Scenario: Condonar deuda de un curso cerrado
- **WHEN** el usuario condona un cargo pendiente de un curso cerrado
- **THEN** el cargo queda condonado con su motivo

### Requirement: Un curso cerrado no vuelve a abrirse
El sistema SHALL rechazar activar o poner en cierre un curso cerrado.

#### Scenario: Reactivar un curso cerrado
- **WHEN** el usuario intenta activar un curso cerrado
- **THEN** el sistema lo rechaza con un error de curso cerrado

### Requirement: Feedback y guía en el cierre
El sistema SHALL informar del resultado de cada operación del cierre con recuentos y SHALL guiar al usuario sobre el siguiente paso.

#### Scenario: Resultado de la liberación
- **WHEN** termina la liberación masiva
- **THEN** el sistema indica cuántas taquillas se liberaron y sugiere devolver las llaves si quedan sin resolver

#### Scenario: Error comprensible
- **WHEN** una operación de cierre falla
- **THEN** el sistema muestra un mensaje claro en el idioma activo, sin detalles técnicos, y no deja datos a medias

#### Scenario: Doble ejecución
- **WHEN** el usuario pulsa dos veces confirmar en una operación de cierre
- **THEN** la operación se ejecuta una sola vez
