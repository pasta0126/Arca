## Purpose

Permitir que quien lo desee registre de forma voluntaria y transparente su instalación de ARCA, enviando solo un conjunto mínimo y cerrado de datos, sin bloquear nunca la aplicación ni exigir conexión.

## ADDED Requirements

### Requirement: Registro opcional y desactivado por defecto
El sistema SHALL mantener el registro de la instalación desactivado por defecto y SHALL activarlo solo cuando el usuario marque de forma expresa la casilla correspondiente.

#### Scenario: Instalación nueva
- **WHEN** se abre ARCA por primera vez
- **THEN** el registro está desactivado y no se ha enviado nada

#### Scenario: Activación expresa
- **WHEN** el usuario marca la casilla de registro
- **THEN** el registro queda activado y se registra la fecha del consentimiento

### Requirement: Casilla en la primera ejecución y en ajustes
El sistema SHALL ofrecer la casilla de registro en la primera ejecución y en ajustes, junto con la lista exacta de lo que se enviará, y SHALL permitir cambiarla en cualquier momento.

#### Scenario: Primera ejecución
- **WHEN** el usuario llega al paso de registro de la primera ejecución
- **THEN** ve la casilla sin marcar, la lista de datos que se enviarían y un enlace a la descripción completa

#### Scenario: Cambio posterior
- **WHEN** el usuario abre ajustes
- **THEN** puede activar o desactivar el registro con la misma información visible

#### Scenario: Omitir el paso
- **WHEN** el usuario continúa sin marcar la casilla
- **THEN** la aplicación funciona igual y el registro sigue desactivado

### Requirement: Datos enviados cerrados
El sistema SHALL enviar únicamente un identificador de instalación, la versión de la aplicación, el sistema operativo con su arquitectura y el idioma, y SHALL enviar además el nombre del centro, el código del centro y un correo de contacto solo si el usuario los ha escrito de forma voluntaria.

#### Scenario: Contenido mínimo
- **WHEN** se envía el registro sin datos opcionales
- **THEN** la petición contiene solo esos cuatro datos

#### Scenario: Con datos opcionales
- **WHEN** el usuario ha escrito el nombre del centro
- **THEN** la petición incluye además el nombre del centro y nada más

#### Scenario: Datos que nunca se envían
- **WHEN** se inspecciona la petición con una base de datos llena
- **THEN** no aparece ningún dato de alumnos, taquillas, cobros, llaves, rutas, nombre de usuario ni nombre del equipo

### Requirement: Identificador de instalación aleatorio
El sistema SHALL generar un identificador aleatorio al activar el registro, guardarlo en los ajustes locales y no derivarlo del equipo, del usuario ni de los datos.

#### Scenario: Generación
- **WHEN** el usuario activa el registro por primera vez
- **THEN** se genera un identificador aleatorio nuevo

#### Scenario: Estabilidad
- **WHEN** la aplicación se reinicia o se restaura una copia de seguridad en el mismo equipo
- **THEN** el identificador no cambia

#### Scenario: Sin relación con el equipo
- **WHEN** se compara el identificador con los datos del equipo
- **THEN** no contiene ningún dato derivable del equipo ni del usuario

### Requirement: Envío puntual y no bloqueante
El sistema SHALL enviar el registro a lo sumo una vez cada 24 horas, al arrancar y en segundo plano, con un tiempo de espera corto y sin reintentos, y SHALL no bloquear nunca la interfaz ni exigir conexión.

#### Scenario: Con conexión
- **WHEN** la aplicación arranca con el registro activado, con conexión y sin envío en las últimas 24 horas
- **THEN** se envía en segundo plano sin retrasar el arranque ni el trabajo

#### Scenario: Ya enviado hoy
- **WHEN** la aplicación se reinicia y ya se envió en las últimas 24 horas
- **THEN** no se envía de nuevo

#### Scenario: Sin conexión o fallo
- **WHEN** no hay conexión, el servidor no responde o falla el envío
- **THEN** la aplicación sigue funcionando con normalidad, no muestra ningún error y lo intenta en el siguiente arranque

