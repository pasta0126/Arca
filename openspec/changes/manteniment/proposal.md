## Why

A veces hay que sacar de servicio a la vez un grupo entero de taquillas: se ha roto un conjunto y se espera su reemplazo, o hay que cambiar los bombines de varios grupos. Abrir una incidencia una a una sería muy lento con cientos de taquillas. Hace falta poder marcarlas todas de una vez, con un motivo y una nota opcional, y repararlas también de una vez cuando el trabajo termina.

## What Changes

- Selección de taquillas por zona, por rango de números o por lista elegida a mano, combinables, con un máximo de 1000 por operación.
- Puesta en bloque fuera de servicio: tipo (averiadas o en mantenimiento), motivo de la lista, nota opcional y fecha de inicio comunes. Cada taquilla obtiene su propia incidencia y su propio historial.
- Análisis previo con recuentos: cuántas se marcarán, cuántas se omiten y por qué, y cuántas están ocupadas, sin modificar nada hasta la confirmación.
- Tratamiento de las taquillas ocupadas en bloque con una única decisión: mantener a sus alumnos o liberarlos; no se ofrece reasignar en bloque.
- Reparación en bloque de las incidencias abiertas que cumplan unos filtros, con fecha y nota de resolución comunes.
- Operaciones indivisibles, con confirmación explícita, progreso con recuentos y cancelación antes del guardado.

## Capabilities

### New Capabilities
- `manteniment`: operaciones en bloque de puesta fuera de servicio y reparación de grupos de taquillas.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Este cambio usa los casos de uso de `incidencies` (aún no archivado). -->

## Fuera de alcance (v2 o posterior, o nunca)

- Tareas de mantenimiento programadas, puntuales o recurrentes, y su recurrencia anual.
- Recordatorios, vencimientos y avisos al abrir la aplicación.
- La revisión general de fin de curso como proceso de la aplicación.
- Reasignar a los alumnos en bloque; se hace taquilla a taquilla desde incidencias.
- Un registro de compras o de sustitución de taquillas: se resuelven con alta y baja de taquillas.
- Entidad de "lote" o "trabajo" que agrupe las incidencias creadas juntas: cada taquilla conserva su incidencia independiente y el grupo se recupera filtrando por zona, motivo y fechas.
- Pantallas (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: casos de uso de análisis y confirmación en Application que componen los casos de uso de `incidencies` y `alumnes-i-assignacions`; sin entidades ni migración propias.
- **Datos personales (RGPD)**: ninguno nuevo. La nota es común a todas las taquillas y no incluye datos de alumnos; no aparece en el registro técnico.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones`, `alumnes-i-assignacions` e `incidencies`.
