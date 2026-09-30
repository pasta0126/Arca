// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Screens;

/// <summary>
/// The list of a domain screen (pantalles-de-domini, D4): a search box and the filters of the screen on top, the rows that are chosen
/// with a click to show their detail, and the states of loading, of nothing yet and of a filter without results drawn over them.
/// The row chosen belongs to the model, by identity.
/// </summary>
public sealed class ScreenListView<TRow, TKey> : UserControl
    where TRow : class
    where TKey : notnull
{
    readonly ScreenListViewModel<TRow, TKey> _model;

    public ScreenListView(ScreenListViewModel<TRow, TKey> model, ILocalizer localizer, Control? filters = null)
    {
        _model = model;
        Toolbar = new ListToolbarView<TRow, TKey>(model, localizer, filters);

        Rows = new VirtualizedListView<TRow, TKey>(model.List, showMarks: false);
        Rows.List.SelectionChanged += (_, e) =>
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TRow row)
            {
                model.Select(row); // choosing is the person's; the model's own changes never deselect
            }
        };
        model.PropertyChanged += (_, _) => Show();
        model.List.PropertyChanged += (_, _) => Show();

        var body = new Grid();
        body.Children.Add(Rows);
        body.Children.Add(new ListStateView(model.State));
        var layout = new DockPanel();
        DockPanel.SetDock(Toolbar, Dock.Top);
        layout.Children.Add(Toolbar);
        layout.Children.Add(body);
        Content = layout;
    }

    /// <summary>The search, filters, Reset button, labels and count above the rows.</summary>
    public ListToolbarView<TRow, TKey> Toolbar { get; }

    public TextBox Search => Toolbar.Search;

    public Button ResetButton => Toolbar.ResetButton;

    public WrapPanel TagsPanel => Toolbar.TagsPanel;

    public VirtualizedListView<TRow, TKey> Rows { get; }

    /// <summary>Shows the row the model has chosen as the one selected in the list, so the two always agree.</summary>
    void Show()
    {
        var current = _model.Current;
        if (current is null && Rows.List.SelectedItem is not null)
        {
            Rows.List.SelectedItem = null; // chosen no row (Esc): the list shows none chosen either
            return;
        }

        if (current is not null && !ReferenceEquals(Rows.List.SelectedItem, current) && _model.List.Rows.Contains(current))
        {
            Rows.List.SelectedItem = current;
        }
    }
}
