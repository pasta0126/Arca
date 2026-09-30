// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Backup;
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
    /// <summary>
    /// Makes a backup and gives back where it is and how big. The copy is written next to the destination and checked there, and only a
    /// checked one takes the final name, so a failure or a cancellation leaves no partial file and an older file of the same name intact.
    /// </summary>
    /// <param name="steps">Told the stage it is in, by its resource key, so the screen shows what is really happening.</param>
    public static async Task<Result<BackupResult>> MakeAsync(
        string databasePath, DatabaseKey key, string containerPath, IProgress<string>? steps = null, CancellationToken ct = default)
    {
        var refused = BackupDestination.Check(containerPath, databasePath);
        if (refused is not null)
        {
            return Result<BackupResult>.Failure(refused);
        }

        var keyFile = KeyFileStore.PathFor(databasePath);
        if (!File.Exists(keyFile))
        {
            return Result<BackupResult>.Failure(Arca.Application.Security.KeyErrors.FileMissing(keyFile));
        }

        var work = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(containerPath))!, ".arca-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            ct.ThrowIfCancellationRequested();
            steps?.Report("Backup.Stage.Copying");
            var snapshot = Path.Combine(work, "snapshot.db");
            Snapshot(databasePath, snapshot, key);
            steps?.Report("Backup.Stage.Verifying");
            var unsound = await DatabaseVerifier.VerifyAsync(snapshot, key, ct);
            if (unsound is not null)
            {
                return Result<BackupResult>.Failure(BackupErrors.CopyFailed);
            }

            var candidate = Path.Combine(work, "backup" + BackupContainer.Extension);
            BackupContainer.Write(candidate, snapshot, keyFile);

            // The container itself is checked too: what was written is what will be restored.
            var extracted = BackupContainer.Extract(candidate, Path.Combine(work, "check"));
            var error = extracted.Error ?? await DatabaseVerifier.VerifyAsync(extracted.Value!.DatabasePath, key, ct);
            if (error is not null)
            {
                return Result<BackupResult>.Failure(BackupErrors.CopyFailed);
            }

            ct.ThrowIfCancellationRequested(); // the last point where giving up leaves everything as it was
            steps?.Report("Backup.Stage.Finishing");
            File.Move(candidate, containerPath, overwrite: true);
            return Result<BackupResult>.Success(new BackupResult(containerPath, new FileInfo(containerPath).Length));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SqliteException)
        {
            return Result<BackupResult>.Failure(BackupErrors.CopyFailed);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try
            {
                Directory.Delete(work, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A leftover work folder is harmless.
            }
        }
    }

    /// <summary>Makes a backup and gives back its path; see <see cref="MakeAsync"/>.</summary>
    public static async Task<Result<string>> CreateAsync(
        string databasePath, DatabaseKey key, string containerPath, CancellationToken ct = default)
    {
        var made = await MakeAsync(databasePath, key, containerPath, null, ct);
        return made.IsSuccess ? Result<string>.Success(made.Value!.Path) : Result<string>.Failure(made.Error!);
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
