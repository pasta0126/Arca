## Purpose

Decidir qué se hace con los datos de un curso ya cerrado, que incluyen datos de menores: conservarlos, anonimizarlos conservando totales, o borrarlos, sin fijar una política automática hasta consultarla con los conserjes.

## ADDED Requirements

### Requirement: Decisión de conservación por curso cerrado
El sistema SHALL asociar a cada curso cerrado una decisión de conservación con los valores pendiente de decidir, conservado, anonimizado y borrado, y SHALL no borrar ni anonimizar nada sin una decisión explícita del usuario.

#### Scenario: Curso recién cerrado
- **WHEN** se cierra un curso
- **THEN** su decisión es pendiente de decidir y todos sus datos se conservan

#### Scenario: Sin aplicación automática
- **WHEN** pasan varios cursos sin que el usuario decida
- **THEN** el sistema no borra ni anonimiza ningún dato

### Requirement: Preguntar al finalizar el cierre
El sistema SHALL preguntar al dar un curso por cerrado si se quiere conservar, anonimizar o borrar sus datos, y SHALL permitir decidir más tarde.

#### Scenario: Pregunta al cerrar
- **WHEN** el usuario confirma el cierre definitivo
- **THEN** el sistema ofrece las opciones conservar, anonimizar, borrar y decidir más tarde, explicando la consecuencia de cada una

#### Scenario: Decidir más tarde
- **WHEN** el usuario elige decidir más tarde
- **THEN** el curso queda cerrado con la decisión pendiente de decidir

#### Scenario: Conservar
- **WHEN** el usuario elige conservar
- **THEN** el curso queda cerrado con la decisión conservado y sin cambios en sus datos

### Requirement: Decidir después desde ajustes
El sistema SHALL permitir consultar los cursos cerrados con su decisión y aplicar o cambiar la decisión de un curso cerrado que esté pendiente de decidir o conservado.

#### Scenario: Lista de cursos cerrados
- **WHEN** el usuario abre la conservación de datos
- **THEN** ve cada curso cerrado con su decisión y la fecha en que se tomó

#### Scenario: De conservado a anonimizado
- **WHEN** el usuario decide anonimizar un curso conservado
- **THEN** se aplica el anonimizado con las mismas garantías que al cerrar

#### Scenario: Curso ya anonimizado o borrado
- **WHEN** el usuario intenta cambiar la decisión de un curso anonimizado o borrado
- **THEN** el sistema lo rechaza con un error que indica que la decisión es irreversible

### Requirement: Anonimizar un curso
El sistema SHALL anonimizar un curso cerrado desvinculando de los alumnos sus matrículas, asignaciones, cargos e historiales, conservando su nivel, la taquilla, las fechas, los estados, los importes y los recuentos, de modo que ningún dato del curso permita identificar a una persona.

#### Scenario: Datos que se conservan
- **WHEN** se anonimiza un curso
- **THEN** siguen consultables el número de alumnos por nivel, las asignaciones por taquilla y fechas, y los importes cobrados, exentos y condonados

#### Scenario: Datos que se pierden
- **WHEN** se anonimiza un curso
- **THEN** no se puede saber qué alumno tuvo una matrícula, asignación o cargo de ese curso, ni su grupo

#### Scenario: Historiales de taquillas y llaves
- **WHEN** se anonimiza un curso
- **THEN** los eventos de taquillas, llaves y cargos de ese curso dejan de referenciar al alumno y conservan su fecha y su tipo

#### Scenario: Ficha del alumno
- **WHEN** un alumno activo tiene datos en un curso anonimizado
- **THEN** conserva su ficha y sus datos de otros cursos

### Requirement: Borrar un curso
El sistema SHALL borrar un curso cerrado eliminando sus matrículas, asignaciones, cargos e historiales, y SHALL conservar el curso como registro con su nombre, sus fechas, su registro de cierre y su decisión.

#### Scenario: Borrado
- **WHEN** se borra un curso
- **THEN** desaparecen sus matrículas, asignaciones, cargos y eventos y el curso figura como borrado con su registro de cierre

#### Scenario: Historial de taquillas
- **WHEN** se borra un curso
- **THEN** el historial de las taquillas no conserva eventos de las asignaciones de ese curso

