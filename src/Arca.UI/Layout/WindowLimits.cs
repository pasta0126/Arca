// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;

namespace Arca.UI.Layout;

/// <summary>
/// The sizes the layout is designed around (ux-fonaments, D9). They are constants so they are adjusted in one place when
/// the application is tried on the computers of the centre.
/// </summary>
public static class WindowLimits
{
    /// <summary>The smallest window in which no control is out of reach.</summary>
    public const double MinWidth = 1024;

    public const double MinHeight = 640;

    /// <summary>The size a window opens with when nothing was saved.</summary>
    public const double DefaultWidth = 1200;

    public const double DefaultHeight = 760;

    /// <summary>Below this width of the content area, a secondary panel goes under the main one instead of beside it.</summary>
    public const double StackBelowWidth = 900;

    /// <summary>Stops the window from being made smaller than the minimum.</summary>
    public static void Apply(Window window)
    {
        window.MinWidth = MinWidth;
        window.MinHeight = MinHeight;
    }
}
