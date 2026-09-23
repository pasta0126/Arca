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
- Operaciones masivas (rangos e importación) atómicas y con vista previa idéntica al resultado real.
- Historial de eventos de solo añadir, independiente del idioma.
- Lector de CSV genérico reutilizable por la importación de alumnos y otros cambios.

**Non-Goals:**
- Pantallas y navegación (`ui-shell`).
- Ocupación real, alumnos, llaves, cobros e incidencias.
- Cualquier formato de fichero distinto de CSV.

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
Alta por rangos e importación siguen el mismo patrón: una fase de análisis que produce un plan inmutable en memoria (qué se crearía y qué falla) y una fase de confirmación que **revalida contra el estado actual** y aplica todo en una transacción. La vista previa y la confirmación usan el mismo código de validación, de modo que no pueden discrepar.
- Si al confirmar el plan ya no es válido, no se guarda nada y se devuelve el análisis actualizado.
- Importación: solo se aplican las filas válidas; las erróneas se informan. Es una elección deliberada frente a "todo o nada": con cientos de filas, rechazar el fichero por una línea mala frustra al usuario, y la revisión previa impide que los errores pasen desapercibidos.

### D7. Historial de eventos de solo añadir
Tabla de eventos ligada al identificador de la taquilla, con tipo (código estable), instante UTC y valores anterior y nuevo en forma estructurada. **Nunca se guarda texto ya traducido**: el texto se compone al mostrarlo con las claves del idioma activo. No hay operaciones de edición ni borrado en el modelo. Los eventos se escriben en la misma transacción que el cambio que los origina.
Los demás cambios (asignaciones, llaves, incidencias, mantenimiento) añadirán sus propios tipos de evento a este historial; por eso el tipo es un código extensible y no una enumeración cerrada visible en la interfaz.

### D8. Lector de CSV genérico en Application/Infrastructure
Un puerto `ICsvReader` en `Application`, implementado en `Infrastructure` con una biblioteca de CSV consolidada, que se encarga de: detección de separador, comillas, BOM y UTF-8 estricto, y devuelve filas con su número de línea. La correspondencia de cabeceras (idioma activo, sin mayúsculas ni acentos) se hace fuera, en cada importación, para que este componente sirva también a alumnos.
*Alternativa descartada*: analizador propio. Las comillas y los saltos de línea dentro de campos son fáciles de hacer mal.

### D9. Consulta y filtros
Con este volumen, la consulta carga las taquillas activas con su zona y calcula el estado derivado en memoria, aplicando después los filtros. Se prefiere la claridad de una única función de estado a duplicarla en SQL. Si el rendimiento dejara de bastar, se optimiza entonces, protegido por las mismas pruebas.

### D9b. Feedback en las operaciones de este cambio
Todos los casos de uso devuelven el resultado estructurado de `arquitectura-base` (D12) con recuentos: taquillas creadas, zonas creadas, filas omitidas. Análisis e importación informan progreso con recuentos y aceptan cancelación solo antes de la transacción de guardado. Baja, alta por rangos e importación requieren confirmación con su consecuencia. La vista de inventario se entrega a `ui-shell` y `ux-fonaments`, pero la consulta devuelve datos aptos para virtualizar (orden estable, recuento total y sin carga perezosa) y el detalle e historial de una taquilla se piden al abrirla.

### D10. Orden y comparación
Orden por número entero. Orden de zonas y comparaciones de nombre con el componente central de cultura catalana.

## Risks / Trade-offs

- **Estado derivado más difícil de razonar que un campo** → una única función pura, muy probada, con tabla de casos; ninguna otra parte del código calcula el estado.
- **Baja irreversible** → el mensaje de confirmación debe ser explícito en la interfaz; el historial se conserva y dar de alta otra taquilla con el mismo número es siempre posible.
- **Importación parcial puede dejar filas sin importar sin que el usuario lo note** → la revisión previa es obligatoria y el resultado final repite el recuento de importadas y omitidas.
- **Dos taquillas con el mismo número (una de baja) pueden confundir en listados** → la baja se oculta por defecto y, cuando se muestra, se diferencia claramente y se ordena después de la activa.
- **Opciones de reasignar y liberar diferidas** → mientras no exista el cambio de asignaciones, marcar avería en una taquilla ocupada solo permite mantener; se documenta como dependencia y se cubre con pruebas del error de opción no disponible.
- **El sustituto de ocupación oculta fallos de integración** → el cambio de asignaciones debe reemplazarlo y añadir pruebas de integración de extremo a extremo.
- **Una biblioteca de CSV más como dependencia** → acotada a `Infrastructure` tras un puerto, sustituible.

## Migration Plan

Una migración de EF Core crea las tablas de zonas, taquillas y eventos, con los índices únicos de D4 y D5. Base de datos nueva; no hay datos previos que migrar. Como toda migración, pasa por el migrador con copia previa verificada definido en `arquitectura-base`.

## Open Questions

Ninguna pendiente.
