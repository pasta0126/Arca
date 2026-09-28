// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Preferences;

namespace Arca.UI.Layout;

/// <summary>A screen, or its usable area, in screen pixels.</summary>
public sealed record ScreenArea(int X, int Y, int Width, int Height);

/// <summary>Where and how big the main window should open. A null position leaves the choice to the system.</summary>
public sealed record PlacementResult(int? X, int? Y, double Width, double Height, bool IsMaximized);

/// <summary>
/// Turns what was saved into where the window really opens (ux-fonaments, D10): a size below the minimum is raised, one
/// bigger than the screen is reduced, and a position that no screen shows any more (a monitor that was unplugged, a
/// resolution change) puts the window back, visible and centred, on the main screen. Pure, so it is tested without a window.
/// </summary>
public static class WindowPlacement
{
    /// <summary>How much of the window has to be on a screen to count as visible, in pixels.</summary>
    const int MinimumVisibleWidth = 120;

    const int MinimumVisibleHeight = 60;

    /// <param name="saved">The saved bounds, or null.</param>
    /// <param name="screens">The usable area of every screen; the first is the main one.</param>
    /// <param name="scaling">The scale factor of the main screen (1 for 100 %, 1.5 for 150 %).</param>
    public static PlacementResult Correct(WindowBounds? saved, IReadOnlyList<ScreenArea> screens, double scaling)
    {
        if (saved is null || screens.Count == 0)
        {
            return new PlacementResult(null, null, WindowLimits.DefaultWidth, WindowLimits.DefaultHeight, false);
        }

        var main = screens[0];
        var factor = scaling > 0 ? scaling : 1;
        var width = Math.Clamp(saved.Width, WindowLimits.MinWidth, Math.Max(WindowLimits.MinWidth, main.Width / factor));
        var height = Math.Clamp(saved.Height, WindowLimits.MinHeight, Math.Max(WindowLimits.MinHeight, main.Height / factor));
        var pixelsWide = (int)Math.Round(width * factor);
        var pixelsHigh = (int)Math.Round(height * factor);

        var visible = screens.Any(s => Overlap(saved.X, saved.X + pixelsWide, s.X, s.X + s.Width) >= MinimumVisibleWidth
            && Overlap(saved.Y, saved.Y + pixelsHigh, s.Y, s.Y + s.Height) >= MinimumVisibleHeight);
        return visible
            ? new PlacementResult(saved.X, saved.Y, width, height, saved.IsMaximized)
            : new PlacementResult(
                main.X + Math.Max(0, (main.Width - pixelsWide) / 2), main.Y + Math.Max(0, (main.Height - pixelsHigh) / 2),
                width, height, saved.IsMaximized);
    }

    static int Overlap(int startA, int endA, int startB, int endB) => Math.Max(0, Math.Min(endA, endB) - Math.Max(startA, startB));
}
