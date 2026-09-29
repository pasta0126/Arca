## Purpose

Definir la interfaz de la sección Curso en el hito 1: la lista de cursos escolares, crear y activar un curso y definir los importes de cada concepto, sobre las reglas de `alumnes-i-assignacions` y `pagaments`.

## ADDED Requirements

### Requirement: Lista de cursos
El sistema SHALL mostrar en la sección Curso la lista de cursos escolares, del más reciente al más antiguo, con su nombre, fechas y estado (activo, en cierre o cerrado), y SHALL destacar el curso activo.

#### Scenario: Lista con curso activo
- **WHEN** el usuario abre la sección Curso y existe el curso 2026-2027 activo y el 2025-2026 cerrado
- **THEN** ve ambos ordenados de más reciente a más antiguo, con el activo destacado y cada estado indicado con texto y no solo con color

#### Scenario: Sin cursos
- **WHEN** no existe ningún curso
- **THEN** la sección muestra un estado vacío que explica que hace falta un curso para trabajar y ofrece crear el primero

### Requirement: Crear un curso desde un formulario
El sistema SHALL ofrecer la acción Nuevo curso, con un formulario de año de inicio y fechas de inicio y de fin, que muestra el nombre derivado ("2026-2027") mientras se escribe y valida al guardar con los mensajes de las reglas de `alumnes-i-assignacions`.

#### Scenario: Nombre derivado
- **WHEN** el usuario indica 2026 como año de inicio
- **THEN** el formulario muestra el nombre 2026-2027 antes de guardar

#### Scenario: Fechas no válidas
- **WHEN** el usuario indica una fecha de fin anterior a la de inicio
- **THEN** el formulario marca el campo y explica el problema sin cerrarse ni perder lo escrito

#### Scenario: Curso creado
- **WHEN** el usuario guarda un curso válido
- **THEN** aparece en la lista, se confirma con una notificación y se ofrece definir sus importes

### Requirement: Activar un curso
El sistema SHALL ofrecer activar un curso desde su detalle solo cuando no hay otro activo, con una confirmación que indique qué cambia, y SHALL mostrar la acción deshabilitada con su motivo cuando ya hay un curso activo.

#### Scenario: Activar sin curso activo
- **WHEN** el usuario elige Activar en un curso y no hay otro activo
- **THEN** el sistema pide confirmación indicando que las altas, matrículas y asignaciones se harán en ese curso y, al confirmar, lo marca como activo y actualiza la cabecera

#### Scenario: Ya hay un curso activo
- **WHEN** el usuario mira otro curso mientras hay uno activo
- **THEN** Activar aparece deshabilitada con el motivo de que ya hay un curso activo

### Requirement: Eliminar un curso vacío
El sistema SHALL ofrecer eliminar un curso solo cuando no tiene matrículas ni asignaciones, con confirmación que indique que es irreversible, y SHALL mostrar la acción deshabilitada con su motivo en los demás casos.

#### Scenario: Curso vacío
- **WHEN** el usuario elimina un curso sin matrículas ni asignaciones y confirma
- **THEN** el curso desaparece de la lista y se notifica

#### Scenario: Curso con datos
- **WHEN** el curso tiene matrículas o asignaciones
- **THEN** Eliminar aparece deshabilitada con el motivo

### Requirement: Detalle del curso de solo lectura si no está activo
El sistema SHALL mostrar el detalle de un curso no activo como consulta, sin acciones que modifiquen sus matrículas ni asignaciones, e indicando que es histórico.

#### Scenario: Curso cerrado
- **WHEN** el usuario abre un curso cerrado
- **THEN** ve sus datos e importes como consulta y ninguna acción de modificación de matrículas o asignaciones

### Requirement: Importes del curso
El sistema SHALL mostrar en el detalle de cada curso un formulario con el importe de la cuota, de la fianza y de la reposición de llave, en euros con dos decimales según la cultura activa, que propone los del curso anterior cuando el curso no tiene importes y solo los guarda al confirmarlos.

#### Scenario: Propuesta heredada
- **WHEN** el usuario abre los importes de un curso sin importes y existe un curso anterior con importes
- **THEN** el formulario muestra esos valores como propuesta, indicando de qué curso vienen, y no guarda nada hasta que el usuario confirma

#### Scenario: Primer curso
- **WHEN** no hay ningún curso anterior con importes
- **THEN** los campos aparecen vacíos con una ayuda que indica el rango válido

#### Scenario: Importe no válido
- **WHEN** el usuario escribe un importe cero, negativo, con más de dos decimales o superior a 9999,99
- **THEN** el campo se marca y explica el error, y el formulario no se puede guardar

#### Scenario: Coma decimal
- **WHEN** el usuario escribe 50,5 en la cultura catalana
- **THEN** el campo lo acepta como 50,50 euros

### Requirement: Aviso del efecto de cambiar un importe
El sistema SHALL avisar, antes de guardar un cambio de importe en un curso que ya tiene cargos, de que solo afectará a los cargos que se generen después.

#### Scenario: Cambio con cargos existentes
- **WHEN** el usuario cambia la cuota de un curso con cargos generados y guarda
- **THEN** el sistema muestra el aviso y guarda solo al confirmar

#### Scenario: Historial de importes
- **WHEN** el usuario abre el historial de importes de un curso
- **THEN** ve cada cambio con el valor anterior, el nuevo y el instante

### Requirement: Curso sin importes visible
El sistema SHALL señalar en la lista de cursos y en la cabecera del curso activo que faltan sus importes, con un acceso directo a definirlos.

#### Scenario: Curso activo sin importes
- **WHEN** el curso activo no tiene importes definidos
- **THEN** la sección Curso muestra un aviso con la acción Definir importes y explica que sin ellos no se generan cargos

### Requirement: Feedback y doble ejecución en la sección Curso
El sistema SHALL confirmar el resultado de cada operación con una notificación, mostrar el indicador de trabajo si tarda y proteger de la doble ejecución los guardados y las activaciones.

#### Scenario: Doble clic en guardar
- **WHEN** el usuario hace doble clic en Guardar importes
- **THEN** se guarda una sola vez y se muestra una sola notificación
