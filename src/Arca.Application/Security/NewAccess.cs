// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;

namespace Arca.Application.Security;

/// <summary>
/// What creating the access produces: the new database key, the recovery key to show once, and the key file to write
/// next to the database in the same atomic operation that creates it. Disposing wipes the database key.
/// </summary>
/// <param name="DataKey">The random key that will encrypt the database.</param>
/// <param name="RecoveryKey">The recovery key in canonical form. It is never stored: only its wrapper is.</param>
/// <param name="KeyFile">The key file with both wrappers.</param>
public sealed record NewAccess(DatabaseKey DataKey, string RecoveryKey, KeyFile KeyFile) : IDisposable
{
    public void Dispose() => DataKey.Dispose();
}
