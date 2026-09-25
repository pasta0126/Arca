## 1. Curso escolar

- [x] 1.1 Crear la entidad `AcademicYear` con nombre derivado, fechas, validación de coherencia y de solapamiento, y estado activo
- [x] 1.2 Reglas de un solo curso activo, primera activación automática, activación solo sin otro activo y eliminación solo sin datos
- [x] 1.3 Casos de uso: crear, activar, listar, consultar y eliminar cursos
- [x] 1.4 Guarda de dominio que rechaza modificar matrículas y asignaciones de un curso no activo (D12), como comprobación por curso y tipo de operación que `cursos-i-historial` amplía
- [x] 1.5 Códigos de error y claves de recurso en catalán del curso escolar
- [x] 1.6 Pruebas: fechas incoherentes, solapamiento, nombre duplicado, segundo curso sin activar, activar con otro activo, sin curso activo, histórico de solo lectura, eliminar con y sin datos

## 2. Dominio de alumnos

- [ ] 2.1 Crear `Student` con nombre y apellidos obligatorios (máx. 100), correo obligatorio y único, y estado activo o de baja con motivo y fecha
- [ ] 2.2 Normalizar y guardar el correo (único, incluye bajas) y la clave de nombre y apellidos para búsquedas, con el componente de comparación central (D2)
- [ ] 2.3 Crear `Enrollment` con nivel obligatorio, grupo opcional y una sola matrícula por alumno y curso
- [ ] 2.4 Crear el catálogo `Level` y `Group` con claves normalizadas, grupo por nivel, creación de valores nuevos y ordenación natural (D6)
- [ ] 2.5 Reglas de baja (con motivo y liberación de taquilla), reactivación y edición de datos, con evento estructurado en el historial del alumno
- [ ] 2.6 Códigos de error y claves de recurso en catalán de alumnos
- [ ] 2.7 Pruebas: datos obligatorios, longitud, equivalencia de grafías en grupos, mismo grupo en distinto nivel, correo obligatorio, inválido y repetido (con otras mayúsculas), homónimos con correos distintos, baja ya registrada, reactivar activo, continuidad de ficha entre cursos

## 3. Casos de uso de alumnos

- [ ] 3.1 Casos de uso de alta manual con confirmación de valores nuevos de catálogo, edición, cambio de nivel o grupo, baja y reactivación
- [ ] 3.2 Búsqueda por nombre, apellidos, nivel, grupo, número de taquilla y estado de asignación, sin mayúsculas ni acentos, mostrando por defecto activos del curso activo (D13)
- [ ] 3.3 Consulta del historial del alumno con texto compuesto desde claves del idioma activo
- [ ] 3.4 Objetos de transferencia de listados sin correo; el detalle de la ficha y de la revisión de la importación sí lo llevan (D11)
- [ ] 3.5 Pruebas con repositorios en memoria: búsquedas, filtro de sin taquilla, incluir bajas, lista vacía y privacidad de listados

## 4. Asignaciones

- [ ] 4.1 Crear `Assignment` con curso, inicio, fin y motivo de cierre, y las reglas de una vigente por alumno y una por taquilla
- [ ] 4.2 Implementar el caso de uso único `AssignLocker` con todas las validaciones de alumno, matrícula, taquilla asignable y reserva (D7)
- [ ] 4.3 Definir `IAssignmentGuard` con hallazgos de dos tipos (aviso que exige confirmación e impedimento que rechaza) y aplicarlos antes de asignar (D8)
- [ ] 4.4 Definir `IAssignmentOpenedHandler`, `IAssignmentClosedHandler` e `IStudentLifecycleHandler` e invocarlos dentro de la transacción, con un contexto de operación extensible, al abrir o cerrar una asignación y al dar de baja o reactivar a un alumno (D9)
- [ ] 4.5 Implementar cambio de taquilla y liberación, ambos indivisibles y con eventos en historiales de alumno y taquilla
- [ ] 4.6 Implementar la sugerencia de taquilla libre de número más bajo de la zona y de la siguiente con disponibilidad
- [ ] 4.7 Implementar la reserva para un alumno, su consumo al asignar y su retirada al dar de baja al alumno
- [ ] 4.8 Implementar las decisiones de reasignar y liberar al averiarse una taquilla ocupada, ampliando el caso de uso de `taquilles-i-zones`
- [ ] 4.9 Consultas de historial de asignaciones por alumno y por taquilla, sin mezclar taquillas de baja con número reutilizado
- [ ] 4.10 Pruebas: asignación correcta, alumno con taquilla, taquilla ocupada, alumno de baja, sin matrícula, taquilla averiada, de baja, reservada sin alumno, para otro alumno y para el mismo alumno, cambio, mismo destino, destino no asignable, liberación, aviso confirmado y rechazado
- [ ] 4.11 Pruebas de equivalencia: asignar desde el alumno, desde la taquilla y arrastrando produce el mismo resultado y las mismas validaciones

