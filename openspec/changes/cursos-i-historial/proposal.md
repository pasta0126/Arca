## Why

Al terminar el curso hay que liberar cientos de taquillas, recuperar las llaves y dejar el curso siguiente listo, y no siempre se puede hacer todo a la vez: hay llaves que tardan en volver y temas pendientes con las familias. Los conserjes necesitan un cierre guiado pero flexible, que se pueda dejar a medias y retomar, y una decisión explícita sobre qué se hace con los datos de un curso ya terminado (menores), sin fijar una política que aún no se ha consultado con ellos.

## What Changes

- Estado intermedio "en cierre" para un curso: se inicia el cierre desde el curso activo, que deja de ser el activo. Así se puede activar el curso siguiente mientras el anterior termina sus pendientes.
- Asistente de cierre guiado con pasos opcionales y reanudables: liberar taquillas del curso, devolver llaves, revisar la deuda pendiente (solo informativo: se arrastra y se gestiona aparte) y dar el curso por cerrado. Cada paso se puede hacer, saltar o dejar para después, en cualquier orden y en varias sesiones.
- Liberación masiva de las taquillas de las asignaciones vigentes del curso, con exclusiones, en dos fases y con historial. Las llaves quedan pendientes de devolución y se resuelven con la devolución masiva de `claus`.
- Cierre definitivo del curso (cerrado) con aviso de lo que queda pendiente y confirmación explícita; queda registrado con recuentos. Un curso cerrado es de solo lectura.
- Un curso en cierre solo admite las operaciones de cierre: liberar taquillas, devolver llaves y gestionar cobros pendientes. Puede volver a activarse si no hay otro activo.
- Conservación por curso cerrado: al cerrarlo se elige conservar, anonimizar o borrar; se puede decidir más tarde y "conservar" se puede cambiar después. Anonimizar conserva importes y recuentos sin identificar a nadie; borrar elimina el curso. No hay número de cursos ni valor por defecto hasta consultarlo con los conserjes.
- **BREAKING** (sobre `alumnes-i-assignacions`, aún no archivado): "Activación de un curso" y "Histórico de solo lectura" de `curs-escolar` se ajustan al nuevo ciclo; la regla "conservación a N cursos configurable" de `openspec/config.yaml` se sustituye por la decisión por curso.

## Capabilities

### New Capabilities
- `tancament-de-curs`: ciclo de vida del curso (activo, en cierre, cerrado), asistente de cierre con pasos opcionales, liberación masiva de taquillas y cierre definitivo con pendientes.
- `conservacio-de-dades`: decisión por curso cerrado de conservar, anonimizar o borrar, con sus reglas y salvaguardas.

### Modified Capabilities

<!-- Ninguna en las specs vigentes (openspec/specs/ está vacío). `curs-escolar`, de `alumnes-i-assignacions` (aún no archivado), se ajusta en ese cambio; ver design.md D9. -->

## Fuera de alcance

- Renovación automática de asignaciones y promoción de alumnos: los alumnos se actualizan con la importación anual (`alumnes-i-assignacions`).
- Condonar o arrastrar la deuda (`pagaments`); el asistente solo la muestra.
- Devolver llaves (`claus`); el asistente invoca la devolución masiva existente.
- Número de cursos por defecto de conservación y borrado automático por antigüedad: se consulta primero con los conserjes; en v1 la decisión es manual.
- Revisión general de taquillas de fin de curso como proceso (v2).
- Informes de cierre en CSV (`informes-csv`) y copia de seguridad (`copies-de-seguretat`); aquí solo se ofrece hacer copia antes de una operación destructiva.
- Pantallas (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: máquina de estados del curso y casos de uso en Domain y Application; entidades y migración EF Core en Infrastructure (estado del curso, registro de cierre, decisión de conservación); implementación de anonimizado y borrado por curso.
- **Datos personales (RGPD)**: es el cambio que decide qué ocurre con datos de menores al terminar el curso. Anonimizar y borrar reducen los datos conservados; por defecto no se borra nada. El registro técnico y el historial no incluyen datos personales. Al borrar o anonimizar también se limpian las referencias al alumno en historiales de taquillas y llaves.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones`, `alumnes-i-assignacions`, `pagaments` y `claus`.
- **Documentación**: actualizar `openspec/config.yaml` (final de curso y conservación) y `docs/roadmap.md`.
