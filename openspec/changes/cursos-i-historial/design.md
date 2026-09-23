## Context

Octavo cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `alumnes-i-assignacions` (curso, matrícula, asignación y sus ganchos), `pagaments` (cargos y fianza), `claus` (llaves y devolución masiva) y en los patrones de `arquitectura-base` (operaciones en bloque en dos fases, historial de solo añadir sin texto traducido, resultado estructurado).

Restricciones propias:
- El cierre no puede exigir hacerlo todo a la vez: hay llaves y deudas que se resuelven semanas después.
- Se opera con datos de menores: por defecto no se destruye nada y toda destrucción es explícita, confirmada e indivisible.
- La política de conservación no está decidida con los conserjes: el diseño debe permitir fijarla después sin cambiar el modelo.
- Un solo PC y una sola instancia: no hay concurrencia.

## Goals / Non-Goals

**Goals:**
- Máquina de estados del curso explícita y verificable sin base de datos.
- Cierre reanudable cuyo estado se deduce de los datos, sin duplicar información que pueda desincronizarse.
- Liberación masiva, anonimizado y borrado indivisibles y revalidados.
- Decisión de conservación por curso, sin automatismos.

**Non-Goals:**
- Borrado o anonimizado automático por antigüedad (queda para cuando se consulte a los conserjes).
- Promoción de alumnos y renovación de asignaciones.
- Reabrir un curso cerrado.

## Decisions

### D1. Estado del curso como máquina de estados
`SchoolYear` gana un estado: sin activar, activo, en cierre y cerrado. Transiciones permitidas, todas las demás se rechazan:
- sin activar → activo (solo si no hay otro activo).
- activo → en cierre (inicio del cierre).
- en cierre → activo (solo si no hay otro activo).
- en cierre → cerrado (cierre definitivo).
- cerrado: final.
La transición vive en el dominio como método del curso, no en la interfaz. *Alternativa descartada*: mantener el curso activo hasta cerrarlo del todo. Bloquearía la importación de septiembre mientras faltan llaves por devolver, que es el caso habitual.

### D2. Operaciones permitidas según el estado
Un único punto de comprobación de escritura (el que ya usa `alumnes-i-assignacions` para el histórico) recibe el tipo de operación y decide según el estado del curso:
- activo: todas.
- en cierre: solo cierre de asignaciones (liberación, baja del alumno, baja por importación y decisión de liberar por avería; la reasignación por avería crea una asignación nueva y se rechaza), resolución de llaves y gestión de cobros.
- cerrado: solo resolución de llaves y gestión de cobros (una llave o un cobro pueden aparecer tarde).
- sin activar: nada, salvo activarlo.
Los cobros de `pagaments` y las llaves de `claus` no dependen del estado del curso, así que este cambio solo los deja de bloquear en el punto único de escritura de matrículas y asignaciones.

### D2b. El curso nuevo convive con asignaciones sin liberar
La unicidad de asignación vigente por alumno y por taquilla es global, no por curso (D7 de `alumnes-i-assignacions`), porque la taquilla es un objeto físico. Por eso, mientras el curso anterior conserva asignaciones vigentes, un alumno que continúa no puede recibir taquilla en el curso nuevo y esas taquillas siguen ocupadas: se rechaza con un error que guía a liberar y enlaza con el asistente. *Alternativa descartada*: cerrar automáticamente la asignación anterior al asignar en el curso nuevo; sería una renovación implícita que el producto ha descartado y podría dejar llaves sin registrar.

### D3. Estado de los pasos derivado, salvo la omisión
Los pasos del asistente no se almacenan: liberar taquillas está hecho si no hay asignaciones vigentes del curso; llaves está hecho si no hay llaves entregadas ni perdidas sin resolver de sus asignaciones; la deuda es solo un recuento informativo. Lo único que se guarda es el marcador "omitido" por paso y curso, para que el asistente recuerde lo que el usuario decidió no hacer. Así retomar en otra sesión muestra siempre el estado real, sin estados que se desincronicen. Los mismos recuentos sirven para el resumen de pendientes del cierre definitivo.

### D4. Liberación masiva en dos fases
Mismo patrón que `claus` y `pagaments`: un análisis genera un plan inmutable con las asignaciones vigentes del curso (filtros por zona, nivel y grupo, exclusiones); la confirmación **revalida** contra el estado actual y aplica todo en una transacción; si alguna asignación cambió, no se aplica nada y se devuelve la selección actualizada. Cada cierre pasa por el mismo caso de uso de cierre de asignación que las liberaciones manuales, con el motivo "fin de curso" y con el contexto de operación de un proceso automático, de modo que `claus` deja la llave pendiente de devolución sin pedir decisión por cada una. La fianza y las bajas no se tocan. El cierre definitivo con asignaciones vigentes reutiliza el mismo caso de uso.

### D5. Registro de cierre
Al cerrar el curso se guarda una fila con la fecha y los recuentos de asignaciones vigentes, llaves sin resolver y cargos pendientes en ese momento. No tiene datos personales, no depende de los datos del curso y sobrevive al anonimizado y al borrado.

