// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Microsoft.Data.Sqlite;

namespace Arca.Infrastructure.Backup;

/// <summary>
/// Restoring a backup with its own keys (acces-i-xifrat, D7). The steps are separate so the screen can go between them:
/// open the file, unlock it with the password or the recovery key of that backup, warn that the centre password will
/// become the backup's, and only after the person confirms, complete. The current data and its key file are kept as
/// they were in a previous copy, and put back if anything fails.
/// </summary>
public sealed class BackupRestorer(AccessService access, TimeProvider? timeProvider = null)
{
    public const string PreviousMarker = ".prerestore-";
    public const string PreviousExtension = ".bak";

    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <summary>Takes the backup apart in a work folder next to the target. Changes nothing else.</summary>
    /// <param name="containerPath">The backup file the person chose.</param>
    /// <param name="workParent">A folder on the same volume as the database, where the work folder goes.</param>
    public Result<RestoreSession> Open(string containerPath, string workParent)
    {
        var work = Path.Combine(workParent, ".arca-restore-" + Guid.NewGuid().ToString("N"));
        var extracted = BackupContainer.Extract(containerPath, work);
        if (!extracted.IsSuccess)
        {
            DeleteQuietly(work);
            return Result<RestoreSession>.Failure(extracted.Error!);
        }

        var session = new RestoreSession(work, extracted.Value!);
        var keyFile = access.CheckKeyFile(session.DatabasePath);
        if (!keyFile.IsSuccess)
        {
            session.Dispose();
            return Result<RestoreSession>.Failure(keyFile.Error!);
        }

        return Result<RestoreSession>.Success(session);
    }

    /// <summary>
    /// Unlocks the backup with the password it had when it was made. Success carries the warning that restoring makes
    /// that password the centre's; it must be shown before the person confirms.
    /// </summary>
    public async Task<Result<bool>> UnlockWithPasswordAsync(RestoreSession session, string? password, CancellationToken ct = default)
    {
        var unlocked = access.Unlock(session.DatabasePath, password);
        if (!unlocked.IsSuccess)
        {
            return Result<bool>.Failure(unlocked.Error!);
        }

        using var key = unlocked.Value!;
        return await AdoptAsync(session, key, ct);
    }

    /// <summary>
    /// Forgotten password of the backup: its recovery key opens it, and a new password and recovery key are set for the
    /// restored data, as for any reset. Nothing is applied until the new key is confirmed in <see cref="ConfirmRecoveryAsync"/>.
    /// </summary>
    public Result<PendingKeyChange> BeginRecovery(RestoreSession session, string? recoveryKey, string? newPassword, string? confirmation) =>
        access.PrepareReset(session.DatabasePath, recoveryKey, newPassword, confirmation);

    public async Task<Result<bool>> ConfirmRecoveryAsync(
        RestoreSession session, PendingKeyChange pending, IReadOnlyList<string?>? typedGroups, CancellationToken ct = default)
    {
        var committed = access.Commit(session.DatabasePath, pending, typedGroups);
        return committed.IsSuccess ? await AdoptAsync(session, pending.DataKey, ct) : committed;
    }

    /// <summary>
    /// Replaces the current database and key file with those of the backup. The current ones are first kept as a previous
    /// copy (database and key file, byte for byte, even if the database is damaged). If anything fails they are put back.
    /// The database must be closed by the caller.
    /// </summary>
    public Result<string> Complete(RestoreSession session, string databasePath)
    {
        if (!session.IsUnlocked)
        {
            return Result<string>.Failure(KeyErrors.WrongCredentials);
        }

        SqliteConnection.ClearAllPools();
        var keyFile = KeyFileStore.PathFor(databasePath);
        var previous = PreviousPathFor(databasePath);
        var hadDatabase = File.Exists(databasePath);
        var hadKeys = File.Exists(keyFile);
        try
        {
            if (hadDatabase)
            {
                File.Copy(databasePath, previous, overwrite: false);
            }

            if (hadKeys)
            {
                File.Copy(keyFile, previous + KeyFileStore.Extension, overwrite: false);
            }

            var staging = databasePath + ".restoring";
            File.Copy(session.DatabasePath, staging, overwrite: true);
            File.Move(staging, databasePath, overwrite: true);
            File.Copy(KeyFileStore.PathFor(session.DatabasePath), keyFile + ".restoring", overwrite: true);
            File.Move(keyFile + ".restoring", keyFile, overwrite: true);
            DiscardPreviousKeyFile(databasePath); // it belonged to the data that was replaced
            return Result<string>.Success(previous);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            PutBack(databasePath, keyFile, previous, hadDatabase, hadKeys);
            return Result<string>.Failure(KeyErrors.RestoreFailed);
        }
    }

    /// <summary>The previous copies of a database, oldest first.</summary>
    public static IReadOnlyList<string> PreviousCopiesOf(string databasePath)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(databasePath))!;
        var prefix = Path.GetFileName(databasePath) + PreviousMarker;
        return [.. Directory.GetFiles(folder, prefix + "*" + PreviousExtension).OrderBy(f => f, StringComparer.Ordinal)];
    }

    static async Task<Result<bool>> AdoptAsync(RestoreSession session, DatabaseKey key, CancellationToken ct)
    {
        var unsound = await DatabaseVerifier.VerifyAsync(session.DatabasePath, key, ct);
        if (unsound is not null)
        {
            return Result<bool>.Failure(unsound);
        }

        session.Key?.Dispose();
        session.Key = new DatabaseKey(key.ToArray());
        return Result<bool>.Success(true, new Notice("Keys.RestoreAdoptsPassword"));
    }

    string PreviousPathFor(string databasePath) =>
        databasePath + PreviousMarker + _time.GetUtcNow().ToString("yyyyMMdd'T'HHmmssfff'Z'", System.Globalization.CultureInfo.InvariantCulture)
        + PreviousExtension;

    static void PutBack(string databasePath, string keyFile, string previous, bool hadDatabase, bool hadKeys)
    {
        try
        {
            if (hadDatabase && File.Exists(previous))
            {
                File.Copy(previous, databasePath, overwrite: true);
            }
            else if (!hadDatabase)
            {
                DeleteQuietly(databasePath); // there was none before: the restored one must not stay behind
            }

            if (hadKeys && File.Exists(previous + KeyFileStore.Extension))
            {
                File.Copy(previous + KeyFileStore.Extension, keyFile, overwrite: true);
            }
            else if (!hadKeys)
            {
                DeleteQuietly(keyFile);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Both previous files are still next to the database; nothing is deleted.
        }
        finally
        {
            DeleteQuietly(databasePath + ".restoring");
            DeleteQuietly(keyFile + ".restoring");
        }
    }

    /// <summary>A leftover file that cannot be removed must not turn a restore that worked into a failure.</summary>
    static void DiscardPreviousKeyFile(string databasePath)
    {
        try
        {
            KeyFileStore.DiscardPrevious(databasePath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Harmless: the next unlock removes it.
        }
    }

    static void DeleteQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A leftover is harmless.
        }
    }
}
