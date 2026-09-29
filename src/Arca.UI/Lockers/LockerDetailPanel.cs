// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Lockers;

/// <summary>
/// The detail of the locker chosen: its status, zone and notes, its student with the mark of debt, the actions that apply (each
/// disabled with its reason when it does not) and its history, which is fetched only when the person opens it. A retired locker
/// shows its history and no action that changes it.
/// </summary>
public sealed class LockerDetailPanel : UserControl
{
    readonly LockersViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);

    public LockerDetailPanel(LockersViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        model.Detail.PropertyChanged += (_, _) => Rebuild();
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingMedium);
        Content = new ScrollViewer { Content = _body };
        Rebuild();
    }

    /// <summary>The buttons of the actions of the locker shown, so a test can press them.</summary>
    public IReadOnlyList<Button> ActionButtons { get; private set; } = [];

    public Button? HistoryButton { get; private set; }

    void Rebuild()
    {
        _body.Children.Clear();
        ActionButtons = [];
        HistoryButton = null;
        if (_model.Detail.Detail is not { } detail)
        {
            _body.Children.Add(Line(_localizer.Get("Lockers.Empty.PickLocker"), secondary: true));
            return;
        }

        var row = detail.Row;
        _body.Children.Add(ThemedText.Title(_localizer.Get("Lockers.Label.LockerTitle", row.Number, row.ZoneName)));
        _body.Children.Add(Line(_localizer.Get("Lockers.Label.StatusLine", LockerStatusPresentation.Text(row.Status, _localizer))));
        if (row.StudentName is not null)
        {
            _body.Children.Add(Line(_localizer.Get("Lockers.Label.StudentLine", row.StudentName)));
            _body.Children.Add(Line(_localizer.Get(row.HasDebt ? "Lockers.Label.PaymentDebt" : "Lockers.Label.PaymentUpToDate")));
        }

        if (row.ReservationNote is { Length: > 0 } reservation)
        {
            _body.Children.Add(Line(_localizer.Get("Lockers.Label.ReservationLine", reservation)));
        }

        if (row.Note is { Length: > 0 } note)
        {
            _body.Children.Add(Line(_localizer.Get("Lockers.Label.NoteLine", note)));
        }

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
        HistoryButton = new Button { Content = _localizer.Get("Lockers.Action.History"), HorizontalAlignment = HorizontalAlignment.Left };
        HistoryButton.Click += async (_, _) => await _model.Detail.LoadHistoryAsync();
        _body.Children.Add(HistoryButton);
        if (_model.Detail.HistoryLoaded)
        {
            foreach (var entry in _model.Detail.History)
            {
                _body.Children.Add(Line("• " + entry));
            }
        }
    }

    static TextBlock Line(string text, bool secondary = false) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
