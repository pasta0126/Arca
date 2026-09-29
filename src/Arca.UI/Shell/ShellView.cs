// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;

namespace Arca.UI.Shell;

/// <summary>
/// The frame of the application (ui-shell, D1): a header on top, the sidebar on the left and the open section in the rest.
/// It does not know what any section contains, so replacing the start screen or adding a section never touches it.
/// </summary>
public sealed class ShellView : UserControl
{
    readonly ContentControl _content = new();

    /// <param name="overlay">Something drawn over the content, such as the stack of notifications.</param>
    public ShellView(NavigationViewModel navigation, ILocalizer localizer, Control? overlay = null)
    {
        Sidebar = new SidebarView(navigation, localizer);
        HeaderSlot = new ContentControl();
        _content.Content = navigation.CurrentRoot;
        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationViewModel.CurrentRoot))
            {
                _content.Content = navigation.CurrentRoot;
            }
        };

        var header = new Border { Child = HeaderSlot }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Surface)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(_content, 1);
        body.Children.Add(Sidebar);
        body.Children.Add(_content);
        if (overlay is not null)
        {
            Grid.SetColumn(overlay, 1);
            body.Children.Add(overlay);
        }

        var layout = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        layout.Children.Add(header);
        layout.Children.Add(body);
        Content = layout;
    }

    public SidebarView Sidebar { get; }

    /// <summary>Where the header shows the name, the logo and the year of the centre.</summary>
    public ContentControl HeaderSlot { get; }

    /// <summary>The open section's screen.</summary>
    public object? Current => _content.Content;
}
