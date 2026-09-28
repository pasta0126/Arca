// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Layout;
using Arca.UI.Lists;
using Arca.UI.Notifications;
using Arca.UI.Preferences;

namespace Arca.UI.Map;

/// <summary>A zone of the map as it is shown now: its lockers after the filters, and its own counters.</summary>
public sealed class ZoneMapViewModel(ZoneMap zone, IReadOnlyList<MapLocker> visible, CollapsibleSectionViewModel section)
{
    public Guid ZoneId => zone.ZoneId;

    public string Name => zone.ZoneName;

    /// <summary>The lockers that pass the filters, by number.</summary>
    public IReadOnlyList<MapLocker> Visible => visible;

    /// <summary>The counts of the whole zone, whatever the filters hide.</summary>
    public LockerCounters Counters => zone.Counters;

    /// <summary>The folding of the zone, remembered on this computer.</summary>
    public CollapsibleSectionViewModel Section => section;
}

/// <summary>
/// The map of lockers by zone without any window (ui-shell, pantalla-principal): it loads the whole map in one query, keeps
/// the counts by status, filters by status and by zone, highlights a locker chosen in the search, and, after a change, reads
/// again only the locker that changed and recomputes the counters from what it already has (Actualización tras un cambio).
/// </summary>
public sealed class LockerMapViewModel : ObservableObject
{
    readonly Func<CancellationToken, Task<Result<LockerMapData>>> _load;
    readonly Func<Guid, CancellationToken, Task<Result<MapLocker?>>> _loadOne;
    readonly UiPreferencesSession _preferences;
    readonly ResultNotifier _notifier;
    readonly ILocalizer _localizer;
    List<(Guid ZoneId, string Name, List<MapLocker> Lockers)> _zones = [];
    IReadOnlyList<ZoneMapViewModel> _view = [];
    LockerStatusView? _statusFilter;
    Guid? _zoneFilter;
    Guid? _selected;
    Guid? _highlighted;

    public LockerMapViewModel(
        Func<CancellationToken, Task<Result<LockerMapData>>> load, Func<Guid, CancellationToken, Task<Result<MapLocker?>>> loadOne,
        UiPreferencesSession preferences, ResultNotifier notifier, ILocalizer localizer)
    {
        _load = load;
        _loadOne = loadOne;
        _preferences = preferences;
        _notifier = notifier;
        _localizer = localizer;
        State = new ListStateViewModel(localizer);
    }

    /// <summary>Loading, the map, no lockers, or a filter that leaves none.</summary>
    public ListStateViewModel State { get; }

    /// <summary>The zones to draw now, with the filters applied. A zone with nothing to show is left out while a filter is on.</summary>
    public IReadOnlyList<ZoneMapViewModel> Zones
    {
        get => _view;
        private set => Set(ref _view, value);
    }

    /// <summary>The counts by status of every locker on the map, whatever the filters hide.</summary>
    public LockerCounters Counters => GetLockerMapHandler.Count(_zones.SelectMany(z => z.Lockers));

    /// <summary>The zones, to offer them in the zone filter.</summary>
    public IReadOnlyList<(Guid ZoneId, string Name)> ZoneNames => [.. _zones.Select(z => (z.ZoneId, z.Name))];

