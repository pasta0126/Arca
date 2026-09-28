// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;

namespace Arca.UI.Shell;

/// <summary>
/// What a section shows while it has no screen of its own: its title and the list of what it will hold, so the person always
/// finds an answer to "where is this?" and never a blank area. It is replaced by giving the section a root screen.
/// </summary>
public static class SectionPlaceholder
{
    public static ScreenView Create(SectionDefinition section, SectionRegistry registry, ILocalizer localizer)
    {
        var list = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        list.Children.Add(new TextBlock { Text = localizer.Get("Shell.Empty.SectionUnavailable"), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody));
        foreach (var screen in registry.ScreensOf(section.Id))
        {
            list.Children.Add(new TextBlock { Text = "• " + localizer.Get(screen.TitleKey) }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody));
        }

        return new ScreenView(localizer.Get(section.TitleKey), [], list);
    }
}
