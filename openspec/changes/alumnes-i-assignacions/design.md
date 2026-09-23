## Context

Tercer cambio del proyecto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Se apoya en `arquitectura-base` (capas, EF Core cifrado, migraciones, i18n, comparación de texto catalana, resultado estructurado, progreso y cancelación) y en `taquilles-i-zones` (taquillas, estado derivado, historial de eventos, lector de CSV, patrón de análisis y confirmación en dos fases).

Restricciones propias:
- Es el primer cambio con datos de menores: el diseño minimiza qué se guarda, dónde se muestra y qué llega al registro técnico.
- El formato del fichero de secretaría es desconocido hasta recibir una muestra; el diseño no puede depender de él.
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
- Cualquier formato distinto de CSV.
- Fijar ahora qué identificador oficial se guarda.

## Decisions

### D1. Modelo de dominio
- `AcademicYear`: año de inicio, fechas, activo. Nombre derivado.
- `Student`: identidad interna, nombre, apellidos, correo e identificador opcionales, estado activo o de baja con motivo y fecha.
- `Enrollment`: alumno, curso, nivel y grupo (grupo opcional). Máximo una por alumno y curso.
- `Level` y `Group`: catálogo dinámico; el grupo pertenece a un nivel.
- `Assignment`: alumno, taquilla, curso, inicio, fin y motivo de cierre. Vigente mientras no tiene fin.
- Reserva: el hecho de reserva de `Locker` (D1 de `taquilles-i-zones`) gana un alumno opcional.
- Todo cambio escribe un evento en el historial de alumno o de taquilla (mismo patrón append-only y estructurado de `taquilles-i-zones`, sin texto traducido).

### D2. Claves de reconocimiento normalizadas
Al guardar un alumno se calculan y almacenan claves normalizadas: nombre y apellidos (sin mayúsculas, acentos ni espacios repetidos), correo (minúsculas y recortado) e identificador (recortado, sin espacios internos). Se indexan **sin unicidad**, porque los homónimos son legítimos. La normalización usa el componente central de comparación de `arquitectura-base`.
Cuáles de las tres claves se usan es configuración (activar o desactivar cada una), lo que permite fijar el criterio cuando llegue la muestra sin cambiar el modelo.

### D3. Conciliación como función pura en dos fases
Un servicio de dominio recibe los alumnos existentes y las filas del fichero ya mapeadas y devuelve un **plan inmutable** con categorías: nuevo, actualizado, sin cambios, baja propuesta, reactivación propuesta, dudoso y error. No accede a datos ni al reloj.
Reconocimiento por prioridad:
1. Cada fila busca candidatos por cada clave activa (identificador, correo, nombre).
2. Si dos claves apuntan a alumnos distintos: dudoso.
3. Si solo hay una vía de reconocimiento con un único candidato: reconocido.
4. Si el nombre da varios candidatos: se desempata por nivel y grupo de su matrícula del curso activo, solo entre los candidatos que la tienen (a mitad de curso); en la importación de inicio de curso los candidatos aún no tienen matrícula del curso nuevo y su nivel cambia cada año, así que no hay desempate y el caso es dudoso; si sigue habiendo más de uno o ninguno coincide: dudoso.
5. Sin candidatos: nuevo.
Dos filas que se reconocen como el mismo alumno existente son un error; dos filas idénticas sin alumno existente son dudosas (¿persona repetida o dos personas distintas?).
Al confirmar se **revalida** contra el estado actual y se aplica en una transacción (patrón D6 de `taquilles-i-zones`). Vista previa y confirmación usan el mismo código.
*Alternativa descartada*: aplicar fila a fila con decisiones inmediatas. Impide la revisión global y deja estados intermedios ante fallos.

### D4. Bajas por ausencia y salvaguarda
Todo alumno activo que no se reconoce en ninguna fila se propone como baja, sin distinguir niveles: así se cubren de una vez los finalistas y los alumnos que se van. El usuario puede excluir bajas concretas en la revisión. Si las bajas propuestas superan el 30 % de los alumnos activos, se exige una segunda confirmación. El umbral es una única constante configurable. La baja es reversible (reactivación), lo que hace tolerable el error.

