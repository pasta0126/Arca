# Componentes de interfaz y cómo los usa `ui-shell`

Guía de uso de los componentes del cambio `ux-fonaments` (proyecto `Arca.UI`). Está pensada para quien construya las
pantallas: qué ofrece cada componente, cómo se conecta y qué **no** hay que reimplementar. El ejemplo completo y verificado
es la prueba `ComponentUsageExamplesTests` (`tests/Arca.UI.Tests`); si deja de funcionar, esta guía está desfasada.

## Reglas que valen para todos

- **Nada literal**: ningún componente escribe un color, una tipografía ni un texto. Todo viene de recursos de tema
  (`ArcaResourceKeys`) o de claves de recurso con `ILocalizer`. Una prueba de arquitectura (`UiComponentTests`) lo vigila.
  El espaciado también viene del tema, pero aún no se comprueba automáticamente.
- **Lógica en el modelo de vista**: cada componente tiene su comportamiento en una clase sin ventana, probada sin abrir
  ninguna, y una vista fina que solo dibuja.
- **Un solo camino para cambiar datos**: cualquier acción que escribe pasa por `RunOnceCommand` (se protege de la doble
  ejecución, muestra el indicador de trabajo tras 300 ms y convierte el resultado en notificación).
- **`Arca.UI` no conoce Infrastructure ni el modelo de Domain** (solo `Domain.Common`: `Result`, `Error`, `Notice`).

## Qué tiene que hacer la aplicación, una vez

```csharp
// App.Initialize
RequestedThemeVariant = ArcaTheme.Variant;
Styles.Add(ArcaTheme.CreateFluent());
Styles.Add(ArcaTheme.CreateStyles());            // anillo de foco: sin esto los controles no muestran el foco
Resources.MergedDictionaries.Add(ArcaTheme.CreateResources());   // o los recursos propios de ui-shell

// Composición (una instancia de cada una)
var center = new NotificationCenter(clock, delay);                // INotificationService
var registry = new ActionRegistry(localizer, UiPlatforms.Current);
var preferences = new UiPreferencesSession(new LocalUiPreferencesStore(new LocalSettingsStore(settingsFile)));
var confirmations = new DialogConfirmationService(() => mainWindow, localizer);   // IConfirmationService

// Ventana principal
WindowStateKeeper.Attach(window, preferences);   // tamaño mínimo, posición recordada y corregida
ShortcutDispatcher.Attach(window, registry);     // buscar, nuevo, confirmar, cancelar y ayuda
// y en algún lugar visible de su diseño: new NotificationHostView(center, localizer)
```

## Componentes

### Notificaciones — `NotificationCenter`, `ResultNotifier`, `NotificationHostView`, `NotificationHistoryViewModel`
- `ResultNotifier` es el **único** punto que convierte un resultado, un código de error o una excepción en notificación:
  `notifier.Notify(result, r => "text d'èxit")`, `notifier.Error(error)`, `notifier.Unexpected(exception, "NomDeLaAcció")`.
- El éxito desaparece a los 5 s (y espera con el ratón encima); avisos y errores permanecen. Máximo visible configurable,
  el resto en cola. `NotificationHostView` es la pila visible: colóquela en una esquina de la ventana principal.
- `NotificationHistoryViewModel(center, localizer).Entries` da el historial de la sesión, del más reciente al más antiguo.

### Confirmación — `IConfirmationService`
`await confirmations.ConfirmAsync(new ConfirmationRequest(título, consecuencia, "Etiqueta", destructive, detalles))`.
Devuelve `false` si se cancela o si ya hay otro diálogo abierto. Los textos ya vienen hechos de Application
(`ChargeConfirmations`, `StudentConfirmations`…); la pantalla no arma diálogos propios. El foco vuelve al control que
lo abrió.

### Comando de ejecución única — `RunOnceCommand<T>` (+ `WorkIndicatorView`)
```csharp
var create = new RunOnceCommand<int>(
    (ct, progress) => handler.HandleAsync(request, progress, ct),   // Task<Result<int>>
    n => $"{n} taquilles creades", "CreateLockerRange", center, localizer, log, delay);
button.Command = create;
panel.Children.Add(new WorkIndicatorView(create, localizer));       // indicador, "120 de 300" y cancelar con su motivo
```
Una operación solo es cancelable mientras informa `CanCancel: true` en su progreso. Si escribe datos, pásele como último parámetro `afterSuccess: () => globalState.RefreshAsync()`: así la cabecera, los avisos y los indicadores de la barra lateral se ponen al día tras cada cambio (no hay sondeo).

### Acciones, atajos y menús — `ActionRegistry`, `AppAction`, `ActionControls`
- El registro tiene las cinco acciones estándar (`StandardActions`): la pantalla les **engancha** su comportamiento y las
  suelta al salir: `using var search = registry[StandardActions.Search].Attach(() => searchBox.Focus());`.
  Una acción sin comportamiento en la pantalla actual está deshabilitada y no hace nada, sin error.
