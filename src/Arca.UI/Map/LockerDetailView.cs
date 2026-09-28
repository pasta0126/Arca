// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Arca.UI.Map;

/// <summary>
/// The panel of the chosen locker: its number, zone and status (colour, icon and word), its student with level, group and
/// payment state (never their email or identifier), its notes, and its actions as buttons, the unavailable ones disabled with
/// their reason in the tooltip. With no locker chosen it tells the person to choose one.
/// </summary>
public sealed class LockerDetailView : UserControl
{
    readonly LockerDetailViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _body = new();
    readonly WrapPanel _actions = new();

    public LockerDetailView(LockerDetailViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        _body.Bind(StackPanel.SpacingProperty, this.GetResourceObservable(ArcaResourceKeys.SpacingMedium));
        Content = new Border { Child = _body }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Surface)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);
        model.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
    }

    /// <summary>The buttons of the actions on screen, in order.</summary>
    public IReadOnlyList<Button> ActionButtons => [.. _actions.Children.OfType<Button>()];

    /// <summary>Every text on the panel, so a test can read what it says.</summary>
    public IReadOnlyList<string> Texts => [.. _body.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty)];

    void Rebuild()
    {
        _body.Children.Clear();
        _actions.Children.Clear();
        _body.Children.Add(ThemedText.Title(_localizer.Get("Shell.Detail.Title")));
        if (_model.Detail is not { } detail)
        {
            _body.Children.Add(Line(_localizer.Get("Shell.Detail.Nothing"), secondary: true));
            return;
        }

        _body.Children.Add(Line($"{_localizer.Get("Shell.Search.LockerNumber", detail.Number)} · {detail.ZoneName}"));
        var status = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        status.Children.Add(ThemedIcon.Create(LockerStatusPresentation.Icon(detail.Status), 18));
        status.Children.Add(Line(LockerStatusPresentation.Text(detail.Status, _localizer)));
        _body.Children.Add(status);

        if (detail.StudentName is not null)
        {
            _body.Children.Add(Line(detail.StudentName));
            var group = string.Join(' ', new[] { detail.LevelName, detail.GroupName }.Where(x => !string.IsNullOrEmpty(x)));
            if (group.Length > 0)
            {
                _body.Children.Add(Line(group, secondary: true));
            }

            _body.Children.Add(Line(detail.HasDebt ? _localizer.Get("Shell.Detail.Owes", detail.PendingTotal) : _localizer.Get("Shell.Detail.UpToDate"), secondary: true));
        }

        if (!string.IsNullOrWhiteSpace(detail.Note ?? detail.ReservationNote))
        {
            _body.Children.Add(Line(detail.Note ?? detail.ReservationNote!, secondary: true));
        }

        foreach (var action in _model.Actions)
        {
            var button = ActionControls.Button(action);
            button.Margin = new Avalonia.Thickness(0, 0, 6, 6);
            _actions.Children.Add(button);
        }

        _body.Children.Add(_actions);
    }

    static TextBlock Line(string text, bool secondary = false) =>
        new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
