## Why

ARCA es un producto de pago con licencia por centro, pero la aplicación debe seguir siendo fiable para conserjes sin conocimientos técnicos y en PCs que a veces no tienen red. Hace falta un cliente de licencias que valide la clave sin conexión, avise con antelación antes de limitar el uso, no ponga nunca en peligro los datos del centro y no dependa del servidor para funcionar.

## What Changes

- Clave de licencia firmada (criptografía asimétrica, solo la clave pública en la aplicación), verificable sin conexión, con centro, vencimiento y número de equipos.
- Activación pegando o cargando la clave, sin conexión obligatoria; renovación con una clave nueva.
- Estados de licencia: prueba, activa, en gracia y solo lectura, calculados por una función pura.
- Período de prueba de 30 días desde la primera ejecución sin clave y, después, solo lectura. Sin gracia.
- Gracia de 30 días tras caducar o revocarse la licencia, con aviso al abrir e indicador permanente con los días restantes; después, solo lectura. Ambas duraciones son constantes configurables.
- En solo lectura se puede consultar todo, exportar, hacer copia, restaurar y activar la licencia; el resto de escrituras se rechaza en la capa de aplicación, no solo en la interfaz.
- Comprobación online oportunista en segundo plano, como mucho una vez al día, que solo envía identificador de centro, huella de equipo, versión y fecha, y puede revocar la licencia o renovarla con una respuesta firmada. Nunca es imprescindible.
- Estado de la licencia guardado fuera de la base de datos y de las copias, para que restaurar una copia antigua no la revierta ni reinicie la prueba.

## Capabilities

### New Capabilities
- `llicencia-signada`: formato y validación de la clave, activación, renovación, huella de equipo y almacenamiento local.
- `estat-de-llicencia`: estados, prueba, gracia, solo lectura y operaciones siempre disponibles.
- `comprovacio-online`: comprobación oportunista, datos enviados, respuesta firmada, revocación y renovación.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Aplica el requisito de "siempre disponibles" que declaran `informes-csv` y `copies-de-seguretat`. -->

## Fuera de alcance

- El servidor de licencias, que es otro proyecto: aquí solo el cliente y el contrato que necesita el servidor.
- Modelo comercial: precios, tipos de licencia y facturación.
- Antipiratería avanzada: ofuscación, protección del ejecutable o detección de manipulación del reloj más allá de lo básico.
- Cuentas de usuario, roles o registro de quién hace cada operación.
- Actualización automática de la aplicación.
- Pantallas de licencia y avisos (`ui-shell`, `ux-fonaments`); aquí sus reglas y datos.

## Impacto

- **Código**: dominio de licencia y función de estado en Domain, puerta de licencia y casos de uso en Application, almacén local, huella de equipo y cliente HTTP en Infrastructure.
- **Datos personales (RGPD)**: no se envían datos de alumnos ni de ningún otro tipo al servidor. Solo el identificador del centro, una huella de equipo derivada y no reversible, la versión y la fecha. La huella no contiene datos personales.
- **Depende de**: `arquitectura-base`, y de los datos del proyecto de licencias (pendientes de recibir) para cerrar el contrato con el servidor.
