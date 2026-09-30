// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Arca.Domain.Common;

namespace Arca.Application.Backup;

/// <summary>What a restoration would do, shown before it is confirmed: the content of the backup against the current data, counts only.</summary>
/// <param name="Backup">What the backup holds.</param>
/// <param name="Current">What the centre has now, which is what is replaced.</param>
/// <param name="Relation">Whether the backup is of this version or of an older one, that will be brought up to date while restoring.</param>
public sealed record RestorePreview(ContentCounts Backup, ContentCounts Current, SchemaRelation Relation);

/// <summary>
/// A backup file opened to be restored (copies-de-seguretat, restauracio). It asks for the password of the backup, or its recovery key, before
/// anything of its content is shown, and it is disposed when the person finishes or gives up, which removes what was extracted.
/// </summary>
public interface IRestoreSession : IDisposable
{
    /// <summary>Opens the backup with its password. Its notices carry the warning that restoring makes it the password of the centre.</summary>
    Task<Result<bool>> UnlockWithPasswordAsync(string? password, CancellationToken ct = default);

    /// <summary>Checks a recovery key of the backup without changing anything.</summary>
    Result<bool> CheckRecoveryKey(string? recoveryKey);

    /// <summary>Forgotten password of the backup: the recovery key and a new password, prepared and not applied yet.</summary>
    Result<PendingKeyChange> BeginRecovery(string? recoveryKey, string? newPassword, string? confirmation);

    /// <summary>Applies the new password and recovery key to the extracted backup, after the new key is confirmed, and opens it.</summary>
    Task<Result<bool>> ConfirmRecoveryAsync(PendingKeyChange pending, IReadOnlyList<string?>? typedGroups, CancellationToken ct = default);

    /// <summary>The comparison of the backup with the current data; a backup of a newer version is refused here, before confirming.</summary>
    Task<Result<RestorePreview>> PreviewAsync(CancellationToken ct = default);

    /// <summary>Replaces the current data with the backup, with its guarantees; <paramref name="steps"/> gets the stage it is in, by its resource key.</summary>
    Task<Result<RestoreOutcome>> RestoreAsync(IProgress<string>? steps = null, CancellationToken ct = default);
}

/// <summary>Backups and restorations for the screens, which know nothing of files of the database or of how they are made.</summary>
public interface IBackupService
{
    /// <summary>The name proposed for a backup made at <paramref name="moment"/>.</summary>
    string SuggestedName(DateTimeOffset moment);

    /// <summary>Whether a file already exists at the destination, to ask before overwriting it.</summary>
    bool Exists(string destination);

    /// <summary>Makes a backup at the destination, telling the stages as they happen.</summary>
    Task<Result<BackupResult>> BackUpAsync(string destination, IProgress<string>? steps = null, CancellationToken ct = default);

    /// <summary>Opens a backup file to restore it, or says why it is not one.</summary>
    Result<IRestoreSession> Open(string file);
}

/// <summary>Starts the application again after a restoration, so nothing of the previous data stays in memory (copies-de-seguretat D13).</summary>
public interface IApplicationRestarter
{
    /// <summary>Whether this installation can launch itself again.</summary>
    bool CanRestart { get; }

    /// <summary>Closes the application and launches it again. Only called after <see cref="CanRestart"/> was true.</summary>
    void Restart();

    /// <summary>Closes the application without launching it again, when it cannot.</summary>
    void Close();
}
