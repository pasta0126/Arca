// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Theme;

/// <summary>
/// The default look of ARCA: a light, neutral, pastel palette that is easy on the eyes for a person who spends
/// the day in front of it. Values are hexadecimal so they can be tested for contrast without Avalonia.
/// Text colours are dark on every pastel surface, and the pairs that carry text are checked in the tests.
/// </summary>
public static class ArcaPalette
{
    // Surfaces (warm greige)
    public const string Background = "#F6F3EE";
    public const string Surface = "#FBFAF7";
    public const string SurfaceRaised = "#FFFFFF";
    public const string SurfaceHover = "#EDE8DF";
    public const string SurfacePressed = "#E3DDD1";
    public const string Border = "#DDD7CB";

    // Text
    public const string Text = "#38362F";
    public const string TextSecondary = "#635F55";
    public const string TextDisabled = "#A29D91";

    // Accent (dusty pastel blue) with dark text on top of it
    public const string Accent = "#A9C4D3";
    public const string AccentHover = "#9BB9CB";
    public const string AccentPressed = "#8CAEC2";
    public const string OnAccent = "#1F2E38";

    // Semantic states, each with the dark text that goes on it (never colour alone: icon or text too)
    public const string Success = "#B5D6BF";
    public const string Warning = "#EBD59E";
    public const string Error = "#E8B0AA";
    public const string OnSemantic = "#2B2A26";

    // Text of a validation error shown on the page background
    public const string ErrorText = "#9B3B34";

    // Keyboard focus ring
    public const string Focus = "#5F86A0";
}
