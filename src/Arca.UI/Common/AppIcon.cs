// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Arca.UI.Common;

/// <summary>
/// The icon of the application (icona-d-aplicacio, Icono visible en la aplicación): one image kept inside the assembly, given to every window
/// the application opens so none shows the generic one. It is never read from the database: the logo of the centre is a different thing.
/// </summary>
public static class AppIcon
{
    const string Address = "avares://Arca.UI/Assets/arca-icon.png";

    static WindowIcon? _icon;

    /// <summary>The icon, or null if it cannot be read, in which case the window simply carries the one of the system.</summary>
    public static WindowIcon? Load()
    {
        if (_icon is not null)
        {
            return _icon;
        }

        try
        {
            using var stream = AssetLoader.Open(new Uri(Address));
            return _icon = new WindowIcon(stream);
        }
        catch (Exception)
        {
            return null; // an icon is never worth stopping a window for
        }
    }

    /// <summary>Gives a window the icon of the application.</summary>
    public static void ApplyTo(Window window) => window.Icon = Load();
}
