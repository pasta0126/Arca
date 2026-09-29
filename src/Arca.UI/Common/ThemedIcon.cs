// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Theme;
using Avalonia.Controls.Primitives;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Arca.UI.Common;

/// <summary>
/// An icon from the Material Design Icons set (thousands of icons, free), drawn in the theme's text colour so it follows
/// whatever theme is active. Choose the icon by its <see cref="MaterialIconKind"/>; nothing draws an icon by hand.
/// </summary>
public static class ThemedIcon
{
    /// <param name="kind">The icon.</param>
    /// <param name="size">Its width and height, in device-independent units.</param>
    public static MaterialIcon Create(MaterialIconKind kind, double size = 24) =>
        new MaterialIcon { Kind = kind, Width = size, Height = size }.Themed(TemplatedControl.ForegroundProperty, ArcaResourceKeys.Text);
}
