// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Theme;

/// <summary>WCAG contrast ratio between two colours: 1 is none, 21 is black on white. Text needs at least 4.5.</summary>
public static class Contrast
{
    public const double MinimumForText = 4.5;

    public static double Ratio(string foregroundHex, string backgroundHex)
    {
        var a = Luminance(foregroundHex);
        var b = Luminance(backgroundHex);
        var (light, dark) = a >= b ? (a, b) : (b, a);
        return (light + 0.05) / (dark + 0.05);
    }

    static double Luminance(string hex)
    {
        var (r, g, b) = Channels(hex);
        return (0.2126 * Linear(r)) + (0.7152 * Linear(g)) + (0.0722 * Linear(b));
    }

    static double Linear(int channel)
    {
        var c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    static (int R, int G, int B) Channels(string hex)
    {
        var h = hex.TrimStart('#');
        return (Convert.ToInt32(h[..2], 16), Convert.ToInt32(h[2..4], 16), Convert.ToInt32(h[4..6], 16));
    }
}
