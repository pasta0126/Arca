## 1. Importes por curso

- [ ] 1.1 Crear el catálogo cerrado de conceptos (cuota, fianza, reposición) y `ConceptAmount` por curso con el tipo de importe exacto y sus límites
- [ ] 1.2 Regla de modificación solo si la fecha de fin del curso no ha pasado y propuesta de los importes del curso anterior (D5)
- [ ] 1.3 Historial de cambios de importe de solo añadir
- [ ] 1.4 Casos de uso: definir, consultar y modificar importes de un curso
- [ ] 1.5 Códigos de error y claves de recurso en catalán de importes
- [ ] 1.6 Pruebas: importe cero, negativo, con más decimales y superior al máximo, herencia del curso anterior, cambio sin efecto retroactivo, curso finalizado, curso sin importes

## 2. Dominio de cargos

- [ ] 2.1 Crear `Charge` con alumno, concepto, curso, importe fijado, estado y datos de la última transición (D1)
- [ ] 2.2 Implementar la tabla de transiciones permitidas y rechazar todas las demás con un error de estado no válido
- [ ] 2.3 Reglas de fecha de pago (por defecto hoy, nunca futura) y de motivo obligatorio de 1 a 500 caracteres
- [ ] 2.4 Reversión de pagado, exento y condonado a pendiente con motivo, y anulación solo de pendientes
- [ ] 2.5 Ajuste del importe de un cargo pendiente con motivo
- [ ] 2.6 Historial de eventos del cargo de solo añadir, con datos estructurados y sin texto traducido (D2)
- [ ] 2.7 Códigos de error y claves de recurso en catalán de cargos
- [ ] 2.8 Pruebas de la tabla completa de transiciones, fechas pasada, futura y por defecto, motivos vacío y largo, reversión, anulación de pagado, ajuste de importe y historial inmutable

## 3. Generación de cargos y avisos

- [ ] 3.1 Implementar `IAssignmentOpenedHandler`: generar la cuota del curso si no existe y la fianza si no hay una vigente, de forma idempotente (D4)
- [ ] 3.2 Cuota completa en llegadas a mitad de curso, sin prorrateo, y sin reembolso al liberar
- [ ] 3.3 Rechazar la asignación completa si faltan los importes del curso, con aviso de que hay que definirlos
- [ ] 3.4 Implementar la reposición de llave a demanda, con posibilidad de varias en un curso
- [ ] 3.5 Implementar `IAssignmentGuard` que avisa de la deuda de cursos anteriores con conceptos, cursos e importe total
- [ ] 3.6 Permitir gestionar cargos de cursos anteriores, incluido marcar como pagados los antiguos
- [ ] 3.7 Pruebas: primera asignación del curso, cambio de taquilla, liberar y reasignar, llegada a mitad de curso, salida con cuota pendiente, importes sin definir, aviso con y sin deuda anterior, aviso rechazado

## 4. Fianza

- [ ] 4.1 Definir la fianza vigente y la generación de una sola por alumno (D3)
- [ ] 4.2 Modelar la devolución (sin devolución, por devolver, devuelta) con fecha y nota opcional de hasta 500 caracteres
- [ ] 4.3 Implementar `IStudentLifecycleHandler`: en baja, pagada pasa a por devolver, pendiente se anula con motivo automático y exenta o condonada sin cambios
- [ ] 4.4 Implementar la reactivación: por devolver vuelve a pagada; devuelta o anulada permite generar una nueva
- [ ] 4.5 Garantizar que liberar la taquilla, cerrar curso o devolver la llave no altera la fianza
- [ ] 4.6 Casos de uso de devolución individual y corrección de una devolución con motivo, y regla de no revertir a pendiente una devuelta
- [ ] 4.7 Rechazar la devolución de la fianza de un alumno activo y de fianzas que no están por devolver
- [ ] 4.8 Pruebas: fianza única entre cursos, exenta vigente, baja con cada estado, bajas masivas, reactivación en cada caso, devolución con fecha futura, alumno activo y no pagada

## 5. Operaciones en bloque

- [ ] 5.1 Implementar el análisis y la confirmación de la condonación en bloque con revalidación y una sola transacción (D6)
- [ ] 5.2 Implementar el análisis y la confirmación de la devolución de fianzas en bloque con fecha y nota comunes
- [ ] 5.3 Progreso con recuentos y cancelación solo antes del guardado; resultado con recuentos e importes
- [ ] 5.4 Pruebas: condonar 20 cargos, cargo que deja de ser pendiente, motivo obligatorio, devolver 40 fianzas, fianza que deja de estar por devolver, fallo a mitad y cancelación sin efectos

## 6. Estado de pago y consultas

- [ ] 6.1 Implementar la función pura del estado al corriente de un alumno, con desglose por concepto y curso y distinción de exención (D7)
- [ ] 6.2 Implementar el estado de pago de una taquilla ocupada a partir de su alumno
- [ ] 6.3 Consulta de morosos con filtros por curso, concepto, nivel, grupo y zona, totales, alumnos de baja marcados y orden por apellidos (D8)
- [ ] 6.4 Consulta de fianzas por devolver con totales, ordenada por fecha de baja y sin las exentas
- [ ] 6.5 Objetos de transferencia sin correo ni identificador, y motivos solo en la ficha del cargo
- [ ] 6.6 Pruebas: al corriente, con deuda, sin cargos, todo exento, taquilla con alumno moroso y libre, filtros, alumno de baja con deuda, sin morosos y privacidad de los listados

## 7. Persistencia

- [ ] 7.1 Entidades y configuraciones EF Core de importes, cargos, eventos y devolución, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 7.2 Índice único parcial de la cuota por alumno y curso, e índices para consultar cargos pendientes por alumno y por curso
- [ ] 7.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [ ] 7.4 Implementación de repositorios y de las operaciones transaccionales, incluidos los manejadores dentro de la transacción de la asignación
- [ ] 7.5 Pruebas de integración sobre SQLite cifrado temporal: asignación que genera cargos, reversión completa si faltan importes, baja masiva con fianzas y atomicidad de operaciones en bloque
- [ ] 7.6 Comprobar que las pruebas pasan en Windows, Linux y macOS con los scripts de verificación (`docs/stack.md`)

## 8. Feedback y guía al usuario

- [ ] 8.1 Devolver el resultado estructurado con recuentos e importes en todos los casos de uso nuevos
- [ ] 8.2 Preparar confirmaciones con su consecuencia: reversión de un pago, condonación en bloque y devolución en bloque
- [ ] 8.3 Preparar los estados vacíos: alumno sin cargos, sin morosos, sin fianzas por devolver, importes sin definir
- [ ] 8.4 Claves de recurso en catalán para todos los mensajes, incluidos los avisos de deuda anterior
- [ ] 8.5 Pruebas de mensajes de resultado y de estados vacíos

## 9. Verificación transversal

- [ ] 9.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core
- [ ] 9.2 Prueba de privacidad: un error provocado con un motivo o datos de un alumno no deja rastro en el registro técnico
- [ ] 9.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 9.4 Prueba de extremo a extremo: alumno nuevo asignado, cargos generados, pago, baja, fianza por devolver, devolución y reactivación con fianza nueva
- [ ] 9.5 Documentar los puntos de enganche para `claus` (cargo de reposición al perder una llave) y `cursos-i-historial` (revisión de la deuda pendiente al cerrar curso)
