// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Theme;

/// <summary>
/// Every colour of a theme, as hexadecimal text so it can be tested for contrast without Avalonia (ui-shell, D9). There is one set
/// for the light theme (the pastel palette of <see cref="ArcaPalette"/>) and one for the dark theme, and the accent of the centre
/// is put on top of either with <see cref="AccentTheme"/>.
/// </summary>
public sealed record PaletteColors(
    string Background, string Surface, string SurfaceRaised, string SurfaceHover, string SurfacePressed, string Border,
    string Text, string TextSecondary, string TextDisabled,
    string Accent, string AccentHover, string AccentPressed, string OnAccent,
    string Success, string Warning, string Error, string OnSemantic,
    string StatusFree, string StatusOccupied, string StatusReserved, string StatusBroken, string StatusMaintenance,
    string ErrorText, string Focus)
{
    /// <summary>The light theme, the default: warm greys with a dusty pastel blue accent.</summary>
    public static PaletteColors Light { get; } = new(
        ArcaPalette.Background, ArcaPalette.Surface, ArcaPalette.SurfaceRaised, ArcaPalette.SurfaceHover, ArcaPalette.SurfacePressed, ArcaPalette.Border,
        ArcaPalette.Text, ArcaPalette.TextSecondary, ArcaPalette.TextDisabled,
        ArcaPalette.Accent, ArcaPalette.AccentHover, ArcaPalette.AccentPressed, ArcaPalette.OnAccent,
        ArcaPalette.Success, ArcaPalette.Warning, ArcaPalette.Error, ArcaPalette.OnSemantic,
        ArcaPalette.StatusFree, ArcaPalette.StatusOccupied, ArcaPalette.StatusReserved, ArcaPalette.StatusBroken, ArcaPalette.StatusMaintenance,
        ArcaPalette.ErrorText, ArcaPalette.Focus);

    /// <summary>The dark theme: warm dark greys, light text and muted colours that still carry the light text of <see cref="OnSemantic"/>.</summary>
    public static PaletteColors Dark { get; } = new(
        "#23211E", "#2B2925", "#33302B", "#3A3731", "#45413A", "#4A463E",
        "#E9E5DC", "#B9B3A6", "#7E796E",
        "#7FA3B8", "#8FB0C3", "#6E93A9", "#12202A",
        "#3F6B4C", "#7A6428", "#85443E", "#F4F1EA",
        "#3F6B4C", "#3E5F78", "#5E4A7A", "#85443E", "#7A6428",
        "#F0A9A2", "#8FB8D2");

    /// <summary>The same colours with another accent, whose hover, pressed and focus colours and the text on it come from <see cref="AccentTheme"/>.</summary>
    public PaletteColors WithAccent(AccentColors accent) =>
        this with { Accent = accent.Accent, AccentHover = accent.Hover, AccentPressed = accent.Pressed, OnAccent = accent.OnAccent, Focus = accent.Focus };
}
