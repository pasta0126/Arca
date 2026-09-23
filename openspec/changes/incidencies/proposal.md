## Why

Una taquilla puede dejar de estar operativa por una avería o por un mantenimiento, y el conserje necesita anotarlo con un gesto mínimo: qué le pasa, por qué y desde cuándo, y marcarla como reparada cuando vuelve a servir. Además, con el tiempo, cada taquilla debe conservar el historial de lo que le ha ocurrido, para detectar las más problemáticas.

## What Changes

- Incidencia como periodo en que una taquilla está fuera de servicio: tipo (averiada o en mantenimiento), motivo de una lista, nota opcional, fecha de inicio y, al repararla, fecha y nota de resolución.
- Una sola incidencia abierta por taquilla; el conserje es quien la registra.
- Lista de motivos editable (añadir, renombrar, desactivar), con una lista inicial de motivos habituales.
- Cambiar el tipo o corregir el motivo y la nota de una incidencia abierta, con historial.
- Marcar como reparada: la taquilla vuelve a operativa y su estado visible se recalcula.
- Al poner fuera de servicio una taquilla ocupada se aplica la decisión ya definida (mantener, reasignar o liberar).
- La baja de una taquilla cierra automáticamente su incidencia abierta.
- Historial de incidencias por taquilla y lista de taquillas fuera de servicio con filtros.

## Capabilities

### New Capabilities
- `incidencies`: incidencias de taquillas fuera de servicio, catálogo de motivos, reparación, historial y listado.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Este cambio se apoya en el estado de fuera de servicio y en el gancho de baja de `taquilles-i-zones` (aún no archivado). -->

## Fuera de alcance

- Estados intermedios (abierta, en curso, resuelta), prioridades, responsables, adjuntos y costes.
- Tareas de mantenimiento programadas, recurrentes o sobre varias taquillas a la vez (`manteniment`).
- Registro por parte de alumnos u otras personas: solo el conserje usa la aplicación.
- Reabrir una incidencia reparada: si vuelve a fallar, se abre otra.
- Pantallas (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: entidad de incidencia, catálogo de motivos, casos de uso y consultas en Domain y Application; entidades EF Core y migración en Infrastructure; implementación de `ILockerRetiredHandler`.
- **Datos personales (RGPD)**: las notas son texto libre y podrían incluir datos de alumnos; se guía al usuario para no incluirlos, no aparecen en el registro técnico y no salen en listados generales.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones` y `alumnes-i-assignacions` (decisión sobre la taquilla ocupada).
