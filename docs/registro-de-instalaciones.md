# Registro de instalaciones y avisos de versión

Descripción pública de lo que ARCA puede enviar fuera del equipo y de lo que hace el servidor de registro. Es también el **contrato** del servidor, que es un proyecto aparte. Especificación en `openspec/changes/registre-i-actualitzacions/`.

## Principios

- **Todo es opcional y está desactivado por defecto.** ARCA funciona exactamente igual sin conexión y sin activar nada.
- **Nunca se envían datos de alumnos, taquillas, cobros, llaves, rutas, nombre de usuario ni nombre del equipo, ni contraseñas ni claves de recuperación.** El mantenedor no custodia ninguna llave y no puede recuperar los datos de un centro.
- **Nada bloquea el uso**: no hay licencias ni límites, y una versión antigua sigue funcionando.
- **No se descarga ni se instala nada** recibido de un servidor. Actualizar es sustituir el programa por el de la versión nueva.
- Lo que se envía es **cerrado**: cualquier campo nuevo debe aparecer en este documento en el mismo cambio.

## Qué se puede enviar

| Opción en ARCA | Datos que se envían | Finalidad |
|----------------|---------------------|-----------|
| Ninguna activada | **Nada** | — |
| «Avisar de versiones noves» | Versión de ARCA, sistema operativo y arquitectura | Saber si hay una versión más nueva |
| «Registrar aquesta instal·lació» | Lo anterior más un identificador aleatorio de instalación e idioma | Saber qué versiones están en uso y poder avisar |
| Datos opcionales que el usuario escribe | Nombre del centro y código del centro | Saber qué centros usan ARCA |
| «Avisar-me per correu» (casilla propia) | Correo de contacto | Enviar un único correo por versión nueva |
| Botón «Comprovar ara» | Versión y sistema, una sola vez | Comprobar a petición |

El **identificador de instalación** es un valor aleatorio generado por ARCA. No se deriva del equipo, del usuario ni de los datos, y se guarda en los ajustes locales.

## Cuándo se envía

- Como mucho **una vez cada 24 horas**, al arrancar y en segundo plano.
- Con un tiempo de espera corto y **sin reintentos**. Si falla, no se muestra nada y se intenta en el siguiente arranque.
- **Sin consentimiento, ARCA no realiza ninguna llamada de red** de este tipo.

## Qué hace el servidor

| Aspecto | Compromiso |
|---------|------------|
| Dirección IP | No se conserva. El registro transitorio de red se elimina en 7 días como máximo. El país puede derivarse en el momento de recibir la petición y la IP se descarta |
| Retención | Un registro se elimina tras 24 meses sin actividad, y en cualquier momento a petición del usuario |
| Correo | Solo se usa para avisar de versiones nuevas, con confirmación previa y enlace de baja en cada mensaje |
| Finalidad | Estadística orientativa y avisos. Nunca decisiones automáticas ni cesión a terceros |
| Responsable | La persona que mantiene el servidor |

## API mínima

```
POST   /v1/installations       Recibe el contenido enviado, lo guarda o actualiza por identificador
                               y devuelve la información de la última versión, firmada.
DELETE /v1/installations/{id}  Borra el registro asociado al identificador y lo confirma.
GET    /v1/latest              Información firmada de la última versión, sin registrar nada
                               (comprobación sin registro y «Comprovar ara»).
```

Respuesta de versión (firmada con Ed25519, con la clave privada guardada fuera de cualquier repositorio):

```json
{ "format": 1, "latest": "1.3.0", "releasedOn": "2026-10-01",
  "url": "https://<dominio-oficial>/descarregues", "notes": "…", "critical": false,
  "signature": "…" }
```

## Lo que hace ARCA con la respuesta

1. Verifica la firma con la clave pública incluida en la aplicación. Si no es válida, la ignora.
2. Comprueba que el enlace es `https` y pertenece a un dominio oficial.
3. Si la versión es posterior, muestra un aviso no bloqueante con las notas y ofrece hacer antes una copia de seguridad. La descarga la hace el usuario desde el navegador.

## Cómo pedir el borrado

Desde ajustes, «Esborrar el meu registre». Si no hay conexión, queda pendiente y se envía en el siguiente arranque. También podrá solicitarse por el contacto que se publique junto con el servidor de registro. Hasta que exista el servidor no se recoge ningún dato, así que no hay nada que borrar.

## Para los centros públicos

Antes de activar el registro conviene consultarlo con la dirección y con el delegado de protección de datos del centro. Como todo es opcional, ARCA funciona igual sin activarlo.
