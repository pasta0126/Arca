## Why

Cuando un centro tiene un problema, quien mantiene ARCA no puede verlo: la aplicación es local, sin servidor y sin telemetría. El registro técnico (Serilog, solo tipos de error, contexto y pila) ayudaría a diagnosticarlo, pero hoy el conserje tendría que localizar el fichero y enviarlo a mano, algo poco realista para un perfil no técnico. Hace falta un botón que, a petición expresa del usuario, envíe el fichero actual al servicio de soporte, sin romper la regla de que ningún dato sale del equipo sin control.

## What Changes

- Acción explícita «Enviar registre tècnic» que envía **solo el fichero de registro actual** a un endpoint público de soporte. Nunca hay envío automático ni en segundo plano.
- Paso previo de transparencia: se muestra el nombre y el tamaño del fichero y qué acompaña al envío, y se pide confirmación antes de enviar.
- Contenido del envío cerrado: el fichero, el identificador de la instalación (solo si el usuario tiene activado el registro; si no, ninguno), la versión de ARCA, el sistema operativo y su arquitectura, y un código de incidencia opcional. Sin nota libre.
- Disponible aunque el registro de instalaciones y los avisos estén desactivados: es una acción puntual y expresa, no un consentimiento permanente.
- Endpoint anónimo, sin credenciales ni secretos en el cliente. El servidor limita tamaño y frecuencia; el cliente respeta un tamaño máximo antes de enviar.
- Feedback claro: progreso, éxito con referencia del envío, y errores comprensibles (sin red, tiempo agotado, rechazo por tamaño o frecuencia). Sin red no bloquea la aplicación y no hay reintentos automáticos.
- Contrato público `POST /v1/logs` documentado en `docs/registro-de-logs.md` (el servidor es otro proyecto).
- **Enmienda de reglas**: la regla 8.4 de `docs/convenciones.md`, el resumen de `AGENTS.md`, `openspec/config.yaml` y `docs/registro-de-instalaciones.md` (principios) admiten esta tercera comunicación de red, siempre explícita y a petición.
- Prueba de privacidad del fichero enviado: provocar un error con datos de un alumno y comprobar que ni el fichero ni la petición dejan rastro.

## Capabilities

### New Capabilities
- `enviament-de-registre-tecnic`: envío a petición del usuario del fichero actual de registro técnico al servicio de soporte, con transparencia previa, contenido cerrado y feedback.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. La enmienda de la regla de red es documental (convenciones, AGENTS.md, config) y se recoge en las tareas. `registre-i-actualitzacions` aún no está archivado. -->

## Fuera de alcance (v2 o posterior, o nunca)

- Envío automático, periódico o al producirse un error.
- Enviar los ficheros rotados anteriores o la base de datos, copias de seguridad o exportaciones.
- Nota libre escrita por el usuario, capturas de pantalla o adjuntos.
- Autenticación de usuarios o token embebido en el cliente.
- Reintentos en segundo plano o cola de envíos pendientes.
- El servidor de soporte (proyecto aparte) y el análisis de los ficheros recibidos.
- Aumentar el contenido del registro técnico: sigue siendo el de la convención 8.2.

## Impacto

- **Código**: servicio de envío en Infrastructure (cliente HTTP y lectura del fichero actual) tras una interfaz de Application; casos de uso de preparación y de envío con resultado estructurado; pantalla o diálogo de confirmación en Desktop; ajuste mínimo en la configuración de Serilog si hace falta compartir lectura del fichero abierto.
- **Documentación**: `docs/registro-de-logs.md` (nuevo), `docs/registro-de-instalaciones.md`, `docs/convenciones.md` (8.4), `AGENTS.md`, `openspec/config.yaml`, `docs/riesgos.md`.
- **Datos personales (RGPD)**: el fichero no contiene datos de alumnos por construcción (convención 8.2) y esto se verifica con una prueba. El envío es voluntario, informado y de un solo fichero; el servidor debe fijar retención y no conservar la IP (contrato).
- **Depende de**: `arquitectura-base` (registro técnico y ajustes locales). Se coordina con `registre-i-actualitzacions` (identificador de instalación y versión) y con `ui-shell`/`ux-fonaments` (notificaciones y ubicación del botón).
