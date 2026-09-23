## 1. Dominio de incidencias y motivos

- [ ] 1.1 Crear `Incident` con taquilla, tipo (averiada o en mantenimiento), motivo, nota (máx. 500), fecha de inicio, fecha y nota de resolución y código de cierre automático por baja (D1)
- [ ] 1.2 Reglas de fechas: inicio nunca futuro, reparación nunca futura ni anterior al inicio
- [ ] 1.3 Reglas de una sola incidencia abierta por taquilla, de que no se reabre y de que una incidencia reparada no se modifica
- [ ] 1.4 Crear `IncidentReason` con nombre (máx. 60), clave normalizada, estado activo y reglas de duplicado, desactivación y eliminación solo sin uso (D5)
- [ ] 1.5 Códigos de error y claves de recurso en catalán de incidencias y motivos
- [ ] 1.6 Pruebas: fechas en cada límite, segunda incidencia abierta, taquilla de baja, motivo obligatorio, motivo desactivado, nombre duplicado con otra grafía, renombrar, desactivar y eliminar en uso y sin uso

## 2. Casos de uso de incidencias

- [ ] 2.1 Implementar `OpenIncident` como una única operación indivisible que compone comprobaciones, decisión sobre la taquilla ocupada, marca de fuera de servicio y registro (D3)
- [ ] 2.2 Devolver el resultado de "decisión requerida" sin cambiar nada cuando la taquilla está ocupada y falta la decisión, y revertir todo si la decisión no puede aplicarse
- [ ] 2.3 Implementar el cambio de tipo, de motivo y de nota de una incidencia abierta con eventos estructurados en el historial de la taquilla (D7)
- [ ] 2.4 Implementar `RepairIncident` con fecha y nota de resolución opcional, sin confirmación (D4)
- [ ] 2.5 Implementar `ILockerRetiredHandler` que cierra la incidencia abierta con la fecha de baja y un código de cierre automático (D6)
- [ ] 2.6 Casos de uso del catálogo de motivos: añadir, renombrar, desactivar, reactivar, eliminar y listar
- [ ] 2.7 Inicializador de primer arranque de la lista de motivos con las claves de recurso del idioma activo y un indicador para no repetirlo (D5)
- [ ] 2.8 Pruebas con repositorios en memoria: abrir con cada decisión, decisión ausente, decisión que falla con reversión completa, cambio de tipo sin decisión adicional, reparar con su estado visible resultante, baja con incidencia abierta y lista inicial creada una sola vez

## 3. Integración con taquillas

- [ ] 3.1 Hacer internos los casos de uso de fuera de servicio de `taquilles-i-zones` y exponer solo los de incidencias (D1)
- [ ] 3.2 Prueba de arquitectura que impide marcar o resolver el fuera de servicio por otro camino
- [ ] 3.3 Prueba de invariante: una taquilla está fuera de servicio si y solo si tiene una incidencia abierta del mismo tipo, tras cualquier secuencia de operaciones
- [ ] 3.4 Ajustar las pruebas de `taquilles-i-zones` que usaban directamente los casos de uso ahora internos

## 4. Consultas

- [ ] 4.1 Historial de incidencias de una taquilla, de la más reciente a la más antigua, con duración en días calculada con el reloj inyectado (D7)
- [ ] 4.2 Lista de taquillas fuera de servicio con tipo, motivo, inicio, días, indicación de alumno conservado, filtros por tipo, motivo y zona y total (D8)
- [ ] 4.3 Objetos de transferencia sin notas en listados y con notas solo en el detalle de la incidencia
- [ ] 4.4 Pruebas: historial con y sin incidencias, número de taquilla reutilizado, filtros combinados, taquilla con alumno, lista vacía y privacidad de notas

## 5. Persistencia

- [ ] 5.1 Entidades y configuraciones EF Core de incidencias y motivos, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 5.2 Índice único parcial de una incidencia abierta por taquilla e índice único de la clave normalizada del motivo (D2)
- [ ] 5.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [ ] 5.4 Implementación de repositorios y de las operaciones transaccionales, incluida la decisión sobre asignaciones dentro de la misma transacción
- [ ] 5.5 Pruebas de integración sobre SQLite cifrado temporal: índices, atomicidad al abrir con decisión, baja con incidencia abierta y primer arranque
- [ ] 5.6 Comprobar que las pruebas pasan en Windows, Linux y macOS mediante la integración continua

## 6. Feedback y guía al usuario

- [ ] 6.1 Devolver el resultado estructurado con la taquilla afectada en todos los casos de uso nuevos
- [ ] 6.2 Preparar los estados vacíos: sin incidencias en la taquilla, sin taquillas fuera de servicio, sin motivos activos con acción para crearlos
- [ ] 6.3 Claves de recurso en catalán para todos los mensajes, incluida la nota automática de cierre por baja
- [ ] 6.4 Pruebas de mensajes de resultado y de estados vacíos

## 7. Verificación transversal

- [ ] 7.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core
- [ ] 7.2 Prueba de privacidad: un error provocado con una nota que contiene datos de un alumno no deja rastro en el registro técnico
- [ ] 7.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 7.4 Prueba de extremo a extremo: abrir una incidencia en una taquilla ocupada manteniendo al alumno, repararla y comprobar que vuelve a estar ocupada; abrirla reasignando y comprobar ambas taquillas
- [ ] 7.5 Documentar el punto de enganche para `manteniment`: las tareas programadas usarán los casos de uso de incidencias con el tipo en mantenimiento
