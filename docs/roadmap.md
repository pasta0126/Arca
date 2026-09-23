# Roadmap de cambios OpenSpec

Orden de trabajo previsto. Cada cambio vive en `openspec/changes/<nombre>/` (proposal, design, specs, tasks).
Estado: **redactado** (specs listas para revisar), **pendiente** (sin redactar) o **bloqueado** (falta una decisión).

| # | Cambio | Contenido | Estado |
|---|---|---|---|
| 1 | `arquitectura-base` | Capas .NET, EF Core + SQLCipher, migraciones, i18n, distribución, contrato de feedback, arranque y registro | redactado |
| 2 | `taquilles-i-zones` | Zonas, taquillas, estados derivados, altas por rangos y CSV, historial | redactado |
| 3 | `alumnes-i-assignacions` | Curso escolar (año académico), alumno persistente con matrícula por curso, catálogo de niveles y grupos, asignación 1 a 1, importación CSV con correspondencia de columnas y revisión, baja y cambio de taquilla, reasignar y liberar al averiarse | redactado (a la espera del fichero de muestra) |
| 4 | `pagaments` | Importes por curso, cargos con estados (pendiente, pagado, exento, condonado, anulado), fianza única por estancia y devolución, deuda arrastrada o condonada, morosos | redactado |
| 5 | `claus` | Estado de la llave por asignación, entrega, devolución individual y masiva, pérdida con reposición y copia, disponibilidad de la llave de la taquilla | redactado |
| 6 | `incidencies` | Taquilla fuera de servicio (averiada o en mantenimiento) con motivo de una lista y nota, una incidencia abierta por taquilla, reparar, historial y lista | redactado |
| 7 | `manteniment` | Operaciones en bloque sobre incidencias: poner un grupo de taquillas en mantenimiento o averiadas y repararlas de una vez, con motivo y nota opcional | redactado |
| 8 | `cursos-i-historial` | Cierre de curso guiado con liberación masiva de taquillas y devolución masiva de llaves, apertura del curso siguiente, conservación y anonimización. La actualización de alumnos (niveles, grupos, bajas de finalistas, repetidores) se hace con la conciliación de la importación anual, definida en `alumnes-i-assignacions` | pendiente |
| 9 | `informes-csv` | Morosos, taquillas libres y averiadas, asignaciones, resumen de cobros (solo CSV) | pendiente |
| 10 | `copies-de-seguretat` | Copia y restauración manuales | pendiente |
| 11 | `llicencies-client` | Clave firmada, gracia y solo lectura, comprobación online opcional | pendiente |
| 12 | `configuracio-inicial` | Asistente guiado de primera configuración | pendiente |
| 13 | `ux-fonaments` | Sistema de diseño: adaptabilidad, colapsables, arrastrar y soltar, menús, componentes de feedback | pendiente |
| 14 | `ui-shell` | Navegación, búsqueda global, pantalla principal, tema e identidad del centro | bloqueado (pantalla principal por decidir) |

## Fuera de este repositorio
- Servidor de licencias: otro proyecto. Aquí solo se prepara el documento de requisitos en `docs/` cuando se disponga de los datos.

## Decisiones abiertas
- Pantalla principal (dashboard, cercador o treball en detall), a validar con los conserjes.
- Detalles del backend de licencias y límite de equipos por clave.

## Backlog v2 o posterior (fuera de v1)
- Tareas de mantenimiento programadas, recurrentes o anuales, recordatorios y avisos al abrir la aplicación.
- Revisión general de fin de curso como proceso de la aplicación.
- Idiomas castellano e inglés con selector.
- Niveles distintos de ESO y Bachillerato (FP y otros).
- Firma y notarización en macOS; paquetes nativos de Linux y macOS.

## Próxima sesión: preguntas pendientes
Siguiente cambio: `cursos-i-historial`. Hay que decidir:
- Cierre de curso: ¿asistente único que libera taquillas y devuelve llaves de golpe (con exclusiones), o pasos separados y opcionales?
- Deuda pendiente al cerrar: ¿se arrastra o condona dentro del asistente o se gestiona después aparte?
- Conservación: número de cursos por defecto y qué se conserva al anonimizar (¿totales para informes?).

Después: `informes-csv`, `copies-de-seguretat`, `llicencies-client`, `configuracio-inicial`, `ux-fonaments`, `ui-shell`.

## Pendiente de recibir
- Fichero de muestra de secretaría (columnas, si trae todo el centro o solo nuevos, identificador). Bloquea cerrar la importación de alumnos.
- Datos del proyecto de licencias, para preparar el documento de requisitos en `docs/`.
- Valoración RGPD con la dirección del centro (identificador, correo).
- Validación de la pantalla principal con los conserjes.
