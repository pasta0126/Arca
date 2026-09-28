// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Preferences;

namespace Arca.UI.Preferences;

/// <summary>
/// The interface preferences while the application runs: read once at the start, and saved whenever something changes
/// (ux-fonaments, D10). They are a convenience, never a requirement: if they cannot be read the defaults are used and if
/// they cannot be saved (a read-only disk) the application carries on without complaint.
/// </summary>
public sealed class UiPreferencesSession
{
    readonly IUiPreferencesStore _store;
    UiPreferences _current;

    public UiPreferencesSession(IUiPreferencesStore store)
    {
        _store = store;
        _current = SafeLoad(store);
    }

    /// <summary>The last saved position and size of the main window, or null.</summary>
    public WindowBounds? Window => _current.Window;

    /// <summary>Whether lists use the compact density.</summary>
    public bool CompactLists => _current.CompactLists;

    /// <summary>Whether a section is expanded: as the person left it, or its default if they never touched it.</summary>
    public bool IsSectionExpanded(string name, bool defaultValue) =>
        _current.Sections is { } sections && sections.TryGetValue(name, out var expanded) ? expanded : defaultValue;

    public void SetSectionExpanded(string name, bool expanded)
    {
        var sections = new Dictionary<string, bool>(_current.Sections ?? new Dictionary<string, bool>()) { [name] = expanded };
        Update(_current with { Sections = sections });
    }

    public void SetWindow(WindowBounds bounds) => Update(_current with { Window = bounds });

    public void SetCompactLists(bool compact) => Update(_current with { CompactLists = compact });

    void Update(UiPreferences next)
    {
        _current = next;
        try
        {
            _store.Save(next);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The arrangement is not saved this time. Nothing else depends on it.
        }
    }

    static UiPreferences SafeLoad(IUiPreferencesStore store)
    {
        try
        {
            return store.Load();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return new UiPreferences();
        }
    }
}
