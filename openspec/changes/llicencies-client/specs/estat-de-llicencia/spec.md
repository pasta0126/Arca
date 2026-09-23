## Purpose

Definir los estados de la licencia, sus plazos y qué se puede hacer en cada uno, garantizando que exportar, copiar y restaurar estén siempre disponibles.

## ADDED Requirements

### Requirement: Estados de la licencia
El sistema SHALL calcular el estado de la licencia como uno de prueba, activa, en gracia o solo lectura, mediante una función pura de la clave, la revocación, la fecha de inicio de la prueba, la fecha actual y la última fecha vista.

#### Scenario: Clave vigente
- **WHEN** hay una clave válida con vencimiento futuro y sin revocación
- **THEN** el estado es activa

#### Scenario: Sin clave y dentro de la prueba
- **WHEN** no hay clave y no han pasado 30 días desde la primera ejecución
- **THEN** el estado es prueba

### Requirement: Período de prueba
El sistema SHALL ofrecer uso completo durante 30 días naturales desde la primera ejecución sin clave y, al acabar, SHALL pasar a solo lectura sin período de gracia. La duración SHALL ser una única constante configurable.

#### Scenario: Primera ejecución
- **WHEN** el usuario abre ARCA por primera vez sin clave
- **THEN** se registra la fecha de inicio y el estado es prueba con 30 días restantes

#### Scenario: Prueba terminada
- **WHEN** pasan más de 30 días desde el inicio de la prueba sin clave
- **THEN** el estado es solo lectura

#### Scenario: Activar durante la prueba
- **WHEN** el usuario activa una clave durante la prueba
- **THEN** el estado pasa a activa

### Requirement: Período de gracia
El sistema SHALL mantener el uso completo durante 30 días naturales desde el día siguiente al vencimiento o desde la recepción de una revocación y, al acabar, SHALL pasar a solo lectura. La duración SHALL ser una única constante configurable.

#### Scenario: Licencia caducada
- **WHEN** la clave venció ayer
- **THEN** el estado es en gracia con 29 días restantes

#### Scenario: Gracia agotada
- **WHEN** pasan más de 30 días desde el vencimiento sin renovar
- **THEN** el estado es solo lectura

#### Scenario: Licencia revocada
- **WHEN** el sistema recibe una revocación válida
- **THEN** el estado es en gracia desde ese día, aunque la clave no haya vencido

### Requirement: Avisos en la gracia y la prueba
El sistema SHALL mostrar en cada arranque, mientras el estado sea en gracia o prueba, un aviso no bloqueante con los días restantes y cómo renovar o activar, y SHALL mantener un indicador permanente con esos días restantes.

#### Scenario: Aviso al abrir
- **WHEN** el usuario abre la aplicación en estado en gracia
- **THEN** ve un aviso con los días restantes que se puede cerrar sin bloquear el trabajo

#### Scenario: Indicador permanente
- **WHEN** el estado es prueba o en gracia
- **THEN** la interfaz muestra los días restantes en todo momento

#### Scenario: Licencia activa
- **WHEN** el estado es activa
- **THEN** no se muestra ningún aviso ni indicador de gracia

### Requirement: Solo lectura
El sistema SHALL permitir en solo lectura consultar cualquier dato, exportar, hacer copia de seguridad, restaurar una copia y activar o renovar la licencia, y SHALL rechazar cualquier otra operación que modifique datos con un error de licencia que indica cómo reactivarla.

#### Scenario: Consulta
- **WHEN** el estado es solo lectura y el usuario busca un alumno
- **THEN** ve sus datos con normalidad

#### Scenario: Escritura bloqueada
- **WHEN** el estado es solo lectura y el usuario intenta asignar una taquilla, registrar un pago, devolver una llave, importar alumnos o cerrar un curso
- **THEN** el sistema lo rechaza con un error de licencia que explica cómo activarla

#### Scenario: Siempre disponibles
- **WHEN** el estado es solo lectura
- **THEN** el usuario puede exportar cualquier informe, hacer una copia y restaurar una copia

#### Scenario: Reactivar
- **WHEN** el usuario activa una clave válida en solo lectura
- **THEN** el estado pasa a activa y las escrituras vuelven a permitirse sin reiniciar

### Requirement: Comprobación en la capa de aplicación
El sistema SHALL aplicar la restricción de solo lectura en los casos de uso de la capa de aplicación y no solo en la interfaz, clasificando cada caso de uso como de escritura o siempre disponible.

#### Scenario: Caso de uso nuevo sin clasificar
- **WHEN** se añade un caso de uso que escribe datos sin clasificarlo
- **THEN** una prueba automática falla

#### Scenario: Acceso sin interfaz
- **WHEN** un caso de uso de escritura se invoca directamente con el estado en solo lectura
- **THEN** se rechaza igual que desde la interfaz

### Requirement: Migraciones y arranque no bloqueados
El sistema SHALL abrir la base de datos y aplicar sus migraciones de esquema en cualquier estado de la licencia.

#### Scenario: Actualización con licencia caducada
- **WHEN** el usuario actualiza ARCA y la licencia está en solo lectura
- **THEN** la base de datos se migra con normalidad y sigue en solo lectura

### Requirement: Operación en curso
El sistema SHALL evaluar el estado al iniciar cada operación y SHALL no interrumpir una operación ya iniciada por un cambio de estado.

#### Scenario: Cambio de estado a mitad
- **WHEN** el estado pasa a solo lectura durante una importación ya confirmada
- **THEN** la importación termina, y las siguientes operaciones de escritura se rechazan

### Requirement: Protección básica del reloj
El sistema SHALL guardar la última fecha vista y SHALL usar como fecha actual la mayor entre la del reloj y la última fecha vista, de modo que retrasar el reloj no reactive una licencia vencida.

#### Scenario: Reloj atrasado
- **WHEN** la última fecha vista es posterior a la del reloj
- **THEN** el estado se calcula con la última fecha vista

#### Scenario: Reloj normal
- **WHEN** la fecha del reloj es posterior a la última vista
- **THEN** se actualiza la última fecha vista

### Requirement: Feedback del estado
El sistema SHALL explicar el estado en términos comprensibles, indicando qué está permitido, qué no y qué hacer para reactivar la licencia, sin bloquear la interfaz.

#### Scenario: Explicación del solo lectura
- **WHEN** el usuario consulta el estado en solo lectura
- **THEN** ve que puede consultar, exportar, copiar y restaurar, que no puede modificar datos y cómo activar una clave
