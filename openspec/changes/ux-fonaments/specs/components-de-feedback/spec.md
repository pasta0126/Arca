## Purpose

Ofrecer los componentes y reglas comunes con los que toda pantalla comunica resultados, pide confirmaciones y muestra trabajo en curso, de forma uniforme y sin bloquear la interfaz.

## ADDED Requirements

### Requirement: Notificaciones de resultado
El sistema SHALL ofrecer un servicio de notificaciones que muestre el resultado estructurado de una acción como éxito, aviso o error, con el texto del idioma activo obtenido de claves de recurso, sin bloquear la interfaz.

#### Scenario: Éxito
- **WHEN** una acción termina correctamente
- **THEN** se muestra una notificación de éxito con lo ocurrido, que desaparece sola pasados unos segundos

#### Scenario: Error
- **WHEN** una acción falla con un error de negocio
- **THEN** se muestra una notificación de error con la causa y qué hacer, que permanece hasta que el usuario la cierra

#### Scenario: Aviso
- **WHEN** una acción termina con un aviso
- **THEN** se muestra una notificación de aviso que permanece hasta que el usuario la cierra

#### Scenario: Varias notificaciones
- **WHEN** se producen varias notificaciones seguidas
- **THEN** se apilan sin taparse y las de éxito desaparecen por separado

#### Scenario: Error inesperado
- **WHEN** ocurre un error no previsto
- **THEN** se muestra un mensaje genérico comprensible con la referencia del registro técnico, sin datos de alumnos, y se ofrece ver los detalles técnicos bajo demanda

### Requirement: Historial de notificaciones de la sesión
El sistema SHALL permitir consultar las notificaciones de la sesión actual ordenadas de más reciente a más antigua, incluidas las ya desaparecidas.

#### Scenario: Consultar el historial
- **WHEN** el usuario abre el historial de notificaciones
- **THEN** ve las de la sesión, también las de éxito que ya desaparecieron, de más reciente a más antigua

#### Scenario: Nueva sesión
- **WHEN** el usuario reabre la aplicación
- **THEN** el historial empieza vacío y no conserva datos de la sesión anterior

### Requirement: Diálogo de confirmación con consecuencia
El sistema SHALL ofrecer un diálogo de confirmación que indique la acción, su consecuencia y, si procede, los recuentos afectados, con botones de confirmar y cancelar rotulados con la acción, y con el foco inicial en cancelar cuando la acción es irreversible.

#### Scenario: Acción irreversible
- **WHEN** el usuario inicia una acción irreversible
- **THEN** el diálogo muestra la consecuencia y el recuento, marca la acción como destructiva y tiene el foco inicial en cancelar

#### Scenario: Cancelar con Escape
- **WHEN** el usuario pulsa Escape en el diálogo
- **THEN** se cancela la acción y no cambia nada

#### Scenario: Confirmar con el teclado
- **WHEN** el usuario mueve el foco al botón de confirmar y pulsa Intro
- **THEN** se ejecuta la acción

#### Scenario: Un solo diálogo
- **WHEN** ya hay un diálogo de confirmación abierto
- **THEN** no se abre otro para la misma acción

### Requirement: Comando protegido contra doble ejecución
El sistema SHALL ofrecer un comando asíncrono que se deshabilita mientras se ejecuta y que ignora las invocaciones repetidas durante ese tiempo, para todas las acciones que modifican datos.

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces un botón que ejecuta una acción
- **THEN** la acción se ejecuta una sola vez

#### Scenario: Fin de la ejecución
- **WHEN** la acción termina, con éxito o con error
- **THEN** el comando vuelve a estar disponible

### Requirement: Indicador de trabajo y progreso
El sistema SHALL mostrar un indicador de trabajo cuando una acción tarda más de 300 milisegundos y, en las operaciones largas con recuento, un progreso con la forma "120 de 300", con cancelación solo cuando el resultado lo permita, sin bloquear la interfaz.

#### Scenario: Acción rápida
- **WHEN** una acción termina en menos de 300 milisegundos
- **THEN** no aparece ningún indicador

#### Scenario: Acción larga
- **WHEN** una acción tarda más de 300 milisegundos
- **THEN** aparece el indicador de trabajo y los controles de esa acción quedan deshabilitados

#### Scenario: Progreso con recuentos
- **WHEN** una operación procesa 300 elementos y lleva 120
- **THEN** el progreso muestra "120 de 300"

#### Scenario: Cancelación no disponible
- **WHEN** la operación ya no puede cancelarse sin dejar datos a medias
- **THEN** el botón de cancelar está deshabilitado y lo explica

### Requirement: Estados vacíos y de carga
El sistema SHALL ofrecer un componente de estado de carga y otro de estado vacío que explique qué hacer a continuación y ofrezca la acción principal cuando exista.

#### Scenario: Carga
- **WHEN** una lista está obteniendo datos
- **THEN** muestra el estado de carga y no un estado vacío

#### Scenario: Lista vacía
- **WHEN** una lista no tiene elementos
- **THEN** muestra un mensaje que explica qué hacer y, si procede, un botón con la acción para crear el primero

#### Scenario: Filtro sin resultados
- **WHEN** un filtro no devuelve elementos
- **THEN** el mensaje lo indica y ofrece limpiar el filtro

### Requirement: Mensajes desde códigos estables
El sistema SHALL traducir los códigos de error estables y los resultados estructurados de Application a mensajes de recurso, y SHALL mostrar la propia clave si falta el recurso.

#### Scenario: Código conocido
- **WHEN** Application devuelve un código de error con parámetros
- **THEN** el mensaje se compone con su recurso y los parámetros, sin construir texto en Application

#### Scenario: Recurso ausente
- **WHEN** falta el recurso de un mensaje
- **THEN** se muestra la clave y se registra en el registro técnico

### Requirement: Componentes sin colores ni textos propios
El sistema SHALL obtener de recursos de tema los colores y tipografías de los componentes y de claves de recurso todos sus textos, sin valores literales.

#### Scenario: Cambio de tema
- **WHEN** `ui-shell` define otro conjunto de recursos de tema
- **THEN** los componentes adoptan los nuevos valores sin cambios en su código
