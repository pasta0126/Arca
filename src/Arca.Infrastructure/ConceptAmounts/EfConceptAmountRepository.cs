// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Domain.ConceptAmounts;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.ConceptAmounts;

sealed class EfConceptAmountRepository(EfInventory owner) : IConceptAmountRepository
{
    public async Task<IReadOnlyList<ConceptAmount>> ListByYearAsync(Guid yearId, CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<ConceptAmount>)await context.Set<ConceptAmount>().Where(a => a.YearId == yearId).ToListAsync(ct));

    public Task AddAsync(ConceptAmount amount, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<ConceptAmount>().Add(amount);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(ConceptAmount amount, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));
}
