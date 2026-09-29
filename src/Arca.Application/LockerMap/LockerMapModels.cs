// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Application.Search;

namespace Arca.Application.LockerMap;

/// <summary>
/// A locker as the map draws it: its number, its visible status, who holds it and whether they owe anything. It carries no
/// email and no identifier of the student, only the name to recognise them (ui-shell, Detalle y Mapa).
/// </summary>
public sealed record MapLocker(Guid LockerId, int Number, LockerStatusView Status, Guid? StudentId, string? StudentName, bool HasDebt);

/// <summary>A zone of the map with its lockers, by number, and how many there are in each status.</summary>
public sealed record ZoneMap(Guid ZoneId, string ZoneName, IReadOnlyList<MapLocker> Lockers, LockerCounters Counters);

/// <summary>Every active zone with its active lockers, and the counts in all.</summary>
public sealed record LockerMapData(IReadOnlyList<ZoneMap> Zones, LockerCounters Counters)
{
    public static LockerMapData Empty { get; } = new([], new LockerCounters(0, 0, 0, 0, 0, 0));

    public bool IsEmpty => Zones.Sum(z => z.Lockers.Count) == 0;
}
