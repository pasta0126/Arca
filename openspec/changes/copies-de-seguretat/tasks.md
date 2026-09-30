## 1. Verificación de ficheros de base de datos

- [x] 1.1 Extraer del migrador un servicio compartido que abre un fichero con la clave, comprueba la integridad y clasifica su versión: igual, anterior o más nueva (D3)
- [x] 1.2 Servicio de recuentos en solo lectura de cursos, alumnos, taquillas y asignaciones, sin leer datos personales (D4)
- [x] 1.3 Pruebas: fichero que no es de ARCA, clave incompatible, copia dañada, versión anterior, igual y más nueva

## 2. Copia de seguridad

- [x] 2.1 Copia consistente con la API de copia en línea sobre una conexión propia y fichero temporal en el destino (D2)
- [x] 2.2 Verificación de la copia y movimiento atómico al nombre final; borrado del temporal ante error o cancelación
- [x] 2.3 Validación previa del destino: no es la base de datos, existe, admite escritura y tiene espacio (D9)
- [x] 2.4 Nombre propuesto con fecha y hora y extensión propia (la última carpeta usada y la confirmación de sobrescritura van con el flujo de Ajustes, 5.2 y 5.3)
- [x] 2.5 Pruebas de integración sobre SQLite cifrado temporal: copia tras un cambio, copia con la aplicación en uso, fallo a mitad sin fichero parcial, fichero anterior intacto, destino inválido, sin datos legibles en el fichero

## 3. Restauración

- [x] 3.1 Caso de uso de análisis que verifica el fichero y devuelve la vista previa con la comparación de recuentos (D3, D4)
- [x] 3.1b Pedir la contraseña o la clave de recuperación de la copia antes de la vista previa y adoptar su llave al restaurar (`acces-i-xifrat`)
- [x] 3.2 Secuencia de restauración: cerrar, copia previa verificada, temporal, migración, sustitución atómica y reapertura (D5)
- [x] 3.3 Recuperación automática desde la copia previa ante fallo en cualquier punto, e información de rutas si también fallara
- [x] 3.4 Copia previa de una base dañada sin verificar (ofrecer la restauración desde el error de fichero dañado y desde el primer arranque queda para `configuracio-inicial`; aquí el servicio ya lo permite)
- [x] 3.5 Retención de las 3 copias previas a restauración, separada de las de migración (D6)
- [x] 3.6 Limpieza de temporales huérfanos al arrancar
- [x] 3.7 Reinicio de la aplicación tras restaurar, sin caches de los datos anteriores, y aviso si no puede hacerse (D7, D13)
- [x] 3.8 Pruebas de integración: restauración correcta, copia antigua migrada con la original intacta, copia más nueva rechazada, fallo al migrar con recuperación, fallo al sustituir, base dañada, instalación nueva, corte simulado y retención

## 4. Feedback y guía al usuario

- [x] 4.1 Devolver el resultado estructurado con ubicación, tamaño y etapas reales en copia y restauración (D10)
- [x] 4.2 Confirmaciones con su consecuencia: aviso de datos de menores al copiar y sustitución de datos al restaurar
- [x] 4.3 Progreso sin bloquear la interfaz, cancelación antes de sustituir y protección contra doble ejecución
- [x] 4.4 Mensajes comprensibles para cada rechazo de verificación y de destino
- [x] 4.5 Claves de recurso en catalán para todos los mensajes
- [x] 4.6 Pruebas de mensajes, cancelación, doble ejecución y ausencia de datos de alumnos en el registro técnico

## 5. Ajustes: copia y restauración

- [x] 5.1 Servicio de Application (`IBackupService`) que envuelve lo de Infrastructure: nombre propuesto, validación del destino, copia, apertura, desbloqueo, vista previa y restauración (D11)
- [x] 5.2 Selectores de fichero de copia (guardar y abrir) y memoria de la última carpeta en las preferencias locales
- [x] 5.3 Flujo de «Fer una còpia» con aviso de datos de menores, confirmación de sobrescritura, progreso, resultado con ubicación y tamaño y errores comprensibles (D14)
- [x] 5.4 Flujo de «Restaurar una còpia»: fichero, contraseña o clave de recuperación de la copia, vista previa con comparación, aviso del cambio de contraseña, confirmación y aplicación (D14)
- [x] 5.5 Reinicio de la aplicación tras restaurar y aviso si no se puede (D13)
- [x] 5.6 Bloque plegable «Còpia de seguretat» en Ajustes, siempre disponible, y pruebas de modelo y de pantalla

## 6. Verificación transversal

- [ ] 6.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni el sistema de ficheros
- [ ] 6.2 Prueba de que copiar y restaurar funcionan sin conexión a la red (D8)
- [ ] 6.3 Prueba de transportabilidad: copia hecha en un sistema operativo y restaurada en otro con los scripts de verificación (`docs/stack.md`) en Windows, Linux y macOS
- [ ] 6.4 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 6.5 Prueba de extremo a extremo: datos, copia, cambios posteriores, restauración y comprobación de que se pierden los cambios y se conserva la copia previa
- [ ] 6.6 Documentar el punto de enganche con `cursos-i-historial` (oferta de copia antes de borrar o anonimizar)
