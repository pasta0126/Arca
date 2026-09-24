## 1. Verificación de ficheros de base de datos

- [ ] 1.1 Extraer del migrador un servicio compartido que abre un fichero con la clave, comprueba la integridad y clasifica su versión: igual, anterior o más nueva (D3)
- [ ] 1.2 Servicio de recuentos en solo lectura de cursos, alumnos, taquillas y asignaciones, sin leer datos personales (D4)
- [ ] 1.3 Pruebas: fichero que no es de ARCA, clave incompatible, copia dañada, versión anterior, igual y más nueva

## 2. Copia de seguridad

- [ ] 2.1 Copia consistente con la API de copia en línea sobre una conexión propia y fichero temporal en el destino (D2)
- [ ] 2.2 Verificación de la copia y movimiento atómico al nombre final; borrado del temporal ante error o cancelación
- [ ] 2.3 Validación previa del destino: no es la base de datos, existe, admite escritura y tiene espacio (D9)
- [ ] 2.4 Nombre propuesto con fecha y hora, extensión propia, última carpeta usada y confirmación de sobrescritura
- [ ] 2.5 Pruebas de integración sobre SQLite cifrado temporal: copia tras un cambio, copia con la aplicación en uso, fallo a mitad sin fichero parcial, fichero anterior intacto, destino inválido, sin datos legibles en el fichero

## 3. Restauración

- [ ] 3.1 Caso de uso de análisis que verifica el fichero y devuelve la vista previa con la comparación de recuentos (D3, D4)
- [ ] 3.1b Pedir la contraseña o la clave de recuperación de la copia antes de la vista previa y adoptar su llave al restaurar (`acces-i-xifrat`)
- [ ] 3.2 Secuencia de restauración: cerrar, copia previa verificada, temporal, migración, sustitución atómica y reapertura (D5)
- [ ] 3.3 Recuperación automática desde la copia previa ante fallo en cualquier punto, e información de rutas si también fallara
- [ ] 3.4 Copia previa de una base dañada sin verificar, y ofrecer la restauración desde el error de fichero dañado
- [ ] 3.5 Retención de las 3 copias previas a restauración, separada de las de migración (D6)
- [ ] 3.6 Limpieza de temporales huérfanos al arrancar
- [ ] 3.7 Recarga del estado en memoria tras restaurar, sin caches de los datos anteriores (D7)
- [ ] 3.8 Pruebas de integración: restauración correcta, copia antigua migrada con la original intacta, copia más nueva rechazada, fallo al migrar con recuperación, fallo al sustituir, base dañada, instalación nueva, corte simulado y retención

## 4. Feedback y guía al usuario

- [ ] 4.1 Devolver el resultado estructurado con ubicación, tamaño y etapas reales en copia y restauración (D10)
- [ ] 4.2 Confirmaciones con su consecuencia: aviso de datos de menores al copiar y sustitución de datos al restaurar
- [ ] 4.3 Progreso sin bloquear la interfaz, cancelación antes de sustituir y protección contra doble ejecución
- [ ] 4.4 Mensajes comprensibles para cada rechazo de verificación y de destino
- [ ] 4.5 Claves de recurso en catalán para todos los mensajes
- [ ] 4.6 Pruebas de mensajes, cancelación, doble ejecución y ausencia de datos de alumnos en el registro técnico

## 5. Verificación transversal

- [ ] 5.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni el sistema de ficheros
- [ ] 5.2 Prueba de que copiar y restaurar funcionan sin conexión a la red (D8)
- [ ] 5.3 Prueba de transportabilidad: copia hecha en un sistema operativo y restaurada en otro con los scripts de verificación (`docs/stack.md`) en Windows, Linux y macOS
- [ ] 5.4 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 5.5 Prueba de extremo a extremo: datos, copia, cambios posteriores, restauración y comprobación de que se pierden los cambios y se conserva la copia previa
- [ ] 5.6 Documentar el punto de enganche con `cursos-i-historial` (oferta de copia antes de borrar o anonimizar)
