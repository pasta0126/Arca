## Context

Decimoquinto cambio. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Pone interfaz a las capacidades de dominio del hito 1 (`taquilles-i-zones`, `alumnes-i-assignacions`, `pagaments`) usando el marco de `ui-shell` (secciones registradas, patrón de pantalla) y los componentes de `ux-fonaments`. No hay maquetas (decisión D3 de `docs/riesgos.md`): las decisiones de interfaz están en los specs y en este documento, y se ajustan con la aplicación funcionando y los conserjes.

Restricciones: Avalonia con MVVM, ratón y teclado, catalán por claves de recurso, tres sistemas, datos de menores solo en pantalla, sin lógica de negocio en las vistas.

## Goals / Non-Goals

**Goals:**
- Que cada pantalla sea una composición fina de componentes ya especificados y de casos de uso ya definidos.
- Un patrón de pantalla único que haga predecible cualquier sección, incluidas las del hito 2.
- Que se pueda ajustar una pantalla sin tocar reglas de negocio ni otras pantallas.

**Non-Goals:**
- Reglas nuevas, diseño visual final (paleta, iconos, espaciados: `ui-shell`) y las pantallas de hitos posteriores.

## Decisions

### D1. Una pantalla = un modelo de vista de lista y otro de detalle
Cada pantalla de lista y detalle se compone de un modelo de vista de lista (consulta, filtros, orden, selección) y otro de detalle (elemento, acciones, historial) que hablan con Application solo por casos de uso y consultas. Las vistas no contienen lógica. Así el comportamiento se prueba sin ventana, como fija `ux-fonaments` (D2). *Alternativa descartada*: un solo modelo de vista por pantalla; crece sin límite y mezcla consulta con acciones.

### D2. Acciones como objetos compartidos
Cada operación (asignar, reservar, marcar pagado...) es una acción del registro de `ux-fonaments` con nombre, atajo, disponibilidad y motivo, que se enlaza al botón, al menú contextual y al atajo. Los motivos de "deshabilitada" salen de la respuesta de Application (comprobaciones previas), no se recalculan en la vista, para que las tres formas de invocar una operación nunca discrepen.

### D3. Formularios: validar al guardar, sin perder lo escrito
Los formularios de crear y editar usan un modelo de formulario con errores por campo que llegan de los códigos de error estables de Application. Un error no cierra el formulario. Se valida al guardar, no en cada pulsación, salvo lo derivado que se muestra al escribir (nombre del curso). Todos los guardados usan el comando de ejecución única (`ux-fonaments` D5). *Alternativa descartada*: validar en cliente cada regla; duplicaría reglas de Domain.

### D4. Lista con detalle en el mismo panel
Las secciones con lista usan lista a la izquierda y detalle a la derecha, con apilado por debajo del umbral de `ux-fonaments` (D9) y selección persistente por identidad. La ficha del alumno usa pestañas (Datos, Taquilla, Cobros, Historial) cargadas al abrirlas, no antes, por la regla de carga bajo demanda.

### D5. Selectores compartidos de zona, taquilla y alumno
Asignar desde el alumno, desde la taquilla y arrastrando termina en el mismo diálogo de confirmación con los avisos e impedimentos que devuelve `IAssignmentGuard`. Los selectores (taquilla libre por zona con sugerencia, alumno sin taquilla con búsqueda) son componentes de esta capa reutilizables por Inicio, de modo que Inicio y la sección Alumnos no tengan dos flujos.

### D6. Consultas de lectura
Las listas usan consultas agregadas en Application que devuelven objetos de transferencia ya compuestos (por ejemplo, taquilla con zona, estado visible, alumno y marca de deuda) en lotes y sin carga perezosa. Si alguna consulta del hito 1 no existe con esa forma, se añade como tarea de este cambio y sin reglas nuevas, y el hallazgo se anota en el spec de dominio afectado. Los objetos de transferencia no llevan correo ni identificador; solo el formulario de edición de la ficha los lee, con una consulta propia.

### D7. Reparto en el registro de secciones
Cada pantalla se registra en una sola sección, según `ui-shell` (Curso: cursos e importes; Taquillas: taquillas y zonas; Alumnos: alumnos; Cobros: cargos y pendientes). Cobros aparece también como pestaña de la ficha del alumno reutilizando el mismo modelo de vista, no una copia.

### D8. Textos y términos
Todos los textos por clave de recurso en catalán, con los términos de `docs/glosario.md` («Dipòsit», «Pendents de pagament», «Curs en tancament»). Los términos pendientes de validar (E5) se centralizan en recursos para cambiarlos sin tocar vistas.

### D9. Iterar sin maquetas
Sin maquetas, cada pantalla se implementa primero con lo mínimo que cumple el spec y se enseña funcionando antes de pulirla. Lo que los conserjes cambien se recoge editando el spec de esta capacidad, como fija la regla de parar y actualizar la spec de `docs/flujo-de-trabajo.md`.

## Risks / Trade-offs

- **Sin maquetas, la primera versión puede no gustar** → pantallas mínimas y demostración temprana; los specs se ajustan y las vistas son finas.
- **Las reglas se filtran a las vistas** → las acciones y sus motivos vienen de Application; una prueba de arquitectura impide que los modelos de vista dependan de Domain o Infrastructure.
- **Listas con cientos de filas lentas** → virtualización y consultas agregadas de `ux-fonaments` y `ui-shell`; pruebas con los datos de ejemplo (300 taquillas).
- **Solapamiento con Inicio** (asignar y detalle de taquilla existen en dos sitios) → selectores y acciones compartidos (D2, D5).
- **Consultas de lectura que falten** → se añaden como tareas y se documenta cualquier hueco en las specs de dominio.

## Migration Plan

Sin migración de datos. Las pantallas se registran en las secciones existentes y se activan al implementarse, sin afectar a los datos.

## Open Questions

- Columnas exactas y anchos de las listas, y el orden fino de las acciones en los menús: se ajustan con la aplicación funcionando y no cambian los specs.
- Si la sección Cobros necesita a la larga una vista propia de cargos por concepto: se decide tras la demostración, sin afectar al hito 1.
