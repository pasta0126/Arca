// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;

namespace Arca.UI.Shell;

/// <summary>
/// The sections of the sidebar and the screens each one holds (ui-shell, D1). The sidebar, the header and navigation are
/// built from it. It refuses a definition that would put a screen in two sections, so that "where do I find this?" always has
/// one answer.
/// </summary>
public sealed class SectionRegistry
{
    readonly Dictionary<string, SectionDefinition> _sections = [];
    readonly Dictionary<string, ScreenDefinition> _screens = [];

    /// <param name="sections">The sections, by their order.</param>
    /// <param name="screens">Every screen with its section.</param>
    public SectionRegistry(IEnumerable<SectionDefinition> sections, IEnumerable<ScreenDefinition> screens)
    {
        foreach (var section in sections)
        {
            if (!_sections.TryAdd(section.Id, section))
            {
                throw new ArgumentException($"The section '{section.Id}' is registered twice.", nameof(sections));
            }
        }

        foreach (var screen in screens)
        {
            if (!_sections.ContainsKey(screen.SectionId))
            {
                throw new ArgumentException($"The screen '{screen.Id}' belongs to the unknown section '{screen.SectionId}'.", nameof(screens));
            }

            if (!_screens.TryAdd(screen.Id, screen))
            {
                throw new ArgumentException($"The screen '{screen.Id}' is in two sections.", nameof(screens));
            }
        }
    }

    /// <summary>The sections from the top of the sidebar down.</summary>
    public IReadOnlyList<SectionDefinition> Sections => [.. _sections.Values.OrderBy(s => s.Order)];

    public IReadOnlyList<ScreenDefinition> Screens => [.. _screens.Values];

    public SectionDefinition? Find(string id) => _sections.GetValueOrDefault(id);

    /// <summary>The screens of a section, in the order they were declared.</summary>
    public IReadOnlyList<ScreenDefinition> ScreensOf(string sectionId) => [.. _screens.Values.Where(s => s.SectionId == sectionId)];

    /// <summary>The section a screen is in: where the person finds it.</summary>
    public SectionDefinition? SectionOf(string screenId) => _screens.TryGetValue(screenId, out var s) ? _sections[s.SectionId] : null;

    /// <summary>The same sections with the given roots and attention providers, for whoever composes the application.</summary>
    /// <param name="roots">The root screen of each section, by its name; a section without one shows what it will hold.</param>
    /// <param name="attention">The count of each section's indicator.</param>
    /// <param name="home">The screen of the Home section. Giving another one is the only thing needed to replace the start screen.</param>
    public static SectionRegistry Compose(
        IReadOnlyDictionary<string, Func<Control>> roots, IReadOnlyDictionary<string, Func<int>>? attention = null, IHomeScreen? home = null)
    {
        var all = roots.ToDictionary(r => r.Key, r => r.Value);
        if (home is not null)
        {
            all[ShellCatalog.Home] = home.Create;
        }

        return new(
            ShellCatalog.Sections.Select(s => s with
            {
                CreateRoot = all.GetValueOrDefault(s.Id),
                Attention = attention?.GetValueOrDefault(s.Id),
            }),
            ShellCatalog.Screens);
    }
}
