## Context

Tercer cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `arquitectura-base` (capas, EF Core cifrado, migraciones, i18n, comparación de texto catalana, resultado estructurado, progreso y cancelación) y en `taquilles-i-zones` (taquillas, estado derivado, historial de eventos, patrón de análisis y confirmación en dos fases).

Restricciones propias:
- Es el primer cambio con datos de menores: el diseño minimiza qué se guarda, dónde se muestra y qué llega al registro técnico.
- El fichero de secretaría es un ODS con una hoja por grupo y dos columnas relevantes (nombre completo y correo). Su formato es fijo y se documenta en `importacio-alumnes`; hay un fichero de ejemplo anonimizado en `docs/datos-de-ejemplo-anonimizado.ods`.
- Volumen: hasta unos 2000 alumnos y 5000 filas por fichero. Cabe en memoria; la conciliación es un problema de correlación, no de rendimiento.
- Un solo PC y una sola instancia: no hay concurrencia.

## Goals / Non-Goals

**Goals:**
- Modelo de dominio de curso, alumno, matrícula, catálogo y asignación con las reglas de los specs, verificable sin base de datos.
- Conciliación como función pura y determinista: mismas entradas, mismo plan.
- Un único caso de uso de asignar que sirva a todos los caminos de la interfaz.
- Ocupación real de las taquillas, sustituyendo al sustituto de `taquilles-i-zones`.
- Puntos de enganche limpios para `pagaments` y `claus` (D8 y D9).

**Non-Goals:**
- Cierre de curso, cobros, llaves, pantallas y arrastrar y soltar visual.
- Cualquier formato de importación distinto de ODS (la exportación de informes sigue en CSV, en `informes-csv`).
- Correspondencia flexible de columnas: el formato es fijo.

## Decisions

### D1. Modelo de dominio
- `AcademicYear`: año de inicio, fechas, activo. Nombre derivado.
- `Student`: identidad interna, nombre, apellidos, correo obligatorio y único, estado activo o de baja con motivo y fecha.
- `Enrollment`: alumno, curso, nivel y grupo (grupo opcional). Máximo una por alumno y curso.
- `Level` y `Group`: catálogo dinámico; el grupo pertenece a un nivel.
- `Assignment`: alumno, taquilla, curso, inicio, fin y motivo de cierre. Vigente mientras no tiene fin.
- Reserva: el hecho de reserva de `Locker` (D1 de `taquilles-i-zones`) gana un alumno opcional.
- Todo cambio escribe un evento en el historial de alumno o de taquilla (mismo patrón append-only y estructurado de `taquilles-i-zones`, sin texto traducido).

### D2. Correo como identificador único
El correo identifica al alumno mientras pertenezca al centro. Se guarda normalizado (minúsculas, recortado) y se indexa **con unicidad**, incluidos los alumnos de baja, de modo que un alumno que reaparece se reactiva y no se duplica. El nombre y los apellidos no identifican: los homónimos son legítimos y no requieren tratamiento especial. La normalización usa el componente central de comparación de `arquitectura-base`. Para las búsquedas se guarda además la clave normalizada de nombre y apellidos (sin mayúsculas ni acentos), sin unicidad.
El nombre completo del fichero (`Apellidos, Nombre`) se separa en la primera coma. No se intenta interpretar formatos sin coma: esas filas son erróneas.

### D3. Conciliación como función pura en dos fases
Un servicio de dominio recibe los alumnos existentes y las filas del fichero ya leídas (correo, apellidos, nombre, nivel y grupo) y devuelve un **plan inmutable** con categorías: nuevo, actualizado, sin cambios, baja propuesta, reactivación propuesta y error. No accede a datos ni al reloj.
Cada fila se reconoce por correo: si coincide con un alumno, se actualiza o se reactiva; si no, es nuevo. Si un correo aparece en varias filas del fichero (de la misma hoja o de otras), todas son erróneas. No hay casos dudosos ni resolución manual.
Al confirmar se **revalida** contra el estado actual y se aplica en una transacción (patrón D6 de `taquilles-i-zones`). Vista previa y confirmación usan el mismo código.
*Alternativa descartada*: reconocer por nombre además del correo. Reintroduce los homónimos y la revisión manual sin aportar nada, porque el correo no cambia mientras el alumno pertenece al centro.

### D4. Bajas por ausencia y salvaguarda
Todo alumno activo que no se reconoce en ninguna fila se propone como baja, sin distinguir niveles: así se cubren de una vez los finalistas y los alumnos que se van. El usuario puede excluir bajas concretas en la revisión. Si las bajas propuestas superan el 30 % de los alumnos activos, se exige una segunda confirmación. El umbral es una única constante configurable. La baja es reversible (reactivación), lo que hace tolerable el error.

### D5. Lector de ODS y formato fijo
Un puerto `IStudentSheetReader` lee el fichero y devuelve, por hoja, su nombre y sus filas (línea, nombre completo, correo). Su implementación abre el ODS como un zip y lee `content.xml` con `System.IO.Compression` y `System.Xml`, sin dependencias; entra por streaming y expande las repeticiones de celdas y filas (`number-columns-repeated`, `number-rows-repeated`) con un tope, porque una hoja mal guardada puede declarar millones de celdas vacías. Solo se leen valores de texto: las fórmulas y los formatos se ignoran. Las cabeceras `Nom complet` y `Correu` se buscan en la fila 1 de cada hoja con la comparación normalizada, sin depender del orden. Nivel y grupo se derivan del nombre de la hoja (la última palabra es el grupo). La lectura está en Infrastructure; `Domain` y `Application` no conocen el formato.

