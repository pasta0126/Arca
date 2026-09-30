## Context

Ver `proposal.md` (Why). Estado actual relevante:

- La ficha del alumno (`StudentDetailPanel`) usa pestañas y reutiliza `StudentChargesPanel`, que también cuelga de la sección Cobros (`PaymentsSection`, `DebtorsViewModel`). Los cargos, sus acciones y su historial ya funcionan; lo que cambia es dónde se muestran.
- `StudentFilter` no tiene criterio de pagos pendientes; la marca de deuda del detalle sale de `HasDebt`/`PendingTotal` y la del mapa de una consulta agregada.
- El mapa vive en Inicio (`LockerMapHomeScreen`, registrado como `IHomeScreen`) con su propio detalle (`LockerDetailPanel`) y panel de alumnos sin taquilla; la sección Taquillas tiene lista con detalle y Zonas con pestañas.
- `ScreenView` pone `list`/`detail` dentro de un `Grid` y no aporta scroll; `SettingsRoot` mete un `StackPanel` sin `ScrollViewer`.
- Las ventanas modales ya cierran con `Esc`; la búsqueda global también. La lista y el mapa no.

## Goals / Non-Goals

**Goals:**
- Un único patrón de lista con recuento, reinicio, etiquetas de filtro y `Esc`, que reutilicen Alumnos, Taquillas (mapa y lista) y el panel de alumnos sin taquilla.
- Una sola vista de detalle de taquilla y una sola de alumno, sin copias.
- No tocar dominio ni base de datos.

**Non-Goals:**
- Filtros guardados y tarjetas (`filtres-i-targetes`). Se deja preparado que el filtro de una pantalla sea un valor que se pueda construir desde fuera, pero no se guarda nada.
- Importes globales o informes de deuda.

## Decisions

**D1. Estado de lista común (`ListViewModel` existente) con reinicio y etiquetas.** Se amplía el modelo de lista ya compartido con: recuento, comando `Reset`, y una colección de filtros activos (clave, texto visible, quitar). Cada pantalla registra sus filtros en él. *Alternativa*: un botón de limpiar por pantalla. Se descarta porque el reinicio tiene que hacer lo mismo en todas y es lo que pedirá `filtres-i-targetes` para precargar filtros.

**D2. `Esc` se resuelve en el marco, con orden de prioridad.** Un manejador en el contenedor de pantalla actúa solo si nadie lo ha marcado como gestionado: 1) ventanas modales (ya lo hacen), 2) cuadro de búsqueda con texto (lo vacía), 3) selección de la lista o del mapa (la quita y suelta el foco). Se usa enrutado de tunelado para que los controles con foco no se lo coman. *Alternativa*: atajo global en `ActionRegistry`; se descarta porque `Cancel` ya está ligado a los diálogos y chocaría.

**D3. Scroll en el marco, no pantalla a pantalla.** `ScreenView` envuelve el contenido sin detalle en un `ScrollViewer` vertical. Las pantallas con lista virtualizada no se envuelven (la lista ya tiene su scroll y un `ScrollViewer` exterior anularía la virtualización); para ellas el detalle lleva el suyo. Se corrige con una prueba que comprueba que `SettingsRoot` desborda y desplaza. *Alternativa*: añadir `ScrollViewer` solo en Ajustes; se descarta por repetir el fallo en Curso e Informes.

**D4. Ficha del alumno en columna con bloques colapsables reutilizando `CollapsibleSectionView`.** Orden: cabecera, acciones, pendientes (abiertos), y tres bloques cerrados (Historial de pagos, Datos, Historial de actividad). El estado abierto/cerrado se recuerda como preferencia local de interfaz, por bloque y no por alumno. `StudentChargesPanel` se parte en dos: lista de pendientes con acciones en la fila y lista de historial de pagos (la misma fila, sin la sección de resumen, que pasa a la cabecera). Los bloques cargan al abrirse, como hoy las pestañas.

**D5. Filtro de pendientes sobre las filas que ya traen la deuda.** `ListStudentRows` ya añade a cada fila la marca de pagos pendientes con una sola consulta por lotes, y la lista de Alumnos filtra en memoria esas filas (como el resto de filtros). El filtro «Pagaments» (cualquiera, amb pendents, al corrent) se resuelve ahí, sin tocar `StudentFilter` ni consultar alumno a alumno. El estado global gana el recuento de alumnos con pendientes (`StudentsWithPending`, alumnos distintos con algún cargo pendiente de cualquier curso) para el indicador de la barra lateral. Ni las filas ni el recuento muestran importes globales; el importe de un alumno solo está en su ficha. *Alternativa descartada*: resolver el filtro en `SearchStudents`, que no conoce los cargos y obligaría a darle un repositorio más solo para esto. Limitación conocida: un alumno sin matrícula en el curso activo no está en la lista, aunque tenga deuda de un curso anterior; el recuento del indicador cuenta a todos los que deben.

**D6. El mapa como vista de Taquillas, sobre el mismo modelo que la lista.** La sección Taquillas pasa a tres pestañas. Mapa y Lista comparten un único modelo de filtros, búsqueda y selección, y el detalle lo pinta el mismo `LockerDetailPanel` de Taquillas (se elimina el del mapa). El panel de alumnos sin taquilla acompaña al mapa. `IHomeScreen` se mantiene y Inicio pasa a un resumen mínimo con recuentos que abren Taquillas o Alumnos con el filtro adecuado; eso ya ejercita el camino «abrir pantalla con filtro precargado» que reutilizará `filtres-i-targetes`.

**D7. Espacio entre zonas del mapa** con un margen de tema (clave de espaciado existente, nivel grande) entre secciones de zona; no es un literal.

**D8. Se retira la sección Cobros.** Se elimina la entrada de la barra y su pantalla (interfaz solamente: la consulta de morosos de `cobraments` sigue en Application porque la especificación de dominio la exige y la usarán los informes y el cierre de curso); el indicador de pendientes pasa a Alumnos (D5). La búsqueda global que lleve a un alumno abre su ficha; no existe ninguna ruta que abra Cobros. La spec `pantalles-cobraments` conserva las reglas de operaciones; `Fianza` se ve como un cargo más en los pendientes y el historial de pagos.

**UX transversal:** todas las operaciones conservan su feedback, confirmación y doble ejecución previos; el reinicio no pide confirmación porque no destruye datos; los bloques colapsables tienen estado de carga y vacío con guía; `Esc` y el reinicio son accesibles por teclado (el reinicio tiene atajo visible en su descripción emergente).

## Risks / Trade-offs

- [El `ScrollViewer` exterior rompe la virtualización o el arrastre] → Solo se aplica a pantallas sin lista virtualizada (D3); prueba de 300 taquillas y de 900 alumnos tras el cambio.
- [`Esc` quita la selección por error mientras se edita] → Prioridad de D2 y pruebas de formulario, búsqueda y diálogo.
- [Mover el mapa invalida las pruebas de Inicio y la spec «provisional»] → Se reescriben en el mismo grupo de tareas que el movimiento y `openspec validate --all --strict` al final.
- [Quitar Cobros oculta una función que algún conserje usaba] → El filtro de pendientes y el indicador de la barra la sustituyen; se anota en `docs/riesgos.md` para validarlo con conserjes.
- [Dos cambios seguidos tocan Inicio] → Inicio mínimo es deliberadamente barato y se sustituye en `filtres-i-targetes`.
