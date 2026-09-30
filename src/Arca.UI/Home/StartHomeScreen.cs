// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Home;

/// <summary>
/// The start screen as a replaceable piece (pantalla-principal, Inicio como pantalla registrable): the active year and the counts of lockers
/// and students, each a link that opens its section already filtered. It reads the summary when it opens and again whenever the state of
/// the application changes, never on a timer.
/// </summary>
public sealed class StartHomeScreen(StartHomeModel model, GlobalStateService state, ILocalizer localizer) : IHomeScreen
{
    public Control Create()
    {
        var body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingLarge);
        var screen = new ScreenView(localizer.Get("Shell.Screen.Start"), [], body);
        void Show() => Rebuild(body);
        model.PropertyChanged += (_, _) => Show();
        EventHandler changed = (_, _) => _ = model.LoadAsync();
        screen.AttachedToVisualTree += (_, _) =>
        {
            state.Changed += changed;
            _ = model.LoadAsync();
        };
        screen.DetachedFromVisualTree += (_, _) => state.Changed -= changed;
        Show();
        return screen;
    }

    void Rebuild(StackPanel body)
    {
        body.Children.Clear();
        switch (model.State)
        {
            case StartHomeState.Loading:
                body.Children.Add(Line(localizer.Get("Common.Loading.Generic"), secondary: true));
                break;
            case StartHomeState.NoActiveYear:
                body.Children.Add(Line(localizer.Get("Shell.Home.Empty.NoYear")));
                body.Children.Add(ActionControls.Button(model.GoToCourse));
                break;
            case StartHomeState.NotSetUp:
                body.Children.Add(Line(model.YearText));
                body.Children.Add(Line(localizer.Get("Shell.Home.Empty.NotSetUp")));
                var start = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
                start.Children.Add(ActionControls.Button(model.SetUpLockers));
                start.Children.Add(ActionControls.Button(model.AddStudents));
                body.Children.Add(start);
                break;
            default:
                body.Children.Add(Line(model.YearText));
                body.Children.Add(Group(localizer.Get("Shell.Section.Lockers"), model.LockerLinks));
                body.Children.Add(Group(localizer.Get("Shell.Section.Students"), model.StudentLinks));
                break;
        }
    }

    static StackPanel Group(string title, IReadOnlyList<StartHomeLink> links)
    {
        var group = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        group.Children.Add(ThemedText.Title(title));
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 12, LineSpacing = 12 };
        foreach (var link in links)
        {
            var count = new TextBlock { Text = link.Count.ToString(System.Globalization.CultureInfo.CurrentCulture), FontWeight = Avalonia.Media.FontWeight.SemiBold }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeHeading);
            var text = new TextBlock { Text = link.Text, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 160 }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
            var content = new StackPanel { MinWidth = 140 }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
            content.Children.Add(count);
            content.Children.Add(text);
            var button = new Button { Content = content, Command = link.Open, HorizontalContentAlignment = HorizontalAlignment.Left };
            ToolTip.SetTip(button, link.Open.ToolTipText);
            Avalonia.Automation.AutomationProperties.SetName(button, link.Open.Label);
            wrap.Children.Add(button);
        }

        group.Children.Add(wrap);
        return group;
    }

    static TextBlock Line(string text, bool secondary = false) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
