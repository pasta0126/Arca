// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Domain.Common;

namespace Arca.Application.Zones.ListZones;

/// <summary>The zones in Catalan alphabetical order, with the number of lockers that are not retired in each.</summary>
public sealed class ListZonesHandler(IZoneRepository zones, ILockerRepository lockers)
{
    public async Task<Result<IReadOnlyList<ZoneSummary>>> HandleAsync(ListZonesRequest request, CancellationToken ct)
    {
        var all = await zones.ListAsync(ct);
        var counts = (await lockers.ListAsync(includeRetired: false, ct)).GroupBy(l => l.ZoneId).ToDictionary(g => g.Key, g => g.Count());

        IReadOnlyList<ZoneSummary> listed =
        [
            .. all
                .Where(z => request.IncludeInactive || z.IsActive)
                .OrderBy(z => z.Name, TextComparer.Comparer)
                .Select(z => ZoneSummary.Of(z, counts.GetValueOrDefault(z.Id)))
        ];
        return Result<IReadOnlyList<ZoneSummary>>.Success(listed);
    }
}
