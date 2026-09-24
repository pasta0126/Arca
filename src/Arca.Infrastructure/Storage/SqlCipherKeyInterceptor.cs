// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Data.Common;
using Arca.Application.Storage;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// Selects the SQLCipher 4 format and unlocks the file on every connection, before anything else runs
/// (verified in the technical spike; without 'cipher' SQLite3 Multiple Ciphers would use ChaCha20).
/// </summary>
sealed class SqlCipherKeyInterceptor(DatabaseKey key) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
        Unlock(connection, key);

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Unlock(connection, key);
        await Task.CompletedTask;
    }

    internal static void Unlock(DbConnection connection, DatabaseKey key)
    {
        var bytes = key.ToArray();
        try
        {
            var hex = Convert.ToHexString(bytes);
            foreach (var pragma in new[] { "PRAGMA cipher='sqlcipher'", "PRAGMA legacy=4", $"PRAGMA key=\"x'{hex}'\"" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = pragma;
                command.ExecuteNonQuery();
            }
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        }
    }
}
