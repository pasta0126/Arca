// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Identity;
using Arca.Domain.Identity;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Identity;

sealed class EfCentreIdentityRepository(EfInventory owner) : ICentreIdentityRepository
{
    public Task<CentreIdentity?> GetAsync(CancellationToken ct) =>
        owner.UseAsync(context => context.Set<CentreIdentity>().FirstOrDefaultAsync(ct));

    public Task AddAsync(CentreIdentity identity, CancellationToken ct) => owner.WriteAsync(async context =>
    {
        context.Set<CentreIdentity>().Add(identity);
        await context.SaveChangesAsync(ct);
    });

    public Task UpdateAsync(CentreIdentity identity, CancellationToken ct) => owner.WriteAsync(context => context.SaveChangesAsync(ct));
}
