// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.ConceptAmounts;
using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Screens;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Charges;

/// <summary>
/// The charges of a student as a control: the summary of their standing, the key replacement, the list of every charge of any year and,
/// under it, the charge chosen with its operations (each disabled with its reason when it does not apply) and its read-only history.
/// The record of a student and the Payments section show the same control over the same model.
/// </summary>
public sealed class StudentChargesPanel : UserControl
{
    readonly StudentChargesViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _detail = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);

    public StudentChargesPanel(StudentChargesViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        var summary = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        summary.Bind(TextBlock.TextProperty, new Binding(nameof(StudentChargesViewModel.Summary)) { Source = model });
        KeyButton = ActionControls.Button(model.KeyReplacement);
        KeyButton.HorizontalAlignment = HorizontalAlignment.Left;
        var list = new ScreenListView<ChargeLine, Guid>(model.Charges, localizer) { MinHeight = 160 };

        var layout = new DockPanel();
        var top = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        top.Children.Add(summary);
        top.Children.Add(KeyButton);
        DockPanel.SetDock(top, Dock.Top);
        var below = new ScrollViewer { Content = _detail, MaxHeight = 260 };
        DockPanel.SetDock(below, Dock.Bottom);
        layout.Children.Add(top);
        layout.Children.Add(below);
        layout.Children.Add(list);
        Content = layout;
        model.Detail.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
    }

    public Button KeyButton { get; }

    /// <summary>The buttons of the operations of the charge chosen, so a test can press them.</summary>
    public IReadOnlyList<Button> ActionButtons { get; private set; } = [];

    void Rebuild()
    {
        _detail.Children.Clear();
        ActionButtons = [];
        if (_model.Detail.Detail is not { } line)
        {
            return;
        }

        _detail.Children.Add(ThemedText.Title(ConceptNames.Of(_localizer, line.Concept) + " · " + line.YearName));
        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        var buttons = new List<Button>();
        foreach (var action in _model.Detail.Actions)
        {
            var button = ActionControls.Button(action);
            buttons.Add(button);
            actions.Children.Add(button);
        }

        ActionButtons = buttons;
        _detail.Children.Add(actions);
        var history = new Button { Content = _localizer.Get("Charges.Action.History"), HorizontalAlignment = HorizontalAlignment.Left };
        history.Click += async (_, _) => await _model.Detail.LoadHistoryAsync();
        _detail.Children.Add(history);
        if (_model.Detail.HistoryLoaded)
        {
            foreach (var entry in _model.Detail.History)
            {
                _detail.Children.Add(new TextBlock { Text = "• " + entry, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                    .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text));
            }
        }
    }
}
