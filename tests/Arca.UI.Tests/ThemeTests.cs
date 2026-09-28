// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Theme;
using Avalonia.Styling;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ThemeTests
{
    const string Spec = "ui-shell/identitat-i-tema: Tema claro, oscuro o del sistema";

    [Fact]
    [Trait("spec", Spec + " (v1: claro pastel por defecto)")]
    public void The_default_theme_is_light_regardless_of_the_system()
    {
        Assert.Equal(ThemeVariant.Light, ArcaTheme.Variant);
    }

    public static TheoryData<string, string, string> TextPairs => new()
    {
        { "text on background", ArcaPalette.Text, ArcaPalette.Background },
        { "text on surface", ArcaPalette.Text, ArcaPalette.Surface },
        { "text on raised surface", ArcaPalette.Text, ArcaPalette.SurfaceRaised },
        { "text on hover", ArcaPalette.Text, ArcaPalette.SurfaceHover },
        { "text on pressed", ArcaPalette.Text, ArcaPalette.SurfacePressed },
        { "secondary text on background", ArcaPalette.TextSecondary, ArcaPalette.Background },
        { "secondary text on surface", ArcaPalette.TextSecondary, ArcaPalette.Surface },
        { "text on accent", ArcaPalette.OnAccent, ArcaPalette.Accent },
        { "text on accent hover", ArcaPalette.OnAccent, ArcaPalette.AccentHover },
        { "text on accent pressed", ArcaPalette.OnAccent, ArcaPalette.AccentPressed },
        { "text on success", ArcaPalette.OnSemantic, ArcaPalette.Success },
        { "text on warning", ArcaPalette.OnSemantic, ArcaPalette.Warning },
        { "text on error", ArcaPalette.OnSemantic, ArcaPalette.Error },
        { "text on free locker", ArcaPalette.OnSemantic, ArcaPalette.StatusFree },
        { "text on occupied locker", ArcaPalette.OnSemantic, ArcaPalette.StatusOccupied },
        { "text on reserved locker", ArcaPalette.OnSemantic, ArcaPalette.StatusReserved },
        { "text on broken locker", ArcaPalette.OnSemantic, ArcaPalette.StatusBroken },
        { "text on maintenance locker", ArcaPalette.OnSemantic, ArcaPalette.StatusMaintenance },
        { "error text on background", ArcaPalette.ErrorText, ArcaPalette.Background },
        { "error text on surface", ArcaPalette.ErrorText, ArcaPalette.Surface },
    };

    [Theory]
    [MemberData(nameof(TextPairs))]
    [Trait("spec", "ui-shell/identitat-i-tema: Tema como recursos con nombre (contraste en el tema)")]
    public void Every_pair_that_carries_text_meets_the_contrast_minimum(string name, string foreground, string background)
    {
        var ratio = Contrast.Ratio(foreground, background);

        Assert.True(ratio >= Contrast.MinimumForText, $"{name}: contrast {ratio:F2} is below {Contrast.MinimumForText}");
    }

    [Fact]
    public void The_focus_ring_stands_out_from_every_surface()
    {
        foreach (var surface in new[] { ArcaPalette.Background, ArcaPalette.Surface, ArcaPalette.SurfaceRaised })
        {
            Assert.True(Contrast.Ratio(ArcaPalette.Focus, surface) >= 3.0, $"focus on {surface}"); // non-text minimum
        }
    }

    [Fact]
    public void Contrast_calculator_matches_the_known_extremes()
    {
        Assert.Equal(21.0, Contrast.Ratio("#000000", "#FFFFFF"), 1);
        Assert.Equal(1.0, Contrast.Ratio("#777777", "#777777"), 3);
    }

    [Fact]
    [Trait("spec", "ux-fonaments/components-de-feedback: Componentes sin colores ni textos propios (contrato de recursos)")]
    public void The_theme_resources_define_every_resource_of_the_contract_with_the_components()
    {
        var resources = ArcaTheme.CreateResources();
        var light = (Avalonia.Controls.ResourceDictionary)resources.ThemeDictionaries[ThemeVariant.Light];

        Assert.All(ArcaResourceKeys.Brushes, key => Assert.True(light.ContainsKey(key), key));
        Assert.All(ArcaResourceKeys.Metrics, key => Assert.True(resources.ContainsKey(key), key));
        Assert.Equal(ArcaResourceKeys.All.Count, ArcaResourceKeys.All.Distinct().Count());
    }

    [Fact]
    [Trait("spec", "ux-fonaments/components-de-feedback: Componentes sin colores ni textos propios (escala)")]
    public void The_type_and_spacing_scales_grow_in_order()
    {
        var resources = ArcaTheme.CreateResources();
        static double Size(Avalonia.Controls.ResourceDictionary r, string key) => (double)r[key]!;

        Assert.True(Size(resources, ArcaResourceKeys.FontSizeSmall) < Size(resources, ArcaResourceKeys.FontSizeBody));
        Assert.True(Size(resources, ArcaResourceKeys.FontSizeBody) < Size(resources, ArcaResourceKeys.FontSizeTitle));
        Assert.True(Size(resources, ArcaResourceKeys.FontSizeTitle) < Size(resources, ArcaResourceKeys.FontSizeHeading));
        Assert.True(Size(resources, ArcaResourceKeys.SpacingSmall) < Size(resources, ArcaResourceKeys.SpacingMedium));
        Assert.True(Size(resources, ArcaResourceKeys.SpacingMedium) < Size(resources, ArcaResourceKeys.SpacingLarge));
    }
}
