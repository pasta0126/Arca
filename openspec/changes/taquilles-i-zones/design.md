## Context

Segundo cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en lo decidido en `arquitectura-base`: capas Domain, Application e Infrastructure, EF Core con SQLite cifrado, migraciones envueltas, `IClock`, comparación de texto según el catalán, errores de negocio con código estable y textos por clave.

Restricciones propias:
- Las asignaciones, los alumnos y las llaves no existen todavía; este cambio debe poder completarse y probarse sin ellos, y dejar puntos de enganche limpios para el cambio `alumnes-i-assignacions`.
- Unos 300 a 1000 taquillas: el volumen es pequeño y los filtros pueden resolverse sin optimizaciones especiales.
- Un solo PC y una sola instancia: no hay concurrencia que arbitrar.

## Goals / Non-Goals

**Goals:**
- Modelo de dominio de zonas y taquillas con las reglas de los specs, verificables con pruebas sin base de datos.
- Estado visible derivado de hechos, no almacenado.
- Alta masiva por rangos atómica y con vista previa idéntica al resultado real.
- Historial de eventos de solo añadir, independiente del idioma.

**Non-Goals:**
- Pantallas y navegación (`ui-shell`).
- Ocupación real, alumnos, llaves, cobros e incidencias.
- Importación de taquillas desde fichero (ver *Cambios durante la implementación*).

## Decisions

### D1. Estado visible derivado de hechos
Una taquilla almacena hechos, no un estado: número, zona, nota, estado de fuera de servicio con su tipo (averiada o en mantenimiento), reserva (con su nota) y fecha de baja. La ocupación la aporta el cambio de asignaciones. El estado visible se calcula con una única función pura con esta precedencia: de baja, fuera de servicio, ocupada, reservada, libre.
- **Por qué**: la opción de mantener al alumno al averiarse una taquilla hace que averiada y ocupada coexistan. Un campo único de estado obligaría a recordar el estado anterior para restaurarlo; derivarlo lo hace automático y elimina combinaciones incoherentes.
- *Alternativa descartada*: enumeración persistida con tabla de transiciones. Es más directa de leer, pero la coexistencia de avería y ocupación exige un estado extra o un campo "estado previo", con más casos límite.

### D2. Ocupación como puerto, no como dato propio
`Application` define una interfaz de consulta de ocupación (por identificador de taquilla, o para un conjunto) que este cambio implementa con un sustituto que responde "sin asignación". El cambio de asignaciones aportará la implementación real. Las pruebas de este cambio usan un doble configurable para cubrir los escenarios de taquilla ocupada.

### D3. Decisión al averiar una taquilla ocupada
El caso de uso de marcar avería recibe una decisión opcional. Si la taquilla está ocupada y no hay decisión, devuelve un resultado explícito de "decisión requerida" con las opciones y no cambia nada. En este cambio solo se implementa la opción de mantener. Las opciones de reasignar y liberar necesitan asignaciones y se especificarán y construirán en `alumnes-i-assignacions` como requisitos propios de su capacidad de asignaciones, que amplían este comportamiento. Hasta entonces, elegirlas devuelve un error de opción no disponible.

### D3b. Gancho de baja de taquilla
Interfaz `ILockerRetiredHandler`, invocada dentro de la misma transacción que la baja de una taquilla, para que otras capacidades (como incidencias) cierren lo que dependa de ella. Este cambio la define y la invoca, sin implementaciones.

### D4. Unicidad del número entre activas
Índice único parcial en el número, limitado a las filas sin fecha de baja. El dominio valida antes con un error de negocio comprensible; el índice es la red de seguridad. Las bajas quedan fuera del índice, así que pueden repetir número entre sí y con una activa.

### D5. Unicidad del nombre de zona
El nombre se normaliza con el mismo componente de comparación de `arquitectura-base` (sin mayúsculas ni acentos, con espacios recortados) y se guarda la clave normalizada con un índice único. El dominio valida antes; el índice es la red de seguridad. Al renombrar, la comparación excluye a la propia zona.

### D6. Operaciones masivas en dos fases con la misma validación
El alta por rangos sigue este patrón (que reutilizarán otros cambios): una fase de análisis que produce un plan inmutable en memoria (qué se crearía y qué falla) y una fase de confirmación que **revalida contra el estado actual** y aplica todo en una transacción. La vista previa y la confirmación usan el mismo código de validación, de modo que no pueden discrepar.
- Si al confirmar el plan ya no es válido, no se guarda nada y se devuelve el análisis actualizado.

