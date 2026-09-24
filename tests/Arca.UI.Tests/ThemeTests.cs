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
    public void The_theme_resources_define_every_named_semantic_brush()
    {
        var light = (Avalonia.Controls.ResourceDictionary)ArcaTheme.CreateResources().ThemeDictionaries[ThemeVariant.Light];

        foreach (var name in new[] { "Background", "Surface", "Border", "Text", "TextSecondary", "Accent", "OnAccent", "Success", "Warning", "Error", "OnSemantic", "Focus" })
        {
            Assert.True(light.ContainsKey("Arca.Brush." + name), name);
        }
    }
}