### Requirement: Alumnos sin datos restantes
El sistema SHALL eliminar la ficha de un alumno de baja cuando, tras anonimizar o borrar un curso, no le queda ninguna matrícula, asignación o cargo identificado ni fianza vigente o por devolver, y SHALL conservar la ficha de los demás.

#### Scenario: Alumno de baja sin más datos
- **WHEN** un alumno de baja solo tenía datos en el curso anonimizado y no tiene fianza vigente
- **THEN** su ficha se elimina

#### Scenario: Alumno de baja con fianza por devolver
- **WHEN** un alumno de baja tiene la fianza por devolver
- **THEN** su ficha se conserva hasta que se resuelva

#### Scenario: Alumno activo
- **WHEN** un alumno activo tenía datos en el curso
- **THEN** su ficha se conserva

### Requirement: Salvaguardas antes de anonimizar o borrar
El sistema SHALL rechazar anonimizar o borrar un curso mientras tenga cargos pendientes o llaves entregadas o perdidas sin resolver, indicando cuáles son y cómo resolverlas, y SHALL mantener siempre sin cambios los cargos de fianza vigentes o por devolver.

#### Scenario: Cargos pendientes
- **WHEN** el curso tiene 4 cargos pendientes y el usuario decide anonimizar
- **THEN** el sistema lo rechaza indicando los 4 cargos y que hay que cobrarlos, exentarlos o condonarlos

#### Scenario: Llaves sin resolver
- **WHEN** el curso tiene llaves sin resolver y el usuario decide borrar
- **THEN** el sistema lo rechaza indicando que hay llaves por resolver y ofrece abrir la devolución masiva

#### Scenario: Fianza vigente
- **WHEN** un cargo de fianza generado en el curso sigue vigente o por devolver
- **THEN** ese cargo y la ficha de su alumno se mantienen al anonimizar o borrar

#### Scenario: Curso no cerrado
- **WHEN** el usuario intenta anonimizar o borrar un curso activo o en cierre
- **THEN** el sistema lo rechaza con un error de curso no cerrado

### Requirement: Confirmación y operación indivisible
El sistema SHALL exigir para anonimizar o borrar una confirmación explícita con los recuentos de lo que se verá afectado y la indicación de que es irreversible, SHALL ofrecer antes hacer una copia de seguridad, y SHALL aplicar la operación de forma indivisible tras revalidar.

#### Scenario: Resumen previo
- **WHEN** el usuario elige borrar un curso
- **THEN** el sistema muestra cuántas matrículas, asignaciones, cargos y fichas se eliminarán, advierte de que no se puede deshacer y no actúa hasta la confirmación

#### Scenario: Oferta de copia
- **WHEN** el usuario va a confirmar un borrado o anonimizado
- **THEN** el sistema le ofrece hacer antes una copia de seguridad, sin obligarle

#### Scenario: Cambio entre análisis y confirmación
- **WHEN** entre el resumen y la confirmación aparece un cargo pendiente en el curso
- **THEN** el sistema no aplica nada y muestra el resumen actualizado

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error durante la aplicación
- **THEN** los datos del curso quedan como antes

#### Scenario: Progreso y cancelación
- **WHEN** la operación procesa muchos registros
- **THEN** el sistema muestra el avance con recuentos y permite cancelar antes del guardado

### Requirement: Registro de la decisión
El sistema SHALL registrar cada decisión de conservación con su fecha y sus recuentos, sin datos personales, y SHALL excluir los datos de alumnos del registro técnico.

#### Scenario: Registro tras anonimizar
- **WHEN** se anonimiza un curso
- **THEN** queda registrada la decisión con la fecha y los recuentos de registros anonimizados

#### Scenario: Registro técnico
- **WHEN** falla una operación de conservación
- **THEN** el registro técnico no contiene nombres ni datos de alumnos

### Requirement: Feedback en la conservación
El sistema SHALL informar del resultado de cada decisión con recuentos y SHALL guiar al usuario cuando no hay cursos cerrados.

#### Scenario: Resultado
- **WHEN** termina un anonimizado
- **THEN** el sistema indica cuántos registros se anonimizaron y cuántas fichas se eliminaron

#### Scenario: Sin cursos cerrados
- **WHEN** el usuario abre la conservación y no hay cursos cerrados
- **THEN** el sistema lo indica y explica que se decide al cerrar un curso