### D5. Correspondencia de columnas
El asistente recibe las cabeceras y propone el campo de cada columna comparando con sinónimos definidos **en recursos** (no en código), que incluyen variantes en catalán, castellano e inglés, porque los ficheros de secretaría pueden venir en cualquier idioma. La correspondencia elegida se guarda con la firma de las cabeceras (conjunto normalizado); si una importación posterior tiene la misma firma, se reutiliza. Los campos imprescindibles son nombre, apellidos y nivel.
Se define un tipo de correspondencia extensible (campo destino, columna origen) para poder añadir, si la muestra lo exige, formas como una columna única de apellidos y nombre, sin rediseñar el asistente.

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
Correo e identificador se almacenan en columnas propias, dentro de la base de datos cifrada. Las consultas y objetos de transferencia para listados y exportaciones **no los incluyen**; solo un objeto de detalle específico de la revisión de dudosos puede llevarlos. El registro técnico usa las mismas reglas de `arquitectura-base`: nunca valores de datos de alumnos.
El identificador es opcional y su activación como clave es configurable, lo que permite decidir la política de RGPD tras ver la muestra.

### D12. Histórico de solo lectura
Las matrículas y asignaciones de un curso no activo se rechazan en dominio (comprobación en el punto único de escritura), no solo en la interfaz. `cursos-i-historial` refina esa comprobación con el estado del curso (en cierre y cerrado admiten solo las operaciones de cierre): aquí se implementa como una función de curso y tipo de operación que ese cambio amplía, no como un simple "es activo".

### D13. Consulta de alumnos
Como con las taquillas, con este volumen se filtra en memoria sobre datos cargados explícitamente (sin carga perezosa), con orden estable y recuento total para poder virtualizar la lista. El detalle e historial se piden al abrir la ficha.

## Risks / Trade-offs

- **Un fichero incompleto puede proponer bajas masivas** → revisión previa obligatoria, exclusión de bajas concretas, umbral con segunda confirmación y baja reversible.
- **Reconocimiento por nombre puede confundir a homónimos entre cursos, porque nivel y grupo cambian cada año** → cualquier ambigüedad se marca como dudosa y decide el usuario; el identificador o el correo, si el fichero los trae, evitan casi todos estos casos. Es el principal motivo para ver la muestra pronto.
- **Formato del fichero desconocido** → asistente de correspondencia y tipo de correspondencia extensible; si la muestra trae algo no previsto, se añade un requisito antes de implementar la importación.
- **Guardar correo e identificador de menores** → minimización (opcionales, activables), no se muestran ni exportan, base cifrada, registro sin datos personales; la política final se cierra con la dirección del centro.
- **Ganchos sin implementaciones hasta los cambios siguientes** → pruebas con dobles; los cambios `pagaments` y `claus` deben añadir pruebas de integración.
- **Tres caminos de asignación (alumno, taquilla, arrastrar)** → un único caso de uso y pruebas que verifican la equivalencia.
- **Ampliar el hecho de reserva de `taquilles-i-zones`** → cambio pequeño y compatible (alumno opcional); la migración se prueba con reservas existentes.
- **Baja de muchos alumnos libera muchas taquillas en una transacción** → volumen pequeño; se prueba con 300 taquillas y se informa del progreso.

## Migration Plan

Una migración de EF Core crea las tablas de cursos, alumnos, matrículas, niveles, grupos y asignaciones, con sus índices, y añade el alumno opcional a la reserva de taquillas. Sin datos previos que migrar salvo las reservas existentes, que quedan sin alumno. Como toda migración, pasa por el migrador con copia previa verificada.

## Open Questions

- Identificador que se guarda como clave (por ejemplo ID de secretaría) o ninguno: se decide con el fichero de muestra y con la dirección del centro. No altera specs ni tareas, porque el identificador es opcional y su uso como clave es configurable.
