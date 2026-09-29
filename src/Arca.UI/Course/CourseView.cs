// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Course;

/// <summary>Builds the Course section with the common structure of a screen: title and actions, the list with its notice, and the detail.</summary>
public static class CourseView
{
    public static ScreenView Create(CourseViewModel model, ILocalizer localizer)
    {
        var list = new ScreenListView<Arca.Application.SchoolYears.AcademicYearSummary, Guid>(model.Years, localizer);
        var notice = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        notice.Bind(Control.IsVisibleProperty, new Binding(nameof(CourseViewModel.HasMissingAmountsNotice)) { Source = model });
        var text = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        text.Bind(TextBlock.TextProperty, new Binding(nameof(CourseViewModel.MissingAmountsNotice)) { Source = model });
        var define = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CourseViewModel.DefineActiveAmounts))
            {
                define.Content = model.DefineActiveAmounts.Label;
                define.Command = model.DefineActiveAmounts;
            }
        };
        notice.Children.Add(new Border { Child = text, BorderThickness = new Avalonia.Thickness(2, 0, 0, 0) }
            .Themed(Border.BorderBrushProperty, ArcaResourceKeys.Warning).ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium));
        notice.Children.Add(define);

        var left = new DockPanel();
        DockPanel.SetDock(notice, Dock.Top);
        left.Children.Add(notice);
        left.Children.Add(list);
        var screen = new ScreenView(localizer.Get("Shell.Section.Course"), model.MainActions, left, new CourseDetailView(model, localizer));
        IDisposable? shortcut = null;
        screen.AttachedToVisualTree += (_, _) =>
        {
            shortcut = model.StandardNew.Attach(() => model.NewYear.Execute(null)); // Control or Command + N, only while this section is open
            _ = model.LoadAsync();
        };
        screen.DetachedFromVisualTree += (_, _) => shortcut?.Dispose();
        return screen;
    }
}
