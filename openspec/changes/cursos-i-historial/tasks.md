## 1. Estado del curso

- [ ] 1.1 Añadir el estado (sin activar, activo, en cierre, cerrado) a `SchoolYear` con la tabla de transiciones permitidas y rechazo del resto con un error de estado no válido (D1)
- [ ] 1.2 Comprobación única de escritura por estado del curso y tipo de operación (D2), sustituyendo la del histórico de `alumnes-i-assignacions`
- [ ] 1.3 Casos de uso: iniciar el cierre, reactivar un curso en cierre y activar el siguiente con otro en cierre
- [ ] 1.3b Permitir cerrar asignaciones de un curso en cierre por baja, baja por importación y decisión de liberar, y rechazar reasignar; error guiado al asignar en el curso activo con taquilla o alumno del curso en cierre (D2, D2b)
- [ ] 1.4 Códigos de error y claves de recurso en catalán del estado del curso
- [ ] 1.5 Pruebas de la tabla completa de transiciones, un solo curso activo con varios en cierre, operaciones permitidas por estado, curso cerrado que no se reabre, baja con taquilla del curso en cierre, avería con cada decisión y asignación bloqueada por el curso en cierre

## 2. Asistente de cierre

- [ ] 2.1 Servicio que deriva el estado de cada paso y los recuentos de pendientes: asignaciones vigentes, llaves sin resolver y cargos pendientes con su importe (D3)
- [ ] 2.2 Guardar y consultar las omisiones de pasos por curso
- [ ] 2.3 Consulta de la deuda pendiente del curso para el asistente, reutilizando la de `pagaments`
- [ ] 2.4 Pruebas: pasos en cualquier orden, retomar tras reiniciar, paso hecho sin trabajo, omitir y volver a abrir

## 3. Liberación masiva

- [ ] 3.1 Análisis que propone las asignaciones vigentes del curso con filtros por zona, nivel y grupo y exclusiones (D4)
- [ ] 3.2 Confirmación que revalida y libera en una transacción con el motivo de fin de curso y el contexto de proceso automático
- [ ] 3.3 Reutilizar el caso de uso de cierre de asignación con sus ganchos, sin tocar fianzas ni bajas
- [ ] 3.4 Progreso con recuentos y cancelación solo antes del guardado
- [ ] 3.5 Pruebas: exclusiones, asignación que cambia entre fases, fallo a mitad, llave entregada que queda pendiente, fianza intacta, sin asignaciones vigentes

## 4. Cierre definitivo

- [ ] 4.1 Caso de uso de cierre con resumen de pendientes y confirmación, que cierra las asignaciones que queden (D4)
- [ ] 4.2 Registro de cierre con fecha y recuentos, sin datos personales (D5)
- [ ] 4.3 Pruebas: todo resuelto, con pendientes, cierre repetido, curso cerrado de solo lectura, llave tardía y condonación en un curso cerrado

## 5. Decisión de conservación

- [ ] 5.1 Crear `RetentionDecision` con sus valores, transiciones y fecha (D6)
- [ ] 5.2 Pregunta al cerrar con las opciones conservar, anonimizar, borrar y decidir más tarde
- [ ] 5.3 Consulta de cursos cerrados con su decisión y cambio posterior de pendiente o conservado
- [ ] 5.4 Pruebas: sin aplicación automática, decidir más tarde, decisión irreversible y curso no cerrado

## 6. Anonimizar y borrar

- [ ] 6.1 Análisis con recuentos y bloqueos: cargos pendientes, llaves sin resolver, fianzas excluidas (D8)
- [ ] 6.2 Anonimizar: desvincular matrículas, asignaciones, cargos y eventos de historial y descartar el grupo (D7)
- [ ] 6.3 Borrar: eliminar matrículas, asignaciones, cargos y eventos del curso, y conservar el curso con su registro de cierre
- [ ] 6.4 Eliminar la ficha de alumnos de baja sin datos restantes ni fianza vigente o por devolver
- [ ] 6.5 Confirmación que revalida y aplica en una transacción, con progreso y cancelación antes del guardado (D10)
- [ ] 6.6 Registro de la decisión con fecha y recuentos, sin datos personales
- [ ] 6.7 Pruebas: totales que se conservan, alumno activo que conserva la ficha, alumno de baja con fianza por devolver, cargo pendiente que aparece entre fases, fallo a mitad y ausencia de datos de alumnos en el registro técnico

## 7. Persistencia

- [ ] 7.1 Entidades y configuraciones EF Core del estado del curso, omisiones, registro de cierre y decisión, sin filtrar EF Core a `Domain` ni `Application`
- [ ] 7.2 Hacer anulable la referencia al alumno en matrículas, asignaciones, cargos y eventos que se anonimizan
- [ ] 7.3 Migración de EF Core con el estado inicial de los cursos existentes y verificación de que el modelo no tiene cambios sin migrar
- [ ] 7.4 Repositorios y operaciones transaccionales de cierre, anonimizado y borrado
- [ ] 7.5 Pruebas de integración sobre SQLite cifrado temporal: liberación masiva de 300 asignaciones, cierre completo, anonimizado y borrado con reversión completa ante fallos
- [ ] 7.6 Comprobar que las pruebas pasan en Windows, Linux y macOS con los scripts de verificación (`docs/stack.md`)

## 8. Feedback y guía al usuario

- [ ] 8.1 Devolver el resultado estructurado con recuentos en todos los casos de uso nuevos
- [ ] 8.2 Preparar confirmaciones con su consecuencia: inicio del cierre, liberación, cierre definitivo, anonimizar y borrar
- [ ] 8.3 Preparar los estados vacíos: sin asignaciones vigentes, sin cursos cerrados, sin curso activo tras iniciar el cierre
- [ ] 8.4 Protección contra doble ejecución en las operaciones de cierre y conservación
- [ ] 8.5 Claves de recurso en catalán para todos los mensajes
- [ ] 8.6 Pruebas de mensajes de resultado, de estados vacíos y de doble ejecución

## 9. Verificación transversal

- [ ] 9.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core
- [ ] 9.2 Prueba de privacidad: un error provocado durante el anonimizado no deja datos de alumnos en el registro técnico
- [ ] 9.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 9.4 Prueba de extremo a extremo: curso activo, inicio del cierre, activar el siguiente, liberar, devolver llaves, cerrar con pendientes, condonar tarde y anonimizar
- [ ] 9.5 Documentar el punto de enganche con `informes-csv` (totales que se conservan tras anonimizar) y con `copies-de-seguretat` (copia previa a una operación destructiva)
