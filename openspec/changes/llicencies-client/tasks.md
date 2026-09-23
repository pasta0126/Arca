## 1. Clave firmada

- [ ] 1.1 Modelo de la clave (versión de formato, licencia, centro, emisión, vencimiento, equipos) y verificación de la firma Ed25519 con clave pública embebida (D1)
- [ ] 1.2 Lectura del texto pegado o de fichero, ignorando espacios y saltos de línea sobrantes
- [ ] 1.3 Reglas de sustitución: vencimiento posterior, rechazo de igual o anterior, confirmación al cambiar de centro
- [ ] 1.4 Códigos de error y claves de recurso en catalán de la clave
- [ ] 1.5 Pruebas: firma válida, un carácter alterado, formato incorrecto, vencimiento manipulado, renovación, clave antigua y clave de otro centro

## 2. Estado de la licencia

- [ ] 2.1 `LicenseState`, `LicenseSettings` (30 días de prueba y de gracia) y la función pura `Evaluate` (D2)
- [ ] 2.2 Reloj monótono con la última fecha vista (D5)
- [ ] 2.3 Pruebas de plazos: prueba día 1, 30 y 31, vencimiento, gracia día 1, 30 y 31, revocación, activar durante la prueba y reloj atrasado

## 3. Almacén local

- [ ] 3.1 Almacén de clave, inicio de la prueba, última fecha vista, revocación y última comprobación fuera de la base de datos (D3)
- [ ] 3.2 Escritura atómica del almacén y tolerancia a fichero ausente o dañado sin perder la posibilidad de reactivar
- [ ] 3.3 Pruebas de integración: base de datos nueva conserva la prueba, restaurar una copia antigua no cambia la licencia, portable y no portable

## 4. Huella de equipo

- [ ] 4.1 Cálculo de la huella por sistema operativo con valor fijo de la aplicación y alternativa generada (D6)
- [ ] 4.2 Pruebas: estabilidad, ausencia de nombre de usuario y de equipo, alternativa cuando no se puede leer el identificador
- [ ] 4.3 Comprobar el cálculo en Windows, Linux y macOS mediante la integración continua

## 5. Puerta de licencia

- [ ] 5.1 Atributos `RequiresWriteLicense` y `AlwaysAvailable` y decorador común que evalúa el estado al iniciar cada caso de uso (D4)
- [ ] 5.2 Clasificar los casos de uso de todos los cambios anteriores: consultas, exportar, copia, restauración, activación y ajustes locales como siempre disponibles y el resto como escritura
- [ ] 5.3 Excluir de la puerta la apertura y las migraciones de la base de datos
- [ ] 5.4 Prueba de arquitectura: todo caso de uso está clasificado
- [ ] 5.5 Pruebas: escritura rechazada en solo lectura, exportar, copiar y restaurar permitidos, reactivar sin reiniciar y operación en curso no interrumpida

## 6. Comprobación online

- [ ] 6.1 Cliente HTTP con tiempo de espera corto que envía solo centro, huella, versión y fecha (D7)
- [ ] 6.2 Servicio en segundo plano al arrancar, como mucho una vez al día, sin bloquear el arranque
- [ ] 6.3 Verificación de la respuesta firmada y de que corresponde a la licencia del centro
- [ ] 6.4 Aplicar revocación, equipo no autorizado, revocación levantada y renovación reutilizando el caso de uso de activación
- [ ] 6.5 Registro técnico sin clave completa, sin datos de alumnos y sin contenido de la respuesta
- [ ] 6.6 Pruebas con servidor simulado: sin red, tiempo de espera, respuesta sin firma, de otra licencia, revocación, renovación válida e inválida, ya comprobada hoy y contenido exacto de la petición

## 7. Feedback y guía al usuario

- [ ] 7.1 Exponer el estado y los días restantes para el indicador permanente y el aviso al arrancar (D8)
- [ ] 7.2 Resultado de activación y renovación con el nuevo vencimiento
- [ ] 7.3 Error de licencia con código estable y guía para reactivar
- [ ] 7.4 Consulta de la licencia sin mostrar la clave completa
- [ ] 7.5 Claves de recurso en catalán para todos los mensajes
- [ ] 7.6 Pruebas de mensajes, avisos en prueba y gracia y ausencia de aviso con la licencia activa

## 8. Verificación transversal

- [ ] 8.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core, red ni el sistema de ficheros
- [ ] 8.2 Prueba de que la base de datos se abre y migra con la licencia en solo lectura o revocada
- [ ] 8.3 Prueba de privacidad: el tráfico de la comprobación no contiene datos de alumnos con una base llena
- [ ] 8.4 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 8.5 Prueba de extremo a extremo: prueba, activación, caducidad, gracia, solo lectura con exportar y copiar, renovación y reactivación
- [ ] 8.6 Redactar en `docs/` el documento de requisitos del servidor de licencias a partir del contrato (D9), cuando se reciban los datos del proyecto de licencias
