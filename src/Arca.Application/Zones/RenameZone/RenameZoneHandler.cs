// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Lockers;
using Arca.Domain.Common;
using Arca.Domain.Zones;

namespace Arca.Application.Zones.RenameZone;

public sealed class RenameZoneHandler(IZoneRepository zones, ILockerRepository lockers, IUnitOfWork unit)
{
    public Task<Result<ZoneSummary>> HandleAsync(RenameZoneRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var zone = await zones.GetAsync(request.ZoneId, token);
            if (zone is null)
            {
                return Result<ZoneSummary>.Failure(ZoneErrors.NotFound);
            }

            var renamed = zone.Rename(request.Name, await zones.ListAsync(token));
            if (!renamed.IsSuccess)
            {
                return Result<ZoneSummary>.Failure(renamed.Error!);
            }

            await zones.UpdateAsync(zone, token);
            return Result<ZoneSummary>.Success(ZoneSummary.Of(zone, await lockers.CountActiveInZoneAsync(zone.Id, token)));
        }, ct);
}
