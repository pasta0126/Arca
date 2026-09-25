// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Zones;

namespace Arca.Application.Lockers.ChangeLockerZone;

public sealed class ChangeLockerZoneHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(ChangeLockerZoneRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var zone = await zones.GetAsync(request.ZoneId, token);
            if (zone is null)
            {
                return Result<LockerRow>.Failure(ZoneErrors.NotFound);
            }

            return await new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId,
                (locker, _) =>
                {
                    var done = locker.ChangeZone(zone, clock.UtcNow);
                    return done.IsSuccess ? Result<HistoryEvent?>.Success(done.Value) : Result<HistoryEvent?>.Failure(done.Error!);
                },
                token);
        }, ct);
}
