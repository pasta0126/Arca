// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Preferences;
using Arca.UI.Common;
using Arca.UI.Identity;
using Arca.UI.Preferences;

namespace Arca.UI.Identity;

/// <summary>
/// The choice of theme (ui-shell, Tema claro, oscuro o del sistema): light, dark or the system's, kept in the local settings of the
/// computer, not in the data, and applied at once without restarting. The accent of the centre goes on whichever is chosen.
/// </summary>
public sealed class ThemeSettingsViewModel : ObservableObject
{
    readonly UiPreferencesSession _preferences;
    readonly CentreIdentityModel _identity;
    readonly Action<ThemeChoice, string?> _apply;

    /// <param name="apply">Puts the choice and the accent on the running application.</param>
    public ThemeSettingsViewModel(UiPreferencesSession preferences, CentreIdentityModel identity, Action<ThemeChoice, string?> apply)
    {
        _preferences = preferences;
        _identity = identity;
        _apply = apply;
        identity.Changed += (_, _) => _apply(Choice, identity.Current.Accent); // a new accent is put on the theme in use
    }

    /// <summary>The choices, in the order they are offered.</summary>
    public IReadOnlyList<ThemeChoice> Choices { get; } = [ThemeChoice.Light, ThemeChoice.Dark, ThemeChoice.System];

    /// <summary>The theme in use. Setting it applies it and remembers it.</summary>
    public ThemeChoice Choice
    {
        get => _preferences.Theme;
        set
        {
            if (value == _preferences.Theme)
            {
                return;
            }

            _preferences.SetTheme(value);
            _apply(value, _identity.Current.Accent);
            Raise(nameof(Choice));
        }
    }

    /// <summary>Applies what is saved: when the application opens.</summary>
    public void ApplySaved() => _apply(Choice, _identity.Current.Accent);
}
