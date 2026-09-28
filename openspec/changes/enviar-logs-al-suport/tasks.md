## 1. Reglas y contrato público

- [ ] 1.1 Enmendar la regla 8.4 de `docs/convenciones.md`, el resumen de `AGENTS.md`, `openspec/config.yaml` y los principios de `docs/registro-de-instalaciones.md`: tres comunicaciones de red posibles, la tercera a petición expresa (D9)
- [ ] 1.2 Crear `docs/registro-de-logs.md` con la petición `POST /v1/logs`, campos, límites (2 MB), respuestas (`201`, `413`, `429`), compromisos del servidor (IP no conservada, retención propuesta de 90 días) y lo que nunca se envía (D4, D6, D7)
- [ ] 1.3 Anotar en `docs/riesgos.md` el riesgo de contenido personal en el registro y el abuso del endpoint anónimo

## 2. Fichero actual y contenido enviado

- [ ] 2.1 Cambiar `FileErrorLog` a `shared: true` para poder leer el fichero abierto sin detener el registro, con prueba en los tres sistemas (D3)
- [ ] 2.2 Localizar el fichero de registro más reciente por fecha de modificación y leerlo en memoria con `FileShare.ReadWrite` (D3)
- [ ] 2.3 Tipo cerrado `SupportLogPayload` con fichero, nombre, versión, sistema, arquitectura, identificador opcional y código de incidencia opcional (D4)
- [ ] 2.4 Validación del código de incidencia (1 a 32 caracteres `[A-Za-z0-9-]`) y del límite de tamaño de 2 MB (D5)
- [ ] 2.5 Pruebas: fichero actual entre varios rotados, fichero en uso, sin registro o vacío, contenido mínimo, con identificador, con código, código inválido, tamaño excedido, prueba de arquitectura de que el constructor no referencia `Domain` de alumnos y prueba de contrato contra `docs/registro-de-logs.md`

## 3. Envío

- [ ] 3.1 Interfaz `ISupportLogUploader` en Application y caso de uso de preparación (nombre, tamaño, datos que acompañan) con resultado estructurado (D2)
- [ ] 3.2 Implementación HTTP en Infrastructure: HTTPS obligatorio, `multipart/form-data`, tiempo de espera de 30 segundos, sin reintentos, cancelable, sin credenciales (D2, D6)
- [ ] 3.3 Caso de uso de envío con traducción de `201`, `413`, `429`, otros errores, sin red y tiempo agotado a resultados con clave i18n (D7)
- [ ] 3.4 Protección contra doble ejecución y cancelación antes de terminar (D10)
- [ ] 3.5 Registro técnico de fallos del envío sin contenido ni código de incidencia
- [ ] 3.6 Pruebas con manejador HTTP falso: ninguna petición sin acción del usuario, ninguna petición al cancelar, dirección no `https` rechazada, envío correcto con referencia, cada código de error, sin conexión, tiempo agotado, doble clic con un solo envío y sin datos en el registro del propio envío

## 4. Privacidad

- [ ] 4.1 Prueba de privacidad: provocar un error con un alumno de nombre y valores identificables y comprobar que ni el fichero de registro ni la petición de envío dejan rastro (D8)
- [ ] 4.2 Prueba de que el repositorio no contiene tokens ni claves de acceso al servicio y de que el envío funciona con el registro de instalaciones desactivado sin cambiar consentimientos

## 5. Pantallas y enganches

- [ ] 5.1 Acción «Enviar registre tècnic» con diálogo de confirmación: nombre y tamaño del fichero, lista exacta de datos, enlace a la descripción pública y campo opcional de código de incidencia, en coordinación con `ui-shell` (D10)
- [ ] 5.2 Progreso, resultado con la referencia del envío y errores mediante las notificaciones comunes
- [ ] 5.3 Estado «no hay nada que enviar» sin ofrecer confirmar
- [ ] 5.4 Claves de recurso en catalán para todos los textos
- [ ] 5.5 Pruebas de mensajes, estados sin conexión, doble clic y cancelación

## 6. Verificación transversal

- [ ] 6.1 Ejecutar `openspec validate enviar-logs-al-suport` y la suite completa en macOS, con el envío también probado contra un servidor local de pruebas
- [ ] 6.2 Prueba puntual en Windows del bloqueo del fichero de registro y del envío
- [ ] 6.3 Revisar que cualquier campo nuevo aparece en `docs/registro-de-logs.md` y que las cuatro fuentes de la regla de red son coherentes
- [ ] 6.4 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 6.5 Marcar el cambio como listo para revisión de la persona responsable
