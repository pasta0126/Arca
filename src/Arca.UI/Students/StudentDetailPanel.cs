// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Charges;
using Arca.UI.Common;
using Arca.UI.Layout;
using Arca.UI.Preferences;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Students;

/// <summary>
/// The record of the student chosen (pantalles-alumnes-i-assignacions, Ficha del alumno), in one column and without tabs: a header that
/// always shows their state (locker and payment) and the actions that apply, each disabled with its reason when it does not, the pending
/// charges open with their operations in the same row (or that the student is up to date), and three blocks that fold and start
/// folded: the history of payments, the data and the history of activity. Each block remembers whether the person keeps it open, and
/// the history of activity loads only when it is opened.
/// </summary>
public sealed class StudentDetailPanel : UserControl
{
    readonly StudentsViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
    readonly StackPanel _header = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
    readonly StackPanel _pendingTop = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
    readonly StackPanel _dataBody = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
    readonly StackPanel _activityBody = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
    readonly StackPanel _detailArea = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
    readonly TextBlock _empty;
    Guid? _historyAskedFor;

    public StudentDetailPanel(StudentsViewModel model, ILocalizer localizer, UiPreferencesSession preferences)
    {
        _model = model;
        _localizer = localizer;
        _empty = Line(localizer.Get("Students.Empty.PickStudent"), secondary: true);

        Pending = new ChargeRowsPanel(model.Charges, localizer, pendingOnly: true);
        Payments = new ChargeRowsPanel(model.Charges, localizer, pendingOnly: false);
        PaymentsSection = new CollapsibleSectionViewModel(
            "student.payments", localizer.Get("Students.Section.Payments"), () => localizer.Get("Students.Label.PaymentCount", Payments.Lines.Count), preferences, expandedByDefault: false);
        DataSection = new CollapsibleSectionViewModel("student.data", localizer.Get("Students.Section.Data"), () => string.Empty, preferences, expandedByDefault: false);
        ActivitySection = new CollapsibleSectionViewModel("student.activity", localizer.Get("Students.Section.Activity"), () => string.Empty, preferences, expandedByDefault: false);
        ActivitySection.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(CollapsibleSectionViewModel.IsExpanded) && ActivitySection.IsExpanded && !_model.Detail.HistoryLoaded && _model.Detail.Detail is { } shown)
            {
                _historyAskedFor = shown.Student.Id;
                await _model.Detail.LoadHistoryAsync();
            }
        };

        var keyButton = ActionControls.Button(model.Charges.KeyReplacement);
        keyButton.HorizontalAlignment = HorizontalAlignment.Left;
        KeyButton = keyButton;
        var pendingTitle = ThemedText.Title(localizer.Get("Students.Section.Pending"));
        var upToDate = Line(string.Empty);
        _pendingTop.Children.Add(pendingTitle);

        _detailArea.Children.Add(_header);
        _detailArea.Children.Add(_pendingTop);
        _detailArea.Children.Add(Pending);
        _detailArea.Children.Add(new CollapsibleSectionView(PaymentsSection, Payments));
        _detailArea.Children.Add(new CollapsibleSectionView(DataSection, _dataBody));
        _detailArea.Children.Add(new CollapsibleSectionView(ActivitySection, _activityBody));
        _body.Children.Add(_empty);
        _body.Children.Add(_detailArea);
        UpToDateText = upToDate;
        _pendingTop.Children.Add(upToDate);
        _pendingTop.Children.Add(keyButton);

        model.Detail.PropertyChanged += (_, _) => Rebuild();
        model.Charges.Charges.List.PropertyChanged += (_, _) => ShowPending();
        model.Charges.PropertyChanged += (_, _) => ShowPending();
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingMedium);
        Content = new ScrollViewer { Content = _body };
        Rebuild();
    }

    /// <summary>The buttons of the actions of the student shown, so a test can press them.</summary>
    public IReadOnlyList<Button> ActionButtons { get; private set; } = [];

    /// <summary>The pending charges, open, each with its operations in its row.</summary>
    public ChargeRowsPanel Pending { get; }

    /// <summary>Every charge of the student of any year, in the block that folds.</summary>
    public ChargeRowsPanel Payments { get; }

    public CollapsibleSectionViewModel PaymentsSection { get; }

    public CollapsibleSectionViewModel DataSection { get; }

    public CollapsibleSectionViewModel ActivitySection { get; }

    /// <summary>The key replacement action, which belongs to the student and not to one charge.</summary>
    public Button KeyButton { get; }

    /// <summary>What the student stands at: up to date, or that they have no charges yet. Empty while there are pending charges.</summary>
    public TextBlock UpToDateText { get; }

    void Rebuild()
    {
        _header.Children.Clear();
        ActionButtons = [];
        if (_model.Detail.Detail is not { } detail)
        {
            _empty.IsVisible = true;
            _detailArea.IsVisible = false;
            return;
        }

        _empty.IsVisible = false;
        _detailArea.IsVisible = true;
        var student = detail.Student;
        _header.Children.Add(ThemedText.Title(student.FirstName + " " + student.LastName + (student.IsRetired ? " · " + _localizer.Get("Students.State.Retired") : string.Empty)));
        _header.Children.Add(Line(_localizer.Get("Students.Label.LevelLine", student.LevelName ?? "—", student.GroupName ?? "—")));
        _header.Children.Add(Line(student.LockerNumber is { } number
            ? _localizer.Get("Students.Label.LockerLine", number)
            : _localizer.Get("Students.Label.NoLockerLine")));
        _header.Children.Add(Line(detail.HasDebt
            ? _localizer.Get("Students.Label.PaymentLineDebt", _localizer.Format(Arca.Domain.Common.Money.FromCents((long)Math.Round(detail.PendingTotal * 100))))
            : _localizer.Get("Students.Label.PaymentLineUpToDate")));

        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        var buttons = new List<Button>();
        foreach (var action in _model.Detail.Actions)
        {
            var button = ActionControls.Button(action);
            buttons.Add(button);
            actions.Children.Add(button);
        }

        ActionButtons = buttons;
        _header.Children.Add(actions);

        _dataBody.Children.Clear();
        _dataBody.Children.Add(Line(_localizer.Get("Students.Label.NameLine", student.FirstName, student.LastName)));
        _dataBody.Children.Add(Line(_localizer.Get("Students.Label.LevelLine", student.LevelName ?? "—", student.GroupName ?? "—")));
        _dataBody.Children.Add(Line(_localizer.Get("Students.Label.EmailLine", student.Email)));
        if (student.IsRetired && student.RetirementReason is { Length: > 0 } reason)
        {
            _dataBody.Children.Add(Line(_localizer.Get("Students.Label.RetirementLine", reason)));
        }

        _activityBody.Children.Clear();
        if (!_model.Detail.HistoryLoaded)
        {
            _activityBody.Children.Add(Line(_localizer.Get("Common.Loading.Generic"), secondary: true));
            if (ActivitySection.IsExpanded && _historyAskedFor != student.Id)
            {
                _historyAskedFor = student.Id; // once per student: loading it announces a change that rebuilds this record again
                _ = _model.Detail.LoadHistoryAsync(); // an open block follows the student chosen
            }
        }
        else if (_model.Detail.History.Count == 0)
        {
            _activityBody.Children.Add(Line(_localizer.Get("Students.Empty.NoHistory"), secondary: true));
        }
        else
        {
            foreach (var line in _model.Detail.History)
            {
                _activityBody.Children.Add(Line("• " + line));
            }
        }

        ShowPending();
    }

    /// <summary>Shows the heading of the pending charges as what it is: the charges themselves, or that the student is up to date, or has none.</summary>
    void ShowPending()
    {
        var anyPending = Pending.Lines.Count > 0;
        var loaded = _model.Charges.Screen is not null;
        UpToDateText.IsVisible = loaded && !anyPending;
        UpToDateText.Text = !loaded ? string.Empty
            : _model.Charges.Charges.List.TotalCount == 0 ? _localizer.Get("Charges.Empty.NoCharges")
            : _model.Charges.Summary;
    }

    static TextBlock Line(string text, bool secondary = false) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
