## Why

Con el inventario de taquillas ya especificado, falta lo que le da sentido: quién usa cada taquilla. Hay que registrar a los alumnos y su curso escolar, mantenerlos al día cada año sin rehacer el trabajo a mano y asignarles una taquilla con rapidez y sin errores. El fichero de secretaría es la fuente de verdad de quién está en el centro, así que la actualización anual se diseña como una conciliación con revisión previa, no como un proceso de promoción manual.

## What Changes

- Curso escolar como año académico con fechas, con un solo curso activo y el resto como histórico de solo lectura (salvo las operaciones de cierre que define `cursos-i-historial`).
- Alumno como persona que persiste entre cursos (misma ficha), con una matrícula por curso (nivel y grupo) y con estado activo o de baja reversible.
- Catálogo de niveles y grupos que se alimenta de los ficheros importados; la revisión solo muestra los valores nuevos.
- Alta y edición manual de alumnos, baja y reactivación, y búsqueda por nombre, nivel, grupo y taquilla.
- Importación de alumnos desde un fichero ODS (una hoja por grupo; columnas `Nom complet` y `Correu`) como conciliación con revisión previa: los que coinciden se actualizan, los nuevos se crean, los que no constan se dan de baja y los que reaparecen se reactivan. El correo es el identificador único del alumno y el nivel y grupo salen del nombre de la hoja. Incluye validación por fila y salvaguarda ante bajas masivas.
- Asignación uno a uno alumno-taquilla dentro del curso activo, iniciable desde el alumno, desde la taquilla o arrastrando; con sugerencia de la taquilla libre de número más bajo de la zona, cambio de taquilla, liberación y gancho para avisos que exigen confirmación.
- Ocupación real de las taquillas (sustituye al sustituto definido en `taquilles-i-zones`), reserva con o sin alumno y decisiones de reasignar y liberar al averiarse una taquilla ocupada.
- Baja de un alumno: libera su taquilla y queda en el historial.

## Capabilities

### New Capabilities
- `curs-escolar`: años académicos, curso activo e histórico.
- `alumnes`: ficha persistente, matrícula por curso, catálogo de niveles y grupos, alta manual, baja, reactivación y búsqueda.
- `importacio-alumnes`: conciliación de alumnos desde un fichero ODS con formato fijo, reconocimiento por correo, revisión, salvaguarda y confirmación.
- `assignacions`: asignación alumno-taquilla, cambio y liberación, reserva con alumno, decisiones de avería, ocupación y avisos que exigen confirmación.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Este cambio amplía el comportamiento de `taquilles` (aún no archivado) añadiendo requisitos propios en `assignacions`. -->

## Fuera de alcance

- Cierre de curso y apertura del curso siguiente (`cursos-i-historial`): aquí solo se crea el primer curso y se puede tener uno activo.
- Cobros, fianza y comprobación de deuda de cursos anteriores (`pagaments`): aquí solo el gancho para avisos que exigen confirmación.
- Estado de la llave (`claus`) y tratamiento de la fianza al dar de baja (`pagaments` y `claus`).
- Lista de espera: siempre hay taquillas para todos los alumnos.
- Envío de correos de cualquier tipo: el correo del alumno solo sirve para reconocerlo.
- Importar taquillas y zonas por fichero: se crean una vez y luego se modifican a mano (`taquilles-i-zones`).
- Otros formatos de importación (CSV, Excel) y correspondencia flexible de columnas: el formato ODS es fijo.
- Niveles distintos de ESO y Bachillerato (FP y otros): versiones posteriores; el catálogo de niveles ya es dinámico.
- Pantallas y arrastrar y soltar visuales (`ui-shell`, `ux-fonaments`): aquí se define el comportamiento y el caso de uso común.

## Impacto

- **Código**: entidades y casos de uso en Domain y Application; entidades EF Core y migración en Infrastructure; lector de ODS propio en Infrastructure, sin dependencias nuevas.
- **Datos personales (RGPD)**: primer cambio con datos de menores. Se guardan nombre, apellidos, correo (identificador único), nivel y grupo; el correo no se muestra en listados ni se exporta. El registro técnico no debe contener estos datos. El fichero de ejemplo del repositorio es anonimizado (`@test.cat`); los datos reales nunca se versionan.
- **Depende de**: `arquitectura-base` y `taquilles-i-zones`.
- **Afecta a**: `taquilles-i-zones` (ocupación real, reserva con alumno y decisiones de avería).
