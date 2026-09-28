// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Commands;

/// <summary>
/// Shows the work of an action in progress next to the button that started it: a busy indicator that appears only
/// after 300 ms, the progress "120 de 300" when the operation reports it, and a cancel button that is disabled, with the
/// reason as a tooltip, once cancelling would leave data half done (ux-fonaments, D6).
/// </summary>
public sealed class WorkIndicatorView : UserControl
{
    public WorkIndicatorView(IWorkState work, ILocalizer localizer)
    {
        DataContext = work;

        var bar = new ProgressBar { IsIndeterminate = true, MinWidth = 96, VerticalAlignment = VerticalAlignment.Center };
        bar.Bind(IsVisibleProperty, new Binding(nameof(IWorkState.ShowBusyIndicator)));

        var label = new TextBlock { Text = localizer.Get("Common.Label.Working"), VerticalAlignment = VerticalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        label.Bind(IsVisibleProperty, new Binding(nameof(IWorkState.ShowBusyIndicator)));

        var progress = new TextBlock { VerticalAlignment = VerticalAlignment.Center }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text);
        progress.Bind(TextBlock.TextProperty, new Binding(nameof(IWorkState.ProgressText)));

        Cancel = new Button { Content = localizer.Get("Common.Label.Cancel") };
        Cancel.Bind(Button.IsEnabledProperty, new Binding(nameof(IWorkState.CanCancel)));
        Cancel.Bind(ToolTip.TipProperty, new Binding(nameof(IWorkState.CancelDisabledReason)));
        Cancel.Click += (_, _) => work.Cancel();
        Cancel.Bind(IsVisibleProperty, new Binding(nameof(IWorkState.IsRunning)));

        var row = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        row.Children.Add(bar);
        row.Children.Add(label);
        row.Children.Add(progress);
        row.Children.Add(Cancel);
        Content = row;
    }

    public Button Cancel { get; }
}
