## Why

ARCA es software libre y gratuito. Quien lo mantiene quiere saber, de forma voluntaria, qué versiones de la compilación oficial están en uso y poder avisar cuando hay una versión nueva, sin licencias, sin bloquear nunca a un centro y sin instalar nada en remoto. Al ser una aplicación local con datos de menores, cualquier envío debe ser opcional, mínimo y totalmente transparente.

## What Changes

- **Registro opcional de la instalación**, desactivado por defecto: una casilla en la primera ejecución y en ajustes, que muestra exactamente qué se envía. El envío es puntual (a lo sumo una vez cada 24 horas, al arrancar, en segundo plano) y nunca bloquea ni exige conexión.
- **Datos enviados, cerrados por construcción**: identificador aleatorio de instalación, versión de la aplicación, sistema operativo y arquitectura, e idioma. Solo si el usuario los escribe: nombre y código del centro y un correo de contacto. Nunca datos de alumnos, taquillas, cobros ni rutas.
- **Aviso de versión nueva**: la respuesta del servidor incluye la última versión, el enlace de descarga, las notas y si es crítica, y va **firmada** (Ed25519). La aplicación compara y muestra un aviso no bloqueante con el enlace a la página oficial. Sin descarga ni instalación automática.
- **Comprobación de actualizaciones sin registro**: una segunda casilla independiente para avisar de versiones nuevas enviando solo la versión y el sistema, sin identificador; y un botón "Comprovar ara" a petición.
- **Correo opcional**: si el usuario facilita un correo y lo consiente, el servidor envía un único correo por versión nueva con el enlace, con confirmación previa y enlace de baja.
- **Actualizar es sustituir el ejecutable**: la base de datos se conserva y las migraciones se aplican al arrancar con la copia previa ya definida en `arquitectura-base`. El aviso ofrece hacer antes una copia de seguridad.
- **Control del usuario**: desactivar en cualquier momento, ver qué se envía y pedir el borrado del registro.
- **Contrato del servidor de registro** (proyecto pequeño y aparte): API mínima, retención y privacidad, documentados en `docs/registro-de-instalaciones.md`.

## Capabilities

### New Capabilities
- `registre-d-instalacio`: consentimiento, datos enviados, identificador, envío puntual, retirada y borrado, y correo de contacto.
- `avis-d-actualitzacions`: respuesta firmada, comparación de versiones, aviso, actualización manual y comprobación sin registro.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Sustituye al cambio `llicencies-client`, eliminado del proyecto. -->

## Fuera de alcance

- Licencias, claves de activación, límites de equipos, caducidad, período de prueba, solo lectura y huella de equipo: el software es libre y gratuito y no se bloquea nunca.
- Descarga o instalación automática, y cualquier ejecución de código recibido de un servidor.
- Telemetría de uso, estadísticas de funciones o informes de errores automáticos.
- Localización precisa. Como mucho, el servidor puede derivar el país en el momento de recibir la petición y descartar la dirección IP.
- El servidor de registro y el envío de correos, que son un proyecto aparte; aquí solo su contrato.
- Pantallas concretas (`ui-shell` y `ux-fonaments`): aquí sus reglas y datos.

## Impacto

- **Código**: constructor puro del contenido enviado y cliente HTTP en Application e Infrastructure, verificación de firma, ajustes locales de consentimiento, y ganchos en `configuracio-inicial` (casilla) y `ui-shell` (aviso).
- **Datos personales (RGPD)**: es el único cambio que envía algo fuera del equipo. Se basa en **consentimiento explícito** y es opcional. El servidor recibe la dirección IP (dato personal) y, si el usuario lo escribe, un correo. Quien mantiene el servidor es el responsable del tratamiento: debe informar, limitar la retención y permitir el borrado. Los centros públicos pueden necesitar autorización de su dirección o de su delegado de protección de datos antes de activarlo.
- **Depende de**: `arquitectura-base` (migraciones y ajustes locales), `configuracio-inicial` (casilla en la primera ejecución), `ui-shell` (aviso) y `copies-de-seguretat` (copia antes de actualizar).
