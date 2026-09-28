## Context

Decimotercer cambio. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Los principios de UX ya están fijados en `openspec/config.yaml` ("UX transversal") y como requisitos de comportamiento en `arquitectura-base` (`feedback-operacions`): resultado visible, notificaciones no bloqueantes, indicador de trabajo, progreso con recuentos, confirmación con consecuencia, estados vacíos y carga bajo demanda. Los cambios de dominio ya entregan resultados estructurados y códigos de error estables, y definen operaciones masivas en dos fases, asistentes de pasos con estado derivado y la asignación por arrastre. Falta el conjunto de componentes que los materializa.

Restricciones: Avalonia UI con MVVM, ratón y teclado, tres sistemas operativos, catalán en v1 con recursos por clave, sin dependencias de UI en Domain ni Application, y accesibilidad limitada a la de Avalonia más teclado y foco visible.

## Goals / Non-Goals

**Goals:**
- Componentes reutilizables cuyo comportamiento se pueda probar sin abrir una ventana real.
- Que ninguna pantalla implemente por su cuenta notificaciones, confirmaciones, doble ejecución o progreso.
- Un contrato de tema sencillo para que `ui-shell` lo defina sin tocar los componentes.

**Non-Goals:**
- Tema, identidad, navegación, pantallas, accesibilidad avanzada, arrastrar en otros casos y atajos configurables.

## Decisions

### D1. Biblioteca de componentes separada de las pantallas
Un proyecto de UI propio con controles, modelos de vista y servicios (`INotificationService`, `IConfirmationService`), que depende de Application solo por sus interfaces y resultados y no conoce Domain ni Infrastructure. Las pantallas de `ui-shell` lo consumen. Una prueba de arquitectura lo comprueba. *Alternativa descartada*: componentes dentro del proyecto de pantallas; se mezclarían con la lógica de cada vista y la reutilización sería por copia.

### D2. Modelo de vista primero, probado sin ventana
Cada componente tiene su comportamiento en un modelo de vista puro (estado, comandos, temporizadores por reloj inyectable) y una vista fina. Los specs se prueban sobre el modelo; una batería pequeña con el renderizado sin ventana de Avalonia comprueba los enlaces y el foco. Los temporizadores (300 ms del indicador, desaparición de notificaciones) usan un planificador inyectable para pruebas deterministas.

### D3. Notificaciones y errores desde el resultado estructurado
Un único punto convierte el resultado estructurado o el código de error en una notificación: gravedad (éxito, aviso, error), clave de recurso y parámetros. El éxito desaparece a los 5 segundos y se pausa mientras el ratón está encima; avisos y errores permanecen. Máximo visible configurable con el resto en cola. Los errores inesperados muestran texto genérico y la referencia del registro técnico. Nada se traduce en Application: solo aquí se compone el texto, con `ILocalizer`.

### D4. Confirmación con consecuencia
`IConfirmationService` recibe título, consecuencia, recuentos opcionales, etiqueta de la acción y un indicador de destructiva, y devuelve confirmado o cancelado. El foco inicial va a cancelar si es destructiva, Escape cancela y solo hay un diálogo por acción. Los casos de uso que exigen confirmación explícita (avisos de deuda, cierres, borrados) devuelven el texto y los recuentos y el componente los presenta, sin que cada pantalla arme su diálogo.

### D5. Comando de ejecución única
Un comando asíncrono que se deshabilita mientras dura, ignora invocaciones repetidas y propaga el resultado al servicio de notificaciones. Todas las acciones que modifican datos lo usan y una prueba de arquitectura busca comandos de escritura que no lo usen. Es el punto donde también se aplica el indicador de trabajo tras 300 ms.

### D6. Progreso y cancelación
Las operaciones largas de Application informan de progreso mediante `IProgress` con recuento y bandera de cancelable, y aceptan un token de cancelación. El componente muestra "N de M", y deshabilita cancelar cuando la operación indica que ya no es seguro, con el motivo. Es coherente con el patrón de operaciones masivas de dos fases: se puede cancelar hasta antes del guardado.

