// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Arca.UI.Search;

/// <summary>
/// The search box that is always in the header, with the panel of results under it. The arrows move through the results
/// (the headings are skipped), Enter opens the one with the focus, Escape clears and closes, and a click opens the result it
/// lands on. The panel never takes the keyboard from the box, so the person can keep typing while the results update.
/// </summary>
public sealed class SearchBoxView : UserControl
{
    readonly GlobalSearchViewModel _model;
    readonly TextBox _input;
    readonly Popup _popup;
    readonly ListBox _list;
    readonly TextBlock _message;
    readonly CheckBox _retired;

    public SearchBoxView(GlobalSearchViewModel model, ILocalizer localizer)
    {
        _model = model;
        _input = new TextBox { PlaceholderText = localizer.Get("Shell.Search.Placeholder"), MinWidth = 280 };
        _input.TextChanged += (_, _) => model.Text = _input.Text ?? string.Empty;
        _input.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        _message = new TextBlock { TextWrapping = TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        _list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<SearchItem>((item, _) => item is null ? null : Line(item), supportsRecycling: false),
            MaxHeight = 360,
        };
        _list.Tapped += OnTapped;
        _retired = new CheckBox { Content = localizer.Get("Shell.Search.IncludeRetired") };
        _retired.IsCheckedChanged += (_, _) => model.IncludeRetired = _retired.IsChecked == true;

        var panel = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        panel.Children.Add(_message);
        panel.Children.Add(_list);
        panel.Children.Add(_retired);
        _popup = new Popup
        {
            PlacementTarget = _input,
            Placement = PlacementMode.Bottom,
            Child = new Border { Child = panel, MinWidth = 420 }
                .Themed(Border.BackgroundProperty, ArcaResourceKeys.Surface)
                .Themed(Border.BorderBrushProperty, ArcaResourceKeys.Border)
                .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium),
        };
        ((Border)_popup.Child).BorderThickness = new Avalonia.Thickness(1);

        var host = new Grid();
        host.Children.Add(_input);
        host.Children.Add(_popup);
        Content = host;

        model.PropertyChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>The box the person types in.</summary>
    public TextBox Input => _input;

    /// <summary>The list of results, so a test can read it.</summary>
    public ListBox Results => _list;

    /// <summary>Whether the panel of results is showing.</summary>
    public bool IsPanelOpen => _popup.IsOpen;

    /// <summary>Puts the cursor in the box with what was there selected, so typing replaces it: what the search shortcut does.</summary>
    public void FocusInput()
    {
        _input.Focus();
        _input.SelectAll();
    }

    void Refresh()
    {
        if (_input.Text != _model.Text)
        {
            _input.Text = _model.Text;
        }

        _message.Text = _model.Message;
        _message.IsVisible = _model.Message.Length > 0;
        _list.ItemsSource = _model.Items;
        _list.IsVisible = _model.Items.Count > 0;
        _list.SelectedIndex = _model.SelectedIndex;
        if (_model.SelectedIndex >= 0)
        {
            _list.ScrollIntoView(_model.SelectedIndex);
        }

        _retired.IsVisible = _model.IsOpen;
        _retired.IsChecked = _model.IncludeRetired;
        _popup.IsOpen = _model.IsOpen;
    }

    static TextBlock Line(SearchItem item)
    {
        var text = new TextBlock { Text = item.Text, TextTrimming = TextTrimming.CharacterEllipsis }
            .Themed(TextBlock.FontSizeProperty, item.IsHeading ? ArcaResourceKeys.FontSizeSmall : ArcaResourceKeys.FontSizeBody)
            .Themed(TextBlock.ForegroundProperty, item.IsHeading ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text);
        if (item.IsHeading)
        {
            text.FontWeight = FontWeight.SemiBold;
            text.IsHitTestVisible = false;
        }

        return text;
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _model.MoveNext();
                e.Handled = true;
                break;
            case Key.Up:
                _model.MovePrevious();
                e.Handled = true;
                break;
            case Key.Enter:
                e.Handled = _model.OpenSelected();
                break;
            case Key.Escape when _model.IsOpen || _input.Text is { Length: > 0 }:
                _model.Close();
                e.Handled = true;
                break;
        }
    }

    void OnTapped(object? sender, TappedEventArgs e)
    {
        if ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>()?.DataContext is SearchItem { IsHeading: false } item)
        {
            _model.Open(item);
        }
    }
}
