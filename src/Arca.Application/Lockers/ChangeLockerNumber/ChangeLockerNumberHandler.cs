// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;

namespace Arca.Application.Lockers.ChangeLockerNumber;

public sealed class ChangeLockerNumberHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(ChangeLockerNumberRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var all = await lockers.ListAsync(includeRetired: false, token);
            return await new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId,
                (locker, _) =>
                {
                    var done = locker.ChangeNumber(request.Number, all, clock.UtcNow);
                    return done.IsSuccess ? Result<HistoryEvent?>.Success(done.Value) : Result<HistoryEvent?>.Failure(done.Error!);
                },
                token);
        }, ct);
}
