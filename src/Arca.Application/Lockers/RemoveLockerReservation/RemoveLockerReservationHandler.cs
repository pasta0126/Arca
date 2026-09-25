// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;

namespace Arca.Application.Lockers.RemoveLockerReservation;

public sealed class RemoveLockerReservationHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(RemoveLockerReservationRequest request, CancellationToken ct) =>
        unit.RunAsync(
            token => new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId,
                (locker, _) =>
                {
                    var done = locker.RemoveReservation(clock.UtcNow);
                    return done.IsSuccess ? Result<HistoryEvent?>.Success(done.Value) : Result<HistoryEvent?>.Failure(done.Error!);
                },
                token),
            ct);
}