### Requirement: Sin consentimiento no hay tráfico
El sistema SHALL no realizar ninguna llamada de red relacionada con el registro ni con la comprobación de actualizaciones mientras el usuario no las haya activado, salvo la comprobación que el usuario pida expresamente.

#### Scenario: Registro y avisos desactivados
- **WHEN** el registro y los avisos de versión están desactivados
- **THEN** la aplicación no realiza ninguna llamada de red

### Requirement: Retirar el consentimiento y borrar el registro
El sistema SHALL permitir desactivar el registro en cualquier momento y solicitar el borrado del registro asociado al identificador, con confirmación del resultado.

#### Scenario: Desactivar
- **WHEN** el usuario desactiva el registro
- **THEN** la aplicación deja de enviar datos desde ese momento

#### Scenario: Solicitar el borrado
- **WHEN** el usuario pide borrar su registro y hay conexión
- **THEN** la aplicación envía la solicitud con el identificador, informa del resultado y desactiva el registro

#### Scenario: Borrado sin conexión
- **WHEN** el usuario pide borrar su registro y no hay conexión
- **THEN** la solicitud queda pendiente, la aplicación lo indica y la envía en el siguiente arranque con conexión

### Requirement: Correo de contacto opcional con consentimiento separado
El sistema SHALL permitir facilitar un correo de contacto solo mediante una casilla propia y separada, indicando que se usará únicamente para avisar de versiones nuevas, y SHALL no enviarlo sin ese consentimiento.

#### Scenario: Sin consentimiento de correo
- **WHEN** el usuario activa el registro pero no marca la casilla de correo
- **THEN** no se envía ningún correo de contacto

#### Scenario: Con consentimiento
- **WHEN** el usuario escribe un correo y marca la casilla de avisos por correo
- **THEN** el correo se incluye en el registro

#### Scenario: Correo con formato incorrecto
- **WHEN** el usuario escribe un correo con formato no válido
- **THEN** el sistema lo rechaza con un mensaje claro antes de guardarlo

### Requirement: Transparencia
El sistema SHALL mostrar en la propia aplicación qué datos se envían, con qué finalidad y durante cuánto tiempo se conservan, y SHALL mantener esa descripción publicada en el repositorio.

#### Scenario: Pantalla informativa
- **WHEN** el usuario abre la información del registro
- **THEN** ve la lista de datos, su finalidad, la retención y cómo pedir el borrado

#### Scenario: Descripción publicada
- **WHEN** cambian los datos que se envían
- **THEN** la descripción del repositorio y la de la aplicación se actualizan en el mismo cambio

### Requirement: Registro técnico sin datos
El sistema SHALL registrar los fallos de envío en el registro técnico sin incluir el identificador completo, el correo ni el contenido de la petición ni de la respuesta.

#### Scenario: Fallo de red
- **WHEN** falla un envío
- **THEN** el registro técnico anota el tipo de fallo sin datos personales

### Requirement: Estado local fuera de la base de datos
El sistema SHALL guardar el consentimiento, el identificador, los datos opcionales y las fechas de envío en los ajustes locales, fuera de la base de datos, de modo que no viajen en las copias de seguridad.

#### Scenario: Restaurar una copia
- **WHEN** el usuario restaura una copia de seguridad hecha con otro estado de registro
- **THEN** el consentimiento y el identificador siguen como estaban en el equipo

#### Scenario: Equipo nuevo
- **WHEN** el usuario instala ARCA en otro equipo
- **THEN** el registro está desactivado hasta que lo active de nuevo

### Requirement: Feedback y guía
El sistema SHALL informar del resultado de activar, desactivar y borrar el registro con mensajes comprensibles y sin bloquear la interfaz.

#### Scenario: Activación correcta
- **WHEN** el usuario activa el registro
- **THEN** el sistema confirma que está activado y cuándo se enviará por primera vez

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces la solicitud de borrado
- **THEN** la solicitud se envía una sola vez
