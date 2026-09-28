// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace Arca.UI.Notifications;

/// <summary>
/// The stack of notifications on screen: one card each, newest at the bottom, none covering another. A card has the text,
/// a close button and, when there are technical details, a toggle to show them. A success waits while the mouse is over it.
/// The colour of each kind comes from the theme, and never carries the meaning alone: a symbol goes with it.
/// </summary>
public sealed class NotificationHostView : UserControl
{
    readonly NotificationCenter _center;
    readonly ILocalizer _localizer;

    public NotificationHostView(NotificationCenter center, ILocalizer localizer)
    {
        _center = center;
        _localizer = localizer;
        Content = new ItemsControl
        {
            ItemsSource = center.Visible,
            ItemTemplate = new FuncDataTemplate<Notification>((notification, _) => notification is null ? null : Card(notification), supportsRecycling: false),
        }.Themed(ItemsControl.MarginProperty, ArcaResourceKeys.SpacingMedium);
        MaxWidth = 420;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
    }

    Border Card(Notification notification)
    {
        var text = new TextBlock { Text = Symbol(notification.Kind) + " " + notification.Text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.OnSemantic)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        var close = new Button { Content = _localizer.Get("Common.Label.Dismiss"), HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => _center.Dismiss(notification.Id);

        var body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        body.Children.Add(text);
        if (notification.Details is { } details)
        {
            var detailsText = new TextBlock { Text = details, IsVisible = false, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.OnSemantic)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeSmall);
            var toggle = new ToggleButton { Content = _localizer.Get("Common.Label.Details"), HorizontalAlignment = HorizontalAlignment.Left };
            toggle.IsCheckedChanged += (_, _) => detailsText.IsVisible = toggle.IsChecked == true;
            body.Children.Add(toggle);
            body.Children.Add(detailsText);
        }

        body.Children.Add(close);
        var card = new Border { Child = body, CornerRadius = new Avalonia.CornerRadius(6) }
            .Themed(Border.BackgroundProperty, BrushOf(notification.Kind))
            .Themed(Border.BorderBrushProperty, ArcaResourceKeys.Border)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium)
            .ThemedThickness(Border.MarginProperty, ArcaResourceKeys.SpacingSmall);
        card.BorderThickness = new Avalonia.Thickness(1);
        card.PointerEntered += (_, _) => _center.Pause(notification.Id);
        card.PointerExited += (_, _) => _center.Resume(notification.Id);
        return card;
    }

    static string BrushOf(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => ArcaResourceKeys.Success,
        NotificationKind.Warning => ArcaResourceKeys.Warning,
        _ => ArcaResourceKeys.Error,
    };

    static string Symbol(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => "✓",
        NotificationKind.Warning => "!",
        _ => "✕",
    };
}
