// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Zones;
using Arca.Domain.Zones;
using Arca.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Inventory;

sealed class EfZoneRepository(EfInventory owner) : IZoneRepository
{
    public async Task<IReadOnlyList<Zone>> ListAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Zone>)await context.Set<Zone>().ToListAsync(ct));

    public Task<Zone?> GetAsync(Guid id, CancellationToken ct) =>
        owner.UseAsync(context => context.Set<Zone>().FirstOrDefaultAsync(z => z.Id == id, ct));

    public Task AddAsync(Zone zone, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Zone>().Add(zone);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(Zone zone, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));

    public Task RemoveAsync(Zone zone, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Zone>().Remove(zone);
        await context.SaveChangesAsync(ct);
    });
}
