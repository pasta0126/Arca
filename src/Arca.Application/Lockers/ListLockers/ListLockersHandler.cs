// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.ListLockers;

/// <summary>
/// Lists the lockers with their filters and counters (taquilles-i-zones, D9). With this volume it loads the lockers and
/// their zones and computes the status in memory with the one function that knows it, then filters. It loads
/// everything it needs explicitly and asks the assignments about occupancy once for all the lockers.
/// </summary>
public sealed class ListLockersHandler(ILockerRepository lockers, IZoneRepository zones, ILockerOccupancy occupancy)
{
    public async Task<Result<LockerListing>> HandleAsync(ListLockersRequest request, CancellationToken ct)
    {
        var filter = request.Filter ?? new LockerFilter();
        var all = await lockers.ListAsync(includeRetired: true, ct);
        var names = (await zones.ListAsync(ct)).ToDictionary(z => z.Id, z => z.Name);
        var occupied = await occupancy.OccupiedAmongAsync([.. all.Where(l => !l.IsRetired).Select(l => l.Id)], ct);

        var rows = all
            .Select(l => LockerRow.Of(l, names.GetValueOrDefault(l.ZoneId, string.Empty), occupied.Contains(l.Id)))
            .ToList();

        var includeRetired = filter.IncludeRetired || filter.Status == LockerStatus.Retired;
        IReadOnlyList<LockerRow> matching =
        [
            .. rows
                .Where(r => includeRetired || !r.IsRetired)
                .Where(r => filter.ZoneId is null || r.ZoneId == filter.ZoneId)
                .Where(r => filter.Status is null || r.Status == filter.Status)
                .Where(r => filter.Number is null || r.Number == filter.Number)
                .OrderBy(r => r.Number)
                .ThenBy(r => r.IsRetired) // an active locker comes before a retired one with the same number
                .ThenBy(r => r.ZoneName, TextComparer.Comparer)
        ];

        var active = rows.Where(r => !r.IsRetired).ToList();
        IReadOnlyList<ZoneCounters> byZone =
        [
            .. active
                .GroupBy(r => r.ZoneId)
                .Select(g => new ZoneCounters(g.Key, names.GetValueOrDefault(g.Key, string.Empty), Count(g)))
                .OrderBy(z => z.ZoneName, TextComparer.Comparer)
        ];
        return Result<LockerListing>.Success(new LockerListing(matching, Count(active), byZone));
    }

    static LockerCounters Count(IEnumerable<LockerRow> rows)
    {
        var list = rows.ToList();
        return new LockerCounters(
            list.Count,
            list.Count(r => r.Status == LockerStatus.Free),
            list.Count(r => r.Status == LockerStatus.Occupied),
            list.Count(r => r.Status == LockerStatus.Broken),
            list.Count(r => r.Status == LockerStatus.Maintenance),
            list.Count(r => r.Status == LockerStatus.Reserved));
    }
}
