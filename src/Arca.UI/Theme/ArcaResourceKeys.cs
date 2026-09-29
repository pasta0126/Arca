// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Theme;

/// <summary>
/// The contract between the components and the theme (ux-fonaments, D13): the only resources a component may use for
/// colour, type and spacing. <c>ui-shell</c> defines their values; a component never writes a raw colour, size or
/// margin. <see cref="ArcaTheme.CreateResources"/> provides the default set, enough to run and test the components.
/// </summary>
public static class ArcaResourceKeys
{
    // Colours (brushes), semantic: what they are for, not what colour they are.
    public const string Background = "Arca.Brush.Background";
    public const string Surface = "Arca.Brush.Surface";
    public const string Border = "Arca.Brush.Border";
    public const string Text = "Arca.Brush.Text";
    public const string TextSecondary = "Arca.Brush.TextSecondary";
    public const string Accent = "Arca.Brush.Accent";
    public const string OnAccent = "Arca.Brush.OnAccent";
    public const string Success = "Arca.Brush.Success";
    public const string Warning = "Arca.Brush.Warning";
    public const string Error = "Arca.Brush.Error";
    public const string OnSemantic = "Arca.Brush.OnSemantic";
    public const string Focus = "Arca.Brush.Focus";

    /// <summary>The text of a validation or failure message shown on the page itself (not on a coloured card).</summary>
    public const string ErrorText = "Arca.Brush.ErrorText";

    // The status of a locker on the map. They never carry the meaning alone: an icon and a word go with them.
    public const string StatusFree = "Arca.Brush.StatusFree";
    public const string StatusOccupied = "Arca.Brush.StatusOccupied";
    public const string StatusReserved = "Arca.Brush.StatusReserved";
    public const string StatusBroken = "Arca.Brush.StatusBroken";
    public const string StatusMaintenance = "Arca.Brush.StatusMaintenance";

    // Type: one family and a short scale of sizes.
    public const string FontFamilyText = "Arca.FontFamily.Text";

    /// <summary>For text that has to be read character by character, such as a recovery key.</summary>
    public const string FontFamilyMonospace = "Arca.FontFamily.Monospace";
    public const string FontSizeSmall = "Arca.FontSize.Small";
    public const string FontSizeBody = "Arca.FontSize.Body";
    public const string FontSizeTitle = "Arca.FontSize.Title";
    public const string FontSizeHeading = "Arca.FontSize.Heading";

    // Spacing: one scale used for margins, paddings and gaps.
    public const string SpacingSmall = "Arca.Spacing.Small";
    public const string SpacingMedium = "Arca.Spacing.Medium";
    public const string SpacingLarge = "Arca.Spacing.Large";

    /// <summary>Colours that change with the theme variant.</summary>
    public static IReadOnlyList<string> Brushes { get; } =
        [Background, Surface, Border, Text, TextSecondary, Accent, OnAccent, Success, Warning, Error, OnSemantic, Focus, ErrorText, StatusFree, StatusOccupied, StatusReserved, StatusBroken, StatusMaintenance];

    /// <summary>Type and spacing, which do not change with the variant.</summary>
    public static IReadOnlyList<string> Metrics { get; } =
        [FontFamilyText, FontFamilyMonospace, FontSizeSmall, FontSizeBody, FontSizeTitle, FontSizeHeading, SpacingSmall, SpacingMedium, SpacingLarge];

    public static IReadOnlyList<string> All { get; } = [.. Brushes, .. Metrics];
}
