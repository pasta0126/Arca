// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Arca.UI.Actions;

/// <summary>
/// Runs the standard action whose shortcut the person pressed in a window. If the action does not apply on the current
/// screen nothing happens, and no error is shown (teclat-i-menus, Atajo no aplicable). A dialog handles its own Escape
/// before it gets here, so cancelling a dialog never reaches the screen behind it.
/// </summary>
public static class ShortcutDispatcher
{
    public static void Attach(Window window, ActionRegistry registry) =>
        window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (!e.Handled && registry.Find(e.Key, e.KeyModifiers) is { } action && action.CanExecute(null))
            {
                action.Execute(null);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
}
