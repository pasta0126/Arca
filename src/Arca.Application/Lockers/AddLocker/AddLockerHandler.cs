// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;

namespace Arca.Application.Lockers.AddLocker;

/// <summary>Creates one free locker and records its creation in the same transaction.</summary>
public sealed class AddLockerHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, IUnitOfWork unit, IClock clock)
{
    public Task<Result<LockerRow>> HandleAsync(AddLockerRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var zone = await zones.GetAsync(request.ZoneId, token);
            if (zone is null)
            {
                return Result<LockerRow>.Failure(ZoneErrors.NotFound);
            }

            var created = Locker.Create(
                Guid.NewGuid(), request.Number, zone, request.Note, await lockers.ListAsync(includeRetired: false, token), clock.UtcNow);
            if (!created.IsSuccess)
            {
                return Result<LockerRow>.Failure(created.Error!);
            }

            await lockers.AddAsync(created.Value!.Locker, token);
            await events.AddAsync(created.Value.Event, token);
            return Result<LockerRow>.Success(LockerRow.Of(created.Value.Locker, zone.Name, hasAssignment: false));
        }, ct);
}
