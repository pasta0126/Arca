## Context

Undécimo cambio. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Ya decidido en `openspec/config.yaml` y `arquitectura-base`: clave firmada con criptografía asimétrica y solo la pública en la aplicación, comprobación online oportunista que puede revocar, licencia por centro con límite de equipos, gracia y solo lectura, exportar y copiar siempre disponibles, cifrado de la base independiente de la licencia y datos de alumnos fuera del servidor. `informes-csv` y `copies-de-seguretat` ya declaran que exportar, copiar y restaurar no consultan la licencia; aquí se cierra ese enganche.

Restricciones: PCs que pueden no tener red durante meses; conserjes no técnicos; datos de menores; un solo PC y una sola instancia; el servidor es otro proyecto y sus datos aún no se han recibido.

## Goals / Non-Goals

**Goals:**
- Estado de licencia calculado por una función pura, verificable sin reloj ni disco.
- Validación de clave sin red y sin depender del servidor para usar la aplicación.
- Restricción de solo lectura aplicada en la capa de aplicación, con una lista explícita de excepciones.
- Contrato claro con el servidor, con el mínimo de datos.

**Non-Goals:**
- Antipiratería avanzada, modelo comercial, servidor y pantallas.

## Decisions

### D1. Formato de la clave
Una cadena `carga.firma` en base64url: la carga es un documento con identificador de licencia, identificador y nombre del centro, fechas de emisión y vencimiento y límite de equipos, y la firma es Ed25519 sobre los bytes de la carga. La aplicación lleva solo la clave pública. Se elige Ed25519 por su tamaño de firma y clave, la disponibilidad en .NET multiplataforma y la ausencia de parámetros que configurar. *Alternativa descartada*: RSA; claves y firmas mucho mayores, incómodas de pegar, sin ventaja aquí. Las claves de versiones futuras llevan un campo de versión de formato para poder rotar la clave pública.

### D2. Estado como función pura
`LicenseStatus Evaluate(LicenseState state, DateOnly today, LicenseSettings settings)` con `LicenseState` = clave validada (o ninguna), revocación (fecha) o ninguna, inicio de la prueba y última fecha vista. Devuelve estado (prueba, activa, en gracia, solo lectura) y días restantes. Sin acceso a disco, red ni reloj: los casos de uso le pasan la fecha y el estado leído. Es la pieza que concentra las pruebas de plazos.
- Sin clave: prueba durante 30 días desde el inicio; después solo lectura.
- Con clave y sin revocación: activa hasta el vencimiento incluido; gracia los 30 días siguientes; después solo lectura.
- Con revocación: gracia desde la fecha de recepción; después solo lectura.
Ambas duraciones son constantes en `LicenseSettings`.

### D3. Almacén local independiente de la base de datos
Un fichero pequeño en la carpeta de datos de la aplicación (junto al de ajustes locales de la ruta), fuera de la base de datos, con clave, inicio de la prueba, última fecha vista, revocación y fecha de la última comprobación. Consecuencias buscadas: una copia de seguridad no lleva la licencia, restaurar una antigua no la revierte ni reinicia la prueba, y una base nueva en el mismo equipo no reinicia la prueba. Coste asumido: un equipo nuevo requiere volver a introducir la clave, lo que ya ocurre con la activación. *Alternativa descartada*: guardarlo en la base de datos; una restauración devolvería una clave vencida o un estado de prueba anterior. No está firmado: el inicio de la prueba y la última fecha vista se pueden editar; se asume como límite, porque la protección real es la clave firmada.

### D4. Puerta de licencia en Application
Cada caso de uso se marca con un atributo: `RequiresWriteLicense` o `AlwaysAvailable`. Una puerta común (decorador del despachador de casos de uso) evalúa el estado al iniciar y rechaza con un error de licencia los marcados como escritura si el estado es solo lectura. Siempre disponibles: consultas, exportar, copia, restauración, activación y renovación, y ajustes locales (ruta, tema). Una prueba de arquitectura recorre los casos de uso y falla si alguno no está clasificado. *Alternativa descartada*: bloquear en la interfaz; se saltaría con cualquier caso de uso invocado directamente y no protege una futura web. Las migraciones y la apertura de la base no pasan por la puerta.

