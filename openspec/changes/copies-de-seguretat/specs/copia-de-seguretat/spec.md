## Purpose

Permitir al usuario hacer una copia de seguridad manual de todos los datos, consistente, verificada y sin dejar ficheros a medias.

## ADDED Requirements

### Requirement: Copia manual de la base de datos
El sistema SHALL permitir hacer una copia de seguridad como un único fichero que contiene todos los datos de la aplicación, cifrado igual que la base de datos, sin interrumpir el uso de la aplicación.

#### Scenario: Copia correcta
- **WHEN** el usuario elige una carpeta y hace la copia
- **THEN** se crea un fichero con todos los datos actuales y el sistema informa de la ubicación y del tamaño

#### Scenario: Sin datos en claro
- **WHEN** se abre el fichero de copia con un editor de texto
- **THEN** su contenido no es legible

### Requirement: Copia consistente
El sistema SHALL generar la copia como una instantánea coherente de la base de datos, aunque haya cambios recientes, y SHALL incluir todas las operaciones ya confirmadas.

#### Scenario: Copia tras un cambio
- **WHEN** el usuario asigna una taquilla y hace la copia justo después
- **THEN** la copia incluye esa asignación

### Requirement: Verificación de la copia
El sistema SHALL comprobar la integridad de la copia y que se puede abrir con la clave de la aplicación antes de darla por buena.

#### Scenario: Copia verificada
- **WHEN** termina la copia
- **THEN** el sistema comprueba su integridad y solo entonces indica que es correcta

#### Scenario: Copia que no supera la verificación
- **WHEN** la copia no supera la comprobación
- **THEN** el sistema la elimina, indica que no se ha podido hacer una copia válida y no deja fichero en el destino

### Requirement: Nombre y destino de la copia
El sistema SHALL proponer un nombre que incluya la fecha y la hora, permitir elegir la carpeta, recordar la última carpeta usada y pedir confirmación antes de sobrescribir un fichero existente.

#### Scenario: Nombre propuesto
- **WHEN** el usuario hace una copia el 24 de septiembre de 2026 a las 10:30
- **THEN** el nombre propuesto incluye esa fecha y hora y una extensión propia de ARCA

#### Scenario: Última carpeta
- **WHEN** el usuario hace una segunda copia
- **THEN** el sistema propone la carpeta de la copia anterior

#### Scenario: Fichero existente
- **WHEN** ya existe un fichero con ese nombre en el destino
- **THEN** el sistema pide confirmar la sobrescritura y no actúa hasta entonces

### Requirement: Destino no válido
El sistema SHALL rechazar como destino el propio fichero de la base de datos, una carpeta sin permisos y un destino sin espacio suficiente, con un mensaje comprensible.

#### Scenario: Destino igual a la base de datos
- **WHEN** el usuario elige como destino el fichero de la base de datos
- **THEN** el sistema lo rechaza con un error y no modifica nada

#### Scenario: Sin espacio
- **WHEN** el destino no tiene espacio suficiente
- **THEN** el sistema lo indica antes de empezar y sugiere elegir otra ubicación

#### Scenario: Sin permisos
- **WHEN** la carpeta no admite escritura
- **THEN** el sistema lo indica con un mensaje claro y sugiere elegir otra carpeta

### Requirement: Escritura atómica de la copia
El sistema SHALL escribir la copia en un fichero temporal en el destino y moverlo al nombre final solo tras verificarla, de modo que un error o una cancelación no dejen un fichero incompleto ni destruyan uno anterior.

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error durante la copia
- **THEN** no queda ningún fichero parcial y el fichero anterior del mismo nombre sigue intacto

#### Scenario: Cancelación
- **WHEN** el usuario cancela una copia en curso
- **THEN** no se crea ni se modifica ningún fichero y la base de datos no cambia

### Requirement: Copia siempre disponible
El sistema SHALL permitir hacer una copia en cualquier estado de la licencia, incluidos el período de gracia y el modo de solo lectura, y sin conexión a la red.

#### Scenario: Licencia caducada
- **WHEN** la licencia está en modo de solo lectura
- **THEN** el usuario puede hacer una copia

### Requirement: Solo manual y sin datos de última copia
El sistema SHALL hacer copias únicamente cuando el usuario lo pide, SHALL no programarlas ni recordarlas y SHALL no registrar ni mostrar la fecha de la última copia.

#### Scenario: Sin automatismos
- **WHEN** pasan semanas sin que el usuario haga una copia
- **THEN** el sistema no hace ninguna copia ni muestra ningún aviso

### Requirement: Aviso sobre datos de menores
El sistema SHALL avisar antes de hacer la copia de que contiene todos los datos de los alumnos y de que su custodia corresponde al centro.

#### Scenario: Aviso previo
- **WHEN** el usuario inicia una copia
- **THEN** el sistema muestra el aviso y continúa tras la confirmación

### Requirement: Feedback y progreso de la copia
El sistema SHALL informar del resultado de la copia, mostrar el progreso en copias largas sin bloquear la interfaz, permitir cancelar antes de terminar y evitar copias duplicadas por un doble clic, sin incluir datos de alumnos en el registro técnico.

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces hacer copia
- **THEN** se hace una sola copia

#### Scenario: Error comprensible
- **WHEN** falla la copia
- **THEN** el sistema muestra un mensaje claro en el idioma activo, sin detalles técnicos, y el registro técnico no contiene datos de alumnos
