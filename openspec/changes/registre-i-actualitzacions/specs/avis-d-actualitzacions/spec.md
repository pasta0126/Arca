## Purpose

Avisar de forma no bloqueante de que existe una versión nueva de ARCA, con un enlace a la página oficial de descarga, sin descargar ni instalar nada en remoto y conservando siempre los datos del centro.

## ADDED Requirements

### Requirement: Información de versión firmada
El sistema SHALL aceptar la información de la última versión (número de versión, fecha, enlace de descarga, notas y si es crítica) solo si su firma Ed25519 es válida con la clave pública incluida en la aplicación, y SHALL ignorar cualquier otra sin efecto alguno.

#### Scenario: Firma válida
- **WHEN** la respuesta del servidor tiene una firma válida
- **THEN** la aplicación usa su contenido para decidir si avisa

#### Scenario: Firma no válida
- **WHEN** la firma no es válida o falta
- **THEN** la respuesta se ignora y no se muestra ningún aviso

#### Scenario: Contenido manipulado
- **WHEN** se altera cualquier campo de la respuesta firmada
- **THEN** la verificación falla y se ignora

### Requirement: Comparación de versiones y aviso
El sistema SHALL comparar la versión instalada con la última y SHALL mostrar un aviso no bloqueante con el número de la versión nueva, sus notas y un enlace a la descarga cuando la última sea posterior.

#### Scenario: Versión nueva
- **WHEN** la versión instalada es 1.2.0 y la última es 1.3.0
- **THEN** se muestra un aviso con la versión nueva, las notas y el enlace

#### Scenario: Ya actualizada
- **WHEN** la versión instalada es igual o posterior a la última
- **THEN** no se muestra ningún aviso

#### Scenario: Aviso descartable
- **WHEN** el usuario cierra el aviso
- **THEN** no vuelve a aparecer para esa versión, salvo que sea crítica

### Requirement: Versión crítica
El sistema SHALL mantener visible, sin bloquear el uso, el aviso de una versión marcada como crítica hasta que el usuario actualice.

#### Scenario: Aviso crítico
- **WHEN** la última versión está marcada como crítica
- **THEN** el aviso permanece visible con su explicación y no impide trabajar

### Requirement: Sin descarga ni instalación automática
El sistema SHALL no descargar, instalar ni ejecutar nada recibido de un servidor, y SHALL limitarse a abrir en el navegador del sistema el enlace de descarga cuando el usuario lo pida.

#### Scenario: Abrir la descarga
- **WHEN** el usuario pulsa el enlace del aviso
- **THEN** se abre la página de descarga en el navegador del sistema

#### Scenario: Sin acción del usuario
- **WHEN** hay una versión nueva y el usuario no hace nada
- **THEN** no se descarga ni se instala nada

### Requirement: Enlaces solo a la página oficial
El sistema SHALL abrir únicamente enlaces `https` cuyo dominio esté en la lista de dominios oficiales definida en la aplicación, y SHALL rechazar cualquier otro aunque la firma sea válida.

#### Scenario: Enlace oficial
- **WHEN** el enlace es `https` y pertenece a un dominio oficial
- **THEN** se ofrece abrirlo

#### Scenario: Enlace de otro dominio
- **WHEN** el enlace no pertenece a un dominio oficial o no es `https`
- **THEN** no se ofrece abrirlo y el aviso indica que la información no es válida

### Requirement: Actualizar conserva los datos
El sistema SHALL permitir actualizar sustituyendo el ejecutable o la carpeta de la aplicación por los de la versión nueva, conservando la base de datos y los ajustes, y aplicando al siguiente arranque las migraciones pendientes con la copia previa verificada.

#### Scenario: Sustituir el ejecutable
- **WHEN** el usuario sustituye la aplicación por la versión nueva y la abre
- **THEN** los datos siguen intactos y las migraciones se aplican tras la copia previa verificada

#### Scenario: Versión con esquema más nuevo
- **WHEN** el usuario abre una versión antigua sobre una base de datos migrada por una versión posterior
- **THEN** la aplicación se niega a abrirla e indica que hay que actualizar

### Requirement: Copia de seguridad antes de actualizar
El sistema SHALL ofrecer en el aviso de versión nueva hacer una copia de seguridad antes de actualizar, sin obligar a ello.

#### Scenario: Oferta de copia
- **WHEN** se muestra el aviso de una versión nueva
- **THEN** incluye una acción para hacer la copia de seguridad antes de descargar

### Requirement: Avisos de versión como opción independiente
El sistema SHALL ofrecer una casilla independiente del registro para avisar de versiones nuevas, que envía únicamente la versión y el sistema operativo con su arquitectura, sin identificador, y SHALL ofrecer una acción "Comprobar ahora" a petición del usuario.

#### Scenario: Solo avisos de versión
- **WHEN** el usuario activa los avisos de versión y no el registro
- **THEN** la comprobación envía solo la versión y el sistema, sin identificador ni datos opcionales

#### Scenario: Comprobar ahora
- **WHEN** el usuario pulsa "Comprobar ahora" con ambas casillas desactivadas
- **THEN** se hace una única comprobación con la versión y el sistema, y se informa del resultado

#### Scenario: Sin conexión
- **WHEN** el usuario comprueba y no hay conexión
- **THEN** el sistema lo indica con un mensaje claro y sin error técnico

### Requirement: Aviso por correo opcional
El sistema SHALL delegar en el servidor el envío de un único correo por versión nueva al correo facilitado con consentimiento, y SHALL exigir que el servidor confirme la dirección antes de suscribirla y ofrezca un enlace de baja en cada correo.

#### Scenario: Correo con confirmación
- **WHEN** el usuario facilita un correo por primera vez
- **THEN** el servidor envía un mensaje de confirmación y no le avisa hasta que se confirma

#### Scenario: Un correo por versión
- **WHEN** aparece una versión nueva
- **THEN** cada correo confirmado recibe un único mensaje con el enlace de descarga y el enlace de baja

### Requirement: Nunca bloquea
El sistema SHALL funcionar con normalidad, sin restricciones de ningún tipo, aunque no pueda comprobar versiones, no reciba respuesta o esté muy desactualizado.

#### Scenario: Versión muy antigua
- **WHEN** la versión instalada es varias versiones anterior a la última
- **THEN** la aplicación sigue funcionando con todas sus funciones y solo muestra el aviso

#### Scenario: Sin comprobación durante meses
- **WHEN** no hay conexión durante meses
- **THEN** la aplicación funciona con normalidad

### Requirement: Feedback y guía
El sistema SHALL explicar qué ocurre al pulsar cada acción del aviso y SHALL informar del resultado de las comprobaciones sin bloquear la interfaz.

#### Scenario: Resultado de la comprobación
- **WHEN** termina una comprobación a petición del usuario
- **THEN** el sistema indica si hay versión nueva o si ya está actualizado

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces "Comprobar ahora"
- **THEN** se hace una sola comprobación
