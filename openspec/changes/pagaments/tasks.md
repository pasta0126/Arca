## 1. Importes por curso

- [x] 1.1 Crear el catálogo cerrado de conceptos (cuota, fianza, reposición) y `ConceptAmount` por curso con el tipo de importe exacto y sus límites
- [x] 1.2 Regla de modificación solo si la fecha de fin del curso no ha pasado y propuesta de los importes del curso anterior (D5)
- [x] 1.3 Historial de cambios de importe de solo añadir
- [x] 1.4 Casos de uso: definir, consultar y modificar importes de un curso
- [x] 1.5 Códigos de error y claves de recurso en catalán de importes
- [x] 1.6 Pruebas: importe cero, negativo, con más decimales y superior al máximo, herencia del curso anterior, cambio sin efecto retroactivo, curso finalizado, curso sin importes (el caso "cargo generado conserva el importe antiguo" se prueba de extremo a extremo en el grupo 3, cuando existe `Charge`)

## 2. Dominio de cargos

- [x] 2.1 Crear `Charge` con alumno, concepto, curso, importe fijado, estado y datos de la última transición (D1)
- [x] 2.2 Implementar la tabla de transiciones permitidas y rechazar todas las demás con un error de estado no válido
- [x] 2.3 Reglas de fecha de pago (por defecto hoy, nunca futura) y de motivo obligatorio de 1 a 500 caracteres
- [x] 2.4 Reversión de pagado, exento y condonado a pendiente con motivo, y anulación solo de pendientes
- [x] 2.5 Ajuste del importe de un cargo pendiente con motivo
- [x] 2.6 Historial de eventos del cargo de solo añadir, con datos estructurados y sin texto traducido (D2)
- [x] 2.7 Códigos de error y claves de recurso en catalán de cargos
- [x] 2.8 Pruebas de la tabla completa de transiciones, fechas pasada, futura y por defecto, motivos vacío y largo, reversión, anulación de pagado, ajuste de importe y historial inmutable (esto último por diseño: el historial no ofrece ninguna operación de editar ni borrar)

## 3. Generación de cargos y avisos

- [x] 3.1 Implementar `IAssignmentOpenedHandler`: generar la cuota del curso si no existe y la fianza si no hay una vigente, de forma idempotente (D4)
- [x] 3.2 Cuota completa en llegadas a mitad de curso, sin prorrateo, y sin reembolso al liberar
- [x] 3.3 Rechazar la asignación completa si faltan los importes del curso, con aviso de que hay que definirlos (implementado como impedimento de `IAssignmentGuard`, antes de tocar nada, en vez de hacer fallar el gancho de apertura)
- [x] 3.4 Implementar la reposición de llave a demanda, con posibilidad de varias en un curso
- [x] 3.5 Implementar `IAssignmentGuard` que avisa de la deuda de cursos anteriores con conceptos, cursos e importe total
- [x] 3.6 Permitir gestionar cargos de cursos anteriores, incluido marcar como pagados los antiguos (los casos de uso de transición de `Charge` —pagar, exento, condonar, revertir, anular, ajustar importe— se han creado en este grupo, ya que `tasks.md` no los incluía en el grupo 2)
- [x] 3.7 Pruebas: primera asignación del curso, cambio de taquilla, liberar y reasignar, llegada a mitad de curso, salida con cuota pendiente, importes sin definir, aviso con y sin deuda anterior, aviso rechazado

## 4. Fianza

- [x] 4.1 Definir la fianza vigente y la generación de una sola por alumno (D3)
- [x] 4.2 Modelar la devolución (sin devolución, por devolver, devuelta) con fecha y nota opcional de hasta 500 caracteres
- [x] 4.3 Implementar `IStudentLifecycleHandler`: en baja, pagada pasa a por devolver, pendiente se anula con motivo automático y exenta o condonada sin cambios
- [x] 4.4 Implementar la reactivación: por devolver vuelve a pagada; devuelta o anulada permite generar una nueva
- [x] 4.5 Garantizar que liberar la taquilla, cerrar curso o devolver la llave no altera la fianza
- [x] 4.6 Casos de uso de devolución individual y corrección de una devolución con motivo, y regla de no revertir a pendiente una devuelta
- [x] 4.7 Rechazar la devolución de la fianza de un alumno activo y de fianzas que no están por devolver
- [x] 4.8 Pruebas: fianza única entre cursos, exenta vigente, baja con cada estado, bajas masivas, reactivación en cada caso, devolución con fecha futura, alumno activo y no pagada

