// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Shell;

/// <summary>
/// The notices about the state of the application as banners under the header: the text, the button that deals with it and,
/// when it only informs, a button to close it. They never block the work and take no room when there is nothing to say.
/// </summary>
public sealed class NoticeBarView : UserControl
{
    readonly GlobalNoticesViewModel _model;
    readonly ILocalizer _localizer;
    readonly StackPanel _banners = new();

    public NoticeBarView(GlobalNoticesViewModel model, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        Content = _banners;
        model.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
    }

    /// <summary>The banners on screen now.</summary>
    public IReadOnlyList<Border> Banners => [.. _banners.Children.OfType<Border>()];

    void Rebuild()
    {
        _banners.Children.Clear();
        foreach (var notice in _model.Notices)
        {
            var text = new TextBlock { Text = notice.Text, TextWrapping = Avalonia.Media.TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.OnSemantic)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
            var act = new Button { Content = notice.ActionLabel };
            var run = notice.Act;
            act.Click += (_, _) => run();
            var buttons = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
            buttons.Children.Add(act);
            if (notice.CanDismiss)
            {
                var close = new Button { Content = _localizer.Get("Common.Label.Dismiss") };
                var id = notice.Id;
                close.Click += (_, _) => _model.Dismiss(id);
                buttons.Children.Add(close);
            }

            var row = new DockPanel();
            DockPanel.SetDock(buttons, Dock.Right);
            row.Children.Add(buttons);
            row.Children.Add(text);
            _banners.Children.Add(new Border { Child = row }
                .Themed(Border.BackgroundProperty, ArcaResourceKeys.Warning)
                .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium));
        }
    }
}
