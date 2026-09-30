## MODIFIED Requirements

### Requirement: Nomenclatura visible de la deuda
El sistema SHALL usar en la interfaz «Pendents de pagament» y nunca «morosos», siguiendo `docs/glosario.md`, en el filtro de la lista de Alumnos, en la ficha y en los indicadores.

#### Scenario: Título de la vista
- **WHEN** el usuario abre los filtros de Alumnos
- **THEN** el filtro de deuda usa la expresión del glosario

#### Scenario: Ficha
- **WHEN** el usuario abre la ficha de un alumno con cargos pendientes
- **THEN** los títulos y etiquetas usan la expresión del glosario

## REMOVED Requirements

### Requirement: Organización de la sección Cobros
**Reason**: ARCA no es una aplicación de contabilidad; los cobros pertenecen al alumno y a su taquilla, y una sección aparte duplicaba la ficha.
**Migration**: Los cargos de un alumno y sus acciones se ven y se ejecutan desde su ficha en Alumnos (`pantalles-alumnes-i-assignacions`, Ficha del alumno). La búsqueda de un alumno para ver sus cargos es la búsqueda de la lista de Alumnos.

### Requirement: Consulta de morosos
**Reason**: No hace falta una consulta con importes totales; basta saber qué alumnos tienen algo pendiente.
**Migration**: El filtro de pagos pendientes de la lista de Alumnos (`pantalles-alumnes-i-assignacions`, Lista de alumnos con búsqueda y filtros) muestra esos alumnos con su recuento, sin importes globales. El desglose de la deuda de un alumno está en los cargos pendientes de su ficha.
