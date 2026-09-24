// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Arca.UI.Theme;

/// <summary>
/// Turns the palette into Avalonia resources. It gives the Fluent theme the pastel palette, so every standard
/// control picks up the look, and adds the named semantic resources that the components use (arquitectura-base
/// feedback and ux-fonaments). v1 has one theme: light, pastel and neutral.
/// </summary>
public static class ArcaTheme
{
    /// <summary>The variant the application requests. The operating system's dark mode is not followed.</summary>
    public static ThemeVariant Variant => ThemeVariant.Light;

    /// <summary>
    /// The Fluent theme with the pastel palette. The palette must be given to the theme itself: brushes defined inside
    /// Fluent resolve their colours in Fluent's own scope, so overriding colours in the application resources has no effect.
    /// </summary>
    public static FluentTheme CreateFluent()
    {
        var fluent = new FluentTheme();
        fluent.Palettes[ThemeVariant.Light] = Palette();
        return fluent;
    }

    /// <summary>The named semantic resources (Arca.Brush.*) that the components consume.</summary>
    public static ResourceDictionary CreateResources()
    {
        var light = new ResourceDictionary();
        Named(light);

        var root = new ResourceDictionary();
        root.ThemeDictionaries[ThemeVariant.Light] = light;
        return root;
    }

    static ColorPaletteResources Palette()
    {
        static Color C(string hex) => Color.Parse(hex);
        return new ColorPaletteResources
        {
            Accent = C(ArcaPalette.Accent),
            RegionColor = C(ArcaPalette.Background),
            AltHigh = C(ArcaPalette.SurfaceRaised),
            AltMediumHigh = C(ArcaPalette.Surface),
            AltMedium = C(ArcaPalette.Surface),
            AltMediumLow = C(ArcaPalette.SurfaceHover),
            AltLow = C(ArcaPalette.SurfaceHover),
            BaseHigh = C(ArcaPalette.Text),
            BaseMediumHigh = C(ArcaPalette.Text),
            BaseMedium = C(ArcaPalette.TextSecondary),
            BaseMediumLow = C(ArcaPalette.TextSecondary),
            BaseLow = C(ArcaPalette.Border),
            ChromeLow = C(ArcaPalette.SurfaceHover),
            ChromeMediumLow = C(ArcaPalette.SurfaceHover),
            ChromeMedium = C(ArcaPalette.SurfacePressed),
            ChromeHigh = C(ArcaPalette.Border),
            ChromeAltLow = C(ArcaPalette.Surface),
            ChromeDisabledLow = C(ArcaPalette.Surface),
            ChromeDisabledHigh = C(ArcaPalette.Border),
            ChromeGray = C(ArcaPalette.TextSecondary),
            ChromeBlackHigh = C(ArcaPalette.Text),
            ChromeBlackMedium = C(ArcaPalette.TextSecondary),
            ChromeBlackMediumLow = C(ArcaPalette.TextSecondary),
            ChromeBlackLow = C(ArcaPalette.Border),
            ChromeWhite = C(ArcaPalette.OnAccent), // used as the text on accent buttons: dark, because the accent is pastel
            ListLow = C(ArcaPalette.SurfaceHover),
            ListMedium = C(ArcaPalette.SurfacePressed),
            ErrorText = C(ArcaPalette.ErrorText),
        };
    }

    /// <summary>Named resources the components consume: semantic colours, never a raw colour in a component.</summary>
    static void Named(ResourceDictionary d)
    {
        d["Arca.Brush.Background"] = new SolidColorBrush(Color.Parse(ArcaPalette.Background));
        d["Arca.Brush.Surface"] = new SolidColorBrush(Color.Parse(ArcaPalette.Surface));
        d["Arca.Brush.Border"] = new SolidColorBrush(Color.Parse(ArcaPalette.Border));
        d["Arca.Brush.Text"] = new SolidColorBrush(Color.Parse(ArcaPalette.Text));
        d["Arca.Brush.TextSecondary"] = new SolidColorBrush(Color.Parse(ArcaPalette.TextSecondary));
        d["Arca.Brush.Accent"] = new SolidColorBrush(Color.Parse(ArcaPalette.Accent));
        d["Arca.Brush.OnAccent"] = new SolidColorBrush(Color.Parse(ArcaPalette.OnAccent));
        d["Arca.Brush.Success"] = new SolidColorBrush(Color.Parse(ArcaPalette.Success));
        d["Arca.Brush.Warning"] = new SolidColorBrush(Color.Parse(ArcaPalette.Warning));
        d["Arca.Brush.Error"] = new SolidColorBrush(Color.Parse(ArcaPalette.Error));
        d["Arca.Brush.OnSemantic"] = new SolidColorBrush(Color.Parse(ArcaPalette.OnSemantic));
        d["Arca.Brush.Focus"] = new SolidColorBrush(Color.Parse(ArcaPalette.Focus));
    }
}
