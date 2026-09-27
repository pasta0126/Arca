// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Application.Zones;
using Arca.Domain.Assignments;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Assignments.SuggestLocker;

/// <summary>
/// Suggests the free locker with the lowest number in the zone chosen and, if that zone has none, in the next zone that has
/// (zones in Catalan order, going round to the start). If no locker is free anywhere, it says so.
/// </summary>
public sealed class SuggestLockerHandler(ILockerRepository lockers, IZoneRepository zones, ILockerOccupancy occupancy)
{
    public async Task<Result<LockerSuggestion>> HandleAsync(SuggestLockerRequest request, CancellationToken ct)
    {
        var active = (await zones.ListAsync(ct)).Where(z => z.IsActive).OrderBy(z => z.Name, TextComparer.Comparer).ToList();
        var all = await lockers.ListAsync(includeRetired: false, ct);
        var occupied = await occupancy.OccupiedAmongAsync([.. all.Select(l => l.Id)], ct);
        var free = all.Where(l => l.StateWith(occupied.Contains(l.Id)).Status == LockerStatus.Free).ToList();

        var start = request.ZoneId is { } id ? Math.Max(0, active.FindIndex(z => z.Id == id)) : 0;
        for (var step = 0; step < active.Count; step++)
        {
            var zone = active[(start + step) % active.Count];
            var lowest = free.Where(l => l.ZoneId == zone.Id).OrderBy(l => l.Number).FirstOrDefault();
            if (lowest is not null)
            {
                return Result<LockerSuggestion>.Success(new LockerSuggestion(LockerRow.Of(lowest, zone.Name, hasAssignment: false), step > 0));
            }
        }

        return Result<LockerSuggestion>.Failure(AssignmentErrors.NoFreeLockers);
    }
}
