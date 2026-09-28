// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Arca.UI.Shell;

/// <summary>
/// The fixed sidebar with the sections, each with its icon and name, which folds to icons only (the name then goes in a
/// tooltip) and is operated with the mouse and the keyboard: Tab or the arrows move between sections and Enter opens the one
/// with the focus. The open section is marked, and the fold is remembered.
/// </summary>
public sealed class SidebarView : UserControl
{
    readonly NavigationViewModel _navigation;
    readonly ILocalizer _localizer;
    readonly StackPanel _items = new();
    readonly Dictionary<string, (ToggleButton Button, TextBlock Name)> _buttons = [];
    readonly Button _fold;

    public SidebarView(NavigationViewModel navigation, ILocalizer localizer)
    {
        _navigation = navigation;
        _localizer = localizer;
        _items.Bind(StackPanel.SpacingProperty, this.GetResourceObservable(ArcaResourceKeys.SpacingSmall));
        _fold = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
        _fold.Click += (_, _) => _navigation.IsSidebarCollapsed = !_navigation.IsSidebarCollapsed;

        foreach (var section in navigation.Sections)
        {
            _items.Children.Add(Build(section));
        }

        var layout = new DockPanel();
        DockPanel.SetDock(_fold, Dock.Bottom);
        layout.Children.Add(_fold);
        layout.Children.Add(_items);
        Content = new Border { Child = layout }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Surface)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);

        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(NavigationViewModel.CurrentSectionId) or nameof(NavigationViewModel.IsSidebarCollapsed))
            {
                Refresh();
            }
        };
        _items.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        Refresh();
    }

    /// <summary>The button of a section, so a screen or a test can reach the control that takes the focus.</summary>
    public ToggleButton ButtonOf(string sectionId) => _buttons[sectionId].Button;

    /// <summary>The button that folds and unfolds the sidebar.</summary>
    public Button FoldButton => _fold;

    ToggleButton Build(SectionDefinition section)
    {
        var name = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Text = _localizer.Get(section.TitleKey) }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        var row = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        row.Children.Add(SectionIcons.Create(section.Id));
        row.Children.Add(name);

        var button = new ToggleButton { Content = row, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
        var id = section.Id;
        button.Click += (_, _) =>
        {
            _navigation.Navigate(id);
            Refresh(); // a click on the open section must not leave it unmarked
        };
        _buttons[section.Id] = (button, name);
        return button;
    }

    void Refresh()
    {
        var collapsed = _navigation.IsSidebarCollapsed;
        foreach (var section in _navigation.Sections)
        {
            var (button, name) = _buttons[section.Id];
            button.IsChecked = section.Id == _navigation.CurrentSectionId;
            name.IsVisible = !collapsed;
            ToolTip.SetTip(button, collapsed ? _localizer.Get(section.TitleKey) : null);
        }

        _fold.Content = collapsed ? "»" : "«"; // a symbol; what it does is written in its tooltip
        ToolTip.SetTip(_fold, _localizer.Get(collapsed ? "Shell.Label.ExpandSidebar" : "Shell.Label.CollapseSidebar"));
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var step = e.Key == Key.Down ? 1 : e.Key == Key.Up ? -1 : 0;
        if (step == 0 || TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not ToggleButton focused)
        {
            return;
        }

        var order = _navigation.Sections.Select(s => _buttons[s.Id].Button).ToList();
        var next = order.IndexOf(focused) + step;
        if (next >= 0 && next < order.Count)
        {
            order[next].Focus(NavigationMethod.Directional);
            e.Handled = true;
        }
    }
}
