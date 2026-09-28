// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Shell;

namespace Arca.UI.Search;

/// <summary>What a search result stands for.</summary>
public enum SearchTargetKind
{
    Student,
    Locker,
    Group,

    /// <summary>The whole list of students that match, when there are more results than are shown.</summary>
    StudentList,

    LockerList,
    GroupList,
}

/// <summary>Where a result takes the person: the kind of thing, its identity and, for a group, its level; and for a list, the text searched.</summary>
public sealed record SearchTarget(SearchTargetKind Kind, Guid Id = default, Guid? LevelId = null, string? Text = null);

/// <summary>
/// Takes the person from a search result to where it lives (ui-shell, navegacio-i-cerca): the record of a student in the
/// Students section, a locker highlighted on the map of the start screen, a group or a full list in its section. It opens the
/// section and leaves the request for the screen to take, because a screen is built the first time its section is opened and
/// may not exist yet when the person chooses the result.
/// </summary>
public sealed class SearchNavigator(NavigationViewModel navigation) : ObservableObject
{
    SearchTarget? _pending;
    Guid? _highlightedLocker;

    /// <summary>Raised each time a result is opened, for a screen that is already showing to react.</summary>
    public event EventHandler<SearchTarget>? Requested;

    /// <summary>The last request that no screen has taken yet. A screen takes it when it is built, with <see cref="TakePending"/>.</summary>
    public SearchTarget? Pending => _pending;

    /// <summary>The locker to draw highlighted on the map, or null.</summary>
    public Guid? HighlightedLocker
    {
        get => _highlightedLocker;
        private set => Set(ref _highlightedLocker, value);
    }

    /// <summary>Opens what a result stands for.</summary>
    public void Open(SearchTarget target)
    {
        var section = target.Kind switch
        {
            SearchTargetKind.Locker => ShellCatalog.Home,
            SearchTargetKind.LockerList => ShellCatalog.Lockers,
            _ => ShellCatalog.Students,
        };
        HighlightedLocker = target.Kind == SearchTargetKind.Locker ? target.Id : null;
        _pending = target;
        navigation.Navigate(section);
        Requested?.Invoke(this, target);
    }

    /// <summary>Hands the pending request to the screen that shows it, once.</summary>
    public SearchTarget? TakePending()
    {
        var taken = _pending;
        _pending = null;
        return taken;
    }

    /// <summary>Takes the highlight off the map.</summary>
    public void ClearHighlight() => HighlightedLocker = null;
}
