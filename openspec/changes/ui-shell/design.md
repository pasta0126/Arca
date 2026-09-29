## Context

Decimocuarto y último cambio de la fase de especificación. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Compone lo ya definido: casos de uso y consultas de los cambios de dominio, y los componentes, atajos, adaptabilidad y arrastrar y soltar de `ux-fonaments`. La pantalla principal está pendiente de validar con los conserjes (decisión abierta en `openspec/config.yaml`); el usuario ha elegido proponer provisionalmente el mapa de taquillas por zona.

Restricciones: Avalonia con MVVM, ratón y teclado, catalán, tres sistemas operativos, datos de menores visibles solo en pantalla, un solo PC.

## Goals / Non-Goals

**Goals:**
- Un marco de navegación estable que no cambie cuando cambie la pantalla de inicio.
- Búsqueda global rápida, sin datos de identificación de más.
- Identidad y tema con contraste garantizado por construcción.

**Non-Goals:**
- Diseño detallado de cada pantalla de dominio y validación de la pantalla principal.

## Decisions

### D1. Marco con secciones registradas
Cada sección se declara como una definición (identificador, clave de título, icono, orden, pantalla raíz y proveedor de indicador de atención) en un registro. La barra lateral, la cabecera y los atajos de navegación se construyen del registro. Añadir o mover una pantalla es editar una definición. El reparto de secciones está en el spec; una prueba comprueba que ninguna pantalla se registra en dos secciones. *Alternativa descartada*: navegación por pestañas codificada en la vista; obligaría a tocar la ventana principal al añadir una sección.

### D2. Inicio sustituible
`IHomeScreen` es la interfaz de la pantalla de inicio y se registra como la raíz de la sección Inicio. El mapa de taquillas es la implementación por defecto. Cambiarla es registrar otra sin tocar navegación, cabecera ni búsqueda. Como las alternativas descartadas (buscador con resumen, solo buscador) ya son casos de uso de este mismo marco, la validación con los conserjes puede cambiar de propuesta con poco coste.

### D3. Indicadores y avisos del estado global
Un servicio de estado global compone en una sola consulta el curso activo o en cierre, la versión nueva disponible, el estado del asistente de configuración y los recuentos de atención (cargos pendientes, llaves sin resolver, incidencias abiertas y pasos de cierre pendientes), y publica cambios cuando una operación los altera. Cabecera, avisos e indicadores de sección lo consumen. Reutiliza las consultas de los cambios de dominio y no calcula reglas propias. Los recuentos se refrescan tras cada operación de escritura y al arrancar, no por sondeo.

### D4. Búsqueda global
Caso de uso de Application que recibe un texto y devuelve grupos de resultados (alumnos, taquillas y grupos) limitados por tipo, con el total. Usa la comparación de texto centralizada de `arquitectura-base` (sin mayúsculas ni acentos) y las consultas por lotes de alumnos, taquillas y pagos, con objetos de transferencia que no llevan correo ni identificador. En la UI, una espera corta tras la última pulsación y cancelación de la búsqueda anterior. El ámbito por defecto son los alumnos activos del curso activo (o del curso en cierre si no hay activo) y las taquillas activas.

### D5. Mapa por consulta agregada
Una consulta devuelve, para todas las taquillas activas, zona, número, estado visible derivado, alumno asignado, estado de la llave y marca de deuda, en lotes y sin carga perezosa. La actualización tras un cambio usa el resultado estructurado de la operación para refrescar solo esa taquilla y los contadores. Las taquillas se pintan con un control virtualizado por zona. El estado se representa por color semántico e icono, nunca solo por color.

### D6. Panel de detalle y de alumnos sin taquilla
El detalle reutiliza los casos de uso de asignación, liberación, reserva e incidencias por los mismos comandos que las demás pantallas y las acciones del registro de `ux-fonaments`, con no disponibles deshabilitadas y su motivo. El panel de alumnos sin taquilla es la lista de `ux-fonaments` y la fuente del arrastre, con alternativa de menú y teclado.

### D7. Identidad guardada en la base de datos
Nombre, logo (bytes, tipo y tamaño) y color de acento son una fila única de configuración del centro en la base de datos, no en los ajustes locales, para que viajen con las copias. El logo se valida por firma de fichero (PNG o JPEG), se limita a 1 MB y se decodifica al cargarlo para rechazar imágenes dañadas. El arranque muestra la identidad de ARCA porque el splash ocurre antes de abrir la base de datos.

### D8. Contraste por construcción
El acento se convierte en un conjunto de recursos para cada tema: el tono se ajusta hasta cumplir el contraste con el texto que se muestra encima y con la superficie, con un umbral definido como constante (4,5:1 para texto). Una función pura lo calcula y se prueba con colores extremos. El tema (claro, oscuro, del sistema) es una preferencia local del equipo, porque depende del PC y no del centro, y por eso no viaja con la copia.

### D9. Tema como recursos con nombre
Este cambio define los valores finales (por defecto, un tema claro de colores neutros y pastel, decidido por la persona responsable el 2026-09-24) de los recursos que `ux-fonaments` declaró como contrato: superficie, texto, éxito, aviso, error, foco, acento, tipografías y espaciados, para claro y oscuro. Los componentes no cambian.

