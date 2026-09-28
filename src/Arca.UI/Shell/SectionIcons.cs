// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls.Shapes;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using Avalonia.Media;

namespace Arca.UI.Shell;

/// <summary>
/// The icon of each section, drawn as an outline on a 24 by 24 grid so it looks the same at any size and takes its colour
/// from the theme. The state of something is never shown by an icon alone: the section always has its name too.
/// </summary>
public static class SectionIcons
{
    static readonly Dictionary<string, string> _outlines = new()
    {
        [ShellCatalog.Home] = "M3 11 L12 3 L21 11 M5 9 V21 H10 V14 H14 V21 H19 V9",
        [ShellCatalog.Lockers] = "M4 3 H11 V11 H4 Z M13 3 H20 V11 H13 Z M4 13 H11 V21 H4 Z M13 13 H20 V21 H13 Z",
        [ShellCatalog.Students] = "M12 4 A4 4 0 1 1 11.99 4 M4 21 C4 14 20 14 20 21",
        [ShellCatalog.Payments] = "M12 3 A9 9 0 1 1 11.99 3 M8 10 H14 M8 14 H14",
        [ShellCatalog.KeysAndIncidents] = "M8 9 A4 4 0 1 1 7.99 9 M11 12 L20 21 M17 18 L15 20",
        [ShellCatalog.Reports] = "M5 21 V12 M12 21 V5 M19 21 V15",
        [ShellCatalog.Course] = "M4 6 H20 V21 H4 Z M4 11 H20 M8 3 V8 M16 3 V8",
        [ShellCatalog.Settings] = "M4 7 H20 M4 12 H20 M4 17 H20 M9 5 V9 M15 10 V14 M8 15 V19",
    };

    /// <summary>The outline of a section, or of a plain square if the section has none.</summary>
    public static string Outline(string sectionId) => _outlines.GetValueOrDefault(sectionId, "M4 4 H20 V20 H4 Z");

    /// <summary>The icon as a control, coloured by the theme.</summary>
    public static AvaloniaPath Create(string sectionId) => new AvaloniaPath
    {
        Data = Geometry.Parse(Outline(sectionId)),
        StrokeThickness = 2,
        StrokeLineCap = PenLineCap.Round,
        StrokeJoin = PenLineJoin.Round,
        Width = 24,
        Height = 24,
        Stretch = Stretch.None,
    }.Themed(Shape.StrokeProperty, ArcaResourceKeys.Text);
}
