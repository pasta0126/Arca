// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;

namespace Arca.Application.Lockers;

/// <summary>
/// The steps every change of one locker shares (taquilles-i-zones, D7): load it, ask whether a student holds it, apply the
/// rule, save it and, in the same transaction, record the event the rule returns. It must run inside the unit of work.
/// </summary>
internal sealed class LockerChanges(
    ILockerRepository lockers, IZoneRepository zones, ILockerEventRepository events, ILockerOccupancy occupancy)
{
    /// <param name="rule">The rule to apply. It returns the event to record, or null when nothing changed.</param>
    public async Task<Result<LockerRow>> ApplyAsync(
        Guid lockerId, Func<Locker, bool, Result<HistoryEvent?>> rule, CancellationToken ct, Func<Locker, Task>? afterSaved = null)
    {
        var locker = await lockers.GetAsync(lockerId, ct);
        if (locker is null)
        {
            return Result<LockerRow>.Failure(LockerErrors.NotFound);
        }

        var occupied = (await occupancy.OccupiedAmongAsync([lockerId], ct)).Contains(lockerId);
        var applied = rule(locker, occupied);
        if (!applied.IsSuccess)
        {
            return Result<LockerRow>.Failure(applied.Error!);
        }

        if (applied.Value is { } change)
        {
            await lockers.UpdateAsync(locker, ct);
            await events.AddAsync(change, ct);
            if (afterSaved is not null)
            {
                await afterSaved(locker);
            }
        }

        return Result<LockerRow>.Success(await RowAsync(locker, occupied, ct), [.. applied.Notices]);
    }

    public async Task<LockerRow> RowAsync(Locker locker, bool occupied, CancellationToken ct)
    {
        var zone = await zones.GetAsync(locker.ZoneId, ct);
        return LockerRow.Of(locker, zone?.Name ?? string.Empty, occupied);
    }
}