### D7. Historial de eventos de solo añadir
Tabla de eventos ligada al identificador de la taquilla, con tipo (código estable), instante UTC y valores anterior y nuevo en forma estructurada. **Nunca se guarda texto ya traducido**: el texto se compone al mostrarlo con las claves del idioma activo. No hay operaciones de edición ni borrado en el modelo. Los eventos se escriben en la misma transacción que el cambio que los origina.
Los demás cambios (asignaciones, llaves, incidencias, mantenimiento) añadirán sus propios tipos de evento a este historial; por eso el tipo es un código extensible y no una enumeración cerrada visible en la interfaz.

### D8. (Retirada) Importación de taquillas
No hay lector de CSV ni importación de taquillas en este cambio. Se decidió el 2026-09-25 (ver *Cambios durante la implementación*).

### D9. Consulta y filtros
Con este volumen, la consulta carga las taquillas activas con su zona y calcula el estado derivado en memoria, aplicando después los filtros. Se prefiere la claridad de una única función de estado a duplicarla en SQL. Si el rendimiento dejara de bastar, se optimiza entonces, protegido por las mismas pruebas.

### D9b. Feedback en las operaciones de este cambio
Todos los casos de uso devuelven el resultado estructurado de `arquitectura-base` (D12) con recuentos: taquillas creadas y zonas creadas. El análisis del alta por rangos informa progreso con recuentos y acepta cancelación solo antes de la transacción de guardado. Baja y alta por rangos requieren confirmación con su consecuencia. La vista de inventario se entrega a `ui-shell` y `ux-fonaments`, pero la consulta devuelve datos aptos para virtualizar (orden estable, recuento total y sin carga perezosa) y el detalle e historial de una taquilla se piden al abrirla.

### D10. Orden y comparación
Orden por número entero. Orden de zonas y comparaciones de nombre con el componente central de cultura catalana.

## Risks / Trade-offs

- **Estado derivado más difícil de razonar que un campo** → una única función pura, muy probada, con tabla de casos; ninguna otra parte del código calcula el estado.
- **Baja irreversible** → el mensaje de confirmación debe ser explícito en la interfaz; el historial se conserva y dar de alta otra taquilla con el mismo número es siempre posible.
- **Dos taquillas con el mismo número (una de baja) pueden confundir en listados** → la baja se oculta por defecto y, cuando se muestra, se diferencia claramente y se ordena después de la activa.
- **Opciones de reasignar y liberar diferidas** → mientras no exista el cambio de asignaciones, marcar avería en una taquilla ocupada solo permite mantener; se documenta como dependencia y se cubre con pruebas del error de opción no disponible.
- **El sustituto de ocupación oculta fallos de integración** → el cambio de asignaciones debe reemplazarlo y añadir pruebas de integración de extremo a extremo.

## Migration Plan

Una migración de EF Core crea las tablas de zonas, taquillas y eventos, con los índices únicos de D4 y D5. Base de datos nueva; no hay datos previos que migrar. Como toda migración, pasa por el migrador con copia previa verificada definido en `arquitectura-base`.

## Open Questions

Ninguna pendiente.

## Cambios durante la implementación

### 2026-09-25. La tarea 3.1 se divide para el hito 1
El hito 1 (`docs/hito-1.md`) deja fuera `ICsvReader`, que solo usan las importaciones. La tarea 3.1 pasa a ser los puertos sin `ICsvReader`, y el puerto se recoge en la tarea nueva 3.1b (hito 2), para no marcar como hecho lo que no lo está. Además, la unidad de trabajo (`IUnitOfWork`, `docs/convenciones.md`, sección 4) se añade a los puertos de 3.1 porque los casos de uso la necesitan para guardar cambios y evento en una sola transacción.

### 2026-09-25. Se retira la importación de taquillas (y con ella el lector de CSV)
Motivo: la persona responsable indica que el inventario de taquillas se cargará una vez y después lo mantendrán los conserjes a mano; una importación masiva sería, como mucho, una actuación de desarrollo o mantenimiento sobre la base de un centro, y se prevé un uso mínimo. La carga inicial ya la cubren el alta por rangos (con vista previa y sin efectos parciales) y el alta individual. Se elimina la capacidad `importacio-taquilles`, el puerto `ICsvReader` (D8), las tareas 3.1b, 4.4 a 4.9 y 5.5 y las referencias a la importación en las tareas 6.2, 6.3 y 7.2. *Alternativas descartadas*: importar taquillas desde ODS reutilizando el lector de alumnos (coste y pruebas para un uso casi nulo; se retomaría solo si la práctica lo pide) y mantener el CSV (segundo formato y una dependencia más). Se ajustan también `configuracio-inicial`, `pantalles-de-domini`, `docs/` y `openspec/config.yaml`. Los informes siguen exportándose en CSV (`informes-csv`); la importación de alumnos sigue siendo ODS.