### D6. Decisión de conservación por curso
`RetentionDecision`: curso, valor (pendiente, conservado, anonimizado, borrado), fecha y recuentos. Pendiente y conservado se pueden cambiar; anonimizado y borrado son finales. Es un dato del curso, no un ajuste global: no hay número de cursos ni valor por defecto. *Alternativa descartada*: un ajuste "conservar N cursos" con borrado automático. Era la decisión previa (`openspec/config.yaml`), pero sin consultar a los conserjes borrar datos sin que nadie lo pida es un riesgo mayor que conservarlos, y la decisión por curso permite añadir la automatización después sin cambiar el modelo.

### D7. Qué es anonimizar
Desvincular, no sobrescribir con datos falsos: en matrículas, asignaciones, cargos y eventos de historial de ese curso se anula la referencia al alumno y se descarta el grupo, conservando nivel, taquilla, fechas, estados e importes. Los informes de totales siguen funcionando porque solo necesitan esos campos. Se descarta el grupo porque en grupos pequeños el nivel y el grupo pueden identificar a una persona. El estado de la llave y la asignación se conservan porque la disponibilidad de la taquilla depende de ellos.

### D8. Qué se borra y qué se protege
Borrar elimina matrículas, asignaciones, cargos y eventos del curso, incluidos los eventos de los historiales de taquillas y llaves que apuntan a esas asignaciones. Ambas operaciones exigen:
- Curso cerrado.
- Sin cargos pendientes: perder quién debe algo sería perder deuda. Se resuelve cobrando, exentando o condonando.
- Sin llaves entregadas o perdidas sin resolver: el bloqueo de asignación por llave de `claus` depende de ese dato.
- Los cargos de fianza vigente o por devolver se excluyen y su alumno conserva la ficha, porque la fianza es de la estancia y no del curso en que se generó.
Tras la operación, un alumno de baja sin ningún otro dato identificado ni fianza vigente pierde su ficha; los alumnos activos la conservan.

### D9. Ajuste de `curs-escolar` en `alumnes-i-assignacions`
Como `openspec/specs/` aún está vacío, no puede haber delta MODIFIED. Se ajustan directamente los requisitos de `curs-escolar` del cambio no archivado: "Activación de un curso" deja de decir que primero hay que cerrar el curso activo (ahora se inicia su cierre) y "Histórico de solo lectura" admite las operaciones de un curso en cierre o cerrado definidas aquí. Los estados y sus transiciones viven en `tancament-de-curs`.

### D10. Operaciones destructivas
Anonimizar y borrar siguen el patrón de dos fases: el análisis devuelve recuentos y bloqueos, la confirmación revalida y aplica en una transacción con progreso por recuentos y cancelación solo antes del guardado. Se ofrece hacer copia antes (la copia manual es de `copies-de-seguretat`; aquí solo se enlaza) sin obligar a ello, porque la copia y la exportación están siempre disponibles. El registro técnico nunca recibe datos de alumnos.

### D11. Feedback
Resultado estructurado con recuentos. Confirmaciones con su consecuencia (inicio del cierre, liberación, cierre definitivo, anonimizar, borrar), estados vacíos con guía (sin asignaciones vigentes, sin cursos cerrados) y protección contra doble ejecución, según los principios de UX transversal. El asistente es el patrón de configuración guiada ya definido, aquí con pasos opcionales; los componentes visuales son de `ux-fonaments`.

## Risks / Trade-offs

- **Un curso en cierre que nadie cierra queda abierto para siempre** → es un estado válido; el asistente muestra siempre lo que falta y el cierre definitivo admite pendientes con confirmación.
- **Sin curso activo entre el inicio del cierre y la activación del siguiente** → las altas, importaciones y asignaciones se rechazan con un error que indica activar un curso; el asistente sugiere activarlo y reactivar el curso en cierre está permitido si no hay otro activo.
- **Borrar y anonimizar son irreversibles** → nunca automáticos, confirmación con recuentos, oferta de copia previa, bloqueos por deuda y llaves pendientes, y la decisión "conservar" siempre disponible.
- **Las llaves de un curso anonimizado siguen bloqueando la taquilla** → por eso se bloquea el anonimizado con llaves sin resolver, y las llaves regularizadas dejan de bloquear.
- **Anonimizar deja de responder "¿quién tuvo esta taquilla el curso X?"** → es el objetivo; los totales por taquilla y nivel siguen disponibles.
- **Los cursos borrados dejan huecos en el historial de una taquilla** → los eventos de esas asignaciones no se conservan a propósito; el curso figura como borrado con su registro de cierre.
- **Reactivar un curso en cierre podría mezclar datos de dos cursos** → solo se permite sin otro curso activo; las asignaciones ya liberadas no se reabren.

## Migration Plan

Una migración de EF Core añade el estado del curso (los cursos existentes activos siguen activos y el resto queda sin activar), la tabla de omisiones de pasos, el registro de cierre y la decisión de conservación, y hace anulable la referencia al alumno en las tablas que se anonimizan. Sin datos previos que migrar salvo el estado de los cursos existentes. Pasa por el migrador con copia previa verificada.

## Open Questions

- Política de conservación (cuántos cursos, si se automatiza, qué opción por defecto): se consulta con los conserjes y la dirección del centro; no altera el modelo, solo añadiría un ajuste o un aviso posterior. Hasta entonces la decisión es manual y por curso.
- Si conviene ofrecer un informe CSV de totales antes de borrar: se decide en `informes-csv`; no cambia este cambio.
