## 1. Dominio de zonas

- [x] 1.1 Crear la entidad `Zone` con nombre recortado, longitud máxima 60, clave normalizada y estado activo
- [x] 1.2 Reglas de creación, renombrado (excluyéndose a sí misma), desactivación (sin taquillas activas), reactivación y eliminación (solo sin historial)
- [x] 1.3 Códigos de error de negocio de zonas y sus claves de recurso en catalán
- [x] 1.4 Pruebas: nombre vacío, largo, con espacios, duplicado con otra grafía, duplicado con desactivada, renombrar con el mismo nombre, desactivar con taquillas activas, eliminar con historial

## 2. Dominio de taquillas

- [x] 2.1 Crear la entidad `Locker` con identificador interno, número (1 a 99999), zona, nota (máx. 500), estado de fuera de servicio con tipo (averiada o en mantenimiento), reserva con nota y fecha de baja
- [x] 2.2 Implementar la función pura del estado visible con la precedencia baja, fuera de servicio, ocupación, reserva y libre (D1)
- [x] 2.3 Reglas de reserva, avería, mantenimiento (mismas reglas, tipos excluyentes y cambio de tipo) y vuelta a servicio, cambio de número y zona, y baja definitiva sin reactivación
- [x] 2.4 Resultado de "decisión requerida" al averiar una taquilla ocupada y opción de mantener; error de opción no disponible para reasignar y liberar (D3)
- [x] 2.5 Definir el tipo de evento de historial con código extensible y valores anterior y nuevo estructurados (D7)
- [x] 2.6 Códigos de error de negocio de taquillas y sus claves de recurso en catalán
- [x] 2.7 Pruebas del estado derivado con la tabla completa de combinaciones, incluidas avería con alumno, avería con reserva y resolución de avería
- [x] 2.8 Pruebas de reglas: número no válido, reservar una ocupada, avería en una de baja, baja con asignación o con reserva, cambios sobre una de baja

## 3. Casos de uso de aplicación

- [ ] 3.1 Definir `ILockerRetiredHandler` (D3b) y los puertos: repositorios de zonas, taquillas y eventos, consulta de ocupación (D2) y `ICsvReader` (D8)
- [ ] 3.2 Implementar el sustituto de ocupación "sin asignación" y un doble configurable para pruebas
- [ ] 3.3 Casos de uso de zonas: crear, renombrar, desactivar, reactivar, eliminar y listar con recuento de activas
- [ ] 3.4 Casos de uso de taquillas: alta individual, reservar, quitar reserva, marcar y resolver avería, cambiar número y zona, dar de baja
- [ ] 3.5 Registro del evento de historial en la misma operación que origina cada cambio
- [ ] 3.6 Consulta del historial de una taquilla, más reciente primero, con texto compuesto desde claves del idioma activo
- [ ] 3.7 Consulta de taquillas con filtros por zona, estado y número, inclusión opcional de bajas, orden por número y contadores por estado y zona (D9)
- [ ] 3.8 Pruebas de los casos de uso con repositorios en memoria

## 4. Altas masivas

- [ ] 4.1 Análisis y confirmación de alta por rangos en dos fases con revalidación al confirmar (D6)
- [ ] 4.2 Validaciones de rango: invertido, un solo número, máximo 1000, zona activa, conflictos con lista completa de números
- [ ] 4.3 Pruebas: rango correcto, con conflicto, invertido, de un número, excesivo y fallo a mitad de guardado (sin taquillas creadas)
- [ ] 4.4 Análisis de importación: detección de columnas por cabecera del idioma activo, columnas obligatoria y opcional, columnas desconocidas ignoradas con aviso
- [ ] 4.5 Validación por fila: número, repetición en el fichero, número en uso, número de una baja, zona vacía, desactivada, con otra grafía, inexistente y nota larga
- [ ] 4.6 Opción de crear zonas que faltan, con deduplicación de grafías equivalentes y recuento de zonas nuevas
- [ ] 4.7 Confirmación que importa solo las filas válidas en una transacción, revalida y devuelve el análisis actualizado si algo cambió
- [ ] 4.8 Plantilla CSV descargable en UTF-8 con BOM y separador punto y coma
- [ ] 4.9 Pruebas: fichero vacío, más de 5000 filas, codificación no válida, separadores punto y coma y coma, comillas con separador, importar dos veces, errores parciales, todas erróneas, cancelar sin efectos

## 5. Persistencia

- [ ] 5.1 Entidades EF Core y configuraciones de zonas, taquillas y eventos en `Infrastructure`, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 5.2 Índice único parcial del número entre taquillas sin baja (D4) e índice único de la clave normalizada de zona (D5)
- [ ] 5.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [ ] 5.4 Implementación de repositorios y de las operaciones transaccionales de alta masiva e importación
- [ ] 5.5 Implementación de `ICsvReader` con UTF-8 estricto, BOM, detección de separador, comillas y número de línea (D8)
- [ ] 5.6 Pruebas de integración sobre un fichero SQLite cifrado temporal: índices, coexistencia de baja y activa con el mismo número, atomicidad y rechazo de duplicados
- [ ] 5.7 Comprobar que las pruebas se ejecutan en Windows, Linux y macOS con los scripts de verificación (`docs/stack.md`)

## 6. Feedback y guía al usuario

- [ ] 6.1 Devolver el resultado estructurado con recuentos en todos los casos de uso de zonas, taquillas, rangos e importación (D9b)
- [ ] 6.2 Informar progreso con recuentos y aceptar cancelación en el análisis y en la importación, solo antes de la transacción de guardado
- [ ] 6.3 Preparar las solicitudes de confirmación de baja, alta por rangos e importación, con su consecuencia como datos localizados
- [ ] 6.4 Preparar los estados vacíos del inventario (sin zonas, sin taquillas, filtros sin resultados) con su acción sugerida
- [ ] 6.5 Consulta de detalle e historial de una taquilla bajo demanda, sin carga perezosa implícita
- [ ] 6.6 Pruebas: recuentos del resultado, cancelación durante el análisis sin datos creados, confirmación rechazada sin efectos

## 7. Verificación transversal

- [ ] 7.1 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 7.2 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni la biblioteca de CSV
- [ ] 7.3 Prueba de volumen con 1000 taquillas para confirmar que la consulta con filtros responde con fluidez
- [ ] 7.4 Documentar en la config del proyecto el punto de enganche con `alumnes-i-assignacions`: implementar la ocupación real, la reserva con alumno y las decisiones de reasignar y liberar
