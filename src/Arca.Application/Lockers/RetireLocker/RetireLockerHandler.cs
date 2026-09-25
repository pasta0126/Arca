// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;

namespace Arca.Application.Lockers.RetireLocker;

/// <summary>
/// Retires a locker for good. The hooks of other capabilities (<see cref="ILockerRetiredHandler"/>) run inside the same
/// transaction, so if one of them fails the retirement is undone.
/// </summary>
public sealed class RetireLockerHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy,
    IEnumerable<ILockerRetiredHandler> hooks, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(RetireLockerRequest request, CancellationToken ct) =>
        unit.RunAsync(
            token => new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId,
                (locker, occupied) =>
                {
                    var done = locker.Retire(occupied, clock.UtcNow);
                    return done.IsSuccess ? Result<HistoryEvent?>.Success(done.Value) : Result<HistoryEvent?>.Failure(done.Error!);
                },
                token,
                async retired =>
                {
                    foreach (var hook in hooks)
                    {
                        await hook.HandleAsync(retired.Id, retired.RetiredAtUtc!.Value, token);
                    }
                }),
            ct);
}
