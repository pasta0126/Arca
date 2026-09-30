// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;
using Material.Icons;

namespace Arca.UI.Home;

/// <summary>
/// The start screen as a replaceable piece (pantalla-principal, Inicio como panel de tarjetas): the active year and the cards of the centre,
/// each a filter that opens its screen and counts what it finds, with the operations on it. It reads when it opens and again whenever the
/// state of the application changes, never on a timer.
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

    /// <summary>The buttons that open each card drawn now, in order, so a test can press them.</summary>
    public IReadOnlyList<Button> OpenButtons { get; private set; } = [];

    void Rebuild(StackPanel body)
    {
        body.Children.Clear();
        var open = new List<Button>();
        if (model.IsLoading)
        {
            body.Children.Add(Line(localizer.Get("Common.Loading.Generic"), secondary: true));
            OpenButtons = open;
            return;
        }

        if (model.YearText.Length > 0)
        {
            body.Children.Add(Line(model.YearText));
        }

        if (model.NoActiveYear)
        {
            body.Children.Add(Line(localizer.Get("Shell.Home.Empty.NoYear")));
            body.Children.Add(ActionControls.Button(model.GoToCourse));
        }
        else if (model.NotSetUp)
        {
            body.Children.Add(Line(localizer.Get("Shell.Home.Empty.NotSetUp")));
            var start = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
            start.Children.Add(ActionControls.Button(model.SetUpLockers));
            start.Children.Add(ActionControls.Button(model.AddStudents));
            body.Children.Add(start);
        }

        if (model.NoCards)
        {
            body.Children.Add(Line(localizer.Get("Shell.Home.Empty.NoCards")));
        }
        else
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 12, LineSpacing = 12 };
            foreach (var card in model.Cards)
            {
                var (control, button) = Card(card);
                open.Add(button);
                wrap.Children.Add(control);
            }

            body.Children.Add(wrap);
        }

        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        actions.Children.Add(ActionControls.Button(model.RestoreDefaults));
        body.Children.Add(actions);
        OpenButtons = open;
    }

    static (Border Control, Button Open) Card(StartHomeCard card)
    {
        var count = new TextBlock { Text = card.CountText, FontWeight = Avalonia.Media.FontWeight.SemiBold }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeHeading);
        var title = new TextBlock { Text = card.View.Title, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 170 }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        var content = new StackPanel { MinWidth = 150 }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        content.Children.Add(count);
        content.Children.Add(title);
        if (card.StateText.Length > 0)
        {
            content.Children.Add(new TextBlock { Text = "⚠ " + card.StateText, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 170 }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeSmall));
        }

        var open = new Button { Content = content, Command = card.Open, HorizontalContentAlignment = HorizontalAlignment.Left };
        Describe(open, card.Open);

        var tools = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        tools.Children.Add(Tool(card.MoveEarlier, MaterialIconKind.ChevronLeft));
        tools.Children.Add(Tool(card.MoveLater, MaterialIconKind.ChevronRight));
        tools.Children.Add(Tool(card.Delete, MaterialIconKind.TrashCanOutline));
        var box = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        box.Children.Add(open);
        box.Children.Add(tools);
        return (new Border { Child = box }, open);
    }

    static Button Tool(AppAction action, MaterialIconKind icon)
    {
        var button = new Button { Content = ThemedIcon.Create(icon, 16), Command = action };
        Describe(button, action);
        return button;
    }

    /// <summary>The action as the tooltip and the name a screen reader announces, with the reason when it is disabled.</summary>
    static void Describe(Button button, AppAction action)
    {
        ToolTip.SetTip(button, action.ToolTipText);
        Avalonia.Automation.AutomationProperties.SetName(button, action.Label);
    }

    static TextBlock Line(string text, bool secondary = false) => new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        .Themed(TextBlock.ForegroundProperty, secondary ? ArcaResourceKeys.TextSecondary : ArcaResourceKeys.Text)
        .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
}
