## Context

Cuarto cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `arquitectura-base` (importes exactos, reloj, resultado estructurado, historial estructurado sin texto traducido), `taquilles-i-zones` y `alumnes-i-assignacions`, cuyos ganchos implementa: `IAssignmentGuard`, `IAssignmentOpenedHandler` e `IStudentLifecycleHandler`.

Restricciones propias:
- El modelo de cobro debe ser muy simple para conserjes: un estado por concepto y alumno, sin métodos de pago ni importes parciales.
- Hay casos especiales desde la v1: becas y exenciones, una fianza única por estancia y deudas arrastradas o condonadas.
- Los datos de pago de menores son sensibles: mínimo necesario, sin rastro en el registro técnico.
- Un solo PC y una sola instancia: no hay concurrencia.

## Goals / Non-Goals

**Goals:**
- Modelo de cargo con estados y transiciones explícitas, verificable sin base de datos.
- Todos los cambios de estado dejan rastro inmutable; nada se borra.
- Generación automática de cargos coherente con las asignaciones, en la misma transacción.
- Operaciones en bloque (condonar, devolver) indivisibles y revalidadas.
- Consultas de morosos y fianzas por devolver preparadas para los informes CSV.

**Non-Goals:**
- Métodos de pago, importes parciales, plazos, recibos.
- Otros conceptos de cobro configurables.
- Cierre de curso, informes CSV y pantallas.

## Decisions

### D1. Un cargo por alumno, concepto y ocasión, con estado
`Charge`: alumno, concepto (cuota, fianza, reposición), curso de generación, importe fijado al crear, estado y datos de la última transición (fecha de pago o motivo). Estados: pendiente, pagado, exento, condonado, anulado.
Transiciones permitidas (todas las demás se rechazan):
- pendiente → pagado (fecha), exento (motivo), condonado (motivo), anulado (motivo).
- pagado, exento, condonado → pendiente (motivo), mediante reversión.
- anulado: final.
El importe es una fotografía en el momento de crear el cargo y solo cambia con un ajuste explícito de un cargo pendiente.
*Alternativa descartada*: libro de movimientos con pagos parciales. Era la decisión previa, pero el producto ha simplificado: un pagado o pendiente por concepto encaja mejor con el uso de los conserjes y reduce los casos límite.

### D2. Historial del cargo de solo añadir
Tabla de eventos ligada al identificador del cargo, con instante UTC, estado anterior y nuevo, motivo e importes anterior y nuevo cuando cambian, como datos estructurados y nunca como texto traducido (mismo patrón que taquillas y alumnos). Se escribe en la misma transacción que el cambio.
Las correcciones se hacen por reversión, no por edición: se mantiene la regla de que nada se borra ni se sobrescribe.

### D3. Dimensión de devolución de la fianza separada del estado del cargo
El estado de la fianza sigue los del cargo. La devolución se modela como un segundo dato propio de las fianzas pagadas: sin devolución, por devolver o devuelta (con fecha y nota). Así el estado de pago (¿se cobró?) y el ciclo del depósito (¿se devolvió?) no se mezclan y las consultas de deuda no necesitan conocer la devolución.
- Fianza vigente: cargo de fianza en pendiente, pagado, exento o condonado cuya devolución no es "devuelta", y no anulado.
- Reglas de baja y reactivación en los specs; se implementan en `IStudentLifecycleHandler` dentro de la misma transacción.

### D4. Implementación de los ganchos de `alumnes-i-assignacions`
- `IAssignmentOpenedHandler`: si el alumno no tiene cuota en el curso de la asignación, la genera; si no tiene fianza vigente, la genera. Idempotente: la comprobación y la creación van en la misma transacción, apoyadas en un índice único parcial (alumno, curso) para cuotas.
- `IAssignmentGuard`: calcula los cargos pendientes de cursos anteriores del alumno y, si hay alguno, devuelve un aviso con conceptos, cursos e importe total.
- `IStudentLifecycleHandler`: aplica las reglas de fianza en baja y reactivación.
Si faltan los importes del curso, el manejador devuelve un error que revierte la asignación completa (regla de `alumnes-i-assignacions`: los manejadores solo pueden fallar revirtiéndolo todo).

