## 1. Dominio de la llave

- [ ] 1.1 Añadir a `Assignment` el estado de llave, la fecha de la última transición y el recuento de copias (D1)
- [ ] 1.2 Implementar la tabla de transiciones permitidas y rechazar todas las demás con un error de estado no válido
- [ ] 1.3 Reglas de fecha de entrega y de devolución (por defecto hoy, nunca futura) y de nota de hasta 500 caracteres
- [ ] 1.4 Implementar la función pura de disponibilidad de la llave de una taquilla a partir de sus asignaciones cerradas (D2)
- [ ] 1.5 Definir los eventos de llave estructurados y sin texto traducido y su registro en historiales de alumno y taquilla (D7)
- [ ] 1.6 Códigos de error y claves de recurso en catalán de llaves
- [ ] 1.7 Pruebas de la tabla completa de transiciones, disponibilidad con asignaciones de cursos anteriores, fechas futuras y notas largas

## 2. Casos de uso de llaves

- [ ] 2.1 Casos de uso de entrega posterior, incluida la nueva entrega de una llave devuelta, y de devolución individual, también sobre asignaciones cerradas y alumnos de baja
- [ ] 2.2 Caso de uso de pérdida con decisión de cobrar y de entregar copia, en una sola transacción, usando el servicio de reposición de `pagaments` (D4)
- [ ] 2.3 Caso de uso de entrega de copia y contabilización de copias por asignación
- [ ] 2.4 Caso de uso de regularización (llave repuesta) solo para asignaciones cerradas, con fecha y nota (D6)
- [ ] 2.5 Pruebas: pérdida con cobro y copia, sin cobro, con copia pendiente, llave no entregada, importes sin definir con reversión completa, varias pérdidas, llave perdida que aparece, regularización en asignación vigente

## 3. Ganchos de asignación

- [ ] 3.1 Implementar `IAssignmentGuard` con el impedimento de llave no disponible y su mensaje de resolución (D3)
- [ ] 3.2 Implementar `IAssignmentOpenedHandler`: llave pendiente y entrega inmediata por defecto según el contexto de operación
- [ ] 3.3 Implementar `IAssignmentClosedHandler` con la decisión sobre la llave del contexto; exigirla en liberar y cambiar de forma manual y aplicar pendiente en procesos automáticos
- [ ] 3.4 Ajustar los casos de uso de liberar y cambiar de taquilla de `alumnes-i-assignacions` para recoger y transmitir la decisión sobre la llave
- [ ] 3.5 Cierre de una asignación con la llave sin entregar: sin decisión y sin constar como entregada
- [ ] 3.6 Pruebas: entrega inmediata y diferida, impedimento con llave pendiente y con llave de curso anterior, liberar con cada decisión, decisión ausente, cambio de taquilla, bajas por importación y avería con llave pendiente

## 4. Devolución masiva

- [ ] 4.1 Análisis de la devolución masiva que propone las llaves entregadas con filtros por curso, zona, nivel y grupo y permite excluir (D5)
- [ ] 4.2 Confirmación indivisible con revalidación, fecha común y devolución de la selección actualizada si algo cambió
- [ ] 4.3 Progreso con recuentos y cancelación solo antes del guardado
- [ ] 4.4 Pruebas: selección por defecto, exclusión de llaves, cambio de estado entre análisis y confirmación, fallo a mitad, cancelación sin efectos y filtros

## 5. Listas y estado visible

- [ ] 5.1 Lista de llaves pendientes de devolución con filtros, orden por fecha de cierre y totales (D8)
- [ ] 5.2 Lista de llaves perdidas sin resolver
- [ ] 5.3 Indicador de llave no disponible junto al estado visible de la taquilla, sin cambiar la precedencia de estados
- [ ] 5.4 Objetos de transferencia sin correo ni identificador
- [ ] 5.5 Pruebas: listas con y sin pendientes, filtros, indicador de llave no disponible en una taquilla libre y privacidad de los listados

## 6. Persistencia

- [ ] 6.1 Ampliar la entidad de asignación con el estado de llave y crear la tabla de eventos de llave en EF Core, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 6.2 Índices para consultar asignaciones con llave sin resolver por taquilla
- [ ] 6.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [ ] 6.4 Implementación de repositorios y operaciones transaccionales, incluidos los manejadores dentro de la transacción de la asignación
- [ ] 6.5 Pruebas de integración sobre SQLite cifrado temporal: asignar con entrega, liberar con cada decisión, pérdida con reversión completa, devolución masiva atómica
- [ ] 6.6 Comprobar que las pruebas pasan en Windows, Linux y macOS mediante la integración continua

## 7. Feedback y guía al usuario

- [ ] 7.1 Devolver el resultado estructurado con recuentos en todos los casos de uso nuevos
- [ ] 7.2 Preparar confirmaciones con su consecuencia: devolución masiva y decisión al liberar
- [ ] 7.3 Preparar los estados vacíos: sin llaves entregadas, sin pendientes de devolución, sin llaves perdidas
- [ ] 7.4 Claves de recurso en catalán para todos los mensajes, incluido el impedimento de llave no disponible
- [ ] 7.5 Pruebas de mensajes de resultado y de estados vacíos

## 8. Verificación transversal

- [ ] 8.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core, y ningún caso de uso de llaves lee ni escribe la fianza (D9)
- [ ] 8.2 Prueba de comportamiento: entregar, devolver y perder una llave no cambian la fianza
- [ ] 8.3 Prueba de privacidad: un error provocado con datos de un alumno no deja rastro en el registro técnico
- [ ] 8.4 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 8.5 Prueba de extremo a extremo: asignar con entrega, perder con cobro y copia, liberar con llave devuelta y volver a asignar la taquilla
- [ ] 8.6 Documentar el punto de enganche para `cursos-i-historial`: la devolución masiva de llaves como parte del cierre de curso