- Las acciones propias de una pantalla son `new AppAction("Assign", "Assigna")` (sin atajo: el conjunto de atajos es fijo).
  `Attach(handler, () => Availability.Unavailable("motivo"))` deshabilita con explicación.
- Botón, menú y menú contextual salen de la **misma** lista: `ActionControls.Button(action)`, `MenuItem(action)` y
  `AttachContextMenu(listBox, () => acciones)`. El atajo aparece en el tooltip y en el menú; el motivo, en la acción
  deshabilitada. Ojo: una lista se maneja con el teclado desde uno de sus elementos.

### Listas — `ListViewModel<TRow, TKey>`, `VirtualizedListView`, `ListStateViewModel`, `ListStateView`
```csharp
var list = new ListViewModel<Student, Guid>([new ListColumn<Student>("name", "Nom", s => s.Name)], s => s.Id, localizer);
state.BeginLoading();  list.SetItems(rows);  state.ShowContent();   // o state.ShowEmpty(...) / state.ShowNoResults(...)
```
Orden por columna, filtro por palabras (`FilterText`) más una condición (`SetPredicate`) y selección por **identidad**
(sobrevive a ordenar, filtrar y recargar). `ListStateView` dibuja la carga y el vacío con su acción; los mensajes de vacío
los aporta cada pantalla (por ejemplo `ChargeEmptyStates`).

### Disposición y preferencias — `AdaptivePanels`, `CollapsibleSectionViewModel/View`, `UiPreferencesSession`, `WindowLimits`
`new AdaptivePanels(lista, detalle)` apila el detalle bajo la lista por debajo de 900 px. `CollapsibleSectionViewModel("clave",
"Títol", () => "resum", preferences)` recuerda su estado y se abre sola con `SetError(true)`. Las constantes de tamaño están
en `WindowLimits` (se ajustan al probar en los equipos reales).

### Arrastrar y soltar — `AssignLockerInteraction`, `AssignmentDropViewModel`, `StudentDragSource`, `LockerDropTarget`
`AssignLockerInteraction.AssignAsync(new AssignmentIntent(studentId, lockerId))` es el camino de asignación **de todo**:
arrastre, menú y teclado (mismo caso de uso, misma confirmación ante avisos, una ejecución a la vez). Para arrastrar:
`StudentDragSource.Attach(fila, () => studentId, dropModel)` y `LockerDropTarget.Attach(celda, () => lockerId, dropModel)`.

## La pantalla de inicio y cómo sustituirla

La sección Inicio muestra lo que dé un `IHomeScreen` (un solo método, `Create`). La propuesta es `LockerMapHomeScreen`: el mapa de taquillas por zona con, al lado, el detalle de la taquilla elegida y el panel de alumnos sin taquilla. Está pendiente de validar con los conserjes (decisión D7 de `docs/riesgos.md`), y cambiarla no exige tocar nada más:

```csharp
var registry = SectionRegistry.Compose(roots, attention, home: new MiOtroInicio());   // implementa IHomeScreen
```

La navegación, la cabecera, los avisos, la búsqueda y las demás secciones no cambian. Lo que la pantalla lee y hace lo recibe como delegados (`LockerHomeServices`), que arma la composición de la aplicación (`LockerHomeComposition`): la interfaz nunca llama a un caso de uso directamente.

## Puntos de enganche que `ui-shell` tiene que resolver

| Qué | Dónde | Notas |
|-----|-------|-------|
| Tema | `ArcaResourceKeys` | `ui-shell` define los valores (claro, oscuro, identidad del centro); los componentes no cambian. Un juego por defecto sale de `ArcaTheme.CreateResources()`. |
| Navegación | `ActionRegistry` | Cada pantalla engancha y suelta sus acciones estándar al entrar y salir. |
| Dónde se ofrece cada componente | ventana principal | Pila de notificaciones en una esquina, indicador de trabajo junto a la acción que lo origina, `ListStateView` sobre cada lista. |
| Selector de taquilla libre | pantallas de dominio | Falta para las alternativas de menú y teclado de la asignación; producirá el mismo `AssignmentIntent`. |
| Búsqueda | acción `Search` | La pantalla enlaza `Search` con su cuadro de búsqueda. |
| Prueba manual del arrastre | pantallas con alumnos y taquillas | Pendiente hasta que existan (Windows, Linux y macOS). |

## Diferido (hito 2)

Vista compacta de listas (la preferencia `CompactLists` ya se guarda), pruebas de DPI, selección múltiple completa
(seleccionar todo, quitar, invertir), vista de plan de operaciones masivas y componente de pasos.
