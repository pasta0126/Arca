// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges;
using Arca.Domain.Common;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Charges;

sealed class EfChargeEventRepository(EfInventory owner) : IChargeEventRepository
{
    public Task AddAsync(HistoryEvent change, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<ChargeEventRow>().Add(new ChargeEventRow
        {
            ChargeId = change.EntityId,
            Type = change.Type,
            OccurredAtUtc = change.OccurredAtUtc,
            BeforeJson = change.BeforeJson,
            AfterJson = change.AfterJson,
            Reason = change.Reason,
        });
        await context.SaveChangesAsync(ct);
    });

    public async Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid chargeId, CancellationToken ct) =>
        await owner.UseAsync(async context =>
        {
            var rows = await context.Set<ChargeEventRow>()
                .AsNoTracking()
                .Where(e => e.ChargeId == chargeId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .ThenByDescending(e => e.Id)
                .ToListAsync(ct);
            return (IReadOnlyList<HistoryEvent>)[.. rows.Select(e => new HistoryEvent(e.ChargeId, e.Type, e.OccurredAtUtc, e.BeforeJson, e.AfterJson, e.Reason))];
        });
}
