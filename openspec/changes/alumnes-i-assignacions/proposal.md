## Why

Con el inventario de taquillas ya especificado, falta lo que le da sentido: quién usa cada taquilla. Hay que registrar a los alumnos y su curso escolar, mantenerlos al día cada año sin rehacer el trabajo a mano y asignarles una taquilla con rapidez y sin errores. El fichero de secretaría es la fuente de verdad de quién está en el centro, así que la actualización anual se diseña como una conciliación con revisión previa, no como un proceso de promoción manual.

## What Changes

- Curso escolar como año académico con fechas, con un solo curso activo y el resto como histórico de solo lectura.
- Alumno como persona que persiste entre cursos (misma ficha), con una matrícula por curso (nivel y grupo) y con estado activo o de baja reversible.
- Catálogo de niveles y grupos que se alimenta de los ficheros importados; la revisión solo muestra los valores nuevos.
- Alta y edición manual de alumnos, baja y reactivación, y búsqueda por nombre, nivel, grupo y taquilla.
- Importación CSV de alumnos como conciliación con revisión previa: los que coinciden se actualizan, los nuevos se crean, los que no constan se dan de baja y los que reaparecen se reactivan. Incluye asistente de correspondencia de columnas que recuerda la elección, reconocimiento por clave configurable (identificador, correo, nombre y apellidos), resolución de homónimos y dudosos, y salvaguarda ante bajas masivas.
- Asignación uno a uno alumno-taquilla dentro del curso activo, iniciable desde el alumno, desde la taquilla o arrastrando; con sugerencia de la taquilla libre de número más bajo de la zona, cambio de taquilla, liberación y gancho para avisos que exigen confirmación.
- Ocupación real de las taquillas (sustituye al sustituto definido en `taquilles-i-zones`), reserva con o sin alumno y decisiones de reasignar y liberar al averiarse una taquilla ocupada.
- Baja de un alumno: libera su taquilla y queda en el historial.

## Capabilities

### New Capabilities
- `curs-escolar`: años académicos, curso activo e histórico.
- `alumnes`: ficha persistente, matrícula por curso, catálogo de niveles y grupos, alta manual, baja, reactivación y búsqueda.
- `importacio-alumnes`: conciliación de alumnos desde un fichero CSV con correspondencia de columnas, reconocimiento, revisión, salvaguarda y confirmación.
- `assignacions`: asignación alumno-taquilla, cambio y liberación, reserva con alumno, decisiones de avería, ocupación y avisos que exigen confirmación.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Este cambio amplía el comportamiento de `taquilles` (aún no archivado) añadiendo requisitos propios en `assignacions`. -->

## Fuera de alcance

- Cierre de curso y apertura del curso siguiente (`cursos-i-historial`): aquí solo se crea el primer curso y se puede tener uno activo.
- Cobros, fianza y comprobación de deuda de cursos anteriores (`pagaments`): aquí solo el gancho para avisos que exigen confirmación.
- Estado de la llave (`claus`) y tratamiento de la fianza al dar de baja (`pagaments` y `claus`).
- Lista de espera: siempre hay taquillas para todos los alumnos.
- Envío de correos de cualquier tipo: el correo del alumno solo sirve para reconocerlo.
- Formato definitivo del fichero de secretaría y decisión sobre qué identificador se guarda: pendientes de recibir un fichero de muestra. La correspondencia de columnas evita depender de él, pero si la muestra trae un formato distinto del supuesto (por ejemplo, apellidos y nombre en una sola columna) habrá que añadir un requisito.
- Niveles distintos de ESO y Bachillerato (FP y otros): versiones posteriores; el catálogo de niveles ya es dinámico.
- Pantallas y arrastrar y soltar visuales (`ui-shell`, `ux-fonaments`): aquí se define el comportamiento y el caso de uso común.

## Impacto

- **Código**: entidades y casos de uso en Domain y Application; entidades EF Core y migración en Infrastructure; reutiliza el lector de CSV de `taquilles-i-zones`.
- **Datos personales (RGPD)**: primer cambio con datos de menores. Se guardan nombre, apellidos, nivel y grupo, y opcionalmente correo e identificador solo para reconocer al alumno; no se muestran en listados ni se exportan. Qué identificador se guarda queda pendiente de la muestra y de la valoración de protección de datos del centro. El registro técnico no debe contener estos datos.
- **Depende de**: `arquitectura-base` y `taquilles-i-zones`.
- **Afecta a**: `taquilles-i-zones` (ocupación real, reserva con alumno y decisiones de avería).
