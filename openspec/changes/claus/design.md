## Context

Quinto cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `alumnes-i-assignacions` (asignaciones y sus ganchos `IAssignmentGuard`, `IAssignmentOpenedHandler` e `IAssignmentClosedHandler`, con contexto de operación extensible), en `pagaments` (generación del cargo de reposición) y en los patrones de `arquitectura-base` y `taquilles-i-zones` (estado derivado, historial de solo añadir, operaciones en bloque en dos fases, resultado estructurado).

Restricciones propias:
- Una sola llave por taquilla, del centro y sin identificar: no hay entidad "llave" con identidad propia.
- La llave se entrega y devuelve cada curso; la fianza no participa.
- No se puede suponer que una llave ha vuelto: solo lo dice el usuario. Los procesos automáticos dejan la llave pendiente.
- Un solo PC y una sola instancia: no hay concurrencia.

## Goals / Non-Goals

**Goals:**
- Estado de llave por asignación, con transiciones explícitas y verificable sin base de datos.
- Disponibilidad de la llave de una taquilla derivada, no almacenada.
- Devolución masiva indivisible con selección editable.
- Ganchos de asignación implementados sin cambiar las firmas de `alumnes-i-assignacions`.

**Non-Goals:**
- Llaves numeradas o múltiples, candados del alumno, cambio de bombín.
- Cobro, cierre de curso, pantallas.

## Decisions

### D1. El estado de la llave vive en la asignación
Cada `Assignment` gana un estado de llave (pendiente de entrega, entregada, perdida, devuelta, repuesta), la fecha de la última transición y un recuento de copias entregadas. No hay entidad de llave: con una única llave por taquilla, el estado de la llave de una taquilla es el de la asignación que la tiene o la tuvo por última vez.
Transiciones permitidas (todas las demás se rechazan):
- pendiente de entrega → entregada.
- entregada → devuelta, perdida.
- perdida → entregada (copia), devuelta (aparece), repuesta (solo en asignación cerrada).
- entregada → repuesta (solo en asignación cerrada).
- devuelta → entregada (nueva entrega).
- repuesta: final.

### D2. Disponibilidad de la llave derivada
Una taquilla tiene la llave disponible cuando **ninguna** de sus asignaciones cerradas tiene la llave entregada o perdida. Se calcula con una función pura sobre las asignaciones de la taquilla, igual que el estado visible: no hay un campo "sin llave" que pueda desincronizarse. Las asignaciones de cursos anteriores cuentan.
Se expone como indicador junto al estado visible; no cambia la precedencia de estados de `taquilles-i-zones` (la taquilla sigue "libre").

### D3. Implementación de los ganchos
- `IAssignmentGuard`: devuelve un **impedimento** si la taquilla no tiene la llave disponible.
- `IAssignmentOpenedHandler`: fija la llave en pendiente de entrega y, si el contexto de operación pide la entrega inmediata (por defecto sí), la deja entregada con la fecha de hoy.
- `IAssignmentClosedHandler`: lee del contexto la decisión sobre la llave (devuelta, pendiente o perdida). Los casos de uso manuales de liberar y cambiar **exigen** la decisión cuando la llave está entregada; los procesos automáticos (bajas por importación, avería) la pasan como pendiente.
No se supone nunca que una llave ha vuelto sin que lo diga el usuario.

### D4. Pérdida con reposición y copia como una sola operación
`ReportKeyLost(asignación, cobrar, entregarCopia)` compone, en una transacción: evento de pérdida, generación opcional del cargo de reposición mediante el servicio de `pagaments` y entrega opcional de la copia. Si el cobro falla (por ejemplo, importes sin definir), toda la operación se revierte. La copia sin entregar deja la llave perdida y se completa después con una entrega de copia.

### D5. Devolución masiva en dos fases
Análisis que propone las llaves entregadas (con filtros por curso, zona, nivel y grupo) y permite excluir; confirmación que **revalida** contra el estado actual y aplica todo en una transacción; si alguna llave cambió de estado, no se aplica nada y se devuelve la selección actualizada. El mismo patrón que las demás operaciones en bloque del proyecto, con progreso con recuentos y cancelación solo antes del guardado.
Este caso de uso es el que invocará el cierre de curso guiado de `cursos-i-historial`; aquí no se acopla a él.

### D6. Regularización
Solo sobre asignaciones cerradas: registra que el centro dispone de una llave nueva para la taquilla, con fecha y nota. En asignaciones vigentes se usa la pérdida con copia. El cambio físico del bombín se registrará en `manteniment` y no se enlaza aquí.

### D7. Historial de llave
Eventos estructurados y sin texto traducido en el mismo mecanismo de eventos de solo añadir de alumnos y taquillas: entrega, devolución, pérdida, copia, reposición y devolución masiva. Se escriben en la misma transacción que el cambio. Aparecen en el historial del alumno y en el de la taquilla.

### D8. Listas y consultas
Como en los cambios anteriores, con este volumen se cargan explícitamente (sin carga perezosa) las asignaciones con llave sin resolver junto a taquilla y alumno, y se filtra en memoria: pendientes de devolución (entregadas en asignaciones cerradas o de alumnos de baja) y perdidas sin resolver. Los objetos de transferencia no incluyen correo ni identificador.

### D9. Independencia de la fianza
Ninguna operación de este cambio lee ni escribe la fianza. Una prueba de arquitectura y otra de comportamiento lo verifican, porque la decisión de producto es que son ciclos independientes.

## Risks / Trade-offs

- **Nadie marca la devolución y las taquillas quedan bloqueadas por llaves "pendientes"** → lista de pendientes visible, devolución masiva con exclusiones y regularización cuando el centro repone la llave; el impedimento explica cómo resolverlo.
- **Bloquear la asignación puede frenar el inicio de curso** → el bloqueo es solo para llaves que el usuario ha dejado sin resolver o que los procesos automáticos dejaron pendientes; la lista y la devolución masiva están pensadas para ese momento.
- **Dejar la llave pendiente en procesos automáticos genera mucho trabajo tras bajas masivas** → es lo correcto porque no se puede suponer la devolución; la devolución masiva permite resolverlas de golpe.
- **La decisión obligatoria al liberar añade un paso** → solo se pide si la llave está entregada; por defecto se propone la opción más habitual.
- **Estado de llave en la asignación acopla este cambio a `alumnes-i-assignacions`** → los ganchos son la frontera; la migración añade columnas sin cambiar el comportamiento de asignaciones existentes.
- **Pérdida y reposición dependen de `pagaments`** → mediante un puerto; el fallo del cobro revierte la operación completa.

## Migration Plan

Una migración de EF Core añade a las asignaciones el estado de llave, su fecha y el recuento de copias, y crea la tabla de eventos de llave. Las asignaciones existentes reciben el estado pendiente de entrega, o entregada si se decide, y cerradas quedan sin llave sin resolver. Como toda migración, pasa por el migrador con copia previa verificada.

## Open Questions

Ninguna pendiente. Qué estado inicial recibirían las asignaciones ya existentes en una instalación con datos previos no se plantea en la v1, porque el producto no tiene instalaciones anteriores.
