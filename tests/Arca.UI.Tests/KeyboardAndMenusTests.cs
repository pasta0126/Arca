// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Confirmation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

public sealed class KeyboardAndMenusTests
{
    const string Spec = "ux-fonaments/teclat-i-menus";

    readonly ResxLocalizer _localizer = new();

    ActionRegistry Registry(UiPlatform platform) => new(_localizer, platform);

    static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None) =>
        window.KeyPress(key, modifiers, key == Key.Apps ? PhysicalKey.ContextMenu : Enum.Parse<PhysicalKey>(key.ToString()), null);

    /// <summary>A list is used by keyboard from one of its elements, which is what has the focus and receives the keys.</summary>
    static void FocusFirstItem(ListBox list, Window window)
    {
        Dispatcher.UIThread.RunJobs();
        list.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        Assert.True(list.ContainerFromIndex(0)!.Focus());
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(window.FocusManager!.GetFocusedElement());
    }

    // --- The fixed set of shortcuts ---

    [Theory]
    [InlineData(UiPlatform.Windows, KeyModifiers.Control, "Ctrl+F")]
    [InlineData(UiPlatform.Linux, KeyModifiers.Control, "Ctrl+F")]
    [InlineData(UiPlatform.MacOS, KeyModifiers.Meta, "⌘F")]
    [Trait("spec", Spec + ": Conjunto fijo de atajos (Buscar)")]
    public void Search_uses_Control_on_Windows_and_Linux_and_Command_on_macOS(UiPlatform platform, KeyModifiers modifier, string text)
    {
        var search = Registry(platform)[StandardActions.Search];

        Assert.Equal(new Shortcut(Key.F, modifier), search.Shortcut);
        Assert.Equal(text, search.ShortcutText);
        Assert.Equal($"Cerca ({text})", search.ToolTipText.Replace("Cerca (" + text + ")", "Cerca (" + text + ")"));
    }

    [Fact]
    [Trait("spec", Spec + ": Conjunto fijo de atajos (Cancelar)")]
    public void The_fixed_set_is_search_new_confirm_cancel_and_help_with_distinct_shortcuts()
    {
        foreach (var platform in Enum.GetValues<UiPlatform>())
        {
            var registry = Registry(platform);

            Assert.Equal(StandardActions.All.Order(), registry.All.Select(a => a.Id).Order());
            Assert.Equal(5, registry.All.Select(a => a.Shortcut).Distinct().Count());
            Assert.Equal(new Shortcut(Key.Escape, KeyModifiers.None), registry[StandardActions.Cancel].Shortcut);
            Assert.Equal("Esc", registry[StandardActions.Cancel].ShortcutText);
            Assert.Equal("F1", registry[StandardActions.Help].ShortcutText);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Acciones y estados coherentes")]
    public void An_action_keeps_the_same_name_and_shortcut_wherever_it_is_offered()
    {
        var registry = Registry(UiPlatform.Windows);
        var action = registry[StandardActions.New];

        var button = ActionControls.Button(action);
        var item = ActionControls.MenuItem(action);

        Assert.Equal(action.Label, button.Content);
        Assert.Equal(action.Label, item.Header);
        Assert.Same(action, button.Command);
        Assert.Same(action, item.Command);
        Assert.Equal(action.Shortcut!.ToGesture(), item.InputGesture);
    }

    // --- Actions and availability ---

    [Fact]
    [Trait("spec", Spec + ": Conjunto fijo de atajos (Atajo no aplicable)")]
    public void An_action_the_screen_does_not_offer_is_unavailable_and_does_nothing()
    {
        var search = Registry(UiPlatform.Windows)[StandardActions.Search];

        Assert.False(search.IsAvailable);
        Assert.False(search.CanExecute(null));
        search.Execute(null); // no handler: nothing happens and nothing throws
    }

    [Fact]
    [Trait("spec", Spec + ": Acciones y estados coherentes (Acción no disponible)")]
    public void An_attached_action_runs_and_stops_applying_when_the_screen_goes_away()
    {
        var search = Registry(UiPlatform.Windows)[StandardActions.Search];
        var runs = 0;

        var attached = search.Attach(() => runs++);
        search.Execute(null);
        attached.Dispose();
        search.Execute(null);

        Assert.Equal(1, runs);
        Assert.False(search.IsAvailable);
    }

    [Fact]
    [Trait("spec", Spec + ": Acciones y estados coherentes (Acción no disponible)")]
    public void A_disabled_action_explains_why_in_its_tooltip()
    {
        var action = new AppAction("Assign", "Assigna");
        var available = true;
        action.Attach(() => { }, () => available ? Availability.Available : Availability.Unavailable("La taquilla està avariada"));
        Assert.Equal("Assigna", action.ToolTipText);

        available = false;
        action.Refresh();

        Assert.False(action.IsAvailable);
        Assert.Equal("La taquilla està avariada", action.UnavailableReason);
        Assert.Equal("La taquilla està avariada", action.ToolTipText);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Atajos visibles (Menú con atajos)")]
    public void A_button_shows_the_shortcut_in_its_tooltip_and_a_disabled_one_shows_its_reason()
    {
        var search = Registry(UiPlatform.Windows)[StandardActions.Search];
        var button = ActionControls.Button(search);
        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Cerca (Ctrl+F)", ToolTip.GetTip(button));
        Assert.False(button.IsEffectivelyEnabled); // nothing attached yet

        var release = new AppAction("Release", "Allibera");
        release.Attach(() => { }, () => Availability.Unavailable("La taquilla no té cap alumne"));
        var disabled = ActionControls.Button(release);

        Assert.Equal("La taquilla no té cap alumne", ToolTip.GetTip(disabled));
        Assert.True(ToolTip.GetShowOnDisabled(disabled));
    }

    // --- Dispatching shortcuts ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Conjunto fijo de atajos (Buscar)")]
    public void Control_F_runs_search_on_Windows_and_Command_F_on_macOS()
    {
        foreach (var (platform, modifier, other) in new[]
        {
            (UiPlatform.Windows, RawInputModifiers.Control, RawInputModifiers.Meta),
            (UiPlatform.MacOS, RawInputModifiers.Meta, RawInputModifiers.Control),
        })
        {
            var registry = Registry(platform);
            var runs = 0;
            registry[StandardActions.Search].Attach(() => runs++);
            var window = new Window { Content = new TextBox() };
            ShortcutDispatcher.Attach(window, registry);
            window.Show();

            Press(window, Key.F, other);
            Assert.Equal(0, runs); // the other system's modifier is not the shortcut here
            Press(window, Key.F, modifier);

            Assert.Equal(1, runs);
        }
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Conjunto fijo de atajos (Atajo no aplicable)")]
    public void A_shortcut_that_does_not_apply_on_the_screen_does_nothing_and_shows_no_error()
    {
        var registry = Registry(UiPlatform.Windows);
        var notifications = new Arca.Testing.RecordingNotifications();
        var window = new Window { Content = new TextBox() };
        ShortcutDispatcher.Attach(window, registry);
        window.Show();

        Press(window, Key.N, RawInputModifiers.Control); // "new" is not offered here

        Assert.Empty(notifications.Published);
        Assert.False(registry[StandardActions.New].IsAvailable);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Conjunto fijo de atajos (Cancelar)")]
    public void A_control_that_uses_the_key_keeps_it_and_the_screen_action_does_not_run()
    {
        var registry = Registry(UiPlatform.Windows);
        var runs = 0;
        registry[StandardActions.Cancel].Attach(() => runs++);
        var box = new TextBox(); // stands for a drop-down or a menu that closes with Escape
        box.AddHandler(InputElement.KeyDownEvent, (_, e) => e.Handled = e.Key == Key.Escape);
        var window = new Window { Content = box };
        ShortcutDispatcher.Attach(window, registry);
        window.Show();
        box.Focus();
        Dispatcher.UIThread.RunJobs();

        Press(window, Key.Escape);
        Assert.Equal(0, runs);

        registry[StandardActions.Search].Attach(() => runs += 10);
        Press(window, Key.F, RawInputModifiers.Control); // a key no control uses still reaches the screen
        Assert.Equal(10, runs);
    }

    // --- Context menus ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Menús contextuales equivalentes (Menú de una taquilla)")]
    public void The_context_menu_offers_the_same_actions_as_the_buttons_with_the_unavailable_ones_disabled_and_explained()
    {
        var assign = new AppAction("Assign", "Assigna");
        var release = new AppAction("Release", "Allibera");
        assign.Attach(() => { });
        release.Attach(() => { }, () => Availability.Unavailable("La taquilla està lliure"));
        var list = new ListBox { ItemsSource = new[] { "Taquilla 1" } };
        ActionControls.AttachContextMenu(list, () => [assign, release]);
        var window = new Window { Content = list };
        window.Show();

        FocusFirstItem(list, window);
        Press(window, Key.Apps);
        Dispatcher.UIThread.RunJobs();

        var items = list.ContextMenu!.Items.OfType<MenuItem>().ToList();
        Assert.Equal(["Assigna", "Allibera"], items.Select(i => i.Header));
        Assert.True(items[0].IsEffectivelyEnabled);
        Assert.False(items[1].IsEffectivelyEnabled);
        Assert.Equal("La taquilla està lliure", ToolTip.GetTip(items[1]));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Menús contextuales equivalentes (Con el teclado)")]
    public void The_menu_key_opens_the_same_context_menu()
    {
        var assign = new AppAction("Assign", "Assigna");
        assign.Attach(() => { });
        var list = new ListBox { ItemsSource = new[] { "Taquilla 1" } };
        ActionControls.AttachContextMenu(list, () => [assign]);
        var window = new Window { Content = list };
        window.Show();
        FocusFirstItem(list, window);

        Press(window, Key.Apps);
        Dispatcher.UIThread.RunJobs();

        Assert.True(list.ContextMenu!.IsOpen);
        Assert.Single(list.ContextMenu.Items);
    }

    // --- Focus ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Operación completa con teclado (Recorrer un formulario)")]
    public void Tab_goes_through_the_fields_in_reading_order_and_ends_on_the_main_action()
    {
        var name = new TextBox { Text = "" };
        var email = new TextBox { Text = "" };
        var save = new Button { Content = "Desa", IsDefault = true };
        var window = new Window { Content = new StackPanel { Children = { name, email, save } } };
        window.Show();
        name.Focus();
        Dispatcher.UIThread.RunJobs();

        var visited = new List<object?>();
        for (var i = 0; i < 2; i++)
        {
            Press(window, Key.Tab);
            Dispatcher.UIThread.RunJobs();
            visited.Add(window.FocusManager!.GetFocusedElement());
        }

        Assert.Equal([email, save], visited);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Foco visible (Foco en un botón)")]
    public void Every_control_gets_the_focus_ring_of_the_theme()
    {
        var button = new Button { Content = "Desa" };
        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var adorner = button.FocusAdorner!.Build() as Border;

        Assert.NotNull(adorner);
        Assert.Equal(2, adorner!.BorderThickness.Left);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Foco visible (Foco tras cerrar un diálogo)")]
    public async Task After_a_dialog_closes_the_focus_returns_to_the_control_that_opened_it()
    {
        var opener = new Button { Content = "Condona" };
        var owner = new Window { Content = new StackPanel { Children = { new TextBox(), opener } } };
        owner.Show();
        opener.Focus();
        Dispatcher.UIThread.RunJobs();
        var service = new DialogConfirmationService(() => owner, _localizer);

        var asked = service.ConfirmAsync(new ConfirmationRequest("Condonar?", "Es condonaran.", "Condona"));
        Dispatcher.UIThread.RunJobs();
        Press((Window)Assert.Single(owner.OwnedWindows), Key.Escape);
        Dispatcher.UIThread.RunJobs();
        var confirmed = await asked;
        Dispatcher.UIThread.RunJobs();

        Assert.False(confirmed);
        Assert.Same(opener, owner.FocusManager!.GetFocusedElement());
    }
}