### D7. Vista de plan y lista con selección
Un componente de lista virtualizada con orden, filtro y selección persistente por identidad de fila (no por posición), de modo que ordenar o filtrar no pierde marcas. La vista de plan la envuelve: recibe el plan devuelto por el análisis (elementos, bloqueos, recuentos), permite editar la selección, y envía la selección a la confirmación, que revalida en Application. Los mismos componentes sirven a la liberación masiva de curso, la devolución masiva de llaves, la condonación y devolución de fianzas y el mantenimiento en bloque.

### D8. Componente de pasos
Un control de lista de pasos alimentado por definiciones (título, explicación, estado, obligatoriedad, dependencias) que ya expone Application; solo pinta estado, progreso "N de M" y siguiente paso, y delega abrir cada paso. Sirve al cierre de curso y a la configuración guiada sin código propio de ninguno. *Alternativa descartada*: un asistente lineal con siguiente y anterior; no permite abrir cualquier paso ni omitir.

### D9. Adaptabilidad
Tamaño mínimo de ventana como constante (propuesta 1024 × 640). Disposición con paneles que se apilan por debajo de un ancho umbral (constante) usando los mecanismos de Avalonia, sin código de cálculo por pantalla. El escalado por DPI es el de Avalonia; se prueba con 100 %, 150 % y 200 %. Las secciones colapsables muestran el resumen que les aporta la vista y se expanden solas si contienen un error de validación.

### D10. Preferencias locales de interfaz
Tamaño y posición de la ventana, secciones colapsadas y densidad se guardan en el fichero de ajustes locales (el mismo de la ruta de la base y fuera de las copias), con lectura tolerante: si falta o está dañado se usan los valores por defecto, y una posición fuera de las pantallas disponibles se corrige. No contienen datos personales.

### D11. Teclado, foco y atajos
Conjunto fijo: buscar, nuevo, confirmar, cancelar y ayuda, declarados en un único registro de comandos con su gesto por sistema (Control en Windows y Linux, Comando en macOS). Menús y descripciones emergentes leen el atajo del registro, así que nunca se desincronizan. El orden de foco sigue el orden visual y el foco visible usa un recurso de tema. Los menús contextuales se construyen de la misma lista de acciones que los botones, con las no disponibles deshabilitadas y su motivo. Las acciones son objetos con nombre, atajo, disponibilidad y motivo, compartidos por botón, menú y atajo.

### D12. Arrastrar y soltar con una sola ruta de negocio
El arrastre usa la API de Avalonia y solo produce una intención (alumno, taquilla), que se envía por el mismo comando de asignación que el menú y el teclado. La respuesta visual del destino consulta la comprobación previa de la asignación (`IAssignmentGuard`) sin escribir nada. Escape cancela y ninguna función depende del arrastre. Se limita a alumno sobre taquilla libre.

### D13. Contrato con el tema
Los componentes usan solo recursos con nombre (colores semánticos como éxito, aviso, error y foco, tipografías y espaciados) que define `ui-shell`. Este cambio incluye un conjunto mínimo por defecto para poder probar, pero no es el tema final. Todos los textos vienen de claves.

## Risks / Trade-offs

- **Avalonia cambia entre versiones y las pruebas de vista son frágiles** → el comportamiento vive en modelos de vista; la batería de renderizado es pequeña y solo cubre enlaces y foco.
- **Arrastrar y soltar se comporta distinto en Windows, Linux y macOS** → se limita a un caso con alternativas completas de teclado y menú, y se prueba en los tres sistemas.
- **Sin requisitos propios de accesibilidad, un conserje con necesidades especiales puede quedar peor atendido** → decisión de producto para v1; teclado y foco visible ya cubren lo esencial y las claves de recurso permiten añadir nombres accesibles después.
- **Los componentes genéricos pueden crecer sin control** → solo se incluyen los que ya exige algún cambio de dominio; nada especulativo.
- **Un comando de escritura sin protección podría ejecutarse dos veces** → prueba de arquitectura que exige el comando común.
- **Umbrales de disposición pueden no encajar con los PCs reales del centro** → son constantes; se ajustan al probar con los conserjes.

