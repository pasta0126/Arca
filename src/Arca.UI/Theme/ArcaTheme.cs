// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
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

    /// <summary>
    /// The keyboard focus ring (teclat-i-menus, Foco visible): a clear outline in the theme's focus colour around whatever
    /// control has the focus, shown when it arrives by keyboard. It reads the colour from <see cref="ArcaResourceKeys.Focus"/>,
    /// so another theme changes it without touching this code.
    /// </summary>
    public static Styles CreateStyles()
    {
        var ring = new FuncTemplate<Control>(() => new Border
        {
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
        }.Themed(Border.BorderBrushProperty, ArcaResourceKeys.Focus));
        // One style per kind of control: the Fluent theme sets its own focus visual on each of them, and a style for a
        // specific type is the one that wins over it.
        Style For<T>() where T : Control => new(x => x.OfType<T>()) { Setters = { new Setter(Control.FocusAdornerProperty, ring) } };
        return
        [
            For<Button>(), For<Avalonia.Controls.Primitives.ToggleButton>(), For<TextBox>(), For<CheckBox>(), For<RadioButton>(),
            For<ComboBox>(), For<ListBoxItem>(), For<TabItem>(), For<MenuItem>(), For<Slider>(),
        ];
    }

    /// <summary>The named semantic resources (Arca.Brush.*) that the components consume.</summary>
    public static ResourceDictionary CreateResources()
    {
        var light = new ResourceDictionary();
        Named(light);

        var root = new ResourceDictionary();
        root.ThemeDictionaries[ThemeVariant.Light] = light;
        Metrics(root);
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
        Brush(d, ArcaResourceKeys.Background, ArcaPalette.Background);
        Brush(d, ArcaResourceKeys.Surface, ArcaPalette.Surface);
        Brush(d, ArcaResourceKeys.Border, ArcaPalette.Border);
        Brush(d, ArcaResourceKeys.Text, ArcaPalette.Text);
        Brush(d, ArcaResourceKeys.TextSecondary, ArcaPalette.TextSecondary);
        Brush(d, ArcaResourceKeys.Accent, ArcaPalette.Accent);
        Brush(d, ArcaResourceKeys.OnAccent, ArcaPalette.OnAccent);
        Brush(d, ArcaResourceKeys.Success, ArcaPalette.Success);
        Brush(d, ArcaResourceKeys.Warning, ArcaPalette.Warning);
        Brush(d, ArcaResourceKeys.Error, ArcaPalette.Error);
        Brush(d, ArcaResourceKeys.OnSemantic, ArcaPalette.OnSemantic);
        Brush(d, ArcaResourceKeys.Focus, ArcaPalette.Focus);
    }

    /// <summary>The default type and spacing scales. They do not depend on the theme variant.</summary>
    static void Metrics(ResourceDictionary d)
    {
        d[ArcaResourceKeys.FontFamilyText] = FontFamily.Default;
        d[ArcaResourceKeys.FontSizeSmall] = 12d;
        d[ArcaResourceKeys.FontSizeBody] = 14d;
        d[ArcaResourceKeys.FontSizeTitle] = 18d;
        d[ArcaResourceKeys.FontSizeHeading] = 24d;
        d[ArcaResourceKeys.SpacingSmall] = 4d;
        d[ArcaResourceKeys.SpacingMedium] = 8d;
        d[ArcaResourceKeys.SpacingLarge] = 16d;
    }

    static void Brush(ResourceDictionary d, string key, string hex) => d[key] = new SolidColorBrush(Color.Parse(hex));
}
