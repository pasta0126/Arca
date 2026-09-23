## Purpose

Ofrecer una lista con orden, filtro y selección editable para las operaciones masivas, y un componente de pasos con estado reutilizable por los asistentes de la aplicación.

## ADDED Requirements

### Requirement: Lista virtualizada con orden y filtro
El sistema SHALL ofrecer una lista que virtualice sus filas, permita ordenar por columnas y filtrar y conserve la selección al ordenar o filtrar.

#### Scenario: Lista larga
- **WHEN** una lista tiene 300 filas
- **THEN** solo se crean las filas visibles y el desplazamiento es fluido

#### Scenario: Orden por columna
- **WHEN** el usuario pulsa la cabecera de una columna
- **THEN** la lista se ordena por ella y otra pulsación invierte el orden

#### Scenario: Selección y filtro
- **WHEN** el usuario selecciona filas y después filtra
- **THEN** las selecciones de las filas ocultas se conservan y se cuentan

### Requirement: Selección múltiple editable
El sistema SHALL ofrecer selección múltiple con seleccionar todo, quitar todo, invertir y marcar o desmarcar filas individuales con ratón y teclado, y SHALL mostrar en todo momento el recuento seleccionado sobre el total.

#### Scenario: Recuento
- **WHEN** hay 300 filas y el usuario desmarca 5
- **THEN** el recuento muestra "295 de 300"

#### Scenario: Seleccionar con el teclado
- **WHEN** el usuario pulsa la barra espaciadora sobre una fila
- **THEN** alterna su selección

### Requirement: Vista de plan para operaciones masivas
El sistema SHALL ofrecer una vista de plan para las operaciones masivas en dos fases que muestre los elementos propuestos con su selección editable, los recuentos y los bloqueos, y que exija la confirmación explícita antes de aplicar.

#### Scenario: Plan propuesto
- **WHEN** el análisis de una operación masiva devuelve un plan
- **THEN** la vista muestra los elementos seleccionados por defecto, el recuento y las advertencias

#### Scenario: Plan que cambia
- **WHEN** la confirmación devuelve que el plan cambió
- **THEN** la vista muestra la selección actualizada y no aplica nada

#### Scenario: Elementos bloqueados
- **WHEN** el plan contiene elementos que no se pueden aplicar
- **THEN** aparecen marcados con su motivo y no se pueden seleccionar

### Requirement: Componente de pasos
El sistema SHALL ofrecer un componente de pasos que muestre una lista ordenada con título, explicación y estado (pendiente, hecho u omitido) de cada uno, el progreso "N de M" y la sugerencia del siguiente paso pendiente, alimentado por las definiciones de pasos de Application.

#### Scenario: Estado de los pasos
- **WHEN** el asistente tiene tres pasos hechos de seis
- **THEN** el componente muestra "3 de 6", el estado de cada paso y resalta el siguiente pendiente

#### Scenario: Abrir cualquier paso
- **WHEN** el usuario elige un paso
- **THEN** se abre sin exigir el orden, con aviso si depende de otro pendiente

#### Scenario: Omitir un paso
- **WHEN** el usuario omite un paso opcional
- **THEN** el componente lo marca como omitido, y un paso obligatorio no ofrece omitir

### Requirement: Un mismo componente para cierre y configuración
El sistema SHALL usar el mismo componente de pasos para el asistente de cierre de curso y para la configuración guiada, sin lógica propia de ninguno.

#### Scenario: Reutilización
- **WHEN** se abre el asistente de cierre o la configuración guiada
- **THEN** ambos usan el mismo componente con sus definiciones de pasos
