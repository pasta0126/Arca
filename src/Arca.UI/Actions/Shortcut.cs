// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Avalonia.Input;

namespace Arca.UI.Actions;

/// <summary>A key combination that runs an action.</summary>
public sealed record Shortcut(Key Key, KeyModifiers Modifiers)
{
    /// <summary>The gesture Avalonia matches against key presses and shows next to a menu item.</summary>
    public KeyGesture ToGesture() => new(Key, Modifiers);

    /// <summary>Whether a key press is this shortcut.</summary>
    public bool Matches(Key key, KeyModifiers modifiers) => key == Key && modifiers == Modifiers;

    /// <summary>The text shown next to an action and in its tooltip: "Ctrl+F", or "⌘F" on macOS.</summary>
    public string Describe(UiPlatform platform, ILocalizer localizer)
    {
        var key = Key switch
        {
            Key.Enter => localizer.Get("Common.Key.Enter"),
            Key.Escape => localizer.Get("Common.Key.Escape"),
            _ => Key.ToString(),
        };
        if (Modifiers == KeyModifiers.None)
        {
            return key;
        }

        return platform == UiPlatform.MacOS ? "⌘" + key : "Ctrl+" + key;
    }
}
