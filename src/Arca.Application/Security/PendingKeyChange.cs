// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;

namespace Arca.Application.Security;

/// <summary>
/// A change of keys that is ready but not yet saved (acces-i-xifrat, D4): the new recovery key has to be shown and
/// confirmed first. Nothing is written until <see cref="AccessService.Commit"/>; dropping it, by cancelling, leaves
/// the previous keys valid. Disposing wipes the database key.
/// </summary>
/// <param name="DataKey">The database key, which never changes in a reset or a regeneration.</param>
/// <param name="RecoveryKey">The new recovery key in canonical form, to show once.</param>
/// <param name="NewFile">The key file that will replace the current one.</param>
public sealed record PendingKeyChange(DatabaseKey DataKey, string RecoveryKey, KeyFile NewFile) : IDisposable
{
    /// <summary>The confirmation the person has to pass, by typing two groups of the new key.</summary>
    public RecoveryKeyChallenge Challenge { get; } = new(RecoveryKey);

    public void Dispose() => DataKey.Dispose();
}
