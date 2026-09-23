## Why

Cada taquilla se cierra con una única llave del centro que se entrega al alumno, se devuelve al terminar el curso y a veces se pierde. Hay que saber en todo momento dónde está cada llave, poder registrar entregas, devoluciones y pérdidas con poco esfuerzo (incluida la devolución masiva al cerrar el curso) y evitar que se asigne a otro alumno una taquilla cuya llave no ha vuelto.

## What Changes

- Estado de la llave por asignación: pendiente de entrega, entregada, perdida, devuelta o repuesta, con fechas e historial de solo añadir.
- Entrega al asignar la taquilla (lo habitual, con la opción de dejarla para después) y entrega posterior como paso independiente.
- Devolución llave a llave y devolución masiva con selección editable, indivisible y con confirmación.
- Pérdida de llave: se registra, se decide si se cobra la reposición (genera un cargo del concepto de reposición) y se entrega una copia; la taquilla y la asignación no cambian.
- Regularización de llaves no devueltas o perdidas cuando el centro repone la llave.
- Llave disponible derivada: una taquilla cuya llave anterior está entregada o perdida sin resolver no se puede asignar a otro alumno.
- Al liberar o cambiar de taquilla, decisión explícita sobre qué pasó con la llave; en procesos automáticos (bajas por importación, avería) la llave queda pendiente de devolución, sin suponerla devuelta.
- Listas de llaves pendientes de devolución y de llaves perdidas sin regularizar.

## Capabilities

### New Capabilities
- `claus`: estado y ciclo de vida de la llave de cada taquilla, entrega, devolución (individual y masiva), pérdida con copia y reposición, regularización, disponibilidad y listados.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Este cambio implementa ganchos definidos en `alumnes-i-assignacions` (aún no archivado) y usa el cargo de reposición de `pagaments`. -->

## Fuera de alcance

- Llaves numeradas, varias llaves por taquilla y candados aportados por el alumno.
- Cambio de bombín o cerradura y otras tareas de mantenimiento (`manteniment`); aquí solo se registra que el centro ha repuesto la llave.
- El cargo de reposición en sí (`pagaments`); aquí solo se solicita su generación.
- Cierre de curso y liberación masiva de taquillas (`cursos-i-historial`), que invocará la devolución masiva de llaves.
- La fianza: es independiente de la llave y nunca cambia por entregas, devoluciones o pérdidas.
- Pantallas (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: nuevo estado de llave en las asignaciones, eventos de llave, casos de uso y consultas en Domain y Application; entidades EF Core y migración en Infrastructure; implementaciones de `IAssignmentGuard`, `IAssignmentOpenedHandler` e `IAssignmentClosedHandler` definidos en `alumnes-i-assignacions`.
- **Datos personales (RGPD)**: se guarda qué llave tiene cada alumno y cuándo; los listados no incluyen datos de contacto y los motivos libres no van al registro técnico.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones`, `alumnes-i-assignacions` y `pagaments`.
