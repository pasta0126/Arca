// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Lists;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ScreenPatternTests
{
    const string Spec = "ui-llistats-i-detall/navegacio-i-cerca";

    sealed record Row(Guid Id, string Name);

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();

    ScreenListViewModel<Row, Guid> Model()
    {
        IReadOnlyList<Row> rows = [new(Guid.NewGuid(), "Puig"), new(Guid.NewGuid(), "Alsina")];
        return new ScreenListViewModel<Row, Guid>(
            [new ListColumn<Row>("name", "Nom", r => r.Name)], r => r.Id,
            _ => Task.FromResult(Result<IReadOnlyList<Row>>.Success(rows)), _localizer, _notifications, _log);
    }

    (Window Window, ScreenView Screen, ScreenListView<Row, Guid> List, ScreenListViewModel<Row, Guid> Model) Open()
    {
        var model = Model();
        var list = new ScreenListView<Row, Guid>(model, _localizer);
        var screen = new ScreenView("Prova", [], list, new TextBlock { Text = "detall" }, model.State, model);
        var window = new Window { Content = screen, Width = 900, Height = 600 };
        window.Show();
        model.LoadAsync().GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs();
        return (window, screen, list, model);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Patrón común de pantalla (Quitar la selección con Esc)")]
    public void Esc_lets_go_of_the_row_chosen_and_of_the_focus_and_the_list_shows_none_chosen()
    {
        var (window, screen, list, model) = Open();
        model.Select(model.List.Rows[0]);
        list.Rows.List.SelectedItem = model.List.Rows[0];
        list.Search.Focus(); // empty, so Esc goes on to the row chosen
        Dispatcher.UIThread.RunJobs();
        Assert.Same(list.Search, window.FocusManager!.GetFocusedElement());

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(model.HasSelection);
        Assert.Null(model.Current);
        Assert.Null(list.Rows.List.SelectedItem);
        Assert.Same(screen, window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Patrón común de pantalla (Esc con la búsqueda enfocada)")]
    public void Esc_in_the_search_box_with_text_empties_it_and_keeps_the_row_chosen()
    {
        var (window, _, list, model) = Open();
        model.Select(model.List.Rows[0]);
        list.Search.Text = "pu";
        list.Search.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(string.Empty, list.Search.Text);
        Assert.Equal(string.Empty, model.List.FilterText);
        Assert.True(model.HasSelection);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); // now the box is empty, so Esc reaches the row
        Dispatcher.UIThread.RunJobs();
        Assert.False(model.HasSelection);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Patrón común de pantalla (Esc dentro de un formulario)")]
    public void A_dialog_takes_its_own_Esc_and_the_selection_of_the_screen_behind_does_not_change()
    {
        var (_, _, _, model) = Open();
        model.Select(model.List.Rows[0]);
        var dialogBox = new TextBox();
        var dialog = new Window { Content = dialogBox };
        dialog.Show();
        dialogBox.Focus();

        dialog.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(model.HasSelection);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Patrón común de pantalla (Reiniciar la búsqueda)")]
    public void The_reset_button_empties_the_box_and_is_disabled_when_there_is_nothing_to_reset()
    {
        var (_, _, list, model) = Open();
        Assert.False(list.ResetButton.IsEffectivelyEnabled);

        list.Search.Text = "pu";
        Dispatcher.UIThread.RunJobs();
        Assert.True(list.ResetButton.IsEffectivelyEnabled);

        list.ResetButton.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(string.Empty, list.Search.Text);
        Assert.Equal(2, model.List.Rows.Count);
        Assert.False(list.ResetButton.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Scroll en pantallas con contenido largo (Ajustes con ventana pequeña)")]
    public void A_page_of_blocks_taller_than_the_window_scrolls_down_to_its_last_block()
    {
        var content = new StackPanel();
        for (var i = 0; i < 30; i++)
        {
            content.Children.Add(new Border { Height = 120, Child = new TextBlock { Text = "bloc " + i } });
        }

        var last = new Button { Content = "últim" };
        content.Children.Add(last);
        var screen = new ScreenView("Ajustes", [], content);
        var window = new Window { Content = screen, Width = Arca.UI.Layout.WindowLimits.MinWidth, Height = Arca.UI.Layout.WindowLimits.MinHeight }; // the smallest window allowed
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroll = screen.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height);

        scroll.ScrollToEnd();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(scroll.Extent.Height - scroll.Viewport.Height, scroll.Offset.Y, 1);
        var bottom = last.TranslatePoint(new Point(0, last.Bounds.Height), scroll);
        Assert.True(bottom!.Value.Y <= scroll.Viewport.Height + 1);
    }
}
