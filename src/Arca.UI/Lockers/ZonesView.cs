// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Zones.ListZoneRows;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Lockers;

/// <summary>Builds the Zones view: title and actions, the list of zones and the detail of the one chosen with its operations.</summary>
public static class ZonesView
{
    public static ScreenView Create(ZonesViewModel model, ILocalizer localizer)
    {
        var list = new ScreenListView<ZoneRow, Guid>(model.Zones, localizer);
        var screen = new ScreenView(localizer.Get("Shell.Screen.Zones"), model.MainActions, list, new ZoneDetailPanel(model, localizer));
        screen.AttachedToVisualTree += (_, _) => _ = model.LoadAsync();
        return screen;
    }
}

/// <summary>The detail of the zone chosen: its name, state and number of active lockers, and the operations that apply, each disabled with its reason when it does not.</summary>
public sealed class ZoneDetailPanel : UserControl
{
    readonly ZonesViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);

    public ZoneDetailPanel(ZonesViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        model.Detail.PropertyChanged += (_, _) => Rebuild();
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingMedium);
        Content = new ScrollViewer { Content = _body };
        Rebuild();
    }

    /// <summary>The buttons of the operations of the zone shown, so a test can press them.</summary>
    public IReadOnlyList<Button> ActionButtons { get; private set; } = [];

    void Rebuild()
    {
        _body.Children.Clear();
        ActionButtons = [];
        if (_model.Detail.Detail is not { } zone)
        {
            _body.Children.Add(new TextBlock { Text = _localizer.Get("Zones.Empty.PickZone"), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary));
            return;
        }

        _body.Children.Add(ThemedText.Title(zone.Name + " · " + _model.StateOf(zone)));
        _body.Children.Add(new TextBlock { Text = _localizer.Get("Zones.Label.ActiveLockersLine", zone.ActiveLockers) }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text));
        var actions = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        var buttons = new List<Button>();
        foreach (var action in _model.Detail.Actions)
        {
            var button = ActionControls.Button(action);
            buttons.Add(button);
            actions.Children.Add(button);
        }

        ActionButtons = buttons;
        _body.Children.Add(actions);
    }
}
