// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.GetLocker;

/// <summary>
/// The detail of one locker, asked for when it is opened (taquilles-i-zones, D9b): the list carries what a row needs
/// and this loads the rest explicitly, with the history asked for separately. Nothing is loaded lazily.
/// </summary>
public sealed class GetLockerHandler(ILockerRepository lockers, IZoneRepository zones, ILockerOccupancy occupancy)
{
    public async Task<Result<LockerRow>> HandleAsync(GetLockerRequest request, CancellationToken ct)
    {
        var locker = await lockers.GetAsync(request.LockerId, ct);
        if (locker is null)
        {
            return Result<LockerRow>.Failure(LockerErrors.NotFound);
        }

        var occupied = (await occupancy.OccupiedAmongAsync([locker.Id], ct)).Contains(locker.Id);
        var zone = await zones.GetAsync(locker.ZoneId, ct);
        return Result<LockerRow>.Success(LockerRow.Of(locker, zone?.Name ?? string.Empty, occupied));
    }
}
