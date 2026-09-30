// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Storage;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Storage;

/// <summary>An open database with everything that must stay alive while the application runs.</summary>
public sealed class StorageSession : IAsyncDisposable
{
    readonly InstanceLock _lock;
    readonly DatabaseKey _key;

    internal StorageSession(string databasePath, InstanceLock instanceLock, DatabaseKey key, string schemaVersion, bool created)
    {
        DatabasePath = databasePath;
        _lock = instanceLock;
        _key = key;
        SchemaVersion = schemaVersion;
        WasCreated = created;
    }

    public string DatabasePath { get; }

    /// <summary>The last migration applied to the open database.</summary>
    /// <summary>The key of the open data, for the services of this assembly that work on the file itself (backups). Never leaves Infrastructure.</summary>
    internal DatabaseKey Key => _key;

    public string SchemaVersion { get; }

    public bool WasCreated { get; }

    /// <summary>A new context over the open database. Each user of it disposes its own.</summary>
    public ArcaDbContext CreateContext() => new(DatabasePath, _key);

    public AppInfo Info(string applicationVersion) => new(applicationVersion, SchemaVersion);

    public ValueTask DisposeAsync()
    {
        _key.Dispose();
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }

    internal static async Task<string> ReadSchemaVersionAsync(ArcaDbContext context, CancellationToken ct)
    {
        var applied = await context.Database.GetAppliedMigrationsAsync(ct);
        return applied.LastOrDefault() ?? "0";
    }
}
