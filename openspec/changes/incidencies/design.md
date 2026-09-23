## Context

Sexto cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `taquilles-i-zones` (estado de fuera de servicio con tipo averiada o en mantenimiento, historial de eventos, gancho de baja `ILockerRetiredHandler`) y en `alumnes-i-assignacions` (decisión de mantener, reasignar o liberar cuando la taquilla está ocupada).

Restricciones propias:
- El producto quiere gesto mínimo: una taquilla está operativa, averiada o en mantenimiento, con un motivo de una lista y una nota. Sin estados intermedios ni flujo de trabajo.
- Solo el conserje registra incidencias.
- Un solo PC y una sola instancia: no hay concurrencia.

## Goals / Non-Goals

**Goals:**
- Una única fuente de verdad sobre si una taquilla está fuera de servicio, coherente con las incidencias abiertas.
- Abrir y reparar con la menor fricción posible, y con la decisión obligatoria cuando la taquilla está ocupada.
- Historial de incidencias por taquilla útil para detectar las más problemáticas.
- Catálogo de motivos editable y con lista inicial.

**Non-Goals:**
- Flujo de trabajo, prioridades, responsables, adjuntos, costes.
- Mantenimientos programados y operaciones sobre varias taquillas a la vez (`manteniment`).

## Decisions

### D1. La incidencia es el registro y el hecho de fuera de servicio se mantiene junto a ella
`Incident`: taquilla, tipo, motivo, nota, fecha de inicio, fecha de resolución, nota de resolución y, en su caso, un código de cierre automático por baja. `taquilles-i-zones` guarda el hecho de fuera de servicio con su tipo, del que se deriva el estado visible con una función pura y rápida.
Para que no haya dos fuentes de verdad, los casos de uso de fuera de servicio de `taquilles-i-zones` pasan a ser **internos**: la interfaz y los demás cambios solo usan los casos de uso de incidencias, que llaman a los internos y crean o cierran el registro en la misma transacción. Una prueba de arquitectura impide otro camino, y una prueba de invariante comprueba que una taquilla está fuera de servicio si y solo si tiene una incidencia abierta y con el mismo tipo.
*Alternativa descartada*: derivar el fuera de servicio de las incidencias abiertas y dejar de guardarlo en la taquilla. Es más puro, pero obliga a consultar incidencias para calcular cada estado visible y a reescribir la spec de `taquilles-i-zones`.

### D2. Una sola incidencia abierta por taquilla
Índice único parcial sobre la taquilla limitado a las incidencias sin fecha de resolución. El dominio valida antes con un error que sugiere cambiar el tipo o el motivo; el índice es la red de seguridad. No hay reapertura: reparar es final y una nueva avería crea otra incidencia.

### D3. Apertura como una única operación
`OpenIncident(taquilla, tipo, motivo, nota, inicio, decisión?)` compone, en una transacción: comprobaciones, aplicación de la decisión si la taquilla está ocupada (mantener, reasignar o liberar, con los casos de uso de asignaciones), marca de fuera de servicio y creación del registro. Si cualquier paso falla, no queda nada a medias.
Si la taquilla está ocupada y falta la decisión, devuelve el resultado explícito de "decisión requerida" de `taquilles-i-zones` sin cambiar nada.

### D4. Reparación
`RepairIncident(incidencia, fecha, nota)` cierra el registro y quita el fuera de servicio en la misma transacción. No pide confirmación en la interfaz: es reversible abriendo otra incidencia y no toca datos de otras personas.

### D5. Catálogo de motivos
`IncidentReason`: nombre, clave normalizada con índice único y activo. Igual que las zonas: renombrar afecta a todas las incidencias que lo usan (la incidencia guarda la referencia, no el texto), desactivar impide nuevos usos y solo se elimina un motivo que nunca se usó.
La lista inicial se crea con un inicializador de primer arranque que usa las claves de recurso en el idioma activo, **no** con datos en la migración, porque el texto depende del idioma. Un indicador de "lista inicial creada" evita que se vuelva a crear si el usuario elimina motivos. Una vez creados, son datos del usuario y no se retraducen.

### D6. Baja de la taquilla
Se implementa `ILockerRetiredHandler`: cierra la incidencia abierta con la fecha de la baja y un **código** de cierre automático. El texto de esa nota se compone al mostrarlo con las claves del idioma activo, igual que los eventos del historial: nunca se guarda texto traducido.

### D7. Historial
Los cambios de tipo, motivo y nota escriben eventos estructurados en el historial de la taquilla (mismo mecanismo de solo añadir de `taquilles-i-zones`) con el identificador de la incidencia. El historial de incidencias por taquilla se construye con los registros de incidencia y la duración en días se calcula con el reloj inyectado, no se almacena.

### D8. Consulta de taquillas fuera de servicio
Como en los cambios anteriores, con este volumen se cargan explícitamente (sin carga perezosa) las incidencias abiertas con taquilla, zona y presencia de asignación, y se filtra en memoria. Los objetos de transferencia de los listados no incluyen la nota, que solo aparece en el detalle de la incidencia.

### D9. Feedback
Resultado estructurado con la taquilla afectada. Sin confirmación al reparar; confirmación de las decisiones sobre la taquilla ocupada, ya definidas en asignaciones. Estados vacíos con guía: sin motivos activos, sin incidencias, sin taquillas fuera de servicio.

## Risks / Trade-offs

- **Dos lugares con información de fuera de servicio (el hecho y la incidencia)** → casos de uso internos, prueba de arquitectura contra otros caminos y prueba de invariante.
- **Sin reapertura, un cierre por error obliga a abrir otra incidencia** → la nueva incidencia puede llevar la fecha de inicio anterior y el historial de la taquilla muestra ambas; el coste es bajo comparado con el de modelar reaperturas.
- **La lista inicial en catalán queda fija aunque cambie el idioma** → son datos del usuario, editables; se documenta como decisión de v1.
- **Las notas libres pueden contener datos personales** → guía en la interfaz, exclusión de listados y del registro técnico.
- **Abrir una incidencia en una taquilla ocupada acopla este cambio a asignaciones** → se reutilizan los casos de uso de decisión ya definidos y la operación es una sola transacción.
- **Cerrar automáticamente por baja pierde el contexto de la resolución real** → el código de cierre lo deja explícito en el historial.

## Migration Plan

Una migración de EF Core crea las tablas de incidencias y de motivos, con el índice único parcial de una incidencia abierta por taquilla y el índice único de la clave normalizada del motivo. Sin datos previos. La lista inicial de motivos la crea el inicializador de primer arranque. Como toda migración, pasa por el migrador con copia previa verificada.

## Open Questions

Ninguna pendiente. Los motivos iniciales exactos (cerradura, puerta, bisagra, limpieza, vandalismo, otro) son una propuesta editable por el usuario.
