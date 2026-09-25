## Why

Ningún cambio especifica las pantallas concretas de dominio (hallazgo del hito 1, `docs/hito-1.md`): los cambios de dominio las delegan en `ui-shell` y `ux-fonaments`, y estos solo dan el marco, los componentes y el mapa de Inicio. Sin este cambio la etapa 3 (interfaz) no se puede implementar sin improvisar. No habrá maquetas (decisión D3): la interfaz se define aquí, en texto, y se itera con la aplicación funcionando.

## What Changes

- Se definen las pantallas de dominio del **hito 1**, cada una en su sección de la barra lateral (reparto fijado en `ui-shell`): Curso (curso escolar e importes), Taquillas (zonas y taquillas), Alumnos (alta manual, lista, ficha, baja y asignación) y Cobros (cargos de un alumno y morosos).
- Todas siguen el patrón común de `ui-shell` (título y acciones, lista con búsqueda y filtros, detalle, estados vacíos) y se construyen solo con los componentes de `ux-fonaments`; no añaden reglas de negocio, solo las presentan y las invocan.
- Se fijan decisiones de interfaz que las specs de dominio dejaron abiertas: qué columnas tiene cada lista, qué formularios existen, qué se decide en un diálogo y qué en el detalle, y el orden de las acciones.
- Se marca como provisional todo lo que no se ha validado con los conserjes, para ajustarlo con la aplicación funcionando.

## Capabilities

### New Capabilities
- `pantalles-curs-i-imports`: sección Curso: lista de cursos, crear y activar un curso, y definir los importes de cada concepto.
- `pantalles-taquilles-i-zones`: sección Taquillas: gestión de zonas, listado de taquillas con filtros y contadores, alta individual y por rangos, edición, reserva, avería y baja, e historial.
- `pantalles-alumnes-i-assignacions`: sección Alumnos: lista con búsqueda y filtros, alta manual, ficha con matrícula, historial y baja o reactivación, y asignar, cambiar y liberar taquilla.
- `pantalles-cobraments`: sección Cobros: cargos de un alumno con sus operaciones y consulta de morosos.

### Modified Capabilities
<!-- Ninguna: las reglas no cambian. Solo se pone interfaz a comportamiento ya especificado. -->

## Impact

- **Código:** proyecto de pantallas de la aplicación (vistas y modelos de vista de Avalonia), que consume los casos de uso ya definidos. No toca Domain, Application ni Infrastructure, salvo consultas de lectura que falten para las listas (se anotan en `design.md`).
- **Dependencias:** `ux-fonaments` (componentes) y `ui-shell` (marco y registro de secciones) deben existir antes; los casos de uso de `taquilles-i-zones`, `alumnes-i-assignacions` y `pagaments` también.
- **RGPD:** las pantallas muestran nombre, apellidos, nivel, grupo y estado de pago de menores solo en pantalla; el correo y el identificador no aparecen en listas, detalle ni búsquedas, y solo el formulario de alta y edición los pide, como campos opcionales. Nada se exporta ni sale del equipo.

## Fuera de alcance

- Pantallas de llaves, incidencias y mantenimiento en bloque, informes, cierre de curso y conservación, copias de seguridad, importación de alumnos (ODS), devolución de fianza y devolución en bloque, condonación en bloque, asistente de configuración e identidad y tema: van con sus cambios y en hitos posteriores (`docs/hito-1.md`). Cada uno añadirá su pantalla, o se ampliará este cambio, cuando se implemente.
- Marco, navegación, búsqueda global, Inicio y componentes: son de `ui-shell` y `ux-fonaments`.
- Reglas de negocio nuevas y cambios de comportamiento de las capacidades de dominio.
- Maquetas o diseño visual detallado (paleta, iconos, espaciados finales): los define `ui-shell`.
