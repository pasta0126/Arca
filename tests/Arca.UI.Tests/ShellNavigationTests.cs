// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.UI.Actions;
using Arca.UI.Lists;
using Arca.UI.Preferences;
using Arca.UI.Shell;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ShellNavigationTests
{
    const string Spec = "ui-shell/navegacio-i-cerca";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Saved { get; private set; } = new();

        public UiPreferences Load() => Saved;

        public void Save(UiPreferences preferences) => Saved = preferences;
    }

    readonly ResxLocalizer _localizer = new();
    readonly MemoryStore _store = new();

    SectionRegistry Registry() => SectionRegistry.Compose(new Dictionary<string, Func<Control>>());

    NavigationViewModel Navigation(SectionRegistry? registry = null)
    {
        var used = registry ?? Registry();
        return new NavigationViewModel(used, new UiPreferencesSession(_store), s => SectionPlaceholder.Create(s, used, _localizer));
    }

    // --- The registry ---

    [Fact]
    [Trait("spec", Spec + ": Barra lateral de secciones")]
    public void The_sidebar_has_the_eight_sections_in_order()
    {
        var sections = Registry().Sections.Select(s => s.Id);

        Assert.Equal(["Home", "Lockers", "Students", "Payments", "KeysAndIncidents", "Reports", "Course", "Settings"], sections);
    }

    [Fact]
    [Trait("spec", Spec + ": Reparto de las pantallas por sección (Importes del curso)")]
    public void The_amounts_of_the_year_are_in_the_course_section_and_the_backup_in_settings()
    {
        var registry = Registry();

        Assert.Equal("Course", registry.SectionOf("Amounts")!.Id);
        Assert.Equal("Settings", registry.SectionOf("Backup")!.Id);
        Assert.Equal("Students", registry.SectionOf("StudentImport")!.Id);
        Assert.Equal("Payments", registry.SectionOf("Deposits")!.Id);
    }

    [Fact]
    [Trait("spec", Spec + ": Reparto de las pantallas por sección (Una pantalla en una sola sección)")]
    public void No_screen_is_in_two_sections_and_every_section_holds_something()
    {
        var registry = Registry();

        Assert.Equal(registry.Screens.Count, registry.Screens.Select(s => s.Id).Distinct().Count());
        Assert.All(registry.Sections, s => Assert.NotEmpty(registry.ScreensOf(s.Id)));
        Assert.All(registry.Screens, s => Assert.NotNull(registry.Find(s.SectionId)));
    }

    [Fact]
    [Trait("spec", Spec + ": Reparto de las pantallas por sección (Una pantalla en una sola sección)")]
    public void A_screen_registered_in_two_sections_or_in_an_unknown_one_is_refused()
    {
        var sections = ShellCatalog.Sections;

        Assert.Throws<ArgumentException>(() => new SectionRegistry(sections, [.. ShellCatalog.Screens, new ScreenDefinition("Amounts", "x", "Settings")]));
        Assert.Throws<ArgumentException>(() => new SectionRegistry(sections, [new ScreenDefinition("Lost", "x", "Nowhere")]));
        Assert.Throws<ArgumentException>(() => new SectionRegistry([.. sections, sections[0]], []));
    }

    [Fact]
    [Trait("spec", Spec + ": Barra lateral de secciones")]
    public void Every_section_and_screen_has_its_title_in_catalan()
    {
        var registry = Registry();

        Assert.All(registry.Sections, s => Assert.NotEqual(s.TitleKey, _localizer.Get(s.TitleKey)));
        Assert.All(registry.Screens, s => Assert.NotEqual(s.TitleKey, _localizer.Get(s.TitleKey)));
        Assert.Equal("Claus i incidències", _localizer.Get("Shell.Section.KeysAndIncidents"));
    }

    // --- Navigation ---

    [Fact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Cambiar de sección)")]
    public void Choosing_a_section_opens_it_and_going_back_finds_its_screen_as_it_was_left()
    {
        var navigation = Navigation();
        Assert.Equal("Home", navigation.CurrentSectionId);
        var home = navigation.CurrentRoot;

        Assert.True(navigation.Navigate("Students"));
        Assert.Equal("Students", navigation.CurrentSectionId);
        Assert.NotSame(home, navigation.CurrentRoot);

        navigation.Navigate("Home");
        Assert.Same(home, navigation.CurrentRoot);
    }

    [Fact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Cambiar de sección)")]
    public void An_unknown_section_or_the_one_already_open_changes_nothing()
    {
        var navigation = Navigation();

        Assert.False(navigation.Navigate("Nowhere"));
        Assert.False(navigation.Navigate("Home"));
        Assert.Equal("Home", navigation.CurrentSectionId);
    }

    [Fact]
    [Trait("spec", Spec + ": Reparto de las pantallas por sección (Importes del curso)")]
    public void A_screen_can_be_opened_by_taking_the_person_to_its_section()
    {
        var navigation = Navigation();

        Assert.True(navigation.NavigateToScreen("Amounts"));
        Assert.Equal("Course", navigation.CurrentSectionId);
        Assert.False(navigation.NavigateToScreen("DoesNotExist"));
    }

    [Fact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Colapsar la barra)")]
    public void The_fold_of_the_sidebar_is_remembered_when_the_application_is_reopened()
    {
        Navigation().IsSidebarCollapsed = true;

        Assert.True(Navigation().IsSidebarCollapsed);
        Navigation().IsSidebarCollapsed = false;
        Assert.False(Navigation().IsSidebarCollapsed);
    }

    [Fact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Cambiar de sección)")]
    public void A_section_with_its_own_root_screen_opens_it_instead_of_the_placeholder()
    {
        var mine = new TextBlock { Text = "meu" };
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>> { ["Settings"] = () => mine });
        var navigation = new NavigationViewModel(registry, new UiPreferencesSession(_store), s => SectionPlaceholder.Create(s, registry, _localizer));

        navigation.Navigate("Settings");

        Assert.Same(mine, navigation.CurrentRoot);
    }

    // --- The sidebar ---

    static (SidebarView Sidebar, Window Window) Show(NavigationViewModel navigation, ResxLocalizer localizer)
    {
        var sidebar = new SidebarView(navigation, localizer);
        var window = new Window { Width = 400, Height = 700, Content = sidebar };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (sidebar, window);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Cambiar de sección)")]
    public void Clicking_a_section_opens_it_and_marks_it_as_the_open_one()
    {
        var navigation = Navigation();
        var (sidebar, _) = Show(navigation, _localizer);
        Assert.True(sidebar.ButtonOf("Home").IsChecked);

        sidebar.ButtonOf("Students").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Students", navigation.CurrentSectionId);
        Assert.True(sidebar.ButtonOf("Students").IsChecked);
        Assert.False(sidebar.ButtonOf("Home").IsChecked);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Navegación con teclado)")]
    public void With_the_keyboard_the_arrows_move_between_sections_and_enter_opens_the_one_with_the_focus()
    {
        var navigation = Navigation();
        var (sidebar, window) = Show(navigation, _localizer);
        sidebar.ButtonOf("Home").Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(sidebar.ButtonOf("Students"), window.FocusManager!.GetFocusedElement());

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Students", navigation.CurrentSectionId);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Barra lateral de secciones (Colapsar la barra)")]
    public void A_folded_sidebar_shows_only_the_icons_and_puts_the_name_in_a_tooltip()
    {
        var navigation = Navigation();
        var (sidebar, _) = Show(navigation, _localizer);
        var name = sidebar.ButtonOf("Lockers").GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.True(name.IsVisible);
        Assert.Null(ToolTip.GetTip(sidebar.ButtonOf("Lockers")));

        sidebar.FoldButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(navigation.IsSidebarCollapsed);
        Assert.False(name.IsVisible);
        Assert.Equal("Taquilles", ToolTip.GetTip(sidebar.ButtonOf("Lockers")));
        Assert.True(_store.Saved.SidebarCollapsed);
    }

    // --- The frame and the common pattern of a screen ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Patrón común de pantalla (Sección sin datos)")]
    public void Every_section_without_a_screen_of_its_own_says_what_it_will_hold_instead_of_being_blank()
    {
        var registry = Registry();
        var navigation = Navigation(registry);
        var shell = new ShellView(navigation, _localizer);
        var window = new Window { Width = 1100, Height = 700, Content = shell };
        window.Show();

        foreach (var section in registry.Sections)
        {
            navigation.Navigate(section.Id);
            Dispatcher.UIThread.RunJobs();
            var texts = shell.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();

            Assert.Contains(_localizer.Get("Shell.Empty.SectionUnavailable"), texts);
            Assert.All(registry.ScreensOf(section.Id), s => Assert.Contains("• " + _localizer.Get(s.TitleKey), texts));
        }
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Patrón común de pantalla (Pantalla de lista y detalle)")]
    public void A_screen_has_the_title_and_actions_on_top_the_list_and_the_detail_and_the_empty_state()
    {
        var registry = new ActionRegistry(_localizer, UiPlatform.Windows);
        registry[StandardActions.New].Attach(() => { });
        var state = new ListStateViewModel(_localizer);
        state.ShowEmpty("Encara no hi ha zones.", new EmptyStateAction("Afegeix la primera zona", new NoOpCommand()));
        var screen = new ScreenView("Zones", [registry[StandardActions.New]], new ListBox(), new TextBlock { Text = "detall" }, state);
        var window = new Window { Width = 1100, Height = 700, Content = screen };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Zones", screen.Title.Text);
        Assert.Equal("Nou", Assert.Single(screen.Buttons).Content);
        Assert.Contains(screen.GetVisualDescendants().OfType<Button>(), b => Equals(b.Content, "Afegeix la primera zona"));
        Assert.Contains(screen.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "detall");
    }
}
