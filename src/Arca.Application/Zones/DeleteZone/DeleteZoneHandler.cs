// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Lockers;
using Arca.Domain.Common;
using Arca.Domain.Zones;

namespace Arca.Application.Zones.DeleteZone;

/// <summary>Deletes a zone that never had a locker. Returns the identity of the zone that was removed.</summary>
public sealed class DeleteZoneHandler(IZoneRepository zones, ILockerRepository lockers, IUnitOfWork unit)
{
    public Task<Result<Guid>> HandleAsync(DeleteZoneRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var zone = await zones.GetAsync(request.ZoneId, token);
            if (zone is null)
            {
                return Result<Guid>.Failure(ZoneErrors.NotFound);
            }

            var allowed = zone.CheckCanDelete(await lockers.HasEverHadLockersAsync(zone.Id, token));
            if (!allowed.IsSuccess)
            {
                return Result<Guid>.Failure(allowed.Error!);
            }

            await zones.RemoveAsync(zone, token);
            return Result<Guid>.Success(zone.Id);
        }, ct);
}
