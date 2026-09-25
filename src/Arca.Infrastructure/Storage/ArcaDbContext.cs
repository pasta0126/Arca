// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;
using Arca.Infrastructure.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// The EF Core context over the encrypted file. Lazy loading is off (arquitectura-base, D15):
/// relations are loaded explicitly in each query. Each capability adds its own configuration.
/// </summary>
public class ArcaDbContext : DbContext
{
    readonly string _path;
    readonly DatabaseKey _key;
    readonly bool _create;

    public ArcaDbContext(string path, DatabaseKey key, bool create = false)
    {
        _path = path;
        _key = key;
        _create = create;
        ChangeTracker.LazyLoadingEnabled = false; // explicit, so it stays off even if a proxy package were ever added
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZoneConfiguration).Assembly);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // ReadWrite never creates a file: only CreateAsync asks for it, so opening cannot make a new empty database.
        var mode = _create ? "ReadWriteCreate" : "ReadWrite";
        optionsBuilder
            .UseSqlite($"Data Source={_path};Mode={mode};Pooling=False")
            .AddInterceptors(new SqlCipherKeyInterceptor(_key));
    }
}
