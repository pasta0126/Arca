## Purpose

Definir la clave de licencia como un dato firmado y verificable sin conexión, y cómo se activa, se renueva y se guarda en el equipo.

## ADDED Requirements

### Requirement: Clave firmada verificable sin conexión
El sistema SHALL validar la clave de licencia comprobando su firma con una clave pública incluida en la aplicación, sin conexión a la red, y SHALL leer de ella el identificador de licencia, el identificador y el nombre del centro, la fecha de emisión, la fecha de vencimiento y el número de equipos permitidos.

#### Scenario: Clave válida
- **WHEN** el usuario introduce una clave con firma correcta
- **THEN** el sistema la acepta y muestra el centro, el vencimiento y el número de equipos

#### Scenario: Clave manipulada
- **WHEN** se altera cualquier carácter de la clave, incluido el vencimiento
- **THEN** el sistema la rechaza por firma no válida

#### Scenario: Clave con formato incorrecto
- **WHEN** el usuario introduce un texto que no tiene el formato de una clave
- **THEN** el sistema lo rechaza con un mensaje claro que pide revisar la clave

### Requirement: Activación con la clave
El sistema SHALL permitir activar la licencia pegando el texto de la clave o cargándola desde un fichero, mostrando antes de confirmar el centro y el vencimiento, sin necesidad de conexión a la red.

#### Scenario: Activación sin red
- **WHEN** el usuario activa una clave válida en un equipo sin conexión
- **THEN** la licencia queda activa

#### Scenario: Clave cargada desde fichero
- **WHEN** el usuario elige un fichero que contiene la clave
- **THEN** el sistema la lee y muestra el centro y el vencimiento antes de confirmar

#### Scenario: Espacios y saltos de línea
- **WHEN** la clave pegada lleva espacios o saltos de línea sobrantes al principio o al final
- **THEN** el sistema los ignora

### Requirement: Renovación y sustitución de la clave
El sistema SHALL aceptar una clave nueva del mismo centro con un vencimiento posterior al de la actual, SHALL rechazar una con vencimiento igual o anterior, y SHALL pedir confirmación explícita si la clave es de otro centro.

#### Scenario: Renovación
- **WHEN** el usuario introduce una clave del mismo centro con vencimiento posterior
- **THEN** sustituye a la anterior y el estado se recalcula

#### Scenario: Clave más antigua
- **WHEN** el usuario introduce una clave con vencimiento igual o anterior al de la actual
- **THEN** el sistema la rechaza indicando que ya hay una clave con vencimiento igual o posterior

#### Scenario: Clave de otro centro
- **WHEN** el usuario introduce una clave válida de otro centro
- **THEN** el sistema avisa del cambio de centro y no la aplica hasta la confirmación

### Requirement: Estado de la licencia guardado fuera de la base de datos
El sistema SHALL guardar la clave, la fecha de inicio de la prueba, la última fecha vista y la revocación recibida en un almacén local independiente de la base de datos, de modo que no viajen en las copias de seguridad ni cambien al restaurar una.

#### Scenario: Restaurar una copia antigua
- **WHEN** el usuario restaura una copia de seguridad hecha antes de renovar la licencia
- **THEN** la licencia sigue con la clave renovada

#### Scenario: Base de datos nueva
- **WHEN** el usuario empieza con una base de datos vacía en el mismo equipo
- **THEN** el estado de la licencia y la fecha de inicio de la prueba se conservan

#### Scenario: Equipo nuevo
- **WHEN** el usuario instala ARCA en otro equipo
- **THEN** debe introducir la clave en ese equipo

### Requirement: Independencia del cifrado de datos
El sistema SHALL cifrar y abrir la base de datos sin depender de la licencia, de su estado ni del servidor de licencias.

#### Scenario: Licencia revocada
- **WHEN** la licencia está revocada
- **THEN** la base de datos se abre y se lee con normalidad

### Requirement: Huella de equipo
El sistema SHALL calcular una huella del equipo estable y no reversible, a partir de un identificador propio del sistema operativo combinado con un valor fijo de la aplicación, sin incluir nombre de usuario, nombre del equipo ni otros datos personales.

#### Scenario: Estabilidad
- **WHEN** la aplicación se ejecuta varias veces en el mismo equipo
- **THEN** la huella es la misma

#### Scenario: Equipos distintos
- **WHEN** la aplicación se ejecuta en dos equipos distintos
- **THEN** las huellas son distintas

#### Scenario: Sin datos personales
- **WHEN** se inspecciona la huella
- **THEN** no contiene nombre de usuario, nombre del equipo ni identificadores reversibles

### Requirement: Consulta de la licencia
El sistema SHALL permitir consultar el centro, el vencimiento, el estado, los días restantes y el número de equipos permitidos, sin mostrar la clave completa.

#### Scenario: Consulta
- **WHEN** el usuario abre la información de la licencia
- **THEN** ve el centro, el vencimiento, el estado y los días restantes

### Requirement: Feedback y errores de la licencia
El sistema SHALL informar del resultado de la activación y de la renovación, con el nuevo vencimiento, y SHALL mostrar los errores en un lenguaje comprensible sin detalles técnicos ni la clave completa en el registro técnico.

#### Scenario: Activación correcta
- **WHEN** el usuario activa una clave válida
- **THEN** el sistema confirma la activación e indica el vencimiento

#### Scenario: Registro técnico
- **WHEN** falla la validación de una clave
- **THEN** el registro técnico no contiene la clave completa