## 5. Ocupación real e integración con taquillas

- [ ] 5.1 Implementar la consulta real de ocupación por lotes y sustituir el sustituto de `taquilles-i-zones` (D10)
- [ ] 5.2 Ampliar el hecho de reserva de `Locker` con un alumno opcional sin romper las reservas existentes
- [ ] 5.3 Comprobar que el estado visible derivado usa la ocupación real: ocupada, libre tras liberar, averiada con alumno
- [ ] 5.4 Actualizar las pruebas de `taquilles-i-zones` que usaban el doble de ocupación para que pasen también con la implementación real

## 6. Importación de alumnos

- [ ] 6.1 Definir el puerto `IStudentSheetReader` y su resultado (hojas con nombre y filas de nombre completo y correo) y el intérprete de nombre de hoja a nivel y grupo
- [ ] 6.2 Implementar el lector de ODS con `System.IO.Compression` y `System.Xml`: búsqueda de las cabeceras `Nom complet` y `Correu`, expansión de repeticiones con tope y topes de tamaño descomprimido (D5)
- [ ] 6.3 Validar el formato del fichero: no ODS o dañado, cabecera ausente, nombre de hoja no interpretable, fichero vacío y máximo 5000 filas
- [ ] 6.4 Implementar la función pura de conciliación con reconocimiento por correo y actualización de nombre, nivel y grupo (D3)
- [ ] 6.5 Categorías del plan: nuevo, actualizado, sin cambios, baja propuesta, reactivación propuesta y error, con las validaciones por fila (correo inválido o repetido en el fichero, nombre sin coma, longitud)
- [ ] 6.6 Exclusión de bajas propuestas y salvaguarda con segunda confirmación por encima del 30 % (D4)
- [ ] 6.7 Recopilación de valores nuevos de nivel y grupo durante el análisis y creación solo al confirmar
- [ ] 6.8 Aplicación indivisible con revalidación al confirmar, liberación de taquillas de las bajas y eventos en historiales
- [ ] 6.9 Progreso con recuentos y cancelación antes del guardado; resultado final con recuentos
- [ ] 6.10 Pruebas: alumno nuevo, existente, sin cambios, con nombre distinto, de baja que reaparece, baja por ausencia, correo con otras mayúsculas, correo repetido en una hoja y entre hojas, nombre sin coma, hoja vacía y de una palabra, cabecera ausente, fichero no ODS, importar dos veces, cancelar, fallo a mitad y datos cambiados desde la revisión; y lectura del fichero de ejemplo (16 hojas, 358 alumnos)
- [ ] 6.11 Pruebas de seguridad del lector: ODS con repeticiones de celdas enormes y zip con expansión desproporcionada
- [ ] 6.12 Prueba de volumen: 2000 alumnos existentes y 5000 filas conciliados en un tiempo fluido

## 7. Persistencia

- [ ] 7.1 Entidades y configuraciones EF Core de cursos, alumnos, matrículas, niveles, grupos y asignaciones, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 7.2 Índices únicos parciales: una asignación vigente por taquilla y una por alumno; índice único en el correo normalizado e índice no único en la clave de nombre
- [ ] 7.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [ ] 7.4 Implementación de repositorios y de las operaciones transaccionales de importación, cambio y reasignación
- [ ] 7.5 Pruebas de integración sobre SQLite cifrado temporal: índices, atomicidad, migración con reservas existentes y cierre de asignación con manejadores
- [ ] 7.6 Comprobar que las pruebas pasan en Windows, Linux y macOS con los scripts de verificación (`docs/stack.md`)

## 8. Feedback y guía al usuario

- [ ] 8.1 Devolver el resultado estructurado con recuentos en todos los casos de uso nuevos
- [ ] 8.2 Preparar confirmaciones con su consecuencia: baja de alumno, liberación, importación y segunda confirmación de bajas masivas
- [ ] 8.3 Preparar los estados vacíos: sin curso activo, sin alumnos, sin alumnos sin taquilla, sin taquillas libres, filtros sin resultados
- [ ] 8.4 Claves de recurso en catalán para todos los mensajes, incluidos los de la revisión de la importación
- [ ] 8.5 Pruebas de mensajes de resultado y de estados vacíos

## 9. Verificación transversal

- [ ] 9.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni el lector de ODS
- [ ] 9.2 Prueba de privacidad: un error provocado con datos de un alumno no deja rastro personal en el registro técnico y el correo no sale en listados ni exportaciones
- [ ] 9.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 9.4 Documentar los puntos de enganche para `pagaments` (aviso de deuda, cargos al abrir una asignación, fianza en baja y reactivación) y `claus` (estado de la llave al cerrar una asignación)
- [ ] 9.5 Verificar que el fichero de ejemplo anonimizado (`docs/datos-de-ejemplo-anonimizado.ods`) se importa entero y que la documentación del formato coincide con `importacio-alumnes`
