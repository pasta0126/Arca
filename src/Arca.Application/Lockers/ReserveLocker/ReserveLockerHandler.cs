// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;

namespace Arca.Application.Lockers.ReserveLocker;

public sealed class ReserveLockerHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(ReserveLockerRequest request, CancellationToken ct) =>
        unit.RunAsync(
            token => new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId, (locker, occupied) => Nullable(locker.Reserve(request.Note, occupied, clock.UtcNow)), token),
            ct);

    static Result<HistoryEvent?> Nullable(Result<HistoryEvent> result) =>
        result.IsSuccess ? Result<HistoryEvent?>.Success(result.Value) : Result<HistoryEvent?>.Failure(result.Error!);
}
