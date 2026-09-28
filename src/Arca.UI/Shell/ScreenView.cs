// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Layout;
using Arca.UI.Lists;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Shell;

/// <summary>
/// The structure every domain screen shares (ui-shell, navegacio-i-cerca, Patrón común de pantalla): the title and the main
/// actions on top, and below them the list with its search and filters and, beside it or under it, the detail of the selected
/// element, with the states of loading and of nothing to show drawn over the list. The actions are the same objects the menus
/// and the shortcuts use, so a button, a menu entry and a key always do the same.
/// </summary>
public sealed class ScreenView : UserControl
{
    /// <param name="title">The title of the screen, already in the user's language.</param>
    /// <param name="actions">The main actions, in the order they are offered.</param>
    /// <param name="list">The list, with whatever search and filters it has.</param>
    /// <param name="detail">The detail of the selected element, or null when the screen has none.</param>
    /// <param name="state">The loading and empty states of the list, or null when it has none.</param>
    public ScreenView(string title, IReadOnlyList<AppAction> actions, Control list, Control? detail = null, ListStateViewModel? state = null)
    {
        Title = ThemedText.Title(title);
        Buttons = [.. actions.Select(ActionControls.Button)];

        var bar = new DockPanel().ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        buttons.Children.AddRange(Buttons);
        DockPanel.SetDock(buttons, Dock.Right);
        bar.Children.Add(buttons);
        bar.Children.Add(Title);

        var body = new Grid().ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        body.Children.Add(detail is null ? list : new AdaptivePanels(list, detail));
        if (state is not null)
        {
            body.Children.Add(new ListStateView(state));
        }

        var layout = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        layout.Children.Add(bar);
        layout.Children.Add(body);
        Content = layout;
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingLarge);
    }

    /// <summary>The title, so a test or the header can read it.</summary>
    public TextBlock Title { get; }

    /// <summary>The buttons of the main actions, in order.</summary>
    public IReadOnlyList<Button> Buttons { get; }
}
