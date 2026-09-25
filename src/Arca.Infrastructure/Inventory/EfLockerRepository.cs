// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Lockers;
using Arca.Domain.Lockers;
using Arca.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Inventory;

sealed class EfLockerRepository(EfInventory owner) : ILockerRepository
{
    public async Task<IReadOnlyList<Locker>> ListAsync(bool includeRetired, CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Locker>)await context.Set<Locker>()
            .Where(l => includeRetired || l.RetiredAtUtc == null)
            .OrderBy(l => l.Number)
            .ToListAsync(ct));

    public Task<Locker?> GetAsync(Guid id, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Locker>().FirstOrDefaultAsync(l => l.Id == id, ct));

    public Task AddAsync(Locker locker, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Locker>().Add(locker);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(Locker locker, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));

    public Task<int> CountActiveInZoneAsync(Guid zoneId, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Locker>().CountAsync(l => l.ZoneId == zoneId && l.RetiredAtUtc == null, ct));

    public Task<bool> HasEverHadLockersAsync(Guid zoneId, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Locker>().AnyAsync(l => l.ZoneId == zoneId, ct));
}
