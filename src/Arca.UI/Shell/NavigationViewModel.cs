// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Preferences;
using Avalonia.Controls;

namespace Arca.UI.Shell;

/// <summary>
/// Which section is open and whether the sidebar is folded to its icons (ui-shell, navegacio-i-cerca). It knows nothing about
/// windows. The root screen of a section is built the first time it is opened and kept afterwards, so going back to a section
/// finds it as it was left. The fold of the sidebar is remembered on this computer.
/// </summary>
public sealed class NavigationViewModel : ObservableObject
{
    readonly SectionRegistry _registry;
    readonly UiPreferencesSession _preferences;
    readonly Func<SectionDefinition, Control> _placeholder;
    readonly Dictionary<string, Control> _roots = [];
    string _currentId;
    bool _isCollapsed;

    /// <param name="registry">The sections.</param>
    /// <param name="preferences">Where the fold of the sidebar is remembered.</param>
    /// <param name="placeholder">What a section shows when it has no root screen of its own yet.</param>
    /// <param name="startSection">The section that opens first.</param>
    public NavigationViewModel(
        SectionRegistry registry, UiPreferencesSession preferences, Func<SectionDefinition, Control> placeholder, string startSection = ShellCatalog.Home)
    {
        _registry = registry;
        _preferences = preferences;
        _placeholder = placeholder;
        _currentId = registry.Find(startSection)?.Id ?? registry.Sections[0].Id;
        _isCollapsed = preferences.SidebarCollapsed;
    }

    public IReadOnlyList<SectionDefinition> Sections => _registry.Sections;

    /// <summary>Raised when the counts of things that need attention may have changed, so the sidebar redraws its indicators.</summary>
    public event EventHandler? AttentionChanged;

    /// <summary>How many things need attention in a section: the count of its indicator, zero when there is nothing to show.</summary>
    public int AttentionOf(string sectionId) => Math.Max(0, _registry.Find(sectionId)?.Attention?.Invoke() ?? 0);

    /// <summary>Tells the sidebar to read the counts again, after the global state changed.</summary>
    public void RefreshAttention() => AttentionChanged?.Invoke(this, EventArgs.Empty);

    public string CurrentSectionId => _currentId;

    /// <summary>The screen of the open section.</summary>
    public Control CurrentRoot => RootOf(_currentId);

    public bool IsSidebarCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (Set(ref _isCollapsed, value))
            {
                _preferences.SetSidebarCollapsed(value);
            }
        }
    }

    /// <summary>Opens a section. An unknown one, or the one already open, changes nothing.</summary>
    /// <returns>True if the open section changed.</returns>
    public bool Navigate(string sectionId)
    {
        if (_registry.Find(sectionId) is null || sectionId == _currentId)
        {
            return false;
        }

        _currentId = sectionId;
        Raise(nameof(CurrentSectionId));
        Raise(nameof(CurrentRoot));
        return true;
    }

    /// <summary>Opens the section that holds a screen, so a result or a notice can take the person to where it lives.</summary>
    public bool NavigateToScreen(string screenId) => _registry.SectionOf(screenId) is { } section && Navigate(section.Id);

    Control RootOf(string sectionId)
    {
        if (!_roots.TryGetValue(sectionId, out var root))
        {
            var section = _registry.Find(sectionId)!;
            root = section.CreateRoot?.Invoke() ?? _placeholder(section);
            _roots[sectionId] = root;
        }

        return root;
    }
}
