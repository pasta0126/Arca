// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

namespace Arca.UI.Actions;

/// <summary>
/// Builds buttons, menu entries and context menus from the same <see cref="AppAction"/> objects, so what a button says and
/// does is exactly what its menu entry and its shortcut say and do. The shortcut is shown next to the action, and a
/// disabled action explains itself in its tooltip (teclat-i-menus, Atajos visibles y Acciones y estados coherentes).
/// </summary>
public static class ActionControls
{
    /// <summary>A button for an action: its label, its command, and a tooltip with the shortcut or the reason it is disabled.</summary>
    public static Button Button(AppAction action)
    {
        var button = new Button { Content = action.Label, Command = action };
        Describe(button, action);
        return button;
    }

    /// <summary>A menu entry for an action, with its shortcut written beside it.</summary>
    public static MenuItem MenuItem(AppAction action)
    {
        var item = new MenuItem { Header = action.Label, Command = action, InputGesture = action.Shortcut?.ToGesture() };
        Describe(item, action);
        return item;
    }

    /// <summary>
    /// Gives a control a context menu with the given actions, opened with the right button and with the menu key (or
    /// Shift+F10). The list is read each time the menu opens, so it reflects what applies to the selected element then.
    /// </summary>
    public static void AttachContextMenu(Control target, Func<IReadOnlyList<AppAction>> actions)
    {
        var menu = new ContextMenu();
        void Rebuild()
        {
            menu.Items.Clear();
            foreach (var action in actions())
            {
                menu.Items.Add(MenuItem(action));
            }
        }

        // Whichever way it is asked for (right button, menu key, opened by code), it is rebuilt from the current actions.
        menu.Opening += (_, _) => Rebuild();
        target.ContextRequested += (_, _) => Rebuild();
        target.ContextMenu = menu;
        target.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Apps || (e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.Shift))
            {
                Rebuild();
                menu.Open(target);
                e.Handled = true;
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    static void Describe(Control control, AppAction action)
    {
        control.Bind(ToolTip.TipProperty, new Binding(nameof(AppAction.ToolTipText)) { Source = action });
        ToolTip.SetShowOnDisabled(control, true); // a disabled action is the one that has to explain itself
    }
}
