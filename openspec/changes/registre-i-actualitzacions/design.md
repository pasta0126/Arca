## Context

Sustituye al cambio `llicencies-client`, eliminado porque ARCA es software libre y gratuito y no hay nada que licenciar. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `arquitectura-base` (ajustes locales, migraciones con copia previa, registro técnico), `configuracio-inicial` (casilla en la primera ejecución), `ui-shell` (aviso) y `copies-de-seguretat` (copia antes de actualizar).

Restricciones: datos de menores en el equipo, ningún dato de alumnos sale nunca, todo envío es opcional y transparente, la aplicación funciona igual sin red, sin código remoto, coste cero y un solo PC.

## Goals / Non-Goals

**Goals:**
- Un envío mínimo, cerrado por construcción y verificable con pruebas.
- Cero tráfico de red si el usuario no lo ha activado.
- Avisos de versión firmados y sin ningún efecto sobre el uso de la aplicación.

**Non-Goals:**
- Licencias, telemetría de uso, actualización automática y el servidor.

## Decisions

### D1. Estado local fuera de la base de datos
Consentimientos, identificador de instalación, datos opcionales, fecha del último envío, última versión avisada y solicitud de borrado pendiente viven en el fichero de ajustes locales (el mismo de la ruta de la base). Motivos: el identificador es de la instalación y no de una base de datos, restaurar una copia no debe cambiarlo ni reactivar un consentimiento retirado, y no viaja con las copias.

### D2. Contenido enviado cerrado por construcción
`RegistrationPayload` es un tipo con campos fijos (identificador, versión, sistema, arquitectura, idioma y los tres opcionales). Se construye solo a partir de un objeto de ajustes y de datos del sistema, sin acceso a repositorios ni a `Domain` de alumnos. Una prueba de arquitectura comprueba que el constructor no referencia esas capas y una prueba de contrato serializa el tipo y compara la lista exacta de campos con la documentada en `docs/registro-de-instalaciones.md`, para que añadir un campo obligue a actualizar la descripción pública.

### D3. Un único punto de decisión de red
Un servicio decide si se hace una llamada según tres reglas: registro activado, avisos activados o petición explícita del usuario; y a lo sumo una vez cada 24 horas para las automáticas. Todo el tráfico de este cambio pasa por él, de modo que "sin consentimiento no hay tráfico" se prueba con un manejador HTTP falso que falla el test si recibe una petición. Cliente `HttpClient` de la biblioteca estándar, HTTPS, tiempo de espera de 5 segundos, sin reintentos, en segundo plano tras el arranque completo.

### D4. Respuesta firmada
La respuesta es un documento JSON con versión de formato, última versión, fecha, enlace, notas, indicador de crítica y la firma Ed25519 de los bytes canónicos del documento. La aplicación lleva solo la clave pública, que puede estar en un repositorio abierto. Se verifica con NSec.Cryptography (MIT, sobre libsodium), la única pieza criptográfica de la aplicación y solo para verificar. Una respuesta inválida se ignora sin efecto. *Alternativa descartada*: confiar solo en HTTPS; una firma protege el aviso aunque el servidor se viera comprometido. Rotar la clave pública exigiría una versión nueva; el formato lleva un campo de versión para permitirlo.

### D5. Enlaces solo oficiales
La aplicación no abre el enlace de la respuesta sin comprobarlo: `https` y un dominio de una lista fija en la propia aplicación. Es una segunda barrera por si la clave de firma se filtrara.

### D6. Comparación de versiones
Versionado semántico (mayor.menor.parche, con prefijos y etiquetas previas ignoradas para el aviso). Se avisa cuando la última es estrictamente posterior. El aviso se muestra una vez por versión; una crítica permanece hasta actualizar. Ninguna versión bloquea el uso.

### D7. Actualizar es sustituir el ejecutable
No hay mecanismo de instalación en la aplicación. Las versiones portables se actualizan sustituyendo la carpeta de programa sin tocar los datos, y las instaladas ejecutando el instalador nuevo sobre el anterior. La base de datos se migra al siguiente arranque con el migrador de `arquitectura-base` (copia previa verificada, rechazo de esquemas más nuevos). El aviso ofrece hacer una copia de seguridad antes con el asistente de `copies-de-seguretat`.

### D8. Correo gestionado por el servidor
El cliente solo envía el correo cuando el usuario lo ha escrito y ha marcado la casilla separada. El servidor confirma la dirección (doble confirmación) antes de suscribirla, envía un correo por versión y pone un enlace de baja en cada uno. Así nadie puede recibir correos por haber escrito otra dirección. *Alternativa descartada*: enviar avisos por correo a todos los registrados; exigiría recoger correos por defecto y no es proporcionado.

### D9. Borrado
La acción de borrado envía una solicitud con el identificador. Si no hay conexión queda pendiente en los ajustes locales y se envía en el siguiente arranque con conexión. El servidor debe borrar el registro y confirmar.

### D10. Contrato con el servidor
El documento `docs/registro-de-instalaciones.md` define la API mínima, los datos que guarda el servidor, la retención (24 meses de inactividad), que no se conserva la dirección IP más allá del registro transitorio de red (como mucho 7 días) y que el país puede derivarse en el momento de la recepción para descartar después la IP. Es la única descripción de lo que ocurre fuera de la aplicación y es pública.

### D11. Feedback
Resultado estructurado en activar, desactivar, borrar y comprobar; protección contra doble ejecución; mensajes comprensibles y sin bloquear la interfaz, según los principios de UX transversal.

## Risks / Trade-offs

- **Pocas personas activarán el registro** → se asume: el objetivo es visibilidad voluntaria, no censo. Las cifras serán orientativas.
- **Enviar la IP es un dato personal** → consentimiento, transparencia, retención mínima y sin conservarla; quien mantiene el servidor es el responsable del tratamiento.
- **Un centro público no puede activarlo sin autorización** → el registro es opcional y todo funciona igual sin él; se documenta que el centro consulte a su delegado de protección de datos.
- **Cualquiera puede enviar registros falsos al servidor** → límites de frecuencia y tratar los datos como indicativos. No hay decisiones automáticas basadas en ellos.
- **Un servidor caído o comprometido** → la aplicación no depende de él; la firma y la lista de dominios evitan enlaces falsos.
- **Correos a terceros por error al escribir una dirección** → doble confirmación en el servidor.
- **Quien compile su propia versión puede quitar el registro** → es lícito con licencia libre; el registro solo informa de las compilaciones oficiales.

## Migration Plan

Sin migración de base de datos: todo vive en los ajustes locales, y un fichero de ajustes sin estos campos equivale a todo desactivado.

## Open Questions

- Dónde se aloja el servidor y desde qué dominio se envían los correos: es del proyecto aparte y no cambia estos specs.
- Dominios oficiales de descarga que se incluyen en la lista: se fijan al elegir el sitio de publicación.
