// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Charges;

/// <summary>
/// The charges of a student as rows inside their record (pantalles-alumnes-i-assignacions, Ficha del alumno): each one says its concept,
/// year and amount, its state in words, and offers its operations in the same row: the main one, Marcar com a pagat, as a button and
/// the rest, with the history of the charge, in a menu, each disabled with its reason when it does not apply. The record shows the
/// pending ones open and, in a block that folds, all of them. No rule of its own: what can be done is what the model says.
/// </summary>
public sealed class ChargeRowsPanel : UserControl
{
    readonly StudentChargesViewModel _model;
    readonly ILocalizer _localizer;
    readonly bool _pendingOnly;
    readonly StackPanel _rows = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);

    /// <param name="pendingOnly">Show only the charges still pending; otherwise every charge of any year.</param>
    public ChargeRowsPanel(StudentChargesViewModel model, ILocalizer localizer, bool pendingOnly)
    {
        _model = model;
        _localizer = localizer;
        _pendingOnly = pendingOnly;
        Content = _rows;
        model.Charges.List.PropertyChanged += (_, _) => Rebuild();
        model.PropertyChanged += (_, _) => Rebuild();
        model.Detail.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
    }

    /// <summary>The charges shown now, in the order they are drawn.</summary>
    public IReadOnlyList<ChargeLine> Lines => [.. Visible()];

    /// <summary>The main buttons of each row (Marcar com a pagat), so a test can press them.</summary>
    public IReadOnlyList<Button> PayButtons { get; private set; } = [];

    IEnumerable<ChargeLine> Visible() => _model.Charges.List.Rows.Where(l => !_pendingOnly || l.Status == "Pending");

    void Rebuild()
    {
        _rows.Children.Clear();
        var pay = new List<Button>();
        if (_model.Charges.State.IsLoading)
        {
            _rows.Children.Add(Text(_localizer.Get("Common.Loading.Generic"), secondary: true));
        }

        foreach (var line in Visible())
        {
            _rows.Children.Add(Row(line, pay));
        }

        PayButtons = pay;
    }

    Border Row(ChargeLine line, List<Button> pay)
    {
        var box = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        box.Children.Add(new TextBlock { Text = _model.ConceptText(line) + " · " + line.YearName + " · " + _model.AmountText(line), FontWeight = Avalonia.Media.FontWeight.SemiBold, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody));
        var detail = _model.StatusText(line)
            + (line.PaidOn is { } paid ? " · " + _localizer.Format(paid) : string.Empty)
            + (line.Reason is { Length: > 0 } reason ? " · " + reason : string.Empty);
        box.Children.Add(Text(detail, secondary: true));

        var actions = _model.ActionsFor(line);
        var buttons = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        if (actions.FirstOrDefault(a => a.Id == "Pay") is { } main && line.Status == "Pending")
        {
            var button = ActionControls.Button(main);
            pay.Add(button);
            buttons.Children.Add(button);
        }

        var menu = new MenuFlyout();
        foreach (var action in actions.Where(a => a.Id != "Pay" || line.Status != "Pending"))
        {
            menu.Items.Add(ActionControls.MenuItem(action));
        }

        var history = new MenuItem { Header = _localizer.Get("Charges.Action.History") };
        history.Click += async (_, _) => await _model.ShowHistoryAsync(line);
        menu.Items.Add(history);
        buttons.Children.Add(new Button { Content = _localizer.Get("Charges.Action.More"), Flyout = menu });
        box.Children.Add(buttons);

        if (_model.Detail.Detail?.Id == line.Id && _model.Detail.HistoryLoaded)
        {
            foreach (var entry in _model.Detail.History)
            {
                box.Children.Add(Text("• " + entry));
            }
        }

        var border = new Border { Child = box, Padding = new Avalonia.Thickness(8) };
        border.Classes.Add("charge-row");
        return border;
    }

    static TextBlock Text(string text, bool secondary = false) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
