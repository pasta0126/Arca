// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;

namespace Arca.Application.Lockers.RestoreLockerService;

/// <summary>Ends a breakdown or a maintenance. The status is recomputed from the facts; nothing else is done.</summary>
public sealed class RestoreLockerServiceHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(RestoreLockerServiceRequest request, CancellationToken ct) =>
        unit.RunAsync(
            token => new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId,
                (locker, _) =>
                {
                    var done = locker.RestoreService(clock.UtcNow);
                    return done.IsSuccess ? Result<HistoryEvent?>.Success(done.Value) : Result<HistoryEvent?>.Failure(done.Error!);
                },
                token),
            ct);
}
