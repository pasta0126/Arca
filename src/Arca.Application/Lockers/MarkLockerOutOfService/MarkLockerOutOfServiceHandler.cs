// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.MarkLockerOutOfService;

/// <summary>
/// Puts a locker out of service as broken or in maintenance (taquilles-i-zones, D3). An occupied locker asks for a
/// decision first and is not changed without one.
/// </summary>
public sealed class MarkLockerOutOfServiceHandler(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy, IUnitOfWork unit, IClock clock)
{
    public Task<Result<MarkLockerOutOfServiceResult>> HandleAsync(MarkLockerOutOfServiceRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            OutOfServiceOutcome? outcome = null;
            var applied = await new LockerChanges(lockers, zones, events, occupancy).ApplyAsync(
                request.LockerId,
                (locker, occupied) =>
                {
                    var marked = locker.MarkOutOfService(request.Kind, request.Decision, occupied, clock.UtcNow);
                    outcome = marked.Value;
                    return marked.IsSuccess ? Result<HistoryEvent?>.Success(marked.Value!.Event) : Result<HistoryEvent?>.Failure(marked.Error!);
                },
                token);
            return applied.IsSuccess
                ? Result<MarkLockerOutOfServiceResult>.Success(
                    new MarkLockerOutOfServiceResult(applied.Value!, outcome!.NeedsDecision, outcome.DecisionsOffered))
                : Result<MarkLockerOutOfServiceResult>.Failure(applied.Error!);
        }, ct);
}
