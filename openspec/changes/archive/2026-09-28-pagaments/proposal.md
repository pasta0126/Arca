## Why

Saber si cada alumno está al corriente de pago es una de las razones de ser de ARCA. Hace falta un modelo de cobro simple para conserjes: cada concepto de cada alumno está pendiente, pagado, exento o condonado, sin importes parciales ni métodos de pago. También hay que tratar bien lo que no es un caso general: becas y situaciones especiales, una fianza única durante toda la estancia del alumno y deudas de cursos anteriores que se pueden arrastrar o condonar.

## What Changes

- Catálogo de importes por curso para tres conceptos: cuota anual, fianza y reposición de llave. El importe es fijo para todos y un cambio posterior no afecta a lo ya generado.
- Cargos por alumno con cinco estados: pendiente, pagado (con fecha), exento (con motivo), condonado (con motivo) y anulado (con motivo). El importe queda fijado al crear el cargo.
- Generación automática de cargos al abrirse la primera asignación de un alumno en un curso: cuota del curso y, si no tiene una fianza vigente, fianza. La reposición de llave se genera cuando el conserje decide cobrarla.
- Cuota anual completa aunque el alumno llegue o se vaya a mitad de curso; si no procede, se marca exenta o condonada.
- Marcar pagado, exento o condonado, y revertir cualquiera de ellos a pendiente con motivo, sin borrar nada del historial. Ajustar el importe de un cargo pendiente con motivo.
- Deuda de cursos anteriores: se arrastra (queda pendiente y se avisa al asignar, pidiendo confirmación) o se condona, de forma individual o en bloque.
- Fianza única por estancia: se mantiene mientras el alumno siga en el centro y no la toca la devolución anual de la llave. Al causar baja, una fianza pagada pasa a la lista de fianzas por devolver, que el conserge resuelve una a una o en bloque.
- Estado al corriente de pago del alumno y de la taquilla, y consulta de morosos con importes y filtros.

## Capabilities

### New Capabilities
- `conceptes-de-cobrament`: importes por curso de cuota, fianza y reposición de llave.
- `cobraments`: cargos por alumno, sus estados y transiciones, generación, deuda arrastrada o condonada, estado al corriente y morosos.
- `fianca`: ciclo de vida de la fianza única por estancia, devolución y lista de fianzas por devolver.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Este cambio implementa ganchos definidos en `alumnes-i-assignacions` (aún no archivado). -->

## Fuera de alcance

- Métodos de pago, importes parciales, plazos, recibos y justificantes.
- Otros conceptos de cobro configurables: en v1 solo cuota, fianza y reposición de llave.
- El estado de la llave, su pérdida y entrega (`claus`); aquí solo se genera el cargo de reposición cuando el conserge lo decide.
- Cierre de curso guiado, incluida la revisión de la deuda pendiente en ese momento (`cursos-i-historial`); aquí solo las operaciones de arrastrar y condonar.
- Exportación de listados y resumen de cobros (`informes-csv`); aquí la consulta que los alimenta.
- Devoluciones de la cuota.
- Pantallas (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: nuevas entidades y casos de uso en Domain y Application; entidades EF Core y migración en Infrastructure; implementaciones de `IAssignmentGuard`, `IAssignmentOpenedHandler` e `IStudentLifecycleHandler` definidos en `alumnes-i-assignacions`.
- **Datos personales (RGPD)**: el estado de pago de menores es un dato sensible en la práctica. Solo se guarda el estado, el importe y los motivos que escribe el conserje, que no deben incluir datos innecesarios; los motivos no aparecen en el registro técnico y las exportaciones se limitan al mínimo necesario.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones` y `alumnes-i-assignacions`.
