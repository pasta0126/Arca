// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Domain.Common;
using Arca.Domain.Zones;

namespace Arca.Application.Zones.CreateZone;

public sealed class CreateZoneHandler(IZoneRepository zones, IUnitOfWork unit)
{
    public Task<Result<ZoneSummary>> HandleAsync(CreateZoneRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var created = Zone.Create(Guid.NewGuid(), request.Name, await zones.ListAsync(token));
            if (!created.IsSuccess)
            {
                return Result<ZoneSummary>.Failure(created.Error!);
            }

            await zones.AddAsync(created.Value!, token);
            return Result<ZoneSummary>.Success(ZoneSummary.Of(created.Value!, activeLockers: 0));
        }, ct);
}
