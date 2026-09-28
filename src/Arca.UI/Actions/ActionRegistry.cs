// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Avalonia.Input;

namespace Arca.UI.Actions;

/// <summary>The names of the actions every screen shares, with the same shortcut everywhere (teclat-i-menus).</summary>
public static class StandardActions
{
    public const string Search = "Search";
    public const string New = "New";
    public const string Confirm = "Confirm";
    public const string Cancel = "Cancel";
    public const string Help = "Help";

    public static IReadOnlyList<string> All { get; } = [Search, New, Confirm, Cancel, Help];
}

/// <summary>
/// The single list of the fixed shortcuts (ux-fonaments, D11): search, new, confirm, cancel and help, the same on every
/// screen, with Control on Windows and Linux and Command on macOS. Screens attach their own behaviour to these actions;
/// they do not define shortcuts of their own, and the set is not configurable.
/// </summary>
public sealed class ActionRegistry
{
    readonly Dictionary<string, AppAction> _actions;

    public ActionRegistry(ILocalizer localizer, UiPlatform platform)
    {
        var command = platform == UiPlatform.MacOS ? KeyModifiers.Meta : KeyModifiers.Control;
        _actions = new[]
        {
            Make(localizer, platform, StandardActions.Search, "Common.Action.Search", new Shortcut(Key.F, command)),
            Make(localizer, platform, StandardActions.New, "Common.Action.New", new Shortcut(Key.N, command)),
            Make(localizer, platform, StandardActions.Confirm, "Common.Label.Confirm", new Shortcut(Key.Enter, command)),
            Make(localizer, platform, StandardActions.Cancel, "Common.Label.Cancel", new Shortcut(Key.Escape, KeyModifiers.None)),
            Make(localizer, platform, StandardActions.Help, "Common.Action.Help", new Shortcut(Key.F1, KeyModifiers.None)),
        }.ToDictionary(a => a.Id);
    }

    public IReadOnlyCollection<AppAction> All => _actions.Values;

    public AppAction this[string id] => _actions[id];

    /// <summary>The standard action a key press is the shortcut of, or null.</summary>
    public AppAction? Find(Key key, KeyModifiers modifiers) =>
        _actions.Values.FirstOrDefault(a => a.Shortcut?.Matches(key, modifiers) == true);

    static AppAction Make(ILocalizer localizer, UiPlatform platform, string id, string labelKey, Shortcut shortcut) =>
        new(id, localizer.Get(labelKey), shortcut, shortcut.Describe(platform, localizer));
}
