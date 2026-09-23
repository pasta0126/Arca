## Purpose

Definir la comprobación online oportunista con el servidor de licencias: qué envía, qué acepta como respuesta y cómo se comporta cuando no hay conexión, sin ser nunca imprescindible.

## ADDED Requirements

### Requirement: Comprobación oportunista y no bloqueante
El sistema SHALL intentar comprobar la licencia con el servidor en segundo plano al arrancar y como mucho una vez al día, SHALL no bloquear nunca la interfaz ni el arranque y SHALL no exigirla para activar ni para usar la aplicación.

#### Scenario: Con conexión
- **WHEN** la aplicación arranca con conexión y no se ha comprobado hoy
- **THEN** intenta la comprobación en segundo plano sin bloquear el trabajo

#### Scenario: Ya comprobada hoy
- **WHEN** la aplicación se reinicia y ya hubo una comprobación correcta hoy
- **THEN** no vuelve a comprobar

#### Scenario: Sin conexión
- **WHEN** no hay conexión o el servidor no responde
- **THEN** la aplicación sigue con la clave local, sin mostrar errores al usuario

#### Scenario: Tiempo de espera
- **WHEN** el servidor tarda más del tiempo de espera definido
- **THEN** se abandona el intento y se reintentará en el siguiente arranque

### Requirement: Datos enviados al servidor
El sistema SHALL enviar al servidor únicamente el identificador del centro, la huella del equipo, la versión de la aplicación y la fecha, y SHALL no enviar nunca datos de alumnos ni de ningún otro dato del centro.

#### Scenario: Contenido de la petición
- **WHEN** se hace la comprobación
- **THEN** la petición contiene solo esos cuatro datos

#### Scenario: Datos de alumnos
- **WHEN** se inspecciona el tráfico de la comprobación con una base de datos con alumnos
- **THEN** no aparece ningún dato de alumnos, taquillas ni cobros

### Requirement: Respuesta firmada
El sistema SHALL aceptar una respuesta del servidor solo si su firma es válida con la clave pública incluida y corresponde a la licencia del centro, y SHALL ignorar cualquier otra sin efecto sobre el estado.

#### Scenario: Respuesta sin firma válida
- **WHEN** la respuesta no tiene una firma válida
- **THEN** se ignora y el estado no cambia

#### Scenario: Respuesta de otra licencia
- **WHEN** la respuesta firmada corresponde a otra licencia
- **THEN** se ignora y el estado no cambia

### Requirement: Revocación
El sistema SHALL guardar una revocación firmada recibida, con su fecha de recepción, y SHALL pasar al período de gracia desde ese día, tanto si la revocación es de la licencia como si es de este equipo por exceder el número de equipos permitidos.

#### Scenario: Licencia revocada
- **WHEN** la respuesta indica que la licencia está revocada
- **THEN** el estado pasa a en gracia y se muestra el motivo

#### Scenario: Equipo no autorizado
- **WHEN** la respuesta indica que este equipo excede el número de equipos permitidos
- **THEN** el estado de este equipo pasa a en gracia con el motivo de equipos superados

#### Scenario: Revocación levantada
- **WHEN** una comprobación posterior indica que la licencia vuelve a estar activa
- **THEN** la revocación se elimina y el estado se recalcula

### Requirement: Renovación automática
El sistema SHALL sustituir la clave por la que devuelva el servidor en una respuesta firmada cuando cumpla las reglas de renovación, e informar al usuario del nuevo vencimiento.

#### Scenario: Clave renovada por el servidor
- **WHEN** la respuesta incluye una clave válida con vencimiento posterior
- **THEN** la clave se sustituye y el sistema informa del nuevo vencimiento

#### Scenario: Clave no válida en la respuesta
- **WHEN** la respuesta incluye una clave con firma no válida o vencimiento anterior
- **THEN** se ignora y la clave actual no cambia

### Requirement: Funcionamiento sin servidor
El sistema SHALL funcionar con la clave firmada local hasta su vencimiento aunque el servidor no esté disponible durante todo ese tiempo.

#### Scenario: Meses sin conexión
- **WHEN** el equipo no tiene red durante meses y la clave sigue vigente
- **THEN** el estado sigue siendo activa

### Requirement: Registro sin datos sensibles
El sistema SHALL registrar los fallos de la comprobación en el registro técnico sin la clave completa, sin datos de alumnos y sin el contenido de la respuesta.

#### Scenario: Fallo de red
- **WHEN** falla la comprobación
- **THEN** el registro técnico anota el tipo de fallo sin datos personales ni la clave completa
