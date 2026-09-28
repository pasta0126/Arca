// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.UI.Layout;
using Arca.UI.Preferences;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class AdaptabilityTests
{
    const string Spec = "ux-fonaments/adaptabilitat-i-disposicio";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Saved { get; private set; } = new();

        public bool FailOnLoad { get; init; }

        public bool FailOnSave { get; init; }

        public int Saves { get; private set; }

        public UiPreferences Load() => FailOnLoad ? throw new IOException("unreadable") : Saved;

        public void Save(UiPreferences preferences)
        {
            if (FailOnSave)
            {
                throw new IOException("read-only disk");
            }

            Saved = preferences;
            Saves++;
        }
    }

    static readonly ScreenArea _main = new(0, 0, 1920, 1080);

    // --- Minimum window size ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Tamaño mínimo de ventana (Reducir la ventana)")]
    public void The_window_cannot_be_made_smaller_than_the_minimum()
    {
        var window = new Window();

        WindowLimits.Apply(window);

        Assert.Equal((WindowLimits.MinWidth, WindowLimits.MinHeight), (window.MinWidth, window.MinHeight));
        Assert.Equal((1024d, 640d), (WindowLimits.MinWidth, WindowLimits.MinHeight));
    }

    // --- Adaptive layout ---

    [Theory]
    [InlineData(600, PanelArrangement.Stacked)]
    [InlineData(899, PanelArrangement.Stacked)]
    [InlineData(900, PanelArrangement.SideBySide)]
    [InlineData(1600, PanelArrangement.SideBySide)]
    [Trait("spec", Spec + ": Disposición adaptable (Ancho reducido)")]
    public void Panels_stack_below_the_threshold_and_go_side_by_side_from_it(double width, PanelArrangement expected)
    {
        Assert.Equal(expected, AdaptivePanels.For(width));
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Disposición adaptable (Ancho reducido)")]
    public void The_detail_goes_under_the_list_when_the_width_is_reduced_and_beside_it_when_widened()
    {
        var list = new TextBlock { Text = "llista" };
        var detail = new TextBlock { Text = "detall" };
        var panels = new AdaptivePanels(list, detail);
        var window = new Window { Width = 1400, Height = 700, Content = panels };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PanelArrangement.SideBySide, panels.Arrangement);
        Assert.True(Grid.GetColumn(detail) == 1 && Grid.GetRow(detail) == 0);

        window.Width = 700;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(PanelArrangement.Stacked, panels.Arrangement);
        Assert.True(Grid.GetRow(detail) == 1 && Grid.GetColumn(detail) == 0);

        window.Width = 1500;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(PanelArrangement.SideBySide, panels.Arrangement);
    }

    // --- Collapsible sections ---

    static CollapsibleSectionViewModel Section(UiPreferencesSession session, string summary = "Contrasenya del centre") =>
        new("security", "Seguretat", () => summary, session);

    [Fact]
    [Trait("spec", Spec + ": Secciones colapsables (Colapsar)")]
    public void A_folded_section_shows_its_summary_and_an_open_one_shows_none()
    {
        var section = Section(new UiPreferencesSession(new MemoryStore()));
        Assert.True(section.IsExpanded);
        Assert.Equal(string.Empty, section.Summary);

        section.Toggle();

        Assert.False(section.IsExpanded);
        Assert.Equal("Contrasenya del centre", section.Summary);
    }

    [Fact]
    [Trait("spec", Spec + ": Secciones colapsables (Recordar el estado)")]
    public void The_state_of_a_section_is_remembered_when_the_application_is_reopened()
    {
        var store = new MemoryStore();
        Section(new UiPreferencesSession(store)).Toggle();

        var reopened = Section(new UiPreferencesSession(store));

        Assert.False(reopened.IsExpanded);
    }

    [Fact]
    [Trait("spec", Spec + ": Secciones colapsables (Sección con error)")]
    public void A_folded_section_with_a_validation_error_opens_and_goes_back_to_the_remembered_state_when_fixed()
    {
        var store = new MemoryStore();
        var section = Section(new UiPreferencesSession(store));
        section.Toggle(); // folded, and remembered as folded

        section.SetError(true);

        Assert.True(section.IsExpanded);
        Assert.Equal(string.Empty, section.Summary);
        Assert.False(store.Saved.Sections!["security"]); // the error opened it, but the person's choice is still "folded"
        Assert.False(Section(new UiPreferencesSession(store)).IsExpanded);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Secciones colapsables (Colapsar)")]
    public void The_section_view_folds_with_the_expander_and_shows_the_summary()
    {
        var model = Section(new UiPreferencesSession(new MemoryStore()));
        var view = new CollapsibleSectionView(model, new TextBlock { Text = "contingut" });
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.True(view.Section.IsExpanded);

        view.Section.IsExpanded = false;
        Dispatcher.UIThread.RunJobs();

        Assert.False(model.IsExpanded);
        Assert.Contains(view.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Contrasenya del centre" && t.IsVisible);
    }

    // --- Local preferences ---

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ajustes ilegibles)")]
    public void Unreadable_preferences_start_with_the_defaults_without_an_error()
    {
        var session = new UiPreferencesSession(new MemoryStore { FailOnLoad = true });

        Assert.Null(session.Window);
        Assert.False(session.CompactLists);
        Assert.True(session.IsSectionExpanded("anything", true));
    }

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ajustes ilegibles)")]
    public void Preferences_that_cannot_be_saved_do_not_stop_the_application()
    {
        var session = new UiPreferencesSession(new MemoryStore { FailOnSave = true });

        session.SetSectionExpanded("security", false);
        session.SetCompactLists(true);

        Assert.False(session.IsSectionExpanded("security", true)); // it still holds for this session
        Assert.True(session.CompactLists);
    }

    // --- Window placement ---

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ajustes ilegibles)")]
    public void Without_saved_bounds_the_window_opens_with_the_default_size_where_the_system_decides()
    {
        var placement = WindowPlacement.Correct(null, [_main], 1);

        Assert.Equal((null, null, WindowLimits.DefaultWidth, WindowLimits.DefaultHeight), (placement.X, placement.Y, placement.Width, placement.Height));
    }

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ventana fuera de pantalla)")]
    public void A_visible_position_and_size_are_kept()
    {
        var placement = WindowPlacement.Correct(new WindowBounds(200, 100, 1300, 800), [_main], 1);

        Assert.Equal((200, 100, 1300d, 800d), (placement.X, placement.Y, placement.Width, placement.Height));
    }

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ventana fuera de pantalla)")]
    public void A_position_on_a_screen_that_is_gone_puts_the_window_back_centred_on_the_main_screen()
    {
        var second = new ScreenArea(1920, 0, 1920, 1080);
        var saved = new WindowBounds(2500, 200, 1200, 760); // it was on the second monitor
        Assert.Equal(2500, WindowPlacement.Correct(saved, [_main, second], 1).X);

        var placement = WindowPlacement.Correct(saved, [_main], 1);

        Assert.Equal((360, 160), (placement.X, placement.Y)); // (1920 - 1200) / 2 and (1080 - 760) / 2
    }

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ventana fuera de pantalla)")]
    public void A_window_that_only_touches_the_edge_of_a_screen_is_not_considered_visible()
    {
        var placement = WindowPlacement.Correct(new WindowBounds(1900, 200, 1200, 760), [_main], 1);

        Assert.Equal(360, placement.X);
    }

    [Fact]
    [Trait("spec", Spec + ": Tamaño mínimo de ventana (Reducir la ventana)")]
    public void A_saved_size_below_the_minimum_is_raised_and_one_bigger_than_the_screen_is_reduced()
    {
        var small = WindowPlacement.Correct(new WindowBounds(0, 0, 300, 200), [_main], 1);
        var huge = WindowPlacement.Correct(new WindowBounds(0, 0, 9000, 9000), [_main], 1);

        Assert.Equal((WindowLimits.MinWidth, WindowLimits.MinHeight), (small.Width, small.Height));
        Assert.Equal((1920d, 1080d), (huge.Width, huge.Height));
    }

    [Fact]
    [Trait("spec", Spec + ": Escalado por DPI (Pantalla de alta densidad)")]
    public void The_size_limit_of_a_screen_takes_its_scale_factor_into_account()
    {
        var huge = WindowPlacement.Correct(new WindowBounds(0, 0, 9000, 9000), [_main], 1.5);

        Assert.Equal((1280d, 720d), (huge.Width, huge.Height)); // 1920 / 1.5 and 1080 / 1.5
    }

    [Fact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ventana fuera de pantalla)")]
    public void A_maximized_window_stays_maximized()
    {
        Assert.True(WindowPlacement.Correct(new WindowBounds(0, 0, 1300, 800, IsMaximized: true), [_main], 1).IsMaximized);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Preferencias locales de interfaz (Ventana fuera de pantalla)")]
    public void Closing_the_window_saves_its_size_for_the_next_time()
    {
        var store = new MemoryStore();
        var window = new Window { Width = 1111, Height = 777 };
        WindowStateKeeper.Attach(window, new UiPreferencesSession(store));
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Close();

        Assert.NotNull(store.Saved.Window);
        Assert.Equal((WindowLimits.MinWidth, WindowLimits.MinHeight), (window.MinWidth, window.MinHeight));
    }
}
