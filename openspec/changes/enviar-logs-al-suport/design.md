## Context

Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `arquitectura-base` (registro técnico con `FileErrorLog`, ajustes locales) y reutiliza el identificador de instalación y la versión de `registre-i-actualitzacions`. Convención de privacidad: `docs/convenciones.md`, sección 8.

Restricciones: datos de menores en el equipo, ningún dato de alumnos sale sin control, coste cero, funciona igual sin red, servidor en un proyecto aparte, ningún secreto en el repositorio.

Estado actual del registro: `FileErrorLog` (Infrastructure) escribe con Serilog en `arca-.log` con rotación por tamaño (1 MB, 5 ficheros) y `shared: false`, por lo que el fichero actual queda bloqueado por la propia aplicación mientras corre.

## Goals / Non-Goals

**Goals:**
- Un envío puntual, transparente y de contenido cerrado del fichero actual.
- Que quien mantiene ARCA reciba el fichero con la versión y el sistema para correlacionarlo.
- Cero tráfico si el usuario no lo pide.

**Non-Goals:**
- Telemetría, envío automático, nota libre, adjuntos, cola de reintentos y el servidor.

## Decisions

### D1. Acción puntual, no consentimiento permanente
Es un botón con confirmación cada vez, no una casilla en ajustes. Así no hay estado de consentimiento que gestionar, y encaja con «nada sale del equipo sin control». No depende de los consentimientos de `registre-i-actualitzacions`; solo lee de sus ajustes el identificador, si existe.

### D2. Un único punto de red para este envío
Interfaz `ISupportLogUploader` en Application, implementada en Infrastructure con `HttpClient` (HTTPS, tiempo de espera de 30 segundos por el tamaño, sin reintentos, cancelable). El caso de uso de envío recibe la interfaz y no toca la red directamente, de modo que las pruebas usan un manejador HTTP falso que falla si recibe una petición no pedida.

### D3. Localizar y leer el fichero actual
El «actual» es el fichero de registro más reciente por fecha de modificación de la carpeta de registros (la rotación por tamaño añade sufijos numéricos, así que no se deduce del nombre). Como `shared: false` bloquea el fichero, se cambia `FileErrorLog` a `shared: true` para poder abrirlo en lectura con `FileShare.ReadWrite` sin detener el registro. Se lee en memoria (máximo 1 MB más margen; el límite del cliente es 2 MB) y el tamaño mostrado en la confirmación es el de esa lectura, para que lo previsto y lo enviado coincidan. *Alternativa descartada*: cerrar y reabrir el logger para liberar el fichero; añade estado y riesgo de perder entradas.

### D4. Petición cerrada por construcción
`SupportLogPayload` es un tipo con campos fijos: contenido del fichero, nombre del fichero, versión, sistema, arquitectura, identificador de instalación (nulo si no hay registro) y código de incidencia (nulo si no se escribe). Se construye desde el fichero y datos del sistema, sin acceso a repositorios ni a `Domain` de alumnos (prueba de arquitectura, igual que `RegistrationPayload`). Se envía como `multipart/form-data`: un campo `file` con `text/plain` y los demás como campos simples. Una prueba de contrato compara los campos serializados con `docs/registro-de-logs.md`.

### D5. Código de incidencia opcional y validado
Es la única entrada del usuario: 1 a 32 caracteres `[A-Za-z0-9-]`. Sirve para que quien recibe el fichero lo asocie a una conversación previa. Se descarta el texto libre porque podría llevar datos de alumnos y porque el fichero ya es la información útil. Es validación de formato en Application, con resultado estructurado y mensaje por clave i18n.

### D6. Sin autenticación en el cliente
El endpoint acepta subidas anónimas; el servidor limita tamaño (2 MB) y frecuencia por origen y descarta la IP como en el contrato de instalaciones. Cualquiera podría enviar basura al servidor: el impacto es solo almacenamiento, mitigado por los límites y por que nada se ejecuta ni se muestra sin revisión humana. Un token embebido sería un secreto público en un repositorio libre y daría falsa seguridad.

### D7. Respuesta y referencia
El servidor responde `201` con `{ "reference": "…" }`. La aplicación muestra esa referencia para citarla. Códigos de error tratados: `413` (tamaño), `429` (frecuencia), `4xx/5xx` (error genérico) y fallos de red o tiempo agotado. Todos se traducen a mensajes por clave i18n; nunca se muestra el cuerpo de la respuesta.

### D8. Privacidad: defensa en profundidad
El fichero ya es seguro por la convención 8.2 (tipos, contexto y pila, nunca mensajes). Aun así, el diseño no depende solo de eso: (a) prueba que provoca un error con datos de alumno y comprueba que ni el fichero ni la petición los contienen; (b) el envío muestra el tamaño y el nombre, pero no el contenido íntegro; (c) el registro técnico del propio envío no incluye contenido ni código de incidencia. **No se añade una vista del contenido completo** en la confirmación: complicaría la pantalla para un perfil no técnico y el contenido es técnico por construcción; el enlace a la descripción pública explica qué contiene.

### D9. Enmienda de la regla de red
La regla 8.4 pasa a decir que las comunicaciones de red posibles son tres: registro opcional, aviso de versión y envío del registro técnico a petición expresa, todas con contenido cerrado y documentado. Se actualizan `docs/convenciones.md`, `AGENTS.md`, `openspec/config.yaml` y los principios de `docs/registro-de-instalaciones.md`, y se enlaza el nuevo `docs/registro-de-logs.md`. Se hace en el primer grupo de tareas, antes de escribir código.

### D10. Feedback
Resultado estructurado en preparar y en enviar, estados de progreso, protección contra doble ejecución y cancelación antes de completar, según los principios de UX transversal. Ubicación del botón (ajustes o ayuda) a coordinar con `ui-shell`/`ux-fonaments`.

## Risks / Trade-offs

- **El fichero pudiera contener un dato personal por un error de código futuro** → prueba de privacidad por cambio y la regla 8.2 como puerta; el envío es siempre voluntario y con confirmación.
- **Envío de basura o abuso del endpoint anónimo** → límites de tamaño y frecuencia en el servidor y validación en el cliente; sin ejecución de contenido.
- **`shared: true` cambia el modo de apertura del log** → comportamiento de escritura igual; se cubre con pruebas en Windows, Linux y macOS (en Windows es donde el bloqueo importa).
- **El usuario no técnico no sabe qué está enviando** → texto de transparencia claro, enlace a la descripción pública y la lista exacta de campos.
- **Un servidor caído** → el envío falla con mensaje comprensible; la aplicación no depende de él.
- **La IP del remitente llega al servidor** → contrato: no se conserva, igual que en el registro de instalaciones.

## Migration Plan

Sin migración de base de datos ni de ajustes. El único cambio en datos existentes es `shared: true` en el logger, que no altera el formato de los ficheros.

## Open Questions

- Dirección del servicio (dominio) y el límite exacto de frecuencia: los fija el proyecto del servidor; la dirección se configura en la aplicación, no en el repositorio como secreto.
- Retención de los ficheros recibidos: propuesta de 90 días; se confirma al documentar el contrato.
- Ubicación del botón en la interfaz: se decide con `ui-shell`.
