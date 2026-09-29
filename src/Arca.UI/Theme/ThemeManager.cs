// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Preferences;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Arca.UI.Theme;

/// <summary>
/// Puts the theme on the running application (ui-shell, Tema claro, oscuro o del sistema): the variant the person chose and the accent
/// of the centre, without restarting. Changing the accent builds the palettes and the named resources again and swaps them where the
/// old ones were; changing only the variant asks Avalonia for the other one, and «system» leaves it to follow the operating system.
/// </summary>
public sealed class ThemeManager
{
    readonly Avalonia.Application _app;
    FluentTheme _fluent;
    ResourceDictionary _resources;
    string? _accent;

    /// <summary>Adds the light theme, the default, to the application. Call it when the application initializes.</summary>
    public ThemeManager(Avalonia.Application app)
    {
        _app = app;
        _fluent = ArcaTheme.CreateFluent();
        _resources = ArcaTheme.CreateResources();
        app.RequestedThemeVariant = ArcaTheme.Variant;
        app.Styles.Add(_fluent);
        app.Styles.Add(ArcaTheme.CreateStyles());
        app.Resources.MergedDictionaries.Add(_resources);
    }

    /// <summary>The accent in use as #RRGGBB, or null for the default one.</summary>
    public string? Accent => _accent;

    /// <summary>The variant a choice asks for. «System» is the one that follows the operating system.</summary>
    public static ThemeVariant VariantOf(ThemeChoice choice) => choice switch
    {
        ThemeChoice.Dark => ThemeVariant.Dark,
        ThemeChoice.System => ThemeVariant.Default,
        _ => ThemeVariant.Light,
    };

    /// <summary>Applies the choice and the accent. Anything that means nothing gives the light theme and the default accent.</summary>
    public void Apply(ThemeChoice choice, string? accent)
    {
        if (accent != _accent)
        {
            _accent = accent;
            var fluent = ArcaTheme.CreateFluent(accent);
            var resources = ArcaTheme.CreateResources(accent);
            var at = _app.Styles.IndexOf(_fluent);
            _app.Styles[at] = fluent;
            var index = _app.Resources.MergedDictionaries.IndexOf(_resources);
            _app.Resources.MergedDictionaries[index] = resources;
            (_fluent, _resources) = (fluent, resources);
        }

        _app.RequestedThemeVariant = Enum.IsDefined(choice) ? VariantOf(choice) : ArcaTheme.Variant;
    }
}