    public LockerStatusView? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (Set(ref _statusFilter, value))
            {
                Rebuild();
            }
        }
    }

    public Guid? ZoneFilter
    {
        get => _zoneFilter;
        set
        {
            if (Set(ref _zoneFilter, value))
            {
                Rebuild();
            }
        }
    }

    /// <summary>The locker whose detail is open.</summary>
    public Guid? SelectedLockerId
    {
        get => _selected;
        private set => Set(ref _selected, value);
    }

    /// <summary>The locker chosen in the search, drawn highlighted until something else is chosen.</summary>
    public Guid? HighlightedLockerId
    {
        get => _highlighted;
        private set => Set(ref _highlighted, value);
    }

    /// <summary>The locker as the map holds it, or null.</summary>
    public MapLocker? Find(Guid lockerId) => _zones.SelectMany(z => z.Lockers).FirstOrDefault(l => l.LockerId == lockerId);

    /// <summary>Loads the whole map. Loading is shown while it lasts and it never blocks the interface.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        State.BeginLoading();
        try
        {
            var result = await _load(ct);
            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                return;
            }

            _zones = [.. result.Value!.Zones.Select(z => (z.ZoneId, z.ZoneName, z.Lockers.ToList()))];
            Raise(nameof(ZoneNames));
            Rebuild();
        }
        catch (OperationCanceledException)
        {
            // Closed while loading.
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "LoadLockerMap");
        }
    }

    /// <summary>
    /// Reads one locker again and updates it in place, with the counters, without reloading the map: what happens after a
    /// person is assigned to it or released from it.
    /// </summary>
    public async Task RefreshLockerAsync(Guid lockerId, CancellationToken ct = default)
    {
        try
        {
            var result = await _loadOne(lockerId, ct);
            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                return;
            }

            foreach (var zone in _zones)
            {
                var at = zone.Lockers.FindIndex(l => l.LockerId == lockerId);
                if (at < 0)
                {
                    continue;
                }

                if (result.Value is { } fresh)
                {
                    zone.Lockers[at] = fresh;
                }
                else
                {
                    zone.Lockers.RemoveAt(at);
                }

                break;
            }

            Rebuild();
        }
        catch (OperationCanceledException)
        {
            // Closed meanwhile.
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "RefreshLocker");
        }
    }

    /// <summary>Opens the detail of a locker, or closes it with null.</summary>
    public void Select(Guid? lockerId) => SelectedLockerId = lockerId;

    /// <summary>
    /// Draws a locker highlighted and opens its detail, as when it is chosen in the search: its zone opens if it was folded and
    /// the filters that would hide it are cleared, so the person always sees it.
    /// </summary>
    public void Reveal(Guid lockerId)
    {
        if (Find(lockerId) is null)
        {
            return;
        }

        var zone = _zones.First(z => z.Lockers.Any(l => l.LockerId == lockerId));
        if (_statusFilter is not null && Find(lockerId)!.Status != _statusFilter)
        {
            _statusFilter = null;
            Raise(nameof(StatusFilter));
        }

        if (_zoneFilter is not null && _zoneFilter != zone.ZoneId)
        {
            _zoneFilter = null;
            Raise(nameof(ZoneFilter));
        }

        HighlightedLockerId = lockerId;
        SelectedLockerId = lockerId;
        Rebuild();
        Zones.First(z => z.ZoneId == zone.ZoneId).Section.IsExpanded = true;
    }

    /// <summary>Takes off the filters.</summary>
    public void ClearFilters()
    {
        _statusFilter = null;
        _zoneFilter = null;
        Raise(nameof(StatusFilter));
        Raise(nameof(ZoneFilter));
        Rebuild();
    }

    void Rebuild()
    {
        var filtered = _statusFilter is not null || _zoneFilter is not null;
        var built = new List<ZoneMapViewModel>();
        foreach (var (zoneId, name, lockers) in _zones.Where(z => _zoneFilter is null || z.ZoneId == _zoneFilter))
        {
            var visible = lockers.Where(l => _statusFilter is null || l.Status == _statusFilter).ToList();
            if (filtered && visible.Count == 0)
            {
                continue;
            }

            var zone = new ZoneMap(zoneId, name, lockers, GetLockerMapHandler.Count(lockers));
            var summary = _localizer.Get("Shell.Map.ZoneSummary", zone.Counters.Active, zone.Counters.Free);
            built.Add(new ZoneMapViewModel(zone, visible, new CollapsibleSectionViewModel("zone:" + zoneId.ToString("N"), name, () => summary, _preferences)));
        }

        Zones = built;
        Raise(nameof(Counters));
        if (_zones.Sum(z => z.Lockers.Count) == 0)
        {
            State.ShowEmpty(_localizer.Get("Shell.Map.Empty"));
        }
        else if (built.Count == 0)
        {
            var clear = new AppAction("ClearFilters", _localizer.Get("Shell.Map.ClearFilters"));
            clear.Attach(ClearFilters);
            State.ShowNoResults(new EmptyStateAction(clear.Label, clear));
        }
        else
        {
            State.ShowContent();
        }
    }
}
