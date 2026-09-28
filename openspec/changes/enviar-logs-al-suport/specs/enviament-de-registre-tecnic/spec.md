## Purpose

Permitir que el usuario envíe al servicio de soporte, de forma puntual, expresa y transparente, el fichero actual del registro técnico para que quien mantiene ARCA pueda diagnosticar un problema, sin enviar nunca datos de alumnos ni bloquear la aplicación.

## ADDED Requirements

### Requirement: Envío solo a petición expresa
El sistema SHALL enviar el registro técnico únicamente cuando el usuario ejecute la acción «Enviar registre tècnic» y la confirme, y SHALL NOT enviarlo nunca de forma automática, periódica ni en segundo plano.

#### Scenario: Sin acción del usuario
- **WHEN** la aplicación se usa, falla o se reinicia sin que el usuario pida el envío
- **THEN** no se realiza ninguna petición de red de este tipo

#### Scenario: Cancelar la confirmación
- **WHEN** el usuario inicia la acción y cancela en la confirmación
- **THEN** no se envía nada y el registro queda intacto

### Requirement: Transparencia previa al envío
El sistema SHALL mostrar antes de enviar el nombre y el tamaño del fichero, la lista exacta de datos que acompañan al envío y un enlace a la descripción pública, y SHALL pedir confirmación explícita.

#### Scenario: Vista previa
- **WHEN** el usuario inicia la acción
- **THEN** ve el nombre y el tamaño del fichero actual, los datos que se enviarán y los botones de confirmar y cancelar

#### Scenario: Sin registro técnico
- **WHEN** no existe ningún fichero de registro o está vacío
- **THEN** se informa de que no hay nada que enviar y no se ofrece confirmar

### Requirement: Solo el fichero actual
El sistema SHALL enviar únicamente el fichero de registro más reciente, y SHALL NOT enviar los ficheros rotados anteriores, la base de datos, copias de seguridad ni exportaciones.

#### Scenario: Varios ficheros rotados
- **WHEN** existen el fichero actual y otros cuatro anteriores
- **THEN** solo se envía el actual

#### Scenario: Fichero en uso
- **WHEN** el registro está abierto por la propia aplicación
- **THEN** se lee sin interrumpir la escritura y sin fallar por bloqueo

### Requirement: Contenido enviado cerrado
El sistema SHALL enviar únicamente el fichero, la versión de ARCA, el sistema operativo con su arquitectura y, si existen, el identificador de instalación y el código de incidencia, y SHALL NOT admitir texto libre del usuario.

#### Scenario: Contenido mínimo
- **WHEN** el registro de instalaciones está desactivado y no hay código de incidencia
- **THEN** la petición contiene el fichero, la versión y el sistema, y ningún identificador

#### Scenario: Con identificador de instalación
- **WHEN** el usuario tiene activado el registro de instalaciones
- **THEN** la petición incluye además el identificador aleatorio de instalación y nada más

#### Scenario: Con código de incidencia
- **WHEN** el usuario escribe un código de incidencia válido
- **THEN** la petición lo incluye

#### Scenario: Código de incidencia inválido
- **WHEN** el código escrito supera la longitud máxima o contiene caracteres no permitidos
- **THEN** se rechaza con un mensaje comprensible y no se envía nada

#### Scenario: Datos que nunca se envían
- **WHEN** se inspecciona la petición con una base de datos llena
- **THEN** no aparece ningún dato de alumnos, taquillas, cobros, llaves, rutas, nombre de usuario ni nombre del equipo fuera de lo que ya contiene el fichero de registro por la convención de privacidad

### Requirement: Independiente de los consentimientos permanentes
El sistema SHALL permitir el envío aunque el registro de instalaciones y los avisos de versión estén desactivados, y SHALL NOT activar ni modificar esos consentimientos por enviar el registro.

#### Scenario: Registro desactivado
- **WHEN** el usuario envía el registro técnico con el registro de instalaciones desactivado
- **THEN** el envío se realiza y los consentimientos siguen igual

#### Scenario: Sin consentimiento no hay otro tráfico
- **WHEN** termina el envío
- **THEN** no se realiza ninguna otra petición de red que no haya pedido el usuario

