// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Storage;

/// <summary>Creates and opens the single encrypted database file.</summary>
public static class ArcaDatabase
{
    /// <summary>Stored in the file header (PRAGMA application_id): the ASCII bytes "ARCA".</summary>
    public const int ApplicationId = 0x41524341;

    const int SqliteCorrupt = 11;
    const int SqliteNotADatabase = 26;

    /// <summary>
    /// Creates a new database with the current schema. It never overwrites an existing file, and if
    /// creation fails it removes the partial file it created.
    /// </summary>
    public static async Task<Result<ArcaDbContext>> CreateAsync(string path, DatabaseKey key, CancellationToken ct = default)
    {
        if (File.Exists(path))
        {
            return Result<ArcaDbContext>.Failure(StorageErrors.FileAlreadyExists(path));
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }

        var context = new ArcaDbContext(path, key, create: true);
        try
        {
            await new SchemaMigrator(() => new ArcaDbContext(path, key, create: true)).ApplyToNewDatabaseAsync(ct);
            await context.Database.ExecuteSqlRawAsync($"PRAGMA application_id = {ApplicationId}", ct);
            return Result<ArcaDbContext>.Success(context);
        }
        catch
        {
            await context.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(path);
            throw;
        }
    }

    /// <summary>
    /// Opens an existing database. The file is first probed read-only, so a damaged file, a file that is
    /// not a database or one that does not belong to ARCA is reported without being modified.
    /// </summary>
    public static async Task<Result<ArcaDbContext>> OpenAsync(
        string path, DatabaseKey key, IProgress<string>? steps = null, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            return Result<ArcaDbContext>.Failure(StorageErrors.FileNotFound(path));
        }

        var probe = await ProbeAsync(path, key, ct);
        if (probe is not null)
        {
            return Result<ArcaDbContext>.Failure(probe);
        }

        var migrated = await new SchemaMigrator(() => new ArcaDbContext(path, key)).MigrateAsync(path, key, steps, ct);
        return migrated.IsSuccess
            ? Result<ArcaDbContext>.Success(new ArcaDbContext(path, key))
            : Result<ArcaDbContext>.Failure(migrated.Error!);
    }

    static async Task<Error?> ProbeAsync(string path, DatabaseKey key, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync(ct);
            SqlCipherKeyInterceptor.Unlock(connection, key);

            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA application_id";
            var id = Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
            return id == ApplicationId ? null : StorageErrors.NotArcaDatabase(path);
        }
        catch (SqliteException e) when (e.SqliteErrorCode is SqliteNotADatabase or SqliteCorrupt)
        {
            return StorageErrors.Unreadable(path);
        }
    }
}
