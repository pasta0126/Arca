// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Screens;

/// <summary>
/// What sits above a list or a map (navegacio-i-cerca, Patrón común de pantalla): the search box and the filters of the screen, the Reset
/// button, the labels of the filters that are on, each removable on its own, and the count of rows shown. The list of a screen and its
/// map are two views of the same model, so both draw this over it and whatever one changes the other shows.
/// </summary>
public sealed class ListToolbarView<TRow, TKey> : UserControl
    where TRow : class
    where TKey : notnull
{
    public ListToolbarView(ScreenListViewModel<TRow, TKey> model, ILocalizer localizer, Control? filters = null)
    {
        Search = new TextBox { PlaceholderText = localizer.Get("Common.Action.Search"), MinWidth = 200 };
        Search.TextChanged += (_, _) => model.List.FilterText = Search.Text ?? string.Empty;
        model.List.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Lists.ListViewModel<TRow, TKey>.FilterText) && Search.Text != model.List.FilterText)
            {
                Search.Text = model.List.FilterText; // cleared from the empty state, from Reset or from the other view
            }
        };

        // The search and the filters wrap onto a second line when the space is narrow, so none is ever cut off.
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

        bar.ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        var layout = new StackPanel();
        layout.Children.Add(bar);
        layout.Children.Add(tagsRow);
        Content = layout;
    }

    public TextBox Search { get; }

    /// <summary>The Reset button: it empties the search and takes every filter off.</summary>
    public Button ResetButton { get; }

    /// <summary>The labels of the filters that are on; pressing one takes that filter off.</summary>
    public WrapPanel TagsPanel { get; }
}
