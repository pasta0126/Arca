## Purpose

Definir los informes de v1, sus columnas y filtros, y el resumen de cobros, con un catálogo pensado para ampliarse.

## ADDED Requirements

### Requirement: Catálogo de informes
El sistema SHALL ofrecer los informes de morosos, taquillas libres y averiadas, asignaciones, resumen de cobros, fianzas por devolver y llaves pendientes, y SHALL definir cada informe de forma declarativa con sus columnas, filtros y consulta, de modo que añadir uno no exija cambiar el mecanismo de exportación.

#### Scenario: Lista de informes
- **WHEN** el usuario abre los informes
- **THEN** ve los seis informes con una breve descripción

#### Scenario: Informe nuevo
- **WHEN** se declara un informe nuevo con sus columnas y su consulta
- **THEN** aparece en el catálogo y se exporta con el mismo mecanismo

### Requirement: Filtros antes de exportar
El sistema SHALL permitir aplicar a cada informe los filtros de su consulta antes de exportar y SHALL exportar exactamente las filas resultantes.

#### Scenario: Filtro por zona
- **WHEN** el usuario filtra las asignaciones por una zona y exporta
- **THEN** el fichero contiene solo las asignaciones de esa zona

#### Scenario: Recuento previo
- **WHEN** el usuario cambia los filtros
- **THEN** el sistema muestra cuántas filas se exportarán

### Requirement: Informe de morosos
El sistema SHALL exportar los alumnos con cargos pendientes con las columnas apellidos, nombre, nivel, grupo, taquilla, zona, curso, concepto, importe pendiente y si el alumno está de baja, con los filtros de la consulta de morosos.

#### Scenario: Una fila por cargo
- **WHEN** un alumno tiene la cuota y la reposición de una llave pendientes
- **THEN** el fichero tiene dos filas para ese alumno, una por cargo

#### Scenario: Alumno de baja con deuda
- **WHEN** un alumno de baja tiene una cuota pendiente
- **THEN** aparece con la columna de baja marcada

#### Scenario: Sin motivos
- **WHEN** un cargo tiene un motivo escrito por el usuario
- **THEN** el motivo no aparece en el fichero

### Requirement: Informe de taquillas libres y averiadas
El sistema SHALL exportar las taquillas con las columnas zona, número, estado y motivo de la incidencia abierta, filtrables por zona y por estado (libre, avariada, en mantenimiento, reservada).

#### Scenario: Solo averiadas
- **WHEN** el usuario filtra por avariada y exporta
- **THEN** el fichero contiene las taquillas averiadas con su motivo de la lista

#### Scenario: Sin notas
- **WHEN** una taquilla o su incidencia tiene una nota libre
- **THEN** la nota no aparece en el fichero

### Requirement: Informe de asignaciones
El sistema SHALL exportar las asignaciones de un curso con las columnas zona, taquilla, apellidos, nombre, nivel, grupo, estado de la llave y si el alumno está al corriente de pago, filtrables por curso, zona, nivel y grupo, con el curso activo por defecto.

#### Scenario: Por grupo
- **WHEN** el usuario filtra por un grupo y exporta
- **THEN** el fichero contiene las asignaciones de los alumnos de ese grupo

#### Scenario: Curso anonimizado
- **WHEN** el usuario exporta un curso anonimizado
- **THEN** el fichero contiene zona, taquilla, nivel y estados, con los datos del alumno vacíos

### Requirement: Resumen de cobros
El sistema SHALL ofrecer una consulta de resumen de cobros por curso y concepto con el número de cargos y el importe total en cada estado (pendiente, pagado, exento, condonado y anulado), sin datos de alumnos, y exportarla.

#### Scenario: Resumen del curso
- **WHEN** el usuario abre el resumen del curso activo
- **THEN** ve por cada concepto cuántos cargos e importe hay pagados, pendientes, exentos, condonados y anulados

#### Scenario: Curso anonimizado
- **WHEN** el usuario abre el resumen de un curso anonimizado
- **THEN** los totales se conservan igual que antes de anonimizar

#### Scenario: Curso sin cargos
- **WHEN** el curso no tiene cargos
- **THEN** el sistema lo indica y no ofrece exportar

### Requirement: Informe de fianzas por devolver
El sistema SHALL exportar la lista de fianzas por devolver con las columnas apellidos, nombre, importe, curso de generación y fecha de baja, con los filtros de esa lista.

#### Scenario: Fianzas por devolver
- **WHEN** hay 20 fianzas por devolver y el usuario exporta
- **THEN** el fichero tiene 20 filas con su importe

### Requirement: Informe de llaves pendientes
El sistema SHALL exportar las llaves entregadas de asignaciones cerradas y las perdidas sin resolver con las columnas zona, taquilla, apellidos, nombre, curso, estado de la llave y fecha de cierre de la asignación, con los filtros de esa lista.

#### Scenario: Llaves pendientes tras el cierre
- **WHEN** hay 12 llaves sin resolver de un curso en cierre y el usuario exporta
- **THEN** el fichero tiene 12 filas ordenadas por fecha de cierre

### Requirement: Orden y consistencia
El sistema SHALL ordenar cada informe de forma estable y determinista, por apellidos y nombre cuando hay alumnos y por zona y número de taquilla en los demás, y SHALL producir la misma salida para los mismos datos y filtros.

#### Scenario: Exportaciones repetidas
- **WHEN** el usuario exporta dos veces el mismo informe sin cambios de datos
- **THEN** ambos ficheros tienen el mismo contenido y orden
