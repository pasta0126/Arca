// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Theme;

/// <summary>The colours an accent gives a theme: the accent itself, its hover and pressed states, the text on it and the focus ring.</summary>
public sealed record AccentColors(string Accent, string Hover, string Pressed, string OnAccent, string Focus);

/// <summary>
/// Contrast by construction (ui-shell, D8): from the accent the centre chose, works out the colours a theme needs so that the text on
/// a button of that accent is always readable (at least 4,5 to 1) and the focus ring and the selection are always visible against the
/// background (at least 3 to 1), moving the tone of the accent only as far as needed. A pure function, tested with extreme colours.
/// </summary>
public static class AccentTheme
{
    /// <summary>The smallest contrast between the accent and the background that keeps focus and selection visible (a non-text element).</summary>
    public const double MinimumForFocus = 3.0;

    const string DarkText = "#1F2E38";
    const string LightText = "#FFFFFF";

    /// <summary>The colours of an accent on a theme. Null gives the default accent of the theme.</summary>
    public static AccentColors For(string? accentHex, PaletteColors theme, bool dark)
    {
        if (accentHex is null)
        {
            return new(theme.Accent, theme.AccentHover, theme.AccentPressed, theme.OnAccent, theme.Focus);
        }

        var accent = Hsl.From(accentHex);

        // In a dark theme the accent, which also marks the selection, must stand out from the background: lighten it as much as it takes.
        if (dark)
        {
            accent = Move(accent, theme.Background, MinimumForFocus, towardsLight: true);
        }

        // The text on the accent: whichever of the two readable texts contrasts more, and if even that is not enough the accent moves.
        var (onAccent, adjusted) = ChooseText(accent);
        var hover = Shift(adjusted, dark ? 0.05 : -0.05);
        var pressed = Shift(adjusted, dark ? 0.10 : -0.10);
        var focus = Move(Hsl.From(adjusted.Hex), theme.Background, MinimumForFocus, towardsLight: dark);
        return new(adjusted.Hex, EnsureText(hover, onAccent), EnsureText(pressed, onAccent), onAccent, focus.Hex);
    }

    static (string OnAccent, Hsl Accent) ChooseText(Hsl accent)
    {
        var dark = Contrast.Ratio(DarkText, accent.Hex);
        var light = Contrast.Ratio(LightText, accent.Hex);
        if (Math.Max(dark, light) >= Contrast.MinimumForText)
        {
            return (dark >= light ? DarkText : LightText, accent);
        }

        // Neither text reads on it: the accent is a mid tone. Push it towards the side that has more room, until one of them does.
        var towardsLight = accent.L >= 0.5;
        var text = towardsLight ? DarkText : LightText;
        var moved = accent;
        for (var i = 0; i < 100 && Contrast.Ratio(text, moved.Hex) < Contrast.MinimumForText; i++)
        {
            moved = moved with { L = Math.Clamp(moved.L + (towardsLight ? 0.01 : -0.01), 0, 1) };
        }

        return (text, moved);
    }

    /// <summary>Moves the lightness of a colour until it has the contrast against another, in the direction given.</summary>
    static Hsl Move(Hsl colour, string against, double minimum, bool towardsLight)
    {
        var moved = colour;
        for (var i = 0; i < 100 && Contrast.Ratio(moved.Hex, against) < minimum; i++)
        {
            moved = moved with { L = Math.Clamp(moved.L + (towardsLight ? 0.01 : -0.01), 0, 1) };
        }

        return moved;
    }

    static Hsl Shift(Hsl colour, double by) => colour with { L = Math.Clamp(colour.L + by, 0, 1) };

    /// <summary>A hover or pressed colour keeps the text readable too: if the shift spoiled it, it stays as close as the text allows.</summary>
    static string EnsureText(Hsl colour, string text)
    {
        var moved = colour;
        var towardsLight = text == DarkText;
        for (var i = 0; i < 100 && Contrast.Ratio(text, moved.Hex) < Contrast.MinimumForText; i++)
        {
            moved = moved with { L = Math.Clamp(moved.L + (towardsLight ? 0.01 : -0.01), 0, 1) };
        }

        return moved.Hex;
    }

    /// <summary>A colour as hue, saturation and lightness, the way its tone is moved.</summary>
    readonly record struct Hsl(double H, double S, double L)
    {
        public string Hex
        {
            get
            {
                var c = (1 - Math.Abs((2 * L) - 1)) * S;
                var x = c * (1 - Math.Abs((H / 60 % 2) - 1));
                var m = L - (c / 2);
                var (r, g, b) = (int)(H / 60) switch
                {
                    0 => (c, x, 0d),
                    1 => (x, c, 0d),
                    2 => (0d, c, x),
                    3 => (0d, x, c),
                    4 => (x, 0d, c),
                    _ => (c, 0d, x),
                };
                static string B(double v) => Math.Clamp((int)Math.Round((v) * 255), 0, 255).ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
                return "#" + B(r + m) + B(g + m) + B(b + m);
            }
        }

        public static Hsl From(string hex)
        {
            var h = hex.TrimStart('#');
            var r = Convert.ToInt32(h[..2], 16) / 255d;
            var g = Convert.ToInt32(h[2..4], 16) / 255d;
            var b = Convert.ToInt32(h[4..6], 16) / 255d;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var l = (max + min) / 2;
            var d = max - min;
            if (d == 0)
            {
                return new Hsl(0, 0, l);
            }

            var s = d / (1 - Math.Abs((2 * l) - 1));
            var hue = max == r ? 60 * (((g - b) / d) % 6) : max == g ? 60 * (((b - r) / d) + 2) : 60 * (((r - g) / d) + 4);
            return new Hsl(hue < 0 ? hue + 360 : hue, s, l);
        }
    }
}