## Migration Plan

Sin migración de base de datos. Se crea el proyecto de componentes y las preferencias locales son un fichero opcional que se ignora si no existe.

## Open Questions

- Tamaño mínimo exacto de ventana y umbral de apilado: se ajustan al probar en los equipos reales; no alteran los specs.
- Número máximo de notificaciones visibles a la vez: detalle de implementación.

## Cambios durante la implementación

**2026-09-28 (grupo 1):** D1 dice que la biblioteca de componentes «no conoce Domain». Al implementarlo se ve que `Result`, `Error` y `Notice` viven en `Domain.Common` porque todas las capas los comparten, y que los componentes no pueden prescindir de ellos para convertir un resultado en una notificación (D3). La regla queda así: el proyecto no referencia `Arca.Domain` ni `Arca.Infrastructure` y ningún componente usa un espacio de nombres de Domain distinto de `Arca.Domain.Common`; entidades, reglas y repositorios siguen fuera de su alcance. La prueba de arquitectura `The_component_library_uses_neither_the_domain_model_nor_infrastructure` lo comprueba sobre las directivas `using` del proyecto. Además, `arquitectura-base` ya había creado el proyecto `Arca.UI` con las notificaciones, la confirmación, el comando de ejecución única y el tema; este cambio los amplía en lugar de recrearlos.

**2026-09-28 (grupo 2):** D4 dice que no se abre un segundo diálogo de confirmación «para la misma acción». Se ha implementado como: mientras hay un diálogo abierto, cualquier otra petición de confirmación no se abre y **responde que no** (cancelada), en vez de compartir la respuesta del primero, para que un doble clic o un atajo repetido nunca ejecute dos veces la acción. Además, la conversión de resultados a notificaciones (D3) se ha extraído a `ResultNotifier`, que usa el comando de ejecución única y puede usar cualquier pantalla; un texto ausente se muestra como su clave y se anota una sola vez en el registro técnico con la clave en el contexto (nunca datos).

**2026-09-28 (grupo 3):** D10 dice que las preferencias se guardan «en el mismo fichero de ajustes locales». Como `Arca.UI` no puede referenciar Infrastructure, el contrato está en Application (`IUiPreferencesStore`, `UiPreferences`, `WindowBounds`) y Infrastructure lo implementa sobre `LocalSettings` (`LocalUiPreferencesStore`), que gana un apartado `Ui` junto a `DatabasePath`. La lectura del fichero pasa a ser tolerante **por apartado**: un apartado dañado (por ejemplo `Ui`) ya no hace perder los demás (por ejemplo la ruta de la base). Guardar no puede romper la aplicación: un fallo de disco al guardar se ignora. Una sección que se abre sola por un error de validación no cambia lo que la persona dejó guardado, de modo que al corregir el error vuelve a su estado. Posición en píxeles de pantalla y tamaño en unidades independientes de densidad; la ventana se recoloca al abrirse si aún no conocía sus pantallas. Diferidos al hito 2, como fija `docs/hito-1.md`: vista compacta de listas (3.3; la preferencia `CompactLists` ya se guarda) y pruebas por DPI (3.5, parcialmente cubierto por la prueba del factor de escala en la colocación).

