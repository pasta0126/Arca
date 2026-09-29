// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;
using Avalonia.Media;

namespace Arca.UI.Theme;

/// <summary>
/// The accent colours the settings offer and the way an accent is drawn as a sample. Colours live in the theme and nowhere else, so
/// the screens ask this class for them instead of writing one (ux-fonaments, D13).
/// </summary>
public static class AccentSwatches
{
    /// <summary>The colours offered as accents: pastel tones that go with the palette.</summary>
    public static IReadOnlyList<string> Palette { get; } = ["#A9C4D3", "#B5D6BF", "#EBD59E", "#E8B0AA", "#D6C9E3", "#F2C6A0", "#9FCFCB", "#C9C9C9"];

    /// <summary>A brush of a colour written as #RRGGBB.</summary>
    public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    /// <summary>Paints a button as the primary button of an accent looks: the accent behind, the text that reads on it in front.</summary>
    public static void Sample(Button button, AccentColors colours)
    {
        button.Background = Brush(colours.Accent);
        button.Foreground = Brush(colours.OnAccent);
    }
}
