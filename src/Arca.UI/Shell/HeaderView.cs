// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Shell;

/// <summary>
/// The fixed header (ui-shell, Cabecera con el estado global): the logo and name of the centre on the left and the active year
/// on the right, or a plain statement that there is none. It redraws whenever the global state changes.
/// </summary>
public sealed class HeaderView : UserControl
{
    readonly GlobalStateService _state;
    readonly ILocalizer _localizer;

    /// <param name="centreName">The name shown: the centre's own once the identity exists, the name of the application until then.</param>
    public HeaderView(GlobalStateService state, ILocalizer localizer, string centreName)
    {
        _state = state;
        _localizer = localizer;
        Logo = new ContentControl { VerticalAlignment = VerticalAlignment.Center };
        Name = ThemedText.Title(centreName);
        Name.VerticalAlignment = VerticalAlignment.Center;
        Year = new TextBlock { VerticalAlignment = VerticalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);

        var left = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        left.Children.Add(Logo);
        left.Children.Add(Name);
        var bar = new DockPanel();
        DockPanel.SetDock(Year, Dock.Right);
        bar.Children.Add(Year);
        bar.Children.Add(left);
        Content = bar;

        state.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Where the logo of the centre goes, once the centre has one.</summary>
    public ContentControl Logo { get; }

    /// <summary>The name of the centre.</summary>
    public new TextBlock Name { get; }

    /// <summary>The active year, or the statement that there is none. Empty until the state is first loaded.</summary>
    public TextBlock Year { get; }

    void Refresh() => Year.Text = _state.Current switch
    {
        null => string.Empty,
        { ActiveYear: { } year } => _localizer.Get("Shell.Header.Year", year.Name),
        _ => _localizer.Get("Shell.Header.NoYear"),
    };
}