### Requirement: Sin credenciales en el cliente
El sistema SHALL NOT incluir ningún secreto, token ni clave privada para autenticarse con el servicio de soporte, y SHALL usar HTTPS.

#### Scenario: Dirección no segura
- **WHEN** la dirección configurada del servicio no es `https`
- **THEN** el envío se rechaza sin realizar la petición

#### Scenario: Inspección del código
- **WHEN** se revisa el repositorio
- **THEN** no hay ningún token ni clave de acceso al servicio

### Requirement: Límite de tamaño en el cliente
El sistema SHALL comprobar el tamaño del fichero antes de enviarlo y SHALL negarse a enviar uno mayor que el límite documentado, explicando el motivo.

#### Scenario: Dentro del límite
- **WHEN** el fichero pesa menos que el límite
- **THEN** se ofrece el envío

#### Scenario: Por encima del límite
- **WHEN** el fichero supera el límite
- **THEN** no se envía y se informa del motivo

### Requirement: Feedback y resultado claros
El sistema SHALL mostrar progreso durante el envío, informar del éxito con la referencia devuelta por el servidor y mostrar errores comprensibles en catalán, sin bloquear la aplicación y con protección contra doble ejecución.

#### Scenario: Envío correcto
- **WHEN** el servidor acepta el fichero
- **THEN** se muestra un mensaje de éxito con la referencia del envío para citarla en la incidencia

#### Scenario: Sin conexión
- **WHEN** no hay red o el servidor no responde en el tiempo de espera
- **THEN** se informa de que no se ha podido enviar y de que se puede intentar más tarde; no se reintenta solo ni queda nada pendiente

#### Scenario: Rechazo del servidor
- **WHEN** el servidor rechaza el envío por tamaño o por exceso de frecuencia
- **THEN** se explica el motivo en términos comprensibles sin mostrar detalles técnicos

#### Scenario: Doble clic
- **WHEN** el usuario pulsa varias veces el botón de confirmar
- **THEN** se realiza un solo envío

#### Scenario: Aplicación utilizable
- **WHEN** el envío está en curso
- **THEN** la interfaz sigue respondiendo y el usuario puede cancelar antes de que termine

### Requirement: Registro técnico del propio envío sin datos
El sistema SHALL anotar los fallos del envío con el tipo de error y el contexto, y SHALL NOT anotar el contenido del fichero, el código de incidencia ni la respuesta completa del servidor.

#### Scenario: Fallo de red
- **WHEN** el envío falla por un error de red
- **THEN** el registro técnico recibe una entrada con el tipo de error y una referencia, sin el contenido enviado

### Requirement: Privacidad del fichero enviado
El sistema SHALL garantizar que el fichero enviado no contiene datos de alumnos, y SHALL verificarlo con una prueba que provoca un error con datos de un alumno.

#### Scenario: Error con datos de alumno
- **WHEN** se provoca un error durante el procesamiento de un alumno con nombre y valores identificables
- **THEN** ni el fichero de registro ni la petición de envío contienen esos datos

### Requirement: Contrato público documentado
El sistema SHALL documentar en `docs/registro-de-logs.md` la petición `POST /v1/logs`, sus campos, los límites, las respuestas y los compromisos del servidor, y cualquier campo nuevo SHALL aparecer en ese documento en el mismo cambio.

#### Scenario: Prueba de contrato
- **WHEN** se serializa la petición y se compara con la lista de campos documentada
- **THEN** coinciden exactamente, y añadir un campo sin actualizar el documento hace fallar la prueba

### Requirement: Regla de red enmendada
El sistema SHALL recoger en `docs/convenciones.md` (8.4), `AGENTS.md`, `openspec/config.yaml` y `docs/registro-de-instalaciones.md` que las únicas comunicaciones de red son el registro opcional, el aviso de versión y el envío del registro técnico a petición expresa del usuario.

#### Scenario: Documentación coherente
- **WHEN** se leen esas cuatro fuentes
- **THEN** todas enumeran las mismas tres comunicaciones y ninguna afirma que solo existen dos
