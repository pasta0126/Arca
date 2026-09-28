// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Theme;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace Arca.UI.Common;

/// <summary>An outline icon drawn on a 24 by 24 grid, coloured by the theme, so it looks the same at any size.</summary>
public static class ThemedIcon
{
    /// <summary>The eye that shows a password.</summary>
    public const string Eye = "M2 12 C5 6 19 6 22 12 C19 18 5 18 2 12 Z M12 9 A3 3 0 1 1 11.99 9";

    public static AvaloniaPath Create(string outline) => new AvaloniaPath
    {
        Data = Geometry.Parse(outline),
        StrokeThickness = 2,
        StrokeLineCap = PenLineCap.Round,
        StrokeJoin = PenLineJoin.Round,
        Width = 24,
        Height = 24,
        Stretch = Stretch.None,
    }.Themed(Shape.StrokeProperty, ArcaResourceKeys.Text);
}
