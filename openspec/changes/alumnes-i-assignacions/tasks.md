## 1. Curso escolar

- [ ] 1.1 Crear la entidad `AcademicYear` con nombre derivado, fechas, validación de coherencia y de solapamiento, y estado activo
- [ ] 1.2 Reglas de un solo curso activo, primera activación automática, activación solo sin otro activo y eliminación solo sin datos
- [ ] 1.3 Casos de uso: crear, activar, listar, consultar y eliminar cursos
- [ ] 1.4 Guarda de dominio que rechaza modificar matrículas y asignaciones de un curso no activo (D12)
- [ ] 1.5 Códigos de error y claves de recurso en catalán del curso escolar
- [ ] 1.6 Pruebas: fechas incoherentes, solapamiento, nombre duplicado, segundo curso sin activar, activar con otro activo, sin curso activo, histórico de solo lectura, eliminar con y sin datos

## 2. Dominio de alumnos

- [ ] 2.1 Crear `Student` con nombre y apellidos obligatorios (máx. 100), correo e identificador opcionales y estado activo o de baja con motivo y fecha
- [ ] 2.2 Calcular y guardar las claves normalizadas de nombre, correo e identificador con el componente de comparación central (D2)
- [ ] 2.3 Crear `Enrollment` con nivel obligatorio, grupo opcional y una sola matrícula por alumno y curso
- [ ] 2.4 Crear el catálogo `Level` y `Group` con claves normalizadas, grupo por nivel, creación de valores nuevos y ordenación natural (D6)
- [ ] 2.5 Reglas de baja (con motivo y liberación de taquilla), reactivación y edición de datos, con evento estructurado en el historial del alumno
- [ ] 2.6 Códigos de error y claves de recurso en catalán de alumnos
- [ ] 2.7 Pruebas: datos obligatorios, longitud, equivalencia de grafías en grupos, mismo grupo en distinto nivel, baja ya registrada, reactivar activo, continuidad de ficha entre cursos

## 3. Casos de uso de alumnos

- [ ] 3.1 Casos de uso de alta manual con confirmación de valores nuevos de catálogo, edición, cambio de nivel o grupo, baja y reactivación
- [ ] 3.2 Búsqueda por nombre, apellidos, nivel, grupo, número de taquilla y estado de asignación, sin mayúsculas ni acentos, mostrando por defecto activos del curso activo (D13)
- [ ] 3.3 Consulta del historial del alumno con texto compuesto desde claves del idioma activo
- [ ] 3.4 Objetos de transferencia de listados sin correo ni identificador; objeto de detalle específico para la revisión de dudosos (D11)
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

- [ ] 6.1 Definir el tipo de correspondencia de columnas extensible y el asistente que propone campos por sinónimos definidos en recursos en catalán, castellano e inglés (D5)
- [ ] 6.2 Guardar y reutilizar la correspondencia por firma de cabeceras; volver a pedirla si cambian
- [ ] 6.3 Reutilizar el lector de CSV de `taquilles-i-zones` y validar formato: UTF-8, separador, fichero vacío y máximo 5000 filas
- [ ] 6.4 Implementar la función pura de conciliación con el reconocimiento por identificador, correo y nombre, y el desempate por nivel y grupo (D3)
- [ ] 6.5 Categorías del plan: nuevo, actualizado, sin cambios, baja propuesta, reactivación propuesta, dudoso y error, con las validaciones por fila
- [ ] 6.6 Resolución de dudosos por el usuario y bloqueo de la confirmación mientras queden sin resolver
- [ ] 6.7 Exclusión de bajas propuestas y salvaguarda con segunda confirmación por encima del 30 % (D4)
- [ ] 6.8 Recopilación de valores nuevos de nivel y grupo durante el análisis y creación solo al confirmar
- [ ] 6.9 Aplicación indivisible con revalidación al confirmar, liberación de taquillas de las bajas y eventos en historiales
- [ ] 6.10 Progreso con recuentos y cancelación antes del guardado; resultado final con recuentos
- [ ] 6.11 Pruebas: cada escenario de reconocimiento, claves contradictorias, homónimos con y sin desempate, homónimos en el mismo fichero, alumno repetido, grupo ausente, claves desactivadas, importar dos veces, cancelar, fallo a mitad y datos cambiados desde la revisión
- [ ] 6.12 Prueba de volumen: 2000 alumnos existentes y 5000 filas conciliados en un tiempo fluido

## 7. Persistencia

- [ ] 7.1 Entidades y configuraciones EF Core de cursos, alumnos, matrículas, niveles, grupos y asignaciones, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 7.2 Índices únicos parciales: una asignación vigente por taquilla y una por alumno; índices no únicos en las claves de reconocimiento
- [ ] 7.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [ ] 7.4 Implementación de repositorios y de las operaciones transaccionales de importación, cambio y reasignación
- [ ] 7.5 Pruebas de integración sobre SQLite cifrado temporal: índices, atomicidad, migración con reservas existentes y cierre de asignación con manejadores
- [ ] 7.6 Comprobar que las pruebas pasan en Windows, Linux y macOS mediante la integración continua

## 8. Feedback y guía al usuario

- [ ] 8.1 Devolver el resultado estructurado con recuentos en todos los casos de uso nuevos
- [ ] 8.2 Preparar confirmaciones con su consecuencia: baja de alumno, liberación, importación y segunda confirmación de bajas masivas
- [ ] 8.3 Preparar los estados vacíos: sin curso activo, sin alumnos, sin alumnos sin taquilla, sin taquillas libres, filtros sin resultados
- [ ] 8.4 Claves de recurso en catalán para todos los mensajes, incluidos los de la revisión de la importación
- [ ] 8.5 Pruebas de mensajes de resultado y de estados vacíos

## 9. Verificación transversal

- [ ] 9.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni la biblioteca de CSV
- [ ] 9.2 Prueba de privacidad: un error provocado con datos de un alumno no deja rastro personal en el registro técnico y correo e identificador no salen en listados ni exportaciones
- [ ] 9.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 9.4 Documentar los puntos de enganche para `pagaments` (aviso de deuda, cargos al abrir una asignación, fianza en baja y reactivación) y `claus` (estado de la llave al cerrar una asignación)
- [ ] 9.5 Cuando llegue el fichero de muestra: comprobar el formato, ajustar la correspondencia por defecto y decidir la política del identificador
