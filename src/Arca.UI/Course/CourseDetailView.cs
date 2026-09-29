// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Course;

/// <summary>
/// The detail of the year chosen (pantalles-curs-i-imports): its name, dates and state, whether it is history, its three amounts,
/// the actions that apply (each disabled with its reason when it does not) and the history of its amounts, which is fetched only
/// when the person opens it. A year that is not active is shown as a consultation.
/// </summary>
public sealed class CourseDetailView : UserControl
{
    readonly CourseViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);

    public CourseDetailView(CourseViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        model.Detail.PropertyChanged += (_, _) => Rebuild();
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingMedium);
        Content = new ScrollViewer { Content = _body };
        Rebuild();
    }

    /// <summary>The buttons of the actions of the year shown, so a test can press them.</summary>
    public IReadOnlyList<Button> ActionButtons { get; private set; } = [];

    /// <summary>The button that opens the history of the amounts, when a year is shown.</summary>
    public Button? HistoryButton { get; private set; }

    void Rebuild()
    {
        _body.Children.Clear();
        ActionButtons = [];
        HistoryButton = null;
        if (_model.Detail.Detail is not { } detail)
        {
            _body.Children.Add(new TextBlock { Text = _localizer.Get("Course.Empty.PickYear"), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary));
            return;
        }

        var year = detail.Year.Year;
        _body.Children.Add(ThemedText.Title(year.Name + " · " + _model.StateOf(year)));
        _body.Children.Add(Line(_localizer.Get("Course.Label.Dates", _localizer.Format(year.StartDate), _localizer.Format(year.EndDate))));
        if (detail.Year.IsHistoric)
        {
            _body.Children.Add(ThemedText.WarningNote(_localizer.Get("Course.Note.Historic")));
        }

        _body.Children.Add(ThemedText.Small(_localizer.Get("Course.Label.Amounts")));
        var amounts = detail.Amounts;
        foreach (var (concept, value) in new[] { ("Fee", amounts.Fee), ("Deposit", amounts.Deposit), ("KeyReplacementFee", amounts.KeyReplacementFee) })
        {
            var shown = amounts.IsProposed || value is null ? _localizer.Get("Course.Label.NotDefined") : _localizer.Format(Arca.Domain.Common.Money.FromCents((long)(value.Value * 100)));
            var name = ConceptNames.Of(_localizer, concept);
            _body.Children.Add(Line(_localizer.Get("Course.Label.AmountLine", char.ToUpper(name[0], _localizer.Culture) + name[1..], shown)));
        }

        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        var buttons = new List<Button>();
        foreach (var action in _model.Detail.Actions)
        {
            var button = ActionControls.Button(action);
            buttons.Add(button);
            actions.Children.Add(button);
        }

        ActionButtons = buttons;
        _body.Children.Add(actions);

        HistoryButton = new Button { Content = _localizer.Get("Course.Action.AmountsHistory"), HorizontalAlignment = HorizontalAlignment.Left };
        HistoryButton.Click += async (_, _) => await _model.Detail.LoadHistoryAsync();
        _body.Children.Add(HistoryButton);
        if (_model.Detail.HistoryLoaded)
        {
            if (_model.Detail.History.Count == 0)
            {
                _body.Children.Add(Line(_localizer.Get("Course.Empty.NoHistory")));
            }

            foreach (var entry in _model.Detail.History)
            {
                _body.Children.Add(Line("• " + entry));
            }
        }
    }

    static TextBlock Line(string text) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