### D10. Feedback
Resultados estructurados y notificaciones de `ux-fonaments`, estados vacíos con guía en cada sección, vista previa de la identidad antes de aplicar y protección contra doble ejecución en guardados.

## Risks / Trade-offs

- **La pantalla principal puede no gustar a los conserjes** → es una pieza registrada e intercambiable; la búsqueda y la navegación no dependen de ella.
- **El mapa con 300 o más taquillas puede ser lento o denso** → consulta agregada única, control virtualizado por zona, zonas colapsables y vista compacta.
- **La búsqueda muestra nombres y estado de pago de menores en pantalla** → es inherente al trabajo del conserje; no se exporta ni se registra y los resultados no llevan correo ni identificador.
- **Un logo grande o malformado podría estropear la cabecera o la base** → validación de formato, tamaño y decodificación, y límite de 1 MB.
- **Un acento arbitrario puede dar contraste ilegible** → ajuste automático con prueba de casos extremos.
- **El estado global depende de muchas consultas** → un servicio único con actualización por eventos; se prueba con datos de ejemplo de 300 taquillas.
- **Reparto de pantallas discutible (por ejemplo, importes en Curso)** → es una definición del registro; moverla es editar una línea.

## Migration Plan

Una migración de EF Core añade la fila de configuración del centro (nombre, logo y acento), sin datos previos. La preferencia de tema es un ajuste local opcional. Las instalaciones existentes muestran el nombre de la aplicación hasta que se defina el del centro.

## Open Questions

- Validación de la pantalla principal con los conserjes: el mapa es provisional y no altera los specs de navegación, búsqueda ni identidad; solo cambiaría la implementación registrada de Inicio.
- Valores exactos de los recursos de tema para el modo oscuro: detalle de diseño visual sin efecto en los specs. El tema claro pastel por defecto ya está definido en `Arca.UI/Theme/ArcaPalette.cs`.

## Cambios durante la implementación

**2026-09-28 (grupo 1):** D1 dice que los atajos de navegación se construyen del registro de secciones. No se han añadido atajos de teclado para cambiar de sección: `ux-fonaments` fijó un conjunto cerrado de cinco atajos («fijos, no personalizables») y uno nuevo tendría que decidirse allí; la barra se maneja con Tab y flechas, y Intro abre la sección. Las secciones sin pantalla propia (todas menos Ajustes, que ya muestra la versión y la seguridad de los datos) muestran un marcador con la lista de lo que contendrán, en vez de un área en blanco, hasta que `pantalles-de-domini` y los grupos siguientes las sustituyan dándoles una raíz (`SectionDefinition.CreateRoot`). El registro es el único sitio que sabe qué pantalla va en qué sección y se niega a registrar una pantalla en dos o en una sección desconocida; el reparto está en `ShellCatalog`. El estado de la barra (plegada o no) se guarda en las preferencias locales de interfaz (`UiPreferences.SidebarCollapsed`). La cabecera queda como un hueco (`ShellView.HeaderSlot`) que llena el grupo 2.

**2026-09-28 (grupo 2):** el servicio de estado global (D3) se ha construido con lo que ya existe: `GetGlobalStateHandler` (Application) compone el curso activo y el recuento de cargos pendientes a partir de los repositorios de años y de cargos, sin reglas propias. `GlobalStateService` (UI) lo carga al abrir la ventana y de nuevo tras cada escritura, y solo avisa de un cambio si algo es distinto; si la carga falla conserva el estado anterior y avisa una vez. El refresco tras escribir se engancha con un parámetro opcional del comando de ejecución única (`afterSuccess`), que solo se llama tras un resultado correcto: cada pantalla que escribe debe pasarlo (`() => state.RefreshAsync()`), porque no hay sondeo. Con el alcance del hito 1 el único aviso global es «sin curso activo», que bloquea acciones y por eso no se puede cerrar; su acción lleva a la sección Curso, ya que la configuración guiada queda para el hito 2. Falta todavía la identidad del centro (grupo 4): la cabecera muestra el nombre de la aplicación y deja un hueco para el logo. Por primera vez la interfaz lee la base de datos abierta: `AppStartup` crea un `EfInventory` sobre `StorageSession.CreateContext`.

**2026-09-28 (grupo 3):** la búsqueda (D4) es `GlobalSearchHandler` en Application: alumnos por nombre (todas las palabras, sin mayúsculas ni acentos), taquillas por número exacto (y los alumnos que la tienen) y grupos cuando cada palabra empieza una palabra del nivel o del grupo, para que una «a» suelta no encuentre todo. Devuelve por tipo un máximo (8 por defecto) con el total, y cuántas bajas coinciden cuando no se incluyen, para ofrecer incluirlas. Los resultados no llevan correo ni identificador del alumno y muestran el estado de la taquilla con un tipo propio de Application (`LockerStatusView`), para que la interfaz no dependa del modelo de Domain. Por ahora el ámbito es el curso activo; buscar en el curso en cierre cuando no hay activo queda pendiente hasta que exista ese estado. En la interfaz, `GlobalSearchViewModel` espera 250 ms tras la última tecla, cancela la búsqueda anterior y solo muestra la última; las flechas saltan los encabezados, Intro abre, Escape cierra y borra. Abrir un resultado lo hace `SearchNavigator`: abre la sección (Alumnos, o Inicio para una taquilla) y deja la petición pendiente y la taquilla resaltada; como las pantallas se construyen la primera vez que se abre su sección, la petición se recoge con `TakePending`. El atajo de buscar (Ctrl/Cmd+F) enfoca el cuadro de la cabecera desde cualquier pantalla.

