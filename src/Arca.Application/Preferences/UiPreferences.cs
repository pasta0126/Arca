// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Preferences;

/// <summary>Where the main window was: the position in screen pixels and the size in device-independent units.</summary>
/// <param name="X">Left edge, in screen pixels.</param>
/// <param name="Y">Top edge, in screen pixels.</param>
/// <param name="Width">Width of the window content.</param>
/// <param name="Height">Height of the window content.</param>
/// <param name="IsMaximized">Whether the window was maximized; the bounds are those it had before.</param>
public sealed record WindowBounds(int X, int Y, double Width, double Height, bool IsMaximized = false);

/// <summary>
/// What the person arranged in the interface, kept on this computer and not in the database or the backups
/// (ux-fonaments, D10). It holds no personal data: only sizes, positions and which sections are folded.
/// </summary>
/// <param name="Window">The last position and size of the main window, or null if never saved.</param>
/// <param name="Sections">Whether each collapsible section is expanded, by its name; a section not listed uses its default.</param>
/// <param name="CompactLists">Whether the lists of zones and lockers use the compact density.</param>
/// <param name="SidebarCollapsed">Whether the navigation sidebar shows only its icons.</param>
public sealed record UiPreferences(
    WindowBounds? Window = null, IReadOnlyDictionary<string, bool>? Sections = null, bool CompactLists = false, bool SidebarCollapsed = false);

/// <summary>Where the interface preferences are kept. Reading never fails: anything wrong gives the defaults.</summary>
public interface IUiPreferencesStore
{
    UiPreferences Load();

    /// <summary>Saves the preferences. It may throw an IOException if the disk refuses; callers treat that as not important.</summary>
    void Save(UiPreferences preferences);
}