### D6. Catálogo de niveles y grupos
Sin valores fijos en el código. Las claves normalizadas hacen que "1r ESO" y "1R eso" sean el mismo valor. Los valores nuevos se recogen durante el análisis y solo se crean al confirmar. Orden de presentación con ordenación natural (respeta que "2n" va antes que "10è"). No se modela un orden de promoción porque la conciliación lo hace innecesario.

### D7. Asignación: un único caso de uso
`AssignLocker(alumno, taquilla, avisos confirmados)` es el único camino para asignar; la interfaz (desde el alumno, desde la taquilla, arrastrar, reasignar tras avería, cambio de taquilla) invoca variantes de este mismo caso de uso. Así las validaciones y confirmaciones no pueden divergir. Cambiar de taquilla y reasignar por avería componen cierre más apertura en una sola transacción.
Unicidad reforzada por índices únicos parciales: una asignación vigente por taquilla y una por alumno.

### D8. Gancho de comprobaciones de asignación
Interfaz `IAssignmentGuard` que devuelve hallazgos sobre una asignación propuesta. Cada hallazgo es un **aviso** (exige confirmación explícita del llamador) o un **impedimento** (la asignación se rechaza con un error de negocio). Este cambio la define y la invoca sin implementaciones; `pagaments` aportará el aviso de deuda de cursos anteriores y `claus` el impedimento de taquilla sin llave disponible.

### D9. Ganchos de ciclo de vida
Interfaces invocadas **dentro de la misma transacción** que el cambio que las origina, para que nunca queden datos a medias:
- `IAssignmentOpenedHandler`: cuando se abre una asignación (asignar, cambiar, reasignar). `pagaments` la usará para generar cargos.
- `IAssignmentClosedHandler`: cuando se cierra una asignación (liberación, cambio, baja, avería). `claus` la usará para el estado de la llave.
- `IStudentLifecycleHandler`: cuando un alumno causa baja o se reactiva. `pagaments` la usará para la fianza.
Todos reciben un **contexto de operación** con el motivo y datos adicionales opcionales que aporta el llamador (por ejemplo, qué ocurrió con la llave al liberar o cambiar de taquilla), de modo que las capacidades posteriores puedan pedir información sin cambiar las firmas. Este cambio las define y las invoca, sin implementaciones. Los manejadores no pueden rechazar la operación salvo con un error que revierte todo.

### D10. Ocupación real
Se sustituye el sustituto "sin asignación" de `taquilles-i-zones` por una consulta real sobre asignaciones vigentes, que sirve por lotes para no consultar taquilla a taquilla. La reserva con alumno amplía el hecho de reserva; la migración de este cambio añade esa columna.

### D11. Privacidad de los datos de reconocimiento
El correo se almacena en una columna propia, dentro de la base de datos cifrada. Las consultas y objetos de transferencia para listados y exportaciones **no lo incluyen**; solo el detalle de la ficha y de la revisión de la importación pueden llevarlo. El registro técnico usa las mismas reglas de `arquitectura-base`: nunca valores de datos de alumnos. El fichero de secretaría se lee en memoria y no se copia ni se conserva.

### D12. Histórico de solo lectura
Las matrículas y asignaciones de un curso no activo se rechazan en dominio (comprobación en el punto único de escritura), no solo en la interfaz. `cursos-i-historial` refina esa comprobación con el estado del curso (en cierre y cerrado admiten solo las operaciones de cierre): aquí se implementa como una función de curso y tipo de operación que ese cambio amplía, no como un simple "es activo".

### D13. Consulta de alumnos
Como con las taquillas, con este volumen se filtra en memoria sobre datos cargados explícitamente (sin carga perezosa), con orden estable y recuento total para poder virtualizar la lista. El detalle e historial se piden al abrir la ficha.

## Risks / Trade-offs

- **Un fichero incompleto puede proponer bajas masivas** → revisión previa obligatoria, exclusión de bajas concretas, umbral con segunda confirmación y baja reversible.
- **Un correo mal escrito en el fichero crea un alumno duplicado o da de baja al real** → no se detecta por diseño (el correo es la identidad); la revisión previa muestra altas y bajas propuestas y la baja es reversible. Un correo repetido en el fichero se rechaza.
- **Un fichero ODS mal formado o hostil (zip bomb, repeticiones enormes)** → topes de tamaño descomprimido y de repeticiones, y rechazo con error claro.
- **Formato ODS distinto del esperado** → cabeceras y nombres de hoja validados con mensajes que indican hoja y columna; el fichero de ejemplo anonimizado sirve de referencia.
- **Guardar el correo de menores** → no se muestra en listados ni se exporta, base cifrada, registro sin datos personales.
- **Ganchos sin implementaciones hasta los cambios siguientes** → pruebas con dobles; los cambios `pagaments` y `claus` deben añadir pruebas de integración.
- **Tres caminos de asignación (alumno, taquilla, arrastrar)** → un único caso de uso y pruebas que verifican la equivalencia.
- **Ampliar el hecho de reserva de `taquilles-i-zones`** → cambio pequeño y compatible (alumno opcional); la migración se prueba con reservas existentes.
- **Baja de muchos alumnos libera muchas taquillas en una transacción** → volumen pequeño; se prueba con 300 taquillas y se informa del progreso.

## Migration Plan

Una migración de EF Core crea las tablas de cursos, alumnos, matrículas, niveles, grupos y asignaciones, con sus índices, y añade el alumno opcional a la reserva de taquillas. Sin datos previos que migrar salvo las reservas existentes, que quedan sin alumno. Como toda migración, pasa por el migrador con copia previa verificada.

## Open Questions

Ninguna. El identificador queda resuelto: es el correo.
