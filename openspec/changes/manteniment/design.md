## Context

Séptimo cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `incidencies` (abrir y reparar incidencias, motivos, invariante de fuera de servicio), en `alumnes-i-assignacions` (cierre de asignaciones) y en `claus` (llaves pendientes en procesos automáticos), además del patrón de operaciones en bloque en dos fases que usa todo el proyecto.

Restricciones propias:
- El alcance de v1 se reduce a operaciones en bloque; las tareas programadas, las recurrencias y los recordatorios quedan fuera.
- Cada taquilla debe conservar su propia incidencia y su historial: la operación en bloque no crea una entidad nueva.
- Un solo PC y una sola instancia: no hay concurrencia.

## Goals / Non-Goals

**Goals:**
- Poner fuera de servicio y reparar cientos de taquillas de una vez, sin trabajo repetitivo.
- Reutilizar íntegramente los casos de uso de `incidencies`, para que el invariante de fuera de servicio no se rompa.
- Operaciones indivisibles y con análisis previo idéntico al resultado.

**Non-Goals:**
- Tareas programadas, recurrentes o anuales, recordatorios, entidad de lote, reasignación en bloque.

## Decisions

### D1. Sin entidades nuevas
Este cambio compone casos de uso, no añade tablas: cada taquilla obtiene una incidencia normal de `incidencies`, con los datos comunes copiados en ella. No hay entidad de lote. Localizar un grupo se hace con filtros sobre las incidencias abiertas (tipo, motivo, zona, rango y fecha de inicio).
*Alternativa descartada*: entidad de lote con nombre. Es más cómoda para reparar "el grupo de la planta 1", pero añade modelo, pantallas y casos límite (qué pasa si se repara una taquilla suelta) para un problema que los filtros ya resuelven.

### D2. Operaciones en dos fases con la misma validación
Igual que altas por rangos, importaciones, condonaciones y devoluciones: el análisis produce un plan inmutable en memoria y la confirmación **revalida contra el estado actual** y aplica todo en una transacción. Vista previa y confirmación usan el mismo código. Si el plan ya no es válido, no se guarda nada y se devuelve el análisis actualizado.

### D3. Selección
Un componente de selección resuelve zona, rango y lista a un conjunto de identificadores de taquilla, sin duplicados, excluyendo las de baja, con límite de 1000. La resolución es determinista y se comparte entre las dos operaciones (puesta fuera de servicio y reparación cuando la selección parte de taquillas).
Para la reparación en bloque la selección parte de las incidencias abiertas filtradas, no de taquillas.

### D4. Puesta fuera de servicio en bloque
Para cada taquilla elegible, llama al caso de uso de apertura de `incidencies` con los datos comunes y la decisión común sobre las ocupadas. Como todo va en una transacción, el fallo de una taquilla revierte la operación completa.
Decisión sobre las ocupadas:
- **Mantener**: se pasa la decisión de mantener a cada apertura.
- **Liberar**: se pasa la decisión de liberar, que cierra la asignación con el contexto de proceso automático, de modo que la llave entregada queda pendiente de devolución (regla de `claus`) y no se supone devuelta.
- **Reasignar**: no se ofrece en bloque porque exige un destino distinto para cada alumno; se hace taquilla a taquilla desde `incidencies`.

### D5. Reparación en bloque
Para cada incidencia seleccionada, llama al caso de uso de reparación de `incidencies` con la fecha y la nota comunes. El análisis marca como no reparables las que empezaron después de la fecha común y bloquea la confirmación hasta que se excluyan o se cambie la fecha.

### D6. Progreso y cancelación
Progreso con recuentos en análisis y aplicación; cancelación solo antes de la transacción de guardado, como en el resto de operaciones en bloque.

### D7. Feedback
Resultado estructurado con recuentos: marcadas, omitidas por motivo, alumnos liberados, reparadas. Confirmación con la consecuencia, incluidos los alumnos afectados si se liberan. Estados vacíos con guía. Aviso en la nota de que no debe contener datos de alumnos.

## Risks / Trade-offs

- **Liberar en bloque puede dejar sin taquilla a muchos alumnos y con muchas llaves pendientes** → la decisión de liberar es explícita, la confirmación indica cuántos alumnos se verán afectados, y "mantener" es la opción natural cuando solo se trata de sustituir bombines.
- **Sin entidad de lote, reparar "el grupo" depende de filtros** → filtros por tipo, motivo, zona, rango y fecha de inicio, más selección editable; si en la práctica no basta, el lote es una ampliación sencilla y compatible.
- **Una operación grande en una sola transacción** → 1000 taquillas es un volumen pequeño para SQLite; se prueba con el máximo y se informa del progreso.
- **La nota común no se puede ajustar por taquilla** → se puede editar la incidencia individual con los casos de uso de `incidencies`.
- **Tareas programadas y recordatorios fuera de v1** → queda documentado en el roadmap como backlog; la arquitectura no lo impide.

## Migration Plan

No aplica: este cambio no añade tablas ni migraciones. Se apoya en las de `incidencies`.

## Open Questions

Ninguna pendiente.
