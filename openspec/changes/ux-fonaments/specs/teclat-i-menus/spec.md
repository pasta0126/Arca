## Purpose

Garantizar que toda la aplicación se puede usar solo con teclado y con ratón, con un conjunto pequeño y coherente de atajos y menús equivalentes a los botones.

## ADDED Requirements

### Requirement: Operación completa con teclado
El sistema SHALL permitir realizar cualquier acción de la aplicación solo con el teclado, con un orden de foco lógico y coherente con el orden visual.

#### Scenario: Recorrer un formulario
- **WHEN** el usuario pulsa Tab en un formulario
- **THEN** el foco recorre los campos en el orden en que se leen y termina en la acción principal

#### Scenario: Acción sin ratón
- **WHEN** el usuario quiere asignar una taquilla sin usar el ratón
- **THEN** puede hacerlo con el teclado hasta la confirmación

### Requirement: Foco visible
El sistema SHALL mostrar siempre de forma visible qué control tiene el foco, en el tema claro y en el oscuro.

#### Scenario: Foco en un botón
- **WHEN** un botón recibe el foco por teclado
- **THEN** se ve un indicador de foco claramente distinguible

#### Scenario: Foco tras cerrar un diálogo
- **WHEN** se cierra un diálogo
- **THEN** el foco vuelve al control que lo abrió

### Requirement: Conjunto fijo de atajos
El sistema SHALL ofrecer un conjunto pequeño y fijo de atajos iguales en todas las pantallas: buscar, nuevo, confirmar, cancelar y ayuda, con Control en Windows y Linux y Comando en macOS.

#### Scenario: Buscar
- **WHEN** el usuario pulsa Control+F en Windows o Comando+F en macOS
- **THEN** el foco pasa al cuadro de búsqueda de la pantalla

#### Scenario: Cancelar
- **WHEN** el usuario pulsa Escape en un diálogo o en un asistente
- **THEN** se cancela la operación en curso sin cambiar datos

#### Scenario: Atajo no aplicable
- **WHEN** el usuario pulsa un atajo que no tiene sentido en la pantalla actual
- **THEN** no ocurre nada y no se muestra ningún error

### Requirement: Atajos visibles
El sistema SHALL mostrar el atajo junto a cada acción que lo tiene, en los menús y en las descripciones emergentes de los botones.

#### Scenario: Menú con atajos
- **WHEN** el usuario abre un menú
- **THEN** cada acción con atajo lo muestra a su lado

### Requirement: Menús contextuales equivalentes
El sistema SHALL ofrecer en los elementos de una lista un menú contextual con las mismas acciones que los botones de la pantalla, abierto con el botón derecho o con la tecla de menú, con las acciones no disponibles deshabilitadas y explicadas.

#### Scenario: Menú de una taquilla
- **WHEN** el usuario abre el menú contextual de una taquilla
- **THEN** ve las acciones aplicables y las no disponibles deshabilitadas con su motivo

#### Scenario: Con el teclado
- **WHEN** el usuario pulsa la tecla de menú con un elemento seleccionado
- **THEN** se abre el mismo menú contextual

### Requirement: Acciones y estados coherentes
El sistema SHALL mantener el mismo nombre, orden y atajo para una misma acción en todas las pantallas y SHALL deshabilitar con explicación las acciones que no proceden en el estado actual.

#### Scenario: Acción no disponible
- **WHEN** una acción no procede porque la taquilla está averiada
- **THEN** aparece deshabilitada y una descripción emergente indica el motivo

#### Scenario: Licencia en solo lectura
- **WHEN** la licencia está en solo lectura
- **THEN** las acciones que modifican datos aparecen deshabilitadas con el motivo de licencia y cómo activarla, sin depender de que el usuario las ejecute para descubrirlo
