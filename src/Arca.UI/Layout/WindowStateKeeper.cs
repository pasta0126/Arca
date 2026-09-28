// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Preferences;
using Arca.UI.Preferences;
using Avalonia;
using Avalonia.Controls;

namespace Arca.UI.Layout;

/// <summary>
/// Restores the size and position of a window when it opens, corrected so it is always visible, and saves them when it
/// closes (adaptabilitat-i-disposicio, Preferencias locales de interfaz).
/// </summary>
public static class WindowStateKeeper
{
    /// <summary>Applies the limits and the saved placement to the window and starts saving it when it closes.</summary>
    public static void Attach(Window window, UiPreferencesSession preferences)
    {
        WindowLimits.Apply(window);
        window.Closing += (_, _) => Save(window, preferences);
        if (!Place(window, preferences))
        {
            // The window does not know its screens until the system has created it: place it once it is open.
            EventHandler? once = null;
            once = (_, _) =>
            {
                window.Opened -= once;
                Place(window, preferences);
            };
            window.Opened += once;
        }
    }

    /// <returns>False if there were no screens to place the window on yet.</returns>
    static bool Place(Window window, UiPreferencesSession preferences)
    {
        var screens = window.Screens;
        var areas = ToMainFirst(
            screens is null ? [] : [.. screens.All.Select(s => new ScreenArea(s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height))],
            screens);
        var placement = WindowPlacement.Correct(preferences.Window, areas, screens?.Primary?.Scaling ?? 1);
        window.Width = placement.Width;
        window.Height = placement.Height;
        if (placement is { X: { } x, Y: { } y })
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = new PixelPoint(x, y);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (placement.IsMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }

        return areas.Count > 0;
    }

    static void Save(Window window, UiPreferencesSession preferences)
    {
        var maximized = window.WindowState == WindowState.Maximized;
        if (window.WindowState == WindowState.Minimized)
        {
            return;
        }

        if (maximized)
        {
            // Keep the size and place it had before, so leaving the maximized state later goes back to them.
            var before = preferences.Window;
            preferences.SetWindow(before is null ? new WindowBounds(window.Position.X, window.Position.Y, window.Width, window.Height, true) : before with { IsMaximized = true });
            return;
        }

        preferences.SetWindow(new WindowBounds(window.Position.X, window.Position.Y, window.Width, window.Height));
    }

    /// <summary>The main screen has to come first in the list, as <see cref="WindowPlacement"/> expects.</summary>
    static List<ScreenArea> ToMainFirst(List<ScreenArea> areas, Avalonia.Controls.Screens? screens)
    {
        if (screens?.Primary is not { } primary)
        {
            return areas;
        }

        var main = new ScreenArea(primary.WorkingArea.X, primary.WorkingArea.Y, primary.WorkingArea.Width, primary.WorkingArea.Height);
        return [main, .. areas.Where(a => a != main)];
    }
}
