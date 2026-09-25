// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Inventory;

/// <summary>The history over the database. It only adds and lists: there is no way to update or delete an event.</summary>
sealed class EfLockerEventRepository(EfInventory owner) : ILockerEventRepository
{
    public Task AddAsync(HistoryEvent change, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<LockerEventRow>().Add(new LockerEventRow
        {
            LockerId = change.EntityId,
            Type = change.Type,
            OccurredAtUtc = change.OccurredAtUtc,
            BeforeJson = change.BeforeJson,
            AfterJson = change.AfterJson,
            Reason = change.Reason,
        });
        await context.SaveChangesAsync(ct);
    });

    public async Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid lockerId, CancellationToken ct) =>
        await owner.UseAsync(async context =>
        {
            var rows = await context.Set<LockerEventRow>()
                .AsNoTracking()
                .Where(e => e.LockerId == lockerId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .ThenByDescending(e => e.Id)
                .ToListAsync(ct);
            return (IReadOnlyList<HistoryEvent>)[.. rows.Select(e => new HistoryEvent(e.LockerId, e.Type, e.OccurredAtUtc, e.BeforeJson, e.AfterJson, e.Reason))];
        });
}
