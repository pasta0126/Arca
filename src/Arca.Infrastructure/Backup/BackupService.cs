// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Backup;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.Infrastructure.Storage;

namespace Arca.Infrastructure.Backup;

/// <summary>The backup service of the screens over the open database of the centre.</summary>
public sealed class BackupService(StorageSession session, BackupRestorer restorer, TimeProvider? time = null) : IBackupService
{
    readonly TimeProvider _time = time ?? TimeProvider.System;

    public string SuggestedName(DateTimeOffset moment) => BackupDestination.SuggestedName(moment.ToLocalTime());

    public bool Exists(string destination) => File.Exists(destination);

    public Task<Result<BackupResult>> BackUpAsync(string destination, IProgress<string>? steps = null, CancellationToken ct = default) =>
        Task.Run(() => BackupMaker.MakeAsync(session.DatabasePath, session.Key, destination, steps, ct), ct);

    public Result<IRestoreSession> Open(string file)
    {
        var opened = restorer.Open(file, Path.GetDirectoryName(Path.GetFullPath(session.DatabasePath))!);
        return opened.IsSuccess
            ? Result<IRestoreSession>.Success(new Session(opened.Value!, restorer, session))
            : Result<IRestoreSession>.Failure(opened.Error!);
    }

    sealed class Session(RestoreSession inner, BackupRestorer restorer, StorageSession storage) : IRestoreSession
    {
        public Task<Result<bool>> UnlockWithPasswordAsync(string? password, CancellationToken ct = default) =>
            restorer.UnlockWithPasswordAsync(inner, password, ct);

        public Result<bool> CheckRecoveryKey(string? recoveryKey) => restorer.CheckRecoveryKey(inner, recoveryKey);

        public Result<PendingKeyChange> BeginRecovery(string? recoveryKey, string? newPassword, string? confirmation) =>
            restorer.BeginRecovery(inner, recoveryKey, newPassword, confirmation);

        public Task<Result<bool>> ConfirmRecoveryAsync(PendingKeyChange pending, IReadOnlyList<string?>? typedGroups, CancellationToken ct = default) =>
            restorer.ConfirmRecoveryAsync(inner, pending, typedGroups, ct);

        public async Task<Result<RestorePreview>> PreviewAsync(CancellationToken ct = default)
        {
            if (!inner.IsUnlocked)
            {
                return Result<RestorePreview>.Failure(KeyErrors.WrongCredentials);
            }

            var relation = await DatabaseInspector.ClassifyAsync(inner.DatabasePath, inner.Key!, ct);
            if (relation == SchemaRelation.Newer)
            {
                return Result<RestorePreview>.Failure(BackupErrors.VersionNewer);
            }

            var backup = await DatabaseInspector.CountAsync(inner.DatabasePath, inner.Key!, ct);
            var current = await DatabaseInspector.CountAsync(storage.DatabasePath, storage.Key, ct);
            return Result<RestorePreview>.Success(new RestorePreview(backup, current, relation));
        }

        public Task<Result<RestoreOutcome>> RestoreAsync(IProgress<string>? steps = null, CancellationToken ct = default) =>
            Task.Run(() => restorer.CompleteAsync(inner, storage.DatabasePath, storage.Key, steps, ct), ct);

        public void Dispose() => inner.Dispose();
    }
}
