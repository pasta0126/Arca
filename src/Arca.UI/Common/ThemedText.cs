// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Arca.UI.Common;

/// <summary>
/// The pieces of text and of note that every screen repeats, already tied to the theme's type scale and colours
/// (ux-fonaments, D13), so no view writes a size, a family or a colour of its own.
/// </summary>
public static class ThemedText
{
    /// <summary>The name of the application or a screen: the biggest text.</summary>
    public static TextBlock Heading(string text) => Text(text, ArcaResourceKeys.FontSizeHeading, FontWeight.Bold);

    /// <summary>The heading of a window, a dialog or a section.</summary>
    public static TextBlock Title(string text) => Text(text, ArcaResourceKeys.FontSizeTitle, FontWeight.SemiBold);

    /// <summary>Small print, such as a reference number, optionally faded.</summary>
    public static TextBlock Small(string text, double opacity = 1) =>
        Text(text, ArcaResourceKeys.FontSizeSmall, FontWeight.Normal).WithOpacity(opacity);

    /// <summary>A failure or validation message on the page: readable red text, in bold so it is not missed.</summary>
    public static TextBlock Error() =>
        Text(string.Empty, ArcaResourceKeys.FontSizeBody, FontWeight.SemiBold).Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.ErrorText);

    /// <summary>A key or code that is read character by character.</summary>
    public static SelectableTextBlock Monospace(string text)
    {
        var block = new SelectableTextBlock { Text = text, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
        return block
            .Themed(TextBlock.FontFamilyProperty, ArcaResourceKeys.FontFamilyMonospace)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeTitle);
    }

    /// <summary>A note with a warning bar at its side, for what the person must not overlook.</summary>
    public static Border WarningNote(string text) =>
        new Border { BorderThickness = new Thickness(2, 0, 0, 0), Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap } }
            .Themed(Border.BorderBrushProperty, ArcaResourceKeys.Warning)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);

    static TextBlock Text(string text, string sizeKey, FontWeight weight) =>
        new TextBlock { Text = text, FontWeight = weight, TextWrapping = TextWrapping.Wrap }.Themed(TextBlock.FontSizeProperty, sizeKey);

    static TextBlock WithOpacity(this TextBlock block, double opacity)
    {
        block.Opacity = opacity;
        return block;
    }
}
