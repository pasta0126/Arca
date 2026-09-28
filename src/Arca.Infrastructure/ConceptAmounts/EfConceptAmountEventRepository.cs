// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Domain.Common;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.ConceptAmounts;

sealed class EfConceptAmountEventRepository(EfInventory owner) : IConceptAmountEventRepository
{
    public Task AddAsync(HistoryEvent change, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<ConceptAmountEventRow>().Add(new ConceptAmountEventRow
        {
            ConceptAmountId = change.EntityId,
            Type = change.Type,
            OccurredAtUtc = change.OccurredAtUtc,
            BeforeJson = change.BeforeJson,
            AfterJson = change.AfterJson,
            Reason = change.Reason,
        });
        await context.SaveChangesAsync(ct);
    });

    public async Task<IReadOnlyList<HistoryEvent>> ListAsync(Guid conceptAmountId, CancellationToken ct) =>
        await owner.UseAsync(async context =>
        {
            var rows = await context.Set<ConceptAmountEventRow>()
                .AsNoTracking()
                .Where(e => e.ConceptAmountId == conceptAmountId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .ThenByDescending(e => e.Id)
                .ToListAsync(ct);
            return (IReadOnlyList<HistoryEvent>)[.. rows.Select(e => new HistoryEvent(e.ConceptAmountId, e.Type, e.OccurredAtUtc, e.BeforeJson, e.AfterJson, e.Reason))];
        });
}
