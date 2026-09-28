// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Arca.UI.Actions;

/// <summary>
/// Runs the standard action whose shortcut the person pressed in a window. If the action does not apply on the current
/// screen nothing happens, and no error is shown (teclat-i-menus, Atajo no aplicable). It listens after the focused control
/// has had its turn, so a control that uses the key (Escape closing a drop-down or a menu, Control+Enter in a text box)
/// keeps it, and a dialog handles its own Escape so cancelling it never reaches the screen behind.
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
        }, RoutingStrategies.Bubble);
}
