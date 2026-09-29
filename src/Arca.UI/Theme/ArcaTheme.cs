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
/// feedback and ux-fonaments). It has a light theme, pastel and neutral, which is the default, and a dark one; the accent of the centre is put on either.
/// </summary>
public static class ArcaTheme
{
    /// <summary>The variant the application requests. The operating system's dark mode is not followed.</summary>
    public static ThemeVariant Variant => ThemeVariant.Light;

    /// <summary>
    /// The Fluent theme with the pastel palette. The palette must be given to the theme itself: brushes defined inside
    /// Fluent resolve their colours in Fluent's own scope, so overriding colours in the application resources has no effect.
    /// </summary>
    public static FluentTheme CreateFluent(string? accent = null)
    {
        var fluent = new FluentTheme();
        fluent.Palettes[ThemeVariant.Light] = Palette(Colors(accent, dark: false));
        fluent.Palettes[ThemeVariant.Dark] = Palette(Colors(accent, dark: true));
        return fluent;
    }

    /// <summary>The colours of a theme with the accent of the centre on top: the default accent when there is none.</summary>
    public static PaletteColors Colors(string? accent, bool dark)
    {
        var theme = dark ? PaletteColors.Dark : PaletteColors.Light;
        return accent is null ? theme : theme.WithAccent(AccentTheme.For(accent, theme, dark));
    }

    /// <summary>
    /// The styles every application must add next to the theme: the templates of the icons, and the keyboard focus ring.
    /// The focus ring (teclat-i-menus, Foco visible): a clear outline in the theme's focus colour around whatever
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
            new Material.Icons.Avalonia.MaterialIconStyles(null),
            For<Button>(), For<Avalonia.Controls.Primitives.ToggleButton>(), For<TextBox>(), For<CheckBox>(), For<RadioButton>(),
            For<ComboBox>(), For<ListBoxItem>(), For<TabItem>(), For<MenuItem>(), For<Slider>(),
        ];
    }

    /// <summary>The named semantic resources (Arca.Brush.*) that the components consume.</summary>
    public static ResourceDictionary CreateResources(string? accent = null)
    {
        var light = new ResourceDictionary();
        Named(light, Colors(accent, dark: false));
        var dark = new ResourceDictionary();
        Named(dark, Colors(accent, dark: true));

        var root = new ResourceDictionary();
        root.ThemeDictionaries[ThemeVariant.Light] = light;
        root.ThemeDictionaries[ThemeVariant.Dark] = dark;
        Metrics(root);
        return root;
    }

    static ColorPaletteResources Palette(PaletteColors c)
    {
        static Color H(string hex) => Color.Parse(hex);
        return new ColorPaletteResources
        {
            Accent = H(c.Accent),
            RegionColor = H(c.Background),
            AltHigh = H(c.SurfaceRaised),
            AltMediumHigh = H(c.Surface),
            AltMedium = H(c.Surface),
            AltMediumLow = H(c.SurfaceHover),
            AltLow = H(c.SurfaceHover),
            BaseHigh = H(c.Text),
            BaseMediumHigh = H(c.Text),
            BaseMedium = H(c.TextSecondary),
            BaseMediumLow = H(c.TextSecondary),
            BaseLow = H(c.Border),
            ChromeLow = H(c.SurfaceHover),
            ChromeMediumLow = H(c.SurfaceHover),
            ChromeMedium = H(c.SurfacePressed),
            ChromeHigh = H(c.Border),
            ChromeAltLow = H(c.Surface),
            ChromeDisabledLow = H(c.Surface),
            ChromeDisabledHigh = H(c.Border),
            ChromeGray = H(c.TextSecondary),
            ChromeBlackHigh = H(c.Text),
            ChromeBlackMedium = H(c.TextSecondary),
            ChromeBlackMediumLow = H(c.TextSecondary),
            ChromeBlackLow = H(c.Border),
            ChromeWhite = H(c.OnAccent), // used as the text on accent buttons: dark, because the accent is pastel
            ListLow = H(c.SurfaceHover),
            ListMedium = H(c.SurfacePressed),
            ErrorText = H(c.ErrorText),
        };
    }

    /// <summary>Named resources the components consume: semantic colours, never a raw colour in a component.</summary>
    static void Named(ResourceDictionary d, PaletteColors c)
    {
        Brush(d, ArcaResourceKeys.Background, c.Background);
        Brush(d, ArcaResourceKeys.Surface, c.Surface);
        Brush(d, ArcaResourceKeys.Border, c.Border);
        Brush(d, ArcaResourceKeys.Text, c.Text);
        Brush(d, ArcaResourceKeys.TextSecondary, c.TextSecondary);
        Brush(d, ArcaResourceKeys.Accent, c.Accent);
        Brush(d, ArcaResourceKeys.OnAccent, c.OnAccent);
        Brush(d, ArcaResourceKeys.Success, c.Success);
        Brush(d, ArcaResourceKeys.Warning, c.Warning);
        Brush(d, ArcaResourceKeys.Error, c.Error);
        Brush(d, ArcaResourceKeys.OnSemantic, c.OnSemantic);
        Brush(d, ArcaResourceKeys.Focus, c.Focus);
        Brush(d, ArcaResourceKeys.ErrorText, c.ErrorText);
        Brush(d, ArcaResourceKeys.StatusFree, c.StatusFree);
        Brush(d, ArcaResourceKeys.StatusOccupied, c.StatusOccupied);
        Brush(d, ArcaResourceKeys.StatusReserved, c.StatusReserved);
        Brush(d, ArcaResourceKeys.StatusBroken, c.StatusBroken);
        Brush(d, ArcaResourceKeys.StatusMaintenance, c.StatusMaintenance);
    }

    /// <summary>The default type and spacing scales. They do not depend on the theme variant.</summary>
    static void Metrics(ResourceDictionary d)
    {
        d[ArcaResourceKeys.FontFamilyText] = FontFamily.Default;
        d[ArcaResourceKeys.FontFamilyMonospace] = new FontFamily("Menlo, Consolas, monospace");
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
