// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Lists;

/// <summary>
/// Draws the loading state or the empty state of a list, and nothing when there is content: the screen puts it over its
/// list and shows the rows when <see cref="ListStateViewModel.IsContent"/>. Colours, type and spacing come from the theme.
/// </summary>
public sealed class ListStateView : UserControl
{
    readonly ListStateViewModel _model;
    readonly StackPanel _actions;

    public ListStateView(ListStateViewModel model)
    {
        _model = model;
        DataContext = model;
        this.Bind(IsVisibleProperty, new Binding(nameof(ListStateViewModel.IsContent)) { Converter = Avalonia.Data.Converters.BoolConverters.Not });

        var loading = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        loading.Bind(IsVisibleProperty, new Binding(nameof(ListStateViewModel.IsLoading)));
        loading.Children.Add(new ProgressBar { IsIndeterminate = true, MinWidth = 160 });
        loading.Children.Add(new TextBlock { Text = model.LoadingText, HorizontalAlignment = HorizontalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary));

        _actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        var message = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, TextAlignment = Avalonia.Media.TextAlignment.Center, MaxWidth = 480 }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        message.Bind(TextBlock.TextProperty, new Binding(nameof(ListStateViewModel.Message)));
        var empty = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingLarge);
        empty.Bind(IsVisibleProperty, new Binding(nameof(ListStateViewModel.IsEmpty)));
        empty.Children.Add(message);
        empty.Children.Add(_actions);

        var content = new Grid();
        content.Children.Add(loading);
        content.Children.Add(empty);
        Content = content;
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingLarge);

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ListStateViewModel.Actions))
            {
                RebuildActions();
            }
        };
        RebuildActions();
    }

    /// <summary>The buttons currently offered, for tests and for focus.</summary>
    public IReadOnlyList<Button> ActionButtons => [.. _actions.Children.OfType<Button>()];

    void RebuildActions()
    {
        _actions.Children.Clear();
        foreach (var action in _model.Actions)
        {
            _actions.Children.Add(new Button { Content = action.Label, Command = action.Command });
        }
    }
}
