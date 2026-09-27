// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog;
using Arca.Domain.Catalog;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Catalog;

sealed class EfCatalogRepository(EfInventory owner) : ICatalogRepository
{
    public async Task<IReadOnlyList<Level>> ListLevelsAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Level>)await context.Set<Level>().ToListAsync(ct));

    public async Task<IReadOnlyList<Group>> ListGroupsAsync(CancellationToken ct) =>
        await owner.UseAsync(async context => (IReadOnlyList<Group>)await context.Set<Group>().ToListAsync(ct));

    public Task AddLevelAsync(Level level, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Level>().Add(level);
        await context.SaveChangesAsync(ct);
    });

    public Task AddGroupAsync(Group group, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<Group>().Add(group);
        await context.SaveChangesAsync(ct);
    });
}
