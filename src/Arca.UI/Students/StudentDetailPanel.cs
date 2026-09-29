// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Students;

/// <summary>
/// The record of the student chosen (pantalles-alumnes-i-assignacions, Ficha del alumno): a header that always shows their
/// state (locker and payment), the actions that apply, each disabled with its reason when it does not, and the tabs Data, Locker
/// and History, which load only when opened.
/// </summary>
public sealed class StudentDetailPanel : UserControl
{
    readonly StudentsViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
    int _tab;

    public StudentDetailPanel(StudentsViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        model.Detail.PropertyChanged += (_, _) => Rebuild();
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(StudentsViewModel.LockerLines) or nameof(StudentsViewModel.LockerLinesLoaded))
            {
                Rebuild();
            }
        };
        this.ThemedThickness(PaddingProperty, ArcaResourceKeys.SpacingMedium);
        Content = new ScrollViewer { Content = _body };
        Rebuild();
    }

    /// <summary>The buttons of the actions of the student shown, so a test can press them.</summary>
    public IReadOnlyList<Button> ActionButtons { get; private set; } = [];

    /// <summary>The tab control of the record, when a student is shown.</summary>
    public TabControl? Tabs { get; private set; }

    void Rebuild()
    {
        _body.Children.Clear();
        ActionButtons = [];
        Tabs = null;
        if (_model.Detail.Detail is not { } detail)
        {
            _body.Children.Add(Line(_localizer.Get("Students.Empty.PickStudent"), secondary: true));
            return;
        }

        var student = detail.Student;
        _body.Children.Add(ThemedText.Title(student.FirstName + " " + student.LastName + (student.IsRetired ? " · " + _localizer.Get("Students.State.Retired") : string.Empty)));
        _body.Children.Add(Line(student.LockerNumber is { } number
            ? _localizer.Get("Students.Label.LockerLine", number)
            : _localizer.Get("Students.Label.NoLockerLine")));
        _body.Children.Add(Line(detail.HasDebt
            ? _localizer.Get("Students.Label.PaymentLineDebt", _localizer.Format(Arca.Domain.Common.Money.FromCents((long)Math.Round(detail.PendingTotal * 100))))
            : _localizer.Get("Students.Label.PaymentLineUpToDate")));

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

        var tabs = new TabControl { SelectedIndex = _tab };
        tabs.Items.Add(new TabItem { Header = _localizer.Get("Students.Tab.Data"), Content = DataTab(detail) });
        tabs.Items.Add(new TabItem { Header = _localizer.Get("Students.Tab.Locker"), Content = ListTab(_model.LockerLines, _model.LockerLinesLoaded) });
        tabs.Items.Add(new TabItem { Header = _localizer.Get("Students.Tab.History"), Content = ListTab(_model.Detail.History, _model.Detail.HistoryLoaded) });
        tabs.SelectionChanged += async (_, e) =>
        {
            if (e.Source != tabs)
            {
                return;
            }

            _tab = tabs.SelectedIndex;
            if (_tab == 1 && !_model.LockerLinesLoaded)
            {
                await _model.LoadLockerLinesAsync();
            }
            else if (_tab == 2 && !_model.Detail.HistoryLoaded)
            {
                await _model.Detail.LoadHistoryAsync();
            }
        };
        Tabs = tabs;
        _body.Children.Add(tabs);
    }

    StackPanel DataTab(Application.Students.GetStudentScreen.StudentScreenDetail detail)
    {
        var student = detail.Student;
        var panel = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        panel.Children.Add(Line(_localizer.Get("Students.Label.NameLine", student.FirstName, student.LastName)));
        panel.Children.Add(Line(_localizer.Get("Students.Label.LevelLine", student.LevelName ?? "—", student.GroupName ?? "—")));
        panel.Children.Add(Line(_localizer.Get("Students.Label.EmailLine", student.Email)));
        if (student.IsRetired && student.RetirementReason is { Length: > 0 } reason)
        {
            panel.Children.Add(Line(_localizer.Get("Students.Label.RetirementLine", reason)));
        }

        return panel;
    }

    StackPanel ListTab(IReadOnlyList<string> lines, bool loaded)
    {
        var panel = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        if (!loaded)
        {
            panel.Children.Add(Line(_localizer.Get("Common.Loading.Generic"), secondary: true));
        }
        else if (lines.Count == 0)
        {
            panel.Children.Add(Line(_localizer.Get("Students.Empty.NoHistory"), secondary: true));
        }

        foreach (var line in lines)
        {
            panel.Children.Add(Line("• " + line));
        }

        return panel;
    }

    static TextBlock Line(string text, bool secondary = false) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
