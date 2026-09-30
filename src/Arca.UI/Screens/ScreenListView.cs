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

        // The search and the filters wrap onto a second line when the list is narrow, so none is ever cut off.
        var bar = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        bar.Children.Add(Search);
        if (filters is Panel panel)
        {
            foreach (var child in panel.Children.ToList())
            {
                panel.Children.Remove(child);
                bar.Children.Add(child);
            }
        }
        else if (filters is not null)
        {
            bar.Children.Add(filters);
        }

        // Reset, and the labels of the filters that are on, each removable on its own, with the count of rows shown.
        ResetButton = ActionControls.Button(model.Reset);
        bar.Children.Add(ResetButton);
        TagsPanel = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        var count = new TextBlock { VerticalAlignment = VerticalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        var tagsRow = new DockPanel().ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        DockPanel.SetDock(count, Dock.Right);
        tagsRow.Children.Add(count);
        tagsRow.Children.Add(TagsPanel);
        void ShowTags()
        {
            count.Text = model.CountText;
            TagsPanel.Children.Clear();
            foreach (var tag in model.ActiveFilters)
            {
                var remove = new Button { Content = tag.Text + " ✕", Padding = new Avalonia.Thickness(8, 2) };
                Avalonia.Automation.AutomationProperties.SetName(remove, localizer.Get("Common.Action.RemoveFilter", tag.Text));
                ToolTip.SetTip(remove, localizer.Get("Common.Action.RemoveFilter", tag.Text));
                var taken = tag;
                remove.Click += (_, _) => taken.Remove();
                TagsPanel.Children.Add(remove);
            }
        }

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ScreenListViewModel<TRow, TKey>.ActiveFilters) or nameof(ScreenListViewModel<TRow, TKey>.CountText))
            {
                ShowTags();
            }
        };
        ShowTags();

        var body = new Grid();
        body.Children.Add(Rows);
        body.Children.Add(new ListStateView(model.State));
        var layout = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        bar.ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        layout.Children.Add(bar);
        DockPanel.SetDock(tagsRow, Dock.Top);
        layout.Children.Add(tagsRow);
        layout.Children.Add(body);
        Content = layout;
    }

    public TextBox Search { get; }

    /// <summary>The Reset button: it empties the search and takes every filter off.</summary>
    public Button ResetButton { get; }

    /// <summary>The labels of the filters that are on; pressing one takes that filter off.</summary>
    public WrapPanel TagsPanel { get; }

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
