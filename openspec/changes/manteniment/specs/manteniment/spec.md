## Purpose

Permitir poner fuera de servicio y reparar de una vez grupos enteros de taquillas, con un motivo y una nota comunes, sin perder el historial individual de cada taquilla.

## ADDED Requirements

### Requirement: Selección de taquillas por zona, rango o lista
El sistema SHALL permitir seleccionar las taquillas de una operación en bloque por zona, por rango de números o eligiéndolas una a una, combinando los criterios, con un máximo de 1000 taquillas por operación.

#### Scenario: Selección por zona
- **WHEN** el usuario selecciona la zona "Planta 1"
- **THEN** quedan seleccionadas todas las taquillas activas de esa zona

#### Scenario: Selección por rango
- **WHEN** el usuario indica del 101 al 140
- **THEN** quedan seleccionadas las taquillas activas con esos números

#### Scenario: Combinación de criterios
- **WHEN** el usuario combina una zona y varias taquillas elegidas a mano
- **THEN** la selección es la unión de todas, sin duplicados

#### Scenario: Taquillas de baja
- **WHEN** la selección incluye taquillas de baja
- **THEN** se excluyen de la operación y el análisis lo indica

#### Scenario: Selección vacía
- **WHEN** ningún criterio devuelve taquillas
- **THEN** el sistema lo indica y no permite continuar

#### Scenario: Selección excesiva
- **WHEN** la selección supera las 1000 taquillas
- **THEN** el sistema lo rechaza con un error de selección excesiva

#### Scenario: Rango invertido
- **WHEN** el primer número del rango es mayor que el último
- **THEN** el sistema lo rechaza con un error de rango no válido

### Requirement: Datos comunes de la puesta fuera de servicio en bloque
El sistema SHALL pedir un tipo (averiada o en mantenimiento), un motivo de la lista de motivos activos, una nota opcional de hasta 500 caracteres y una fecha de inicio que por defecto es hoy y no puede ser futura, comunes a todas las taquillas.

#### Scenario: Datos válidos
- **WHEN** el usuario indica el tipo en mantenimiento, el motivo "Cerradura" y una nota
- **THEN** el sistema acepta los datos y continúa con el análisis

#### Scenario: Sin motivo
- **WHEN** el usuario no indica motivo
- **THEN** el sistema lo rechaza con un error de motivo obligatorio

#### Scenario: Sin nota
- **WHEN** el usuario no escribe nota
- **THEN** el sistema lo acepta

#### Scenario: Fecha futura
- **WHEN** la fecha de inicio es posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

### Requirement: Análisis previo sin efectos
El sistema SHALL analizar la selección antes de guardar y SHALL mostrar cuántas taquillas se pondrán fuera de servicio, cuántas se omiten y por qué motivo y cuántas están ocupadas, sin modificar ningún dato hasta la confirmación.

#### Scenario: Resumen del análisis
- **WHEN** el usuario completa la selección y los datos comunes
- **THEN** el sistema muestra los recuentos de taquillas que se marcarán, de las omitidas por cada motivo y de las ocupadas

#### Scenario: Taquilla ya fuera de servicio
- **WHEN** una taquilla seleccionada ya tiene una incidencia abierta
- **THEN** se omite y el análisis la lista con ese motivo

#### Scenario: Cancelar tras el análisis
- **WHEN** el usuario cancela después del análisis
- **THEN** no se modifica ningún dato

#### Scenario: Nada que marcar
- **WHEN** todas las taquillas seleccionadas se omiten
- **THEN** el sistema no ofrece la confirmación y explica por qué

### Requirement: Decisión única sobre las taquillas ocupadas
El sistema SHALL exigir, cuando la selección incluye taquillas ocupadas, una decisión común de mantener a sus alumnos o liberarlos, y SHALL no ofrecer reasignar en bloque.

#### Scenario: Selección con ocupadas sin decisión
- **WHEN** el usuario intenta confirmar una operación con taquillas ocupadas sin indicar decisión
- **THEN** el sistema no actúa y pide la decisión indicando cuántas taquillas están ocupadas

#### Scenario: Mantener a los alumnos
- **WHEN** el usuario decide mantener a los alumnos
- **THEN** las taquillas ocupadas quedan fuera de servicio conservando sus asignaciones

#### Scenario: Liberar a los alumnos
- **WHEN** el usuario decide liberar a los alumnos
- **THEN** las asignaciones se cierran, los alumnos quedan sin taquilla y las llaves entregadas quedan pendientes de devolución sin suponerlas devueltas

#### Scenario: Reasignar en bloque
- **WHEN** el usuario consulta las decisiones disponibles para las taquillas ocupadas
- **THEN** no se ofrece reasignar, y se indica que se hace taquilla a taquilla desde las incidencias

#### Scenario: Selección sin ocupadas
- **WHEN** ninguna taquilla seleccionada está ocupada
- **THEN** no se pide ninguna decisión

### Requirement: Confirmación explícita con recuentos
El sistema SHALL pedir confirmación explícita, indicando cuántas taquillas se pondrán fuera de servicio y cuántos alumnos se verán afectados si se liberan, antes de guardar.

#### Scenario: Diálogo de confirmación
- **WHEN** el usuario pulsa confirmar
- **THEN** el sistema muestra el número de taquillas, el tipo, el motivo y, si procede, el número de alumnos que se liberarán, y no guarda hasta que el usuario confirme

#### Scenario: Confirmación rechazada
- **WHEN** el usuario rechaza la confirmación
- **THEN** no se modifica ningún dato

### Requirement: Aplicación indivisible con revalidación
El sistema SHALL aplicar la puesta fuera de servicio de todas las taquillas en una única operación indivisible, revalidando contra el estado actual al confirmar.

