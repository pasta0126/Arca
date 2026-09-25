# clau-de-recuperacio Specification

## Purpose
Evitar la pérdida de datos por olvidar la contraseña: una segunda llave, larga y generada por el sistema, que el centro guarda aparte.

## Requirements

### Requirement: Generación de la clave de recuperación
El sistema SHALL generar al crear la contraseña una clave de recuperación aleatoria de al menos 128 bits, escrita en un alfabeto sin caracteres ambiguos y agrupada para poder leerla y copiarla.

#### Scenario: Formato
- **WHEN** se genera la clave de recuperación
- **THEN** es un código de 26 caracteres en cinco grupos (cuatro de 5 y el último de 6), por ejemplo `K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS`

#### Scenario: Aleatoria
- **WHEN** se generan dos claves
- **THEN** son distintas y no derivan de la contraseña ni del equipo

### Requirement: Mostrarla una sola vez y confirmar que se ha guardado
El sistema SHALL mostrar la clave de recuperación una sola vez tras crear la contraseña, con opciones de imprimirla o copiarla, y SHALL exigir al usuario que la confirme escribiendo dos de sus grupos antes de continuar.

#### Scenario: Mostrar la clave
- **WHEN** el usuario crea la contraseña
- **THEN** ve la clave de recuperación con instrucciones para guardarla fuera del equipo

#### Scenario: Confirmación correcta
- **WHEN** el usuario escribe correctamente los dos grupos que se le piden
- **THEN** puede continuar

#### Scenario: Confirmación incorrecta
- **WHEN** el usuario escribe mal los grupos
- **THEN** el sistema no le deja continuar y puede volver a ver la clave

#### Scenario: Sin confirmar
- **WHEN** el usuario intenta continuar sin confirmarla
- **THEN** el sistema no lo permite y explica por qué

### Requirement: Nunca se guarda en claro
El sistema SHALL no guardar la clave de recuperación en claro en ningún fichero ni volver a mostrarla después de la confirmación.

#### Scenario: Consulta posterior
- **WHEN** el usuario busca la clave de recuperación en ajustes
- **THEN** no se puede ver de nuevo y se ofrece regenerarla

### Requirement: Entrar y restablecer con la clave de recuperación
El sistema SHALL permitir desbloquear con la clave de recuperación y SHALL obligar a definir a continuación una contraseña nueva, con una clave de recuperación nueva.

#### Scenario: Clave correcta
- **WHEN** el usuario escribe la clave de recuperación correcta
- **THEN** se abre la base de datos y se le pide una contraseña nueva

#### Scenario: Tolerancia al escribirla
- **WHEN** el usuario la escribe en minúsculas, sin guiones o con espacios
- **THEN** el sistema la acepta

#### Scenario: Clave incorrecta
- **WHEN** la clave escrita no es válida
- **THEN** el sistema lo indica sin revelar nada y permite reintentar

#### Scenario: Nueva clave tras el restablecimiento
- **WHEN** el usuario define la contraseña nueva
- **THEN** se genera una clave de recuperación nueva, se muestra y se confirma como al principio, y la anterior deja de valer

### Requirement: Regenerar la clave
El sistema SHALL permitir regenerar la clave de recuperación desde ajustes con la aplicación desbloqueada, invalidando la anterior.

#### Scenario: Regenerar
- **WHEN** el usuario regenera la clave y confirma la nueva
- **THEN** la nueva abre la base de datos y la anterior deja de funcionar

#### Scenario: Cancelar antes de confirmar
- **WHEN** el usuario cancela antes de confirmar la nueva clave
- **THEN** la clave anterior sigue siendo válida

### Requirement: Aviso permanente de que sin llave no hay recuperación
El sistema SHALL recordar en los textos de creación y de ajustes que perder la contraseña y la clave de recuperación hace irrecuperables los datos y las copias.

#### Scenario: Ajustes
- **WHEN** el usuario abre la sección de seguridad en ajustes
- **THEN** ve el recordatorio y las acciones de cambiar la contraseña y regenerar la clave

### Requirement: Feedback
El sistema SHALL informar del resultado de cada acción con la clave de recuperación con mensajes comprensibles y sin registrar la clave.

#### Scenario: Registro técnico
- **WHEN** falla el uso de la clave de recuperación
- **THEN** el registro técnico no contiene la clave ni derivados
