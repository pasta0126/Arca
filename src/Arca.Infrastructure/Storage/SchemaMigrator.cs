// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using System.Security.Cryptography;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Arca.Infrastructure.Storage;

/// <summary>What a migration run did.</summary>
/// <param name="AppliedCount">How many migrations were applied.</param>
/// <param name="BackupPath">The safety copy made before migrating, or null when nothing was pending.</param>
public sealed record MigrationOutcome(int AppliedCount, string? BackupPath);

/// <summary>
/// Wraps EF Core migrations (arquitectura-base, D5). It never calls Migrate() blindly:
/// it refuses files from a newer version, makes and verifies a copy before changing anything,
/// migrates atomically and keeps only the most recent copies.
/// </summary>
/// <param name="createContext">Creates a context over the file being migrated (a new instance per call).</param>
/// <param name="verifyBackup">Checks a copy; by default the SQLite integrity check with the same key.</param>
/// <param name="timeProvider">Source of the time stamp in the copy name.</param>
public sealed class SchemaMigrator(
    Func<DbContext> createContext,
    Func<string, DatabaseKey, CancellationToken, Task<bool>>? verifyBackup = null,
    TimeProvider? timeProvider = null)
{
    public const int CopiesToKeep = 3;

    const string BackupMarker = ".premigration-";
    const string BackupExtension = ".bak";

    readonly Func<string, DatabaseKey, CancellationToken, Task<bool>> _verifyBackup = verifyBackup ?? IntegrityCheckAsync;
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <summary>Migrates an existing database file to the current schema.</summary>
    /// <param name="steps">Receives the resource key of each phase (copy, migrate) so the start-up screen can show it.</param>
    public async Task<Result<MigrationOutcome>> MigrateAsync(
        string path, DatabaseKey key, IProgress<string>? steps = null, CancellationToken ct = default)
    {
        string[] known;
        await using (var probe = createContext())
        {
            known = [.. probe.Database.GetMigrations()];
        }

        var applied = await ReadAppliedAsync(path, key, ct);
        if (applied.Except(known, StringComparer.Ordinal).Any())
        {
            return Result<MigrationOutcome>.Failure(StorageErrors.SchemaNewer);
        }

        var pending = known.Except(applied, StringComparer.Ordinal).Count();
        if (pending == 0)
        {
            return Result<MigrationOutcome>.Success(new MigrationOutcome(0, null));
        }

        var backup = BackupPathFor(path);
        steps?.Report("Startup.Stage.BackingUp");
        if (!await TryMakeBackupAsync(path, backup, key, ct))
        {
            return Result<MigrationOutcome>.Failure(StorageErrors.BackupFailed);
        }

        var before = Hash(path);
        steps?.Report("Startup.Stage.Migrating");
        try
        {
            await using var context = createContext();
            await RunAtomicallyAsync(context, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            SqliteConnection.ClearAllPools();
            if (!Hash(path).SequenceEqual(before))
            {
                File.Copy(backup, path, overwrite: true); // belt and braces: the file ends up byte-identical
            }

            return Result<MigrationOutcome>.Failure(StorageErrors.MigrationFailed);
        }

        DeleteOlderCopies(path);
        return Result<MigrationOutcome>.Success(new MigrationOutcome(pending, backup));
    }

    /// <summary>Applies every migration to a database that was just created: same path, no copy to make.</summary>
    public async Task ApplyToNewDatabaseAsync(CancellationToken ct = default)
    {
        await using var context = createContext();
        await RunAtomicallyAsync(context, ct);
    }

    /// <summary>The safety copies of a database, oldest first.</summary>
    public static IReadOnlyList<string> CopiesOf(string path)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var prefix = Path.GetFileName(path) + BackupMarker;
        return [.. Directory.GetFiles(folder, prefix + "*" + BackupExtension).OrderBy(f => f, StringComparer.Ordinal)];
    }

    static async Task RunAtomicallyAsync(DbContext context, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.MigrateAsync(ct);
        await transaction.CommitAsync(ct);
    }

    string BackupPathFor(string path)
    {
        var stamp = _time.GetUtcNow().ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        return path + BackupMarker + stamp + BackupExtension;
    }

    async Task<bool> TryMakeBackupAsync(string path, string backup, DatabaseKey key, CancellationToken ct)
    {
        try
        {
            File.Copy(path, backup, overwrite: false);
            if (await _verifyBackup(backup, key, ct))
            {
                return true;
            }
        }
        catch (Exception e) when (e is IOException or SqliteException or UnauthorizedAccessException)
        {
            // Falls through to the cleanup below: any failure making or checking the copy means "do not migrate".
        }

        SqliteConnection.ClearAllPools();
        File.Delete(backup);
        return false;
    }

    static void DeleteOlderCopies(string path)
    {
        var copies = CopiesOf(path);
        foreach (var old in copies.Take(Math.Max(0, copies.Count - CopiesToKeep)))
        {
            File.Delete(old);
        }
    }

    static async Task<string[]> ReadAppliedAsync(string path, DatabaseKey key, CancellationToken ct)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(ct);
        SqlCipherKeyInterceptor.Unlock(connection, key);

        await using var exists = connection.CreateCommand();
        exists.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory'";
        if (await exists.ExecuteScalarAsync(ct) is null)
        {
            return [];
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory";
        var ids = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetString(0));
        }

        return [.. ids];
    }

    static async Task<bool> IntegrityCheckAsync(string path, DatabaseKey key, CancellationToken ct)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(ct);
        SqlCipherKeyInterceptor.Unlock(connection, key);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        return string.Equals(
            Convert.ToString(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture), "ok", StringComparison.Ordinal);
    }

    static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));
}
