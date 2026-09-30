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
    readonly string _appName;

    /// <param name="centreName">The name shown: the centre's own once the identity exists, the name of the application until then.</param>
    public HeaderView(GlobalStateService state, ILocalizer localizer, string centreName)
    {
        _state = state;
        _localizer = localizer;
        _appName = centreName;
        Logo = new ContentControl { VerticalAlignment = VerticalAlignment.Center };
        Name = ThemedText.Title(centreName);
        Name.VerticalAlignment = VerticalAlignment.Center;
        Name.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis; // when room is short the name gives way, never the version
        Name.MaxWidth = 420;
        Version = new TextBlock { VerticalAlignment = VerticalAlignment.Bottom, IsVisible = false }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeSmall);
        ToolTip.SetTip(Version, localizer.Get("App.Label.VersionTooltip"));
        Year = new TextBlock { VerticalAlignment = VerticalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);

        var left = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        left.Children.Add(Logo);
        left.Children.Add(Name);
        left.Children.Add(Version);
        var bar = new DockPanel();
        DockPanel.SetDock(Year, Dock.Right);
        bar.Children.Add(Year);
        DockPanel.SetDock(left, Dock.Left);
        bar.Children.Add(left);
        var working = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, IsVisible = false }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        working.Children.Add(new ProgressBar { IsIndeterminate = true, MinWidth = 72, VerticalAlignment = VerticalAlignment.Center });
        working.Children.Add(new TextBlock { Text = localizer.Get("Common.Label.Working"), VerticalAlignment = VerticalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary));
        WorkingIndicator = working;
        void OnWork(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => working.IsVisible = Commands.WorkHub.Shared.IsBusy);
        AttachedToVisualTree += (_, _) => Commands.WorkHub.Shared.PropertyChanged += OnWork; // only while the header is on screen
        DetachedFromVisualTree += (_, _) => Commands.WorkHub.Shared.PropertyChanged -= OnWork;
        DockPanel.SetDock(working, Dock.Right);
        bar.Children.Add(working);
        SearchSlot = new ContentControl { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        bar.Children.Add(SearchSlot);
        Content = bar;

        state.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>
    /// Makes the header show the identity of the centre (ui-shell, identitat-i-tema): its name, or the one of the application while it
    /// has none, and its logo when it has one. It follows every save at once.
    /// </summary>
    public void ShowIdentity(Arca.UI.Identity.CentreIdentityModel identity)
    {
        void Update()
        {
            var view = identity.Current;
            Name.Text = view.Name is { Length: > 0 } name ? name : _appName;
            Logo.Content = view.Logo is { } bytes && Arca.UI.Identity.LogoImages.TryDecode(bytes) is { } bitmap
                ? new Image { Source = bitmap, Height = 32, VerticalAlignment = VerticalAlignment.Center }
                : null;
        }

        identity.Changed += (_, _) => Update();
        Update();
    }

    /// <summary>The version of the application, small and next to the title (versio-de-l-aplicacio, Versión visible en la cabecera).</summary>
    public TextBlock Version { get; }

    /// <summary>Shows the version of the application next to the title, whatever the identity of the centre says.</summary>
    public void ShowVersion(string version)
    {
        Version.Text = version;
        Version.IsVisible = version.Length > 0;
    }

    /// <summary>The indicator of long work, shown while any action takes more than 300 ms.</summary>
    public StackPanel WorkingIndicator { get; }

    /// <summary>Where the logo of the centre goes, once the centre has one.</summary>
    public ContentControl Logo { get; }

    /// <summary>Where the global search box goes, in the middle of the header, so it is on every screen.</summary>
    public ContentControl SearchSlot { get; }

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
