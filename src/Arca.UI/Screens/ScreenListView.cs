// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
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
        Search = new TextBox { PlaceholderText = localizer.Get("Common.Action.Search"), MinWidth = 200 };
        Search.TextChanged += (_, _) => model.List.FilterText = Search.Text ?? string.Empty;
        model.List.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Lists.ListViewModel<TRow, TKey>.FilterText) && Search.Text != model.List.FilterText)
            {
                Search.Text = model.List.FilterText; // cleared from the empty state
            }
        };

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

        var bar = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        bar.Children.Add(Search);
        if (filters is not null)
        {
            bar.Children.Add(filters);
        }

        var body = new Grid();
        body.Children.Add(Rows);
        body.Children.Add(new ListStateView(model.State));
        var layout = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        bar.ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        layout.Children.Add(bar);
        layout.Children.Add(body);
        Content = layout;
    }

    public TextBox Search { get; }

    public VirtualizedListView<TRow, TKey> Rows { get; }

    /// <summary>Shows the row the model has chosen as the one selected in the list, so the two always agree.</summary>
    void Show()
    {
        var current = _model.Current;
        if (current is not null && !ReferenceEquals(Rows.List.SelectedItem, current) && _model.List.Rows.Contains(current))
        {
            Rows.List.SelectedItem = current;
        }
    }
}