## 5. Operaciones en bloque

- [x] 5.1 Implementar el análisis y la confirmación de la condonación en bloque con revalidación y una sola transacción (D6)
- [x] 5.2 Implementar el análisis y la confirmación de la devolución de fianzas en bloque con fecha y nota comunes
- [x] 5.3 Progreso con recuentos y cancelación solo antes del guardado; resultado con recuentos e importes
- [x] 5.4 Pruebas: condonar 20 cargos, cargo que deja de ser pendiente, motivo obligatorio, devolver 40 fianzas, fianza que deja de estar por devolver, fallo a mitad y cancelación sin efectos

## 6. Estado de pago y consultas

- [x] 6.1 Implementar la función pura del estado al corriente de un alumno, con desglose por concepto y curso y distinción de exención (D7)
- [x] 6.2 Implementar el estado de pago de una taquilla ocupada a partir de su alumno
- [x] 6.3 Consulta de morosos con filtros por curso, concepto, nivel, grupo y zona, totales, alumnos de baja marcados y orden por apellidos (D8)
- [x] 6.4 Consulta de fianzas por devolver con totales, ordenada por fecha de baja y sin las exentas
- [x] 6.5 Objetos de transferencia sin correo ni identificador, y motivos solo en la ficha del cargo
- [x] 6.6 Pruebas: al corriente, con deuda, sin cargos, todo exento, taquilla con alumno moroso y libre, filtros, alumno de baja con deuda, sin morosos y privacidad de los listados

## 7. Persistencia

- [x] 7.1 Entidades y configuraciones EF Core de importes, cargos, eventos y devolución, sin filtrar EF Core a `Domain` ni `Application`
- [x] 7.2 Índice único parcial de la cuota por alumno y curso, e índices para consultar cargos pendientes por alumno y por curso
- [x] 7.3 Migración de EF Core y verificación de que el modelo no tiene cambios sin migrar
- [x] 7.4 Implementación de repositorios y de las operaciones transaccionales, incluidos los manejadores dentro de la transacción de la asignación
- [x] 7.5 Pruebas de integración sobre SQLite cifrado temporal: asignación que genera cargos, reversión completa si faltan importes, baja masiva con fianzas y atomicidad de operaciones en bloque
- [x] 7.6 Comprobar que las pruebas pasan en Windows, Linux y macOS con los scripts de verificación (`docs/stack.md`)

## 8. Feedback y guía al usuario

- [x] 8.1 Devolver el resultado estructurado con recuentos e importes en todos los casos de uso nuevos
- [x] 8.2 Preparar confirmaciones con su consecuencia: reversión de un pago, condonación en bloque y devolución en bloque
- [x] 8.3 Preparar los estados vacíos: alumno sin cargos, sin morosos, sin fianzas por devolver, importes sin definir
- [x] 8.4 Claves de recurso en catalán para todos los mensajes, incluidos los avisos de deuda anterior
- [x] 8.5 Pruebas de mensajes de resultado y de estados vacíos

## 9. Verificación transversal

- [x] 9.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core (prueba genérica `LayerReferenceTests`, que ya recorre el código de este cambio)
- [x] 9.2 Prueba de privacidad: un error provocado con un motivo o datos de un alumno no deja rastro en el registro técnico (`EfChargesTests`, sobre una condonación en bloque que falla, además de la prueba genérica de `FileErrorLogTests`; los listados no llevan correo, identificador ni motivos: `PaymentQueryTests`)
- [x] 9.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán (`ResourceCoverageTests`, genérica, que ya comprueba los códigos de error y las claves literales de este cambio)
- [x] 9.4 Prueba de extremo a extremo: alumno nuevo asignado, cargos generados, pago, baja, fianza por devolver, devolución y reactivación con fianza nueva
- [x] 9.5 Documentar los puntos de enganche para `claus` (cargo de reposición al perder una llave) y `cursos-i-historial` (revisión de la deuda pendiente al cerrar curso) (en `design.md` y en los comentarios de `ChargeKeyReplacementHandler` e `IChargeRepository`)
