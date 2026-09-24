// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// The EF Core context over the encrypted file. Lazy loading is off (arquitectura-base, D15):
/// relations are loaded explicitly in each query. Entities arrive with each domain capability.
/// </summary>
public sealed class ArcaDbContext(string path, DatabaseKey key, bool create = false) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // ReadWrite never creates a file: only CreateAsync asks for it, so opening cannot make a new empty database.
        var mode = create ? "ReadWriteCreate" : "ReadWrite";
        optionsBuilder
            .UseSqlite($"Data Source={path};Mode={mode};Pooling=False")
            .AddInterceptors(new SqlCipherKeyInterceptor(key));
    }
}
