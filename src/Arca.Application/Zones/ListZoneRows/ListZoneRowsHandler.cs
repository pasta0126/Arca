// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Domain.Common;
using Arca.Domain.Zones;

namespace Arca.Application.Zones.ListZoneRows;

/// <summary>
/// A zone as the Zones view shows it: its name, whether it is active, how many active lockers it has and, for each operation, the
/// reason it is refused now or null when it can be done.
/// </summary>
public sealed record ZoneRow(
    Guid Id, string Name, bool IsActive, int ActiveLockers, Error? DeactivationBlocked, Error? ReactivationBlocked, Error? DeletionBlocked);

/// <summary>
/// Every zone, active or not, ordered as Catalan is read, each with the reasons of its operations. A reason is what the domain rule
/// answers when it is tried on a copy of the zone, so the screen and the operation never disagree. It changes nothing.
/// </summary>
public sealed class ListZoneRowsHandler(IZoneRepository zones, ILockerRepository lockers)
{
    public async Task<Result<IReadOnlyList<ZoneRow>>> HandleAsync(CancellationToken ct)
    {
        var all = await zones.ListAsync(ct);
        var counts = (await lockers.ListAsync(includeRetired: false, ct)).GroupBy(l => l.ZoneId).ToDictionary(g => g.Key, g => g.Count());
        var rows = new List<ZoneRow>();
        foreach (var zone in all.OrderBy(z => z.Name, TextComparer.Comparer))
        {
            var active = counts.GetValueOrDefault(zone.Id);
            Zone Copy() => new(zone.Id, zone.Name, zone.NameKey, zone.IsActive);
            rows.Add(new ZoneRow(
                zone.Id, zone.Name, zone.IsActive, active,
                zone.IsActive ? Copy().Deactivate(active).Error : null,
                zone.IsActive ? null : Copy().Reactivate(all).Error,
                Copy().CheckCanDelete(await lockers.HasEverHadLockersAsync(zone.Id, ct)).Error));
        }

        return Result<IReadOnlyList<ZoneRow>>.Success(rows);
    }
}