### D5. Reloj monótono
Se usa como fecha actual el máximo entre el reloj (`IClock`) y la última fecha vista, que se actualiza en cada arranque y al evaluar. No detecta todos los abusos, pero evita el caso trivial de retrasar el reloj para reactivar una licencia vencida.

### D6. Huella de equipo
Hash SHA-256 de un identificador estable del sistema (identificador de máquina en Windows, `/etc/machine-id` en Linux, UUID de plataforma en macOS) más un valor fijo de la aplicación. No incluye nombre de usuario ni de equipo. Si no se puede leer el identificador, se cae a un valor generado y guardado en el almacén local. La huella solo se usa en la comprobación online: el límite de equipos lo aplica el servidor, porque sin red no se puede contar equipos.

### D7. Comprobación online
Un servicio en segundo plano al arrancar, a lo sumo una vez al día (según la fecha guardada de la última comprobación correcta), con tiempo de espera corto y sin reintentos en la misma sesión. Petición mínima: identificador de centro, huella, versión y fecha. La respuesta es un documento firmado con el mismo esquema Ed25519 que las claves, con: identificador de licencia, estado (activa, revocada, equipo no autorizado), motivo (código, sin texto traducido) y opcionalmente una clave renovada. Se acepta solo con firma válida y para la licencia del centro. Los fallos son silenciosos para el usuario y se registran sin datos. Se aplica en el hilo de fondo mediante el mismo caso de uso que la activación manual, de modo que las reglas de renovación son idénticas.

### D8. Aviso y feedback
El estado calculado alimenta un indicador permanente (días restantes) y un aviso no bloqueante en cada arranque en prueba o gracia. Los textos se sirven por claves; el error de licencia lleva un código estable y la interfaz explica cómo activar. Las pantallas son de `ui-shell`.

### D9. Contrato con el servidor
Este cambio deja definido el contrato mínimo que necesita el cliente (petición, respuesta firmada, formato de clave y esquema de firma) para redactar después el documento de requisitos del servidor en `docs/`, cuando lleguen los datos del proyecto de licencias.

## Risks / Trade-offs

- **El servidor no está definido** → el cliente funciona sin él; el contrato mínimo permite completarlo después sin cambiar los specs.
- **Editar el almacén local reinicia la prueba o retrasa el reloj** → límite asumido; el producto no busca antipiratería avanzada y la clave firmada no se puede falsificar.
- **Un centro con licencia caducada se queda sin poder trabajar** → 30 días de gracia con avisos y, aun en solo lectura, consulta, exportar, copiar y restaurar; los datos nunca quedan inaccesibles.
- **La gracia puede coincidir con vacaciones y perderse el aviso** → el indicador permanente y el aviso en cada arranque; la duración es configurable.
- **El límite de equipos no se aplica sin red** → lo aplica el servidor en las comprobaciones; es un control comercial, no de seguridad.
- **Un caso de uso de escritura sin clasificar quedaría sin protección** → prueba de arquitectura que exige clasificar todos.
- **Rotar la clave pública exigiría actualizar la aplicación** → versión de formato en la clave y en la respuesta.

## Migration Plan

Sin migración de base de datos: el estado vive en el almacén local. Un equipo que ya tenía ARCA instalado antes de este cambio empieza su prueba al primer arranque con la nueva versión.

## Open Questions

- Contrato definitivo con el servidor (endpoint, campos exactos, límite de equipos y su política ante excedentes): se cierra con los datos del proyecto de licencias; el cliente ya cubre los tres estados de respuesta.
- Si la duración de la prueba y de la gracia serán las mismas para todos los clientes: hoy son constantes; hacerlas depender de la clave sería una extensión sin cambiar el modelo.
