// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Domain.Home;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Home;

sealed class EfHomeCardRepository(EfInventory owner) : IHomeCardRepository
{
    public async Task<IReadOnlyList<HomeCard>> ListAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<HomeCard>)await context.Set<HomeCard>().OrderBy(c => c.Position).ToListAsync(ct));

    public Task<HomeCard?> GetAsync(Guid id, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<HomeCard>().FirstOrDefaultAsync(c => c.Id == id, ct));

    public Task AddAsync(HomeCard card, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<HomeCard>().Add(card);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(HomeCard card, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));

    public Task RemoveAsync(HomeCard card, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<HomeCard>().Remove(card);
        await context.SaveChangesAsync(ct);
    });

    public async Task<DateTimeOffset?> GetDefaultsCreatedAtAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (await context.Set<HomeCardsState>().FirstOrDefaultAsync(ct))?.DefaultsCreatedAtUtc);

    public Task MarkDefaultsCreatedAsync(DateTimeOffset at, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        if (!await context.Set<HomeCardsState>().AnyAsync(ct))
        {
            context.Set<HomeCardsState>().Add(new HomeCardsState(HomeCardsState.SingleId, at));
            await context.SaveChangesAsync(ct);
        }
    });
}