#### Scenario: Aplicación correcta
- **WHEN** el usuario confirma un análisis válido
- **THEN** todas las taquillas indicadas quedan fuera de servicio, cada una con su propia incidencia abierta con los datos comunes, y se informa de los recuentos

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error durante la aplicación
- **THEN** no queda ninguna taquilla fuera de servicio ni ninguna asignación cerrada por la operación

#### Scenario: Datos cambiados desde el análisis
- **WHEN** entre el análisis y la confirmación una taquilla cambia de manera que deja de ser válida, como pasar a tener otra incidencia abierta
- **THEN** el sistema no guarda nada y muestra el análisis actualizado

### Requirement: Historial individual de cada taquilla
El sistema SHALL crear una incidencia independiente para cada taquilla de una operación en bloque, de modo que cada una conserve su propio historial.

#### Scenario: Historial tras una operación en bloque
- **WHEN** el usuario consulta el historial de una taquilla incluida en una operación en bloque
- **THEN** ve su incidencia con el tipo, el motivo, la nota y la fecha de inicio comunes de la operación

#### Scenario: Reparación individual posterior
- **WHEN** el usuario repara una sola taquilla de un grupo puesto fuera de servicio en bloque
- **THEN** solo esa incidencia se cierra y las demás siguen abiertas

### Requirement: Progreso y cancelación
El sistema SHALL mostrar el progreso del análisis y de la aplicación con recuentos y SHALL permitir cancelar antes del guardado.

#### Scenario: Progreso con recuentos
- **WHEN** el sistema procesa una selección grande
- **THEN** muestra el avance con recuentos y la interfaz sigue respondiendo

#### Scenario: Cancelar antes de guardar
- **WHEN** el usuario cancela antes de que empiece el guardado
- **THEN** la operación se detiene sin modificar ningún dato y se informa de que se ha cancelado

#### Scenario: Fase no cancelable
- **WHEN** la operación entra en su fase de guardado indivisible
- **THEN** el sistema deshabilita la cancelación e indica que ya no se puede cancelar

### Requirement: Localizar un grupo puesto fuera de servicio
El sistema SHALL permitir filtrar las incidencias abiertas por tipo, motivo, zona, rango de números y fecha de inicio para localizar el grupo de taquillas sobre el que actuar.

#### Scenario: Filtro por motivo y zona
- **WHEN** el usuario filtra por el motivo "Cerradura" y la zona "Planta 1"
- **THEN** ve las incidencias abiertas que cumplen ambos criterios

#### Scenario: Filtro por fecha de inicio
- **WHEN** el usuario filtra por una fecha de inicio concreta
- **THEN** ve las incidencias abiertas que empezaron ese día

#### Scenario: Sin resultados
- **WHEN** ningún filtro devuelve incidencias
- **THEN** el sistema lo indica y ofrece limpiar los filtros

### Requirement: Reparación en bloque
El sistema SHALL permitir marcar como reparadas varias incidencias abiertas a la vez a partir de una selección editable, con una fecha y una nota de resolución opcional comunes, en una operación indivisible y con confirmación explícita.

#### Scenario: Reparar un grupo
- **WHEN** el usuario selecciona las 40 incidencias filtradas, indica la fecha y confirma
- **THEN** las 40 incidencias quedan cerradas con esa fecha y nota, y las taquillas dejan de estar fuera de servicio

#### Scenario: Excluir algunas
- **WHEN** el usuario quita 5 incidencias de la selección
- **THEN** solo se reparan las demás y las 5 siguen abiertas

#### Scenario: Confirmación con recuentos
- **WHEN** el usuario inicia la reparación en bloque
- **THEN** el sistema muestra cuántas incidencias se cerrarán y no actúa hasta la confirmación

#### Scenario: Fecha anterior al inicio de alguna incidencia
- **WHEN** la fecha común es anterior al inicio de alguna de las incidencias seleccionadas
- **THEN** el análisis las marca como no reparables con esa fecha y no permite confirmar hasta que se excluyan o se cambie la fecha

#### Scenario: Fecha futura
- **WHEN** la fecha común es posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

#### Scenario: Incidencia que cambió de estado
- **WHEN** entre la selección y la confirmación una de las incidencias deja de estar abierta
- **THEN** el sistema no repara ninguna y muestra la selección actualizada

#### Scenario: Estado visible tras reparar
- **WHEN** se repara la incidencia de una taquilla que conservaba a su alumno
- **THEN** su estado visible vuelve a ser ocupada sin ninguna otra acción

### Requirement: Resultado y guía en las operaciones en bloque
El sistema SHALL informar del resultado de cada operación con sus recuentos y SHALL guiar al usuario cuando no hay taquillas que seleccionar o incidencias que reparar.

#### Scenario: Resultado de la puesta fuera de servicio
- **WHEN** termina la aplicación
- **THEN** el sistema informa de cuántas taquillas se han puesto fuera de servicio, cuántas se han omitido y cuántos alumnos se han liberado

#### Scenario: Resultado de la reparación
- **WHEN** termina la reparación en bloque
- **THEN** el sistema informa de cuántas taquillas vuelven a estar operativas

#### Scenario: Sin incidencias abiertas
- **WHEN** no hay ninguna incidencia abierta y el usuario abre la reparación en bloque
- **THEN** el sistema lo indica con un mensaje positivo en lugar de una lista vacía sin explicación

### Requirement: Privacidad de la nota
El sistema SHALL tratar la nota común como texto libre sin incluirla en listados generales ni en el registro técnico, y SHALL avisar de que no debe contener datos de alumnos.

#### Scenario: Aviso al escribir la nota
- **WHEN** el usuario escribe la nota de una operación en bloque
- **THEN** la interfaz indica que no debe incluir datos de alumnos