**2026-09-28 (grupo 4):** D11 dice que el foco visible usa un recurso de tema. Se implementa con un estilo (`ArcaTheme.CreateStyles`) que da a cada tipo de control habitual un adorno de foco de 2 px con el pincel `Arca.Brush.Focus`; tiene que ser un estilo **por tipo** (botón, casilla, cuadro de texto, lista…) porque el tema Fluent fija el suyo en cada tipo y solo uno igual de específico lo sustituye. Cada aplicación debe añadir esos estilos junto al tema (`App` ya lo hace). El menú contextual se abre con el botón derecho, con la tecla de menú y con Mayús+F10, y se reconstruye cada vez desde la lista de acciones actual; una lista se usa con el teclado desde uno de sus elementos, que es el que recibe las teclas. El atajo de confirmar es Control/Comando+Intro; el de ayuda, F1.

**2026-09-28 (grupo 5):** D12 pide que la respuesta visual del destino consulte la comprobación previa de la asignación sin escribir. No existía un caso de uso de solo lectura para ello, así que se ha añadido a `alumnes-i-assignacions` (Application) `CheckAssignmentTargetHandler`, que reutiliza `AssignmentFlow.PrepareAsync` (la misma validación y los mismos guardianes que la asignación, sin guardar nada) y responde «no válido» con su motivo en vez de fallar. En la interfaz, `AssignLockerInteraction` es el único camino de asignación para arrastrar, menú y teclado: llama al mismo caso de uso, pide la misma confirmación ante avisos (deuda anterior) y usa el comando de ejecución única, de modo que soltar dos veces, o arrastrar y pulsar a la vez, asigna una sola vez; si la persona no confirma, no se asigna nada y se le dice. Escape durante el arrastre lo gestiona el propio sistema (cancela sin escribir). Lo que falta para las alternativas de menú y teclado (5.4) es solo el selector de una taquilla libre, que es de las pantallas de dominio: el resultado que produce es el mismo `AssignmentIntent`. La prueba manual del arrastre real (5.5) queda pendiente hasta que existan pantallas con alumnos y taquillas.

**2026-09-28 (6.1):** la lista de D7 se ha construido sobre `ListBox` (que ya virtualiza sus elementos) con una cabecera propia, en vez de añadir el paquete `DataGrid`, para no sumar una dependencia. El comportamiento vive en `ListViewModel<TRow, TKey>`: filtro por palabras (sin distinguir mayúsculas ni acentos) más una condición opcional, orden por columna (texto como lo lee una persona, números como números, cada columna puede dar su clave de orden), y selección por identidad (`TKey`), de modo que ordenar, filtrar o recargar los datos no pierde marcas; las de filas ocultas se conservan y se cuentan, y al recargar solo se descartan las de filas que ya no existen. La casilla por fila y Espacio para alternar son la base de la selección múltiple; «seleccionar todo, quitar, invertir» y el resto de 6.2 siguen diferidos, como fija el hito 1.

**2026-09-28 (grupo 7):** D5 pide una prueba de arquitectura de que los comandos que modifican datos usan el comando de ejecución única. Todavía no hay pantallas, así que la prueba comprueba dos cosas que sí se pueden vigilar ya: que los únicos comandos de la biblioteca son `RunOnceCommand` y `AppAction`, y que ningún fichero de `Arca.UI` llama a un caso de uso (`.HandleAsync(`) sin pasar por un `RunOnceCommand`. Cuando existan las pantallas habrá que revisar si esa comprobación textual sigue siendo suficiente. La prueba de «sin colores, tipografías ni textos literales» obligó a migrar los componentes de `arquitectura-base` (acceso, seguridad, información, confirmación y pantalla de arranque): el contrato de tema gana el color `Arca.Brush.ErrorText` y la tipografía `Arca.FontFamily.Monospace`, y `ThemedText` da los textos habituales (título, encabezado, pie, error, clave, nota de aviso) ya ligados al tema. Quedan en esas vistas espaciados y márgenes literales: el contrato los contempla, pero la prueba aún no los exige. Puntos de enganche con `ui-shell` en `docs/componentes-ui.md`.