**2026-09-28 (grupo 5, primera parte):** `IHomeScreen` es una interfaz de un solo método (`Create`) y `SectionRegistry.Compose` acepta la pantalla de inicio como parámetro: cambiarla es dar otra, sin tocar navegación, cabecera ni búsqueda (hay una prueba con una pantalla de inicio distinta). La consulta agregada del mapa (`GetLockerMapHandler`) trae zonas activas, taquillas activas, alumno, estado y marca de deuda en lotes; `GetMapLockerHandler` relee una sola taquilla, y `LockerMapViewModel` actualiza esa taquilla y recalcula los contadores con lo que ya tiene, sin recargar el mapa. Los contadores por estado son también los filtros (pulsar uno filtra, pulsar otra vez lo quita) y hay un filtro por zona; los contadores describen todo el mapa, no lo filtrado. Elegir una taquilla en la búsqueda la resalta y abre su detalle, quitando los filtros que la esconderían y desplegando su zona; si la búsqueda se hizo antes de que existiera la pantalla, la petición pendiente se recoge al cargar (`SearchNavigator.TakePending`). El estado de cada taquilla se ve por color, icono y palabra (tooltip); el contrato de tema gana cinco colores de estado (`StatusFree`…`StatusMaintenance`). Para no tener un ciclo entre la navegación, el registro y la pantalla de inicio, `SearchNavigator` se une a la navegación después de crearse (`Bind`). El mapa dibuja las taquillas en una rejilla que envuelve, sin virtualizar: con 600 taquillas basta; si hiciera falta, el paquete `ItemsRepeater` ya está previsto en `docs/stack.md`. Quedan para la segunda parte el detalle de la taquilla con sus acciones (5.4) y el panel de alumnos sin taquilla con el arrastre (5.5).

**2026-09-28 (grupo 5, segunda parte):** el detalle de la taquilla (`GetLockerDetailHandler`) trae estado, notas, alumno con nivel y grupo y estado de pago, sin correo ni identificador; sus acciones son `AppAction` con su motivo de no disponibilidad, y cada una que escribe se ejecuta con el comando de ejecución única. **Cambiar de taquilla** es un modo de la pantalla: la acción pide elegir la nueva taquilla en el mapa (un aviso lo dice y se puede cancelar); una taquilla no libre se ignora y una libre completa el cambio con el mismo caso de uso y las mismas confirmaciones que asignar (`AssignLockerInteraction` sirve para los dos). **Asignar** tiene tres caminos con el mismo resultado: la acción del detalle (con el alumno elegido en el panel), el menú o Intro sobre el alumno elegido (con la taquilla libre elegida en el mapa) y arrastrar el alumno sobre la taquilla; una prueba comprueba que envían la misma petición y actualizan igual. Tras cada escritura `LockerHomeModel` relee solo lo que cambió: la taquilla (y la anterior, en un cambio), la lista de alumnos, el detalle y el estado global; nunca el mapa entero. **Marcar como averiada una taquilla ocupada** exige elegir qué hacer con el alumno (mantener, reasignar o liberar) y esa decisión todavía no tiene interfaz: hoy la acción está deshabilitada con el motivo «Allibera primer la taquilla». La composición (`LockerHomeComposition`, en el proyecto de escritorio) es la única que conoce los casos de uso y entrega a la pantalla delegados que responden con la frase de lo hecho.

**2026-09-28 (grupos 6 y 7):** con el alcance del hito 1 los estados vacíos con guía son los del mapa (sin taquillas), los de cada sección sin pantalla propia (lista de lo que contendrá), el panel de alumnos (sin curso activo, todos con taquilla, sin coincidencias) y la búsqueda (sin resultados, con la oferta de incluir bajas). La protección contra la doble ejecución de los guardados de identidad (6.3) queda con la identidad, que se difiere entera al hito 2. La prueba de arquitectura de los modelos de vista (7.1) es la ya existente de la biblioteca de componentes (`Arca.UI` no usa Domain salvo `Domain.Common` ni Infrastructure). La prueba de extremo a extremo compone la pantalla de inicio con `LockerHomeComposition` sobre una base cifrada real, lo que obliga a que `Arca.Desktop` exponga sus internos al proyecto de pruebas de interfaz (`InternalsVisibleTo`); el resto de sus internos no se usa. La pantalla de inicio se registra como decisión abierta D7 de `docs/riesgos.md` y `docs/componentes-ui.md` explica cómo sustituirla.