### D5. Importes por curso
`ConceptAmount`: curso, concepto e importe, con historial de cambios. El importe se valida en un tipo de importe exacto (euros, dos decimales, mayor que cero y hasta 9999,99). Los importes de un curso se pueden modificar mientras su fecha de fin no haya pasado. Se propone el importe del curso anterior como valor inicial.
No se usa la noción de curso cerrado, porque el cierre es de `cursos-i-historial`; el criterio por fecha de fin es independiente y ya conocido.

### D6. Operaciones en bloque en dos fases
Condonar en bloque y devolver fianzas en bloque siguen el patrón del proyecto: análisis que genera un plan con los cargos seleccionados, confirmación que **revalida** contra el estado actual y aplica todo en una transacción. Si algún cargo dejó de ser elegible, no se aplica nada y se devuelve la selección actualizada. Progreso con recuentos y cancelación solo antes del guardado.

### D7. Estado de pago derivado, no almacenado
El estado al corriente de un alumno y de una taquilla se calcula a partir de los cargos, como el estado visible de las taquillas: una única función pura con desglose por concepto y curso. No hay un campo "moroso" que pueda desincronizarse.

### D8. Consulta de morosos y de fianzas por devolver
Con este volumen se cargan los cargos pendientes con alumno, matrícula y taquilla de forma explícita (sin carga perezosa) y se filtran en memoria. Los objetos de transferencia no incluyen correo ni identificador. Los motivos que escribe el conserje son datos libres: se muestran en la ficha del cargo y no en listados generales ni se registran en el registro técnico.

### D9. Fechas
Fecha de pago y de devolución son fechas de calendario (`DateOnly`) con el reloj inyectado; nunca futuras. El instante de cada evento del historial es UTC.

### D10. Feedback
Resultado estructurado con recuentos e importes. Confirmación con su consecuencia en reversiones y operaciones en bloque, y estados vacíos con guía (sin cargos, sin morosos, sin fianzas por devolver), según los principios de UX transversal.

## Risks / Trade-offs

- **La generación en el manejador de asignación acopla cobros y asignaciones** → los ganchos ya son la frontera definida; cada implementación tiene pruebas con dobles y pruebas de integración de extremo a extremo.
- **Fianza única por estancia con reglas de reactivación puede sorprender** → las reglas están en los specs con escenarios explícitos y la ficha del alumno muestra el estado y el historial de su fianza.
- **Cuota completa a llegadas y salidas de mitad de curso puede parecer injusta** → es una decisión de producto; la exención y la condonación con motivo son la salida y quedan trazadas.
- **Reversión de un pago puede usarse para ocultar errores** → siempre exige motivo, deja el historial completo y el cambio se muestra en la ficha.
- **Ajuste del importe de un cargo pendiente es una edición** → limitado a pendientes, con motivo y con historial; los cargos pagados son inmutables.
- **Motivos libres pueden contener datos personales innecesarios** → guía en la interfaz para no incluirlos, exclusión del registro técnico y de los listados.
- **Bajas masivas por importación generan muchas fianzas por devolver de golpe** → la lista con totales y la devolución en bloque están diseñadas para ese caso.
- **Solo tres conceptos** → suficiente para v1; añadir conceptos es ampliar un catálogo cerrado en el código, que se abriría en una versión posterior si hiciera falta.

## Migration Plan

Una migración de EF Core crea las tablas de importes de conceptos, cargos, eventos de cargos y datos de devolución de fianza, con el índice único parcial de la cuota por alumno y curso. Sin datos previos. Pasa por el migrador con copia previa verificada.

## Open Questions

Ninguna pendiente. La lista de conceptos cerrada (cuota, fianza y reposición) se ha asumido a partir de lo indicado; si hubiera que admitir otros conceptos, sería un cambio de requisitos, no un detalle de implementación.
