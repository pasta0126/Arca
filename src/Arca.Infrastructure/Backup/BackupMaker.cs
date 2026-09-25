// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace Arca.Infrastructure.Backup;

/// <summary>
/// Makes a backup file (acces-i-xifrat, D7): a consistent snapshot of the open database, taken with SQLite's online
/// backup so a half-written state is never copied, together with the current key file. Because the application is
/// unlocked, the copy is verified at once with the key in memory, and a copy that fails the check is deleted.
/// </summary>
public static class BackupMaker
{
    public static async Task<Result<string>> CreateAsync(
        string databasePath, DatabaseKey key, string containerPath, CancellationToken ct = default)
    {
        var keyFile = KeyFileStore.PathFor(databasePath);
        if (!File.Exists(keyFile))
        {
            return Result<string>.Failure(Arca.Application.Security.KeyErrors.FileMissing(keyFile));
        }

        var work = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(containerPath))!, ".arca-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var snapshot = Path.Combine(work, "snapshot.db");
            Snapshot(databasePath, snapshot, key);
            var unsound = await DatabaseVerifier.VerifyAsync(snapshot, key, ct);
            if (unsound is not null)
            {
                return Result<string>.Failure(unsound);
            }

            BackupContainer.Write(containerPath, snapshot, keyFile);

            // The container itself is checked too: what was written is what will be restored.
            var extracted = BackupContainer.Extract(containerPath, Path.Combine(work, "check"));
            var error = extracted.Error ?? await DatabaseVerifier.VerifyAsync(extracted.Value!.DatabasePath, key, ct);
            if (error is not null)
            {
                File.Delete(containerPath);
                return Result<string>.Failure(error);
            }

            return Result<string>.Success(containerPath);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(work, recursive: true);
        }
    }

    static void Snapshot(string databasePath, string snapshotPath, DatabaseKey key)
    {
        using var source = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
        source.Open();
        SqlCipherKeyInterceptor.Unlock(source, key);
        using var destination = new SqliteConnection($"Data Source={snapshotPath};Pooling=False");
        destination.Open();
        SqlCipherKeyInterceptor.Unlock(destination, key);
        source.BackupDatabase(destination);
    }
}
