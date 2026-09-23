## Purpose

Garantizar que el usuario sabe en todo momento qué está haciendo la aplicación y qué ha resultado de cada acción: pantalla de arranque con etapas reales, indicadores de trabajo, progreso, confirmaciones, mensajes comprensibles y registro técnico de los fallos.

## ADDED Requirements

### Requirement: Pantalla de arranque con etapas reales
El sistema SHALL mostrar durante el arranque una pantalla con la identidad de la aplicación y el texto de la etapa en curso, y SHALL cerrarla en cuanto la aplicación esté lista.

#### Scenario: Arranque normal
- **WHEN** la aplicación arranca
- **THEN** muestra la pantalla de arranque con las etapas reales, como abrir la base de datos, y la sustituye por la ventana principal al terminar

#### Scenario: Arranque con migración
- **WHEN** el arranque incluye una migración de esquema
- **THEN** la pantalla de arranque indica que se está actualizando la base de datos y en qué paso está

#### Scenario: Fallo de arranque
- **WHEN** una etapa del arranque falla
- **THEN** la pantalla de arranque se sustituye por un mensaje comprensible con la causa, la acción recomendada y una referencia de error, sin cerrarse de golpe

### Requirement: Resultado visible de cada acción
El sistema SHALL informar al usuario del resultado de cada acción que realiza: éxito con lo que ha ocurrido, aviso o error con su causa y qué hacer a continuación.

#### Scenario: Éxito con datos
- **WHEN** el usuario completa una operación como crear 40 taquillas
- **THEN** el sistema confirma el resultado indicando la cantidad afectada

#### Scenario: Error de negocio
- **WHEN** una regla de negocio rechaza una acción
- **THEN** el sistema muestra un mensaje en el idioma activo que explica el motivo y cómo corregirlo, sin texto técnico

#### Scenario: Aviso con éxito parcial
- **WHEN** una operación termina con algunas partes omitidas, como una importación con filas erróneas
- **THEN** el sistema informa a la vez de lo realizado y de lo omitido

#### Scenario: Error inesperado
- **WHEN** ocurre un fallo no previsto
- **THEN** el sistema muestra un mensaje genérico comprensible con una referencia de error, mantiene los datos intactos y ofrece ver los detalles técnicos bajo demanda

### Requirement: Notificaciones no bloqueantes
El sistema SHALL mostrar los resultados de éxito como notificaciones que no bloquean el trabajo y desaparecen solas, y SHALL mantener visibles los errores hasta que el usuario los cierre.

#### Scenario: Notificación de éxito
- **WHEN** una acción termina correctamente
- **THEN** aparece una notificación que desaparece sola y no impide seguir trabajando

#### Scenario: Notificación de error
- **WHEN** una acción falla
- **THEN** la notificación de error permanece hasta que el usuario la cierra

#### Scenario: Historial de la sesión
- **WHEN** el usuario consulta las notificaciones recientes
- **THEN** ve las de la sesión actual ordenadas de más reciente a más antigua

### Requirement: Indicador de trabajo sin bloquear la interfaz
El sistema SHALL mostrar un indicador de trabajo cuando una acción tarde más de 300 milisegundos, SHALL mantener la interfaz receptiva mientras dura y SHALL deshabilitar los controles de esa acción para evitar duplicados.

#### Scenario: Acción rápida
- **WHEN** una acción termina en menos de 300 milisegundos
- **THEN** no aparece ningún indicador de trabajo, para evitar parpadeos

#### Scenario: Acción lenta
- **WHEN** una acción supera los 300 milisegundos
- **THEN** aparece un indicador de trabajo y la ventana sigue respondiendo a moverse, redimensionarse y consultar otras áreas

#### Scenario: Doble ejecución
- **WHEN** el usuario pulsa dos veces seguidas el botón de una acción
- **THEN** la acción se ejecuta una sola vez

### Requirement: Progreso y cancelación de operaciones largas
El sistema SHALL mostrar el progreso con recuentos en las operaciones largas y SHALL permitir cancelarlas mientras no queden datos a medias.

#### Scenario: Progreso con recuentos
- **WHEN** una operación procesa varios elementos, como una importación
- **THEN** el sistema muestra el avance en la forma "120 de 300"

#### Scenario: Cancelar antes de guardar
- **WHEN** el usuario cancela mientras la operación aún no ha empezado a guardar
- **THEN** la operación se detiene sin modificar ningún dato y se informa de que se ha cancelado

#### Scenario: Fase no cancelable
- **WHEN** la operación entra en su fase de guardado indivisible
- **THEN** el sistema deshabilita la cancelación e indica que ya no se puede cancelar

### Requirement: Confirmación de acciones irreversibles
El sistema SHALL pedir confirmación explícita, indicando la consecuencia, antes de ejecutar una acción irreversible o masiva.

#### Scenario: Acción irreversible
- **WHEN** el usuario inicia una acción irreversible, como dar de baja una taquilla
- **THEN** el sistema pide confirmación indicando qué ocurrirá y que no se puede deshacer, y no actúa hasta que el usuario confirme

#### Scenario: Cancelar la confirmación
- **WHEN** el usuario rechaza la confirmación
- **THEN** no se modifica ningún dato

#### Scenario: Acción reversible sencilla
- **WHEN** el usuario realiza una acción reversible y de bajo impacto
- **THEN** el sistema no pide confirmación

### Requirement: Estados vacíos y de carga con guía
El sistema SHALL mostrar en las listas y pantallas un estado de carga mientras obtiene datos y un estado vacío que explique qué hacer cuando no hay elementos.

#### Scenario: Lista sin elementos
- **WHEN** una lista no contiene elementos
- **THEN** muestra un mensaje que indica por qué está vacía y cuál es el siguiente paso recomendado

#### Scenario: Carga en curso
- **WHEN** una lista todavía está obteniendo sus datos
- **THEN** muestra un estado de carga y no un estado vacío

### Requirement: Carga bajo demanda
El sistema SHALL cargar los datos bajo demanda: las listas largas se muestran de forma virtualizada o paginada y el detalle de un elemento se obtiene al abrirlo.

#### Scenario: Lista larga
- **WHEN** el usuario abre un listado de 1000 elementos
- **THEN** ve los primeros resultados sin esperar a la carga completa y puede desplazarse con fluidez

#### Scenario: Detalle bajo demanda
- **WHEN** el usuario abre el detalle de un elemento
- **THEN** los datos adicionales se cargan en ese momento y se muestra un estado de carga si tardan

### Requirement: Registro técnico local de errores
El sistema SHALL registrar localmente los errores inesperados con una referencia que el usuario pueda comunicar, SHALL limitar el tamaño del registro y SHALL excluir los datos personales.

#### Scenario: Referencia de error
- **WHEN** ocurre un error inesperado
- **THEN** el mensaje mostrado incluye una referencia que permite localizar el detalle en el registro

#### Scenario: Sin datos personales
- **WHEN** se registra un error ocurrido al procesar datos de un alumno
- **THEN** el registro no contiene nombres, apellidos, grupo ni ningún otro dato identificativo del alumno

#### Scenario: Registro acotado
- **WHEN** el registro supera el tamaño máximo establecido
- **THEN** el sistema elimina las entradas más antiguas y el registro no crece indefinidamente
